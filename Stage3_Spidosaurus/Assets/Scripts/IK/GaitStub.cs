using System.Collections.Generic;
using UnityEngine;

// Unit material this builds on: local vs world space and TransformPoint/InverseTransformPoint
// (Week 3); ground detection with Physics.Raycast and its RaycastHit point (Week 5); the
// periodic function y = A sin(fx) driving the swing arc, with the step phase as x (Week 4);
// and frame-rate independent motion through Time.deltaTime (Week 2).

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
    int       swing = -1;
    float     t;
    Vector3   from, to;

    bool Stepping => swing >= 0;

    // Frame-rate independent exponential approach, shared by the velocity filter and the
    // in-flight re-aim.
    static float Ease(float rate, float dt) => 1f - Mathf.Exp(-rate * dt);

    void Start()
    {
        if (feet.Count == 0 || body == null) { enabled = false; return; }

        homeLocal = new Vector3[feet.Count];

        for (int i = 0; i < feet.Count; i++)
        {
            var foot = feet[i];

            if (snapTargetsToBonesOnStart && foot.tipBone != null)
                foot.target.position = foot.tipBone.position;

            foot.contactPoint    = SnapToGround(foot.target.position, foot);
            foot.target.position = foot.contactPoint;
            foot.isPlanted       = true;
            foot.plantBlend      = 1f;

            homeLocal[i] = body.InverseTransformPoint(foot.contactPoint);
        }

        lastBodyPos = body.position;
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-5f);

        // Horizontal only: the body's height is smoothed every frame elsewhere, so vertical
        // velocity is settling noise that would make footholds bob.
        Vector3 raw = (body.position - lastBodyPos) / dt;
        raw.y = 0f;

        bodyVelocity = Vector3.Lerp(bodyVelocity, raw, Ease(velocitySmoothing, dt));
        lastBodyPos  = body.position;

        if (Stepping) UpdateSwing();
        else          ChooseNextStep();

        // Easing the weight is what stops the junction jumping when contact state changes.
        float blendStep = Time.deltaTime / Mathf.Max(0.01f, blendTime);
        foreach (var foot in feet)
            foot.plantBlend = Mathf.MoveTowards(foot.plantBlend, foot.isPlanted ? 1f : 0f, blendStep);
    }

    // ---- stepping ----------------------------------------------------------

    // One foot swings at a time, which guarantees the planted/lifted contrast the solver relies
    // on. Drift is measured against the planned landing spot rather than the ideal home: home is
    // not reachable once the reach clamp and a sibling's claim have moved it, so a foot already
    // standing on the best available ground would otherwise re-step forever.
    void ChooseNextStep()
    {
        int   worst      = -1;
        float worstDrift = stepTrigger;

        for (int i = 0; i < feet.Count; i++)
        {
            float drift = Vector3.Distance(feet[i].contactPoint, PlanLanding(i));
            if (drift <= worstDrift) continue;

            worstDrift = drift;
            worst      = i;
        }

        if (worst >= 0) StartStep(worst);
    }

    void StartStep(int index)
    {
        swing = index;

        var foot = feet[swing];
        foot.isPlanted = false;

        from = foot.target.position;
        to   = PlanLanding(index);
        t    = 0f;
    }

    void UpdateSwing()
    {
        var foot = feet[swing];
        float dt = Time.deltaTime;

        // The body keeps moving after take-off, so the landing spot is re-planned in flight.
        to = Vector3.Lerp(to, PlanLanding(swing), Ease(landingTrack, dt));
        t += dt / Mathf.Max(0.01f, stepDuration);

        Vector3 p = SwingArc(Mathf.Clamp01(t));

        // A straight lerp across a ledge cuts through it, so the arc is held above the surface.
        float surface = SnapToGround(p, foot).y;
        if (p.y < surface) p.y = surface;

        foot.target.position = p;

        if (t < 1f) return;

        foot.contactPoint    = to;
        foot.target.position = to;
        foot.isPlanted       = true;
        swing                = -1;
    }

    // Half a sine cycle over the step, so the foot leaves and meets the ground at zero height.
    Vector3 SwingArc(float k)
    {
        Vector3 p = Vector3.Lerp(from, to, k);
        p.y += Mathf.Sin(k * Mathf.PI) * stepHeight;
        return p;
    }

    // ---- planning ----------------------------------------------------------

    Vector3 Home(int i) => body.TransformPoint(homeLocal[i]);

    // Home under the body, led by body velocity, pushed off any sibling's claim, pulled back
    // inside the leg's reach, then dropped onto the terrain.
    Vector3 PlanLanding(int index)
    {
        var foot = feet[index];

        Vector3 desired = Home(index) + bodyVelocity * predictTime;
        desired = AvoidClaims(foot, desired);
        desired = ClampToLegReach(foot, desired);

        return SnapToGround(desired, foot);
    }

    // Planning half of the foot-into-sub-foot problem; SeparateSiblings is the solving half.
    Vector3 AvoidClaims(FootEffector mover, Vector3 desired)
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

    // Planning past full extension does not stretch the leg, it just leaves the solver short.
    Vector3 ClampToLegReach(FootEffector foot, Vector3 desired)
    {
        if (solver == null || solver.rootBone == null) return desired;

        float reach = solver.ReachOf(foot) * reachMargin;
        if (reach <= 0f) return desired;   // chain not built yet on the first frame

        Vector3 hip = solver.rootBone.position;
        Vector3 d   = desired - hip;

        return d.magnitude <= reach ? desired : hip + d.normalized * reach;
    }

    // R(t) = O + t·d straight down from above the candidate spot, lifted clear of the surface
    // because the tip bone sits inside the toe mesh.
    Vector3 SnapToGround(Vector3 near, FootEffector foot = null)
    {
        Vector3 origin = near + Vector3.up * rayHeight;

        if (!Physics.Raycast(origin, Vector3.down, out var hit, rayLength, groundMask))
            return near;

        float lift = footClearance + (foot != null ? foot.footRadius : 0f);
        return hit.point + Vector3.up * lift;
    }

    // ---- diagnostics -------------------------------------------------------

    // Run in Play mode when a foot sinks: a collider that is not the slab under the foot means
    // that slab is off the ground layer and the ray passes straight through it.
    [ContextMenu("Log Ground Under Each Foot")]
    void LogGroundUnderFeet()
    {
        foreach (var f in feet)
        {
            if (f == null || f.tipBone == null) continue;

            Vector3 o = f.tipBone.position + Vector3.up * rayHeight;

            if (!Physics.Raycast(o, Vector3.down, out var hit, rayLength, groundMask))
            {
                Debug.LogWarning($"[Gait] {f.name}: ground ray hit NOTHING. " +
                                 $"Either groundMask excludes the surface below, or rayLength " +
                                 $"({rayLength} m) is too short.", f);
                continue;
            }

            float gap = f.tipBone.position.y - hit.point.y;
            Debug.Log($"[Gait] {f.name}: hit '{hit.collider.name}' " +
                      $"(layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}) " +
                      $"at y={hit.point.y:F3}. Tip bone is {gap * 1000f:F0} mm above it. " +
                      $"footRadius {f.footRadius:F3} + clearance {footClearance:F3}.",
                      hit.collider.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (feet == null) return;

        bool showHomes = Application.isPlaying && homeLocal != null;

        for (int i = 0; i < feet.Count; i++)
        {
            var foot = feet[i];
            if (foot == null) continue;

            Gizmos.color = foot.isPlanted ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(foot.contactPoint, plantRadius);

            if (!showHomes || i >= homeLocal.Length) continue;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(Home(i), Vector3.one * 0.05f);
            Gizmos.DrawLine(Home(i), foot.contactPoint);
        }
    }
}
