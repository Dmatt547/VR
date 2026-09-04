using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stands in for the gait and footprint planner of Karim et al. (2013), which is out of scope
/// for this stage. It does three jobs the solver depends on:
///   1. decides which feet are planted, and eases the weight so it never steps
///   2. plans footholds in BODY-LOCAL space with velocity lead, so a stationary creature does
///      not walk its own feet away from itself
///   3. reserves ground: a lifted foot may not plan a foothold inside a sibling's plant radius
///
/// Each foot has a "home" position recorded in the body's local space at Start. A foot steps
/// only once it has drifted further than stepTrigger from that home, and it steps back to
/// home plus a short lead along the body's current velocity. This is the predictive placement
/// idea from Roche and Torres-Cros (2016): plan where the foot should be, rather than react
/// to where it has ended up.
/// </summary>
[DefaultExecutionOrder(100)]
public class GaitStub : MonoBehaviour
{
    public List<FootEffector> feet = new List<FootEffector>();

    [Tooltip("The creature root. Foot homes are stored relative to this, so they travel with it.")]
    public Transform body;

    [Tooltip("Same object as this one. Used to ask each leg how far it can actually reach.")]
    public BranchedFabrikSolver solver;

    [Header("Reach")]
    [Tooltip("Snap each target onto its own tip bone at Start, so a mis-placed empty cannot " +
             "hand the solver a goal metres away from the foot.")]
    public bool snapTargetsToBonesOnStart = true;

    [Tooltip("Fraction of full extension a foothold may use. Below 1 so the leg never has to " +
             "straighten completely, which is where FABRIK stops converging.")]
    [Range(0.3f, 1f)] public float reachMargin = 0.85f;

    [Header("Step")]
    public float stepDuration = 0.45f;
    public float stepHeight   = 0.12f;
    public float blendTime    = 0.12f;

    [Tooltip("How far a foot may drift from its home spot under the body before it takes a step.")]
    public float stepTrigger = 0.22f;

    [Tooltip("Seconds of body velocity to lead the foothold by. 0 disables prediction.")]
    public float predictTime = 0.25f;

    [Tooltip("How fast a foot already in flight re-aims at its landing spot. The body keeps " +
             "moving after a step begins, so a landing spot chosen once at take-off is stale " +
             "by touchdown.")]
    public float landingTrack = 10f;

    [Tooltip("Smoothing on the measured body velocity. Raw frame-to-frame velocity is noisy " +
             "and that noise lands directly in the predicted foothold.")]
    public float velocitySmoothing = 8f;

    [Header("Foothold reservation")]
    [Tooltip("A planted foot claims this radius of ground. Siblings must plan outside it.")]
    public float plantRadius = 0.18f;
    public int   reservationsThisStep;

    [Header("Ground")]
    public LayerMask groundMask = ~0;
    public float rayHeight = 2f;
    public float rayLength = 6f;

    [Tooltip("Lift every foot goal this far above the surface. The tip bone sits inside the " +
             "toe mesh, so planting it exactly on the surface buries the visible foot.")]
    public float footClearance = 0.02f;

    Vector3[] homeLocal;
    Vector3   lastBodyPos, bodyVelocity;
    int       swing = -1;          // -1 = every foot planted
    float     t;
    Vector3   from, to;

    void Start()
    {
        if (feet.Count == 0 || body == null) { enabled = false; return; }

        homeLocal = new Vector3[feet.Count];

        for (int i = 0; i < feet.Count; i++)
        {
            var f = feet[i];

            // Start the goal exactly where the foot already is, then drop it to the ground.
            // Without this the target keeps whatever position the empty was created at.
            if (snapTargetsToBonesOnStart && f.tipBone != null)
                f.target.position = f.tipBone.position;

            f.contactPoint    = Ground(f.target.position);
            f.target.position = f.contactPoint;
            f.isPlanted       = true;
            f.plantBlend      = 1f;

            // Where this foot belongs, expressed in the body's frame. Travels with the body.
            homeLocal[i] = body.InverseTransformPoint(f.contactPoint);
        }

        lastBodyPos = body.position;
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-5f);

        // Horizontal only. BodyGrounder smooths the body's height every frame, so vertical
        // velocity is mostly settling noise; feeding it into the prediction makes footholds
        // bob up and down. Smoothed, because raw per-frame velocity is jittery.
        Vector3 raw = (body.position - lastBodyPos) / dt;
        raw.y = 0f;
        bodyVelocity = Vector3.Lerp(bodyVelocity, raw,
                                    1f - Mathf.Exp(-velocitySmoothing * dt));
        lastBodyPos = body.position;

        if (swing >= 0) AdvanceSwing();
        else            ConsiderNextStep();

        // ease every weight so the junction never jumps when contact state changes
        foreach (var f in feet)
            f.plantBlend = Mathf.MoveTowards(
                f.plantBlend,
                f.isPlanted ? 1f : 0f,
                Time.deltaTime / Mathf.Max(0.01f, blendTime));
    }

    void AdvanceSwing()
    {
        var f = feet[swing];

        // The landing spot chosen at take-off is already stale: the body has moved since.
        // Re-plan it every frame and ease toward the new answer, so the foot lands where the
        // creature will actually be rather than where it was.
        to = Vector3.Lerp(to, PlanFoothold(swing),
                          1f - Mathf.Exp(-landingTrack * Time.deltaTime));

        t += Time.deltaTime / Mathf.Max(0.01f, stepDuration);
        float k = Mathf.Clamp01(t);

        Vector3 p = Vector3.Lerp(from, to, k);
        p.y += Mathf.Sin(k * Mathf.PI) * stepHeight;

        // Never let the swing arc pass below the terrain. Lerping between two points across a
        // ledge cuts a straight line through whatever is in between.
        float surface = Ground(p).y;
        if (p.y < surface) p.y = surface;

        f.target.position = p;

        if (t >= 1f)
        {
            f.contactPoint    = to;
            f.target.position = to;
            f.isPlanted       = true;
            swing             = -1;
        }
    }

    /// <summary>
    /// One foot swings at a time, which guarantees the planted/lifted contrast the solver is
    /// built around. The foot that has drifted furthest from its home goes first; if nothing
    /// has drifted past the trigger, every foot stays planted and the creature simply stands.
    /// </summary>
    void ConsiderNextStep()
    {
        int   worst  = -1;
        float worstD = stepTrigger;

        for (int i = 0; i < feet.Count; i++)
        {
            float d = Vector3.Distance(feet[i].contactPoint, Home(i));
            if (d > worstD) { worstD = d; worst = i; }
        }

        if (worst >= 0) BeginStep(worst);
    }

    Vector3 Home(int i) => body.TransformPoint(homeLocal[i]);

    void BeginStep(int index)
    {
        swing = index;
        var f = feet[swing];
        f.isPlanted = false;
        from = f.target.position;

        // Plan toward where this foot BELONGS under the body, led slightly by body velocity.
        // Planning relative to the previous contact instead would march the feet away from a
        // stationary creature, one stride per step, until the reach clamp stopped them.
        to = PlanFoothold(index);
        t  = 0f;
    }

    /// <summary>
    /// Where foot <paramref name="index"/> should land: its home spot under the body, led by
    /// the body's velocity, pushed out of any sibling's claimed ground, pulled back inside the
    /// leg's reach, and dropped onto the terrain. Called every frame while the foot is in
    /// flight, not just at take-off.
    /// </summary>
    Vector3 PlanFoothold(int index)
    {
        var f = feet[index];
        Vector3 desired = Home(index) + bodyVelocity * predictTime;
        desired = Reserve(f, desired);
        desired = ClampToReach(f, desired);
        return Ground(desired);
    }

    /// <summary>
    /// Ground is a shared resource. Reject any candidate foothold inside a sibling's claim
    /// and push it out to the edge of that claim. This is the planning half of the
    /// foot-into-sub-foot problem; SeparateSiblings is the solving half.
    /// </summary>
    Vector3 Reserve(FootEffector mover, Vector3 desired)
    {
        reservationsThisStep = 0;

        foreach (var other in feet)
        {
            if (other == mover) continue;

            Vector3 d = desired - other.contactPoint;
            d.y = 0f;
            if (d.magnitude >= plantRadius) continue;

            Vector3 n = d.sqrMagnitude < 1e-6f ? body.right : d.normalized;
            desired   = other.contactPoint + n * plantRadius;
            desired.y = mover.contactPoint.y;
            reservationsThisStep++;
        }
        return desired;
    }

    /// <summary>
    /// A leg cannot reach further than the sum of its bone lengths. Planning a foothold past
    /// that point does not stretch the leg, it just leaves the solver permanently short.
    /// </summary>
    Vector3 ClampToReach(FootEffector f, Vector3 desired)
    {
        if (solver == null || solver.rootBone == null) return desired;

        float reach = solver.ReachOf(f) * reachMargin;
        if (reach <= 0f) return desired;

        Vector3 hip = solver.rootBone.position;
        Vector3 d   = desired - hip;
        if (d.magnitude <= reach) return desired;

        return hip + d.normalized * reach;
    }

    Vector3 Ground(Vector3 near)
    {
        Vector3 origin = near + Vector3.up * rayHeight;
        return Physics.Raycast(origin, Vector3.down, out var hit, rayLength, groundMask)
            ? hit.point + Vector3.up * footClearance
            : near;
    }

    void OnDrawGizmosSelected()
    {
        if (feet == null) return;

        for (int i = 0; i < feet.Count; i++)
        {
            var f = feet[i];
            if (f == null) continue;

            Gizmos.color = f.isPlanted ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(f.contactPoint, plantRadius);

            if (Application.isPlaying && homeLocal != null && i < homeLocal.Length)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(Home(i), Vector3.one * 0.05f);
                Gizmos.DrawLine(Home(i), f.contactPoint);
            }
        }
    }
}
