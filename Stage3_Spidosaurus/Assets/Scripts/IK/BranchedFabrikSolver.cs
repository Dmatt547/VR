using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FABRIK extended to branched limbs.
///
/// Standard FABRIK places a shared joint at the plain centroid of what each child chain asks for
/// (Aristidou, Chrysanthou and Lasenby, 2016). That treats every foot as equally important.
/// On a walking creature they are not: a planted foot is touching ground the user can crouch
/// down and inspect, while a lifted foot is in mid air where error is invisible.
///
/// This solver places the junction at the WEIGHTED average instead, so error is pushed into
/// the foot where it cannot be seen. With equal weights it reduces exactly to the centroid,
/// so the published method is the special case of this one.
/// </summary>
[DefaultExecutionOrder(200)]
public class BranchedFabrikSolver : MonoBehaviour
{
    [Header("Rig")]
    public Transform rootBone;
    public List<FootEffector> effectors = new List<FootEffector>();

    [Header("Solve")]
    [Tooltip("Fixed pass count. No tolerance early-exit: the worst frame costs what an average frame costs.")]
    [Range(1, 10)] public int iterations = 4;

    [Header("Sibling separation")]
    public bool  separationEnabled = true;
    [Tooltip("Minimum horizontal gap between two feet under the same junction, in metres.")]
    public float minSeparation = 0.12f;

    [Header("Joint limits")]
    [Tooltip("Maximum swing away from the bind pose, per bone, in degrees. FABRIK on its own " +
             "has no notion of an anatomically possible pose, so without this a bone will fold " +
             "backwards to reach a target. Aristidou, Chrysanthou and Lasenby (2016) add model " +
             "constraints to FABRIK for exactly this reason.")]
    [Range(5f, 180f)] public float maxBendDegrees = 65f;

    [Header("Contact repair")]
    public bool  repairEnabled = true;
    [Tooltip("Perceptual threshold for world-locked content (Guan et al., 2023).")]
    public float contactToleranceMm = 15f;

    [Header("Readouts (leave alone, these are measurements)")]
    public float maxPlantedErrorMm;
    public int   repairsThisFrame;
    public int   separationsThisFrame;

    BranchedChain chain;

    void Start()
    {
        if (rootBone == null) { enabled = false; Debug.LogError("[IK] rootBone not assigned."); return; }
        chain = BranchedChain.Build(rootBone, effectors);
        Debug.Log($"[IK] Built chain: {chain.rootFirst.Count} joints, {chain.tips.Count} feet.");

        // Sanity check: a foot whose target starts beyond full extension can never be reached,
        // and the solver will report a huge planted error rather than converging.
        foreach (var t in chain.tips)
        {
            float reach = chain.ReachTo(t);
            float toTarget = Vector3.Distance(rootBone.position, t.effector.target.position);
            Debug.Log($"[IK] {t.bone.name}: reach {reach:F2} m, target is {toTarget:F2} m from hip.");
            if (toTarget > reach)
                Debug.LogWarning($"[IK] {t.effector.name} target is OUT OF REACH " +
                                 $"({toTarget:F2} m > {reach:F2} m). Move the target near the foot.");
        }
    }

    /// <summary>Full extension of the chain ending at this foot, in metres. Used by the gait
    /// planner to refuse footholds the leg could never get to.</summary>
    public float ReachOf(FootEffector e)
    {
        if (chain == null) return 0f;
        foreach (var t in chain.tips) if (t.effector == e) return chain.ReachTo(t);
        return 0f;
    }

    void LateUpdate()
    {
        if (chain == null) return;
        UnityEngine.Profiling.Profiler.BeginSample("BranchedFabrik.Solve");
        Solve();
        UnityEngine.Profiling.Profiler.EndSample();
    }

    void Solve()
    {
        ReadPose();
        ComputePull(null);

        for (int i = 0; i < iterations; i++)
        {
            BackwardPass();
            ForwardPass();
            if (separationEnabled) SeparateSiblings();
        }
        ForwardPass();          // separation bends bone lengths; restore them once at the end

        MeasureAndRepair();
        ApplyRotations();
    }

    // ---- passes -----------------------------------------------------------

    void ReadPose()
    {
        foreach (var j in chain.rootFirst) j.position = j.bone.position;
    }

    /// <summary>Sum foot weights up the tree. A suppressed foot contributes nothing for one solve.</summary>
    void ComputePull(FootEffector suppressed)
    {
        for (int i = chain.rootFirst.Count - 1; i >= 0; i--)
        {
            var j = chain.rootFirst[i];
            if (j.IsTip)
            {
                j.pull = (j.effector == suppressed) ? 0f : j.effector.Weight;
            }
            else
            {
                float sum = 0f;
                foreach (var c in j.children) sum += c.pull;
                j.pull = sum;
            }
        }
    }

    /// <summary>Feet toward the hip. Each joint lands on the weighted average of its children's demands.</summary>
    void BackwardPass()
    {
        for (int i = chain.rootFirst.Count - 1; i >= 0; i--)
        {
            var j = chain.rootFirst[i];

            if (j.IsTip) { j.position = j.effector.target.position; continue; }

            Vector3 acc = Vector3.zero;
            float   w   = 0f;

            foreach (var c in j.children)
            {
                Vector3 dir = j.position - c.position;
                if (dir.sqrMagnitude < 1e-10f) dir = j.bone.position - c.bone.position;
                if (dir.sqrMagnitude < 1e-10f) dir = Vector3.up;

                Vector3 proposal = c.position + dir.normalized * c.lengthFromParent;
                acc += proposal * c.pull;
                w   += c.pull;
            }

            if (w > 1e-6f) j.position = acc / w;   // <-- the whole contribution, one line
        }
    }

    /// <summary>Hip back down to the feet, re-imposing every cached bone length.</summary>
    void ForwardPass()
    {
        chain.root.position = chain.root.bone.position;   // the body owns the hip

        for (int i = 1; i < chain.rootFirst.Count; i++)
        {
            var j   = chain.rootFirst[i];
            Vector3 dir = j.position - j.parent.position;
            if (dir.sqrMagnitude < 1e-10f) dir = Vector3.down;
            j.position = j.parent.position + dir.normalized * j.lengthFromParent;
        }
    }

    /// <summary>
    /// Two feet under one junction can converge on the same patch of ground and intersect.
    /// Push them apart, splitting the correction INVERSELY to plant weight, so the planted
    /// foot barely moves and the lifted foot absorbs it. Reuses the weights already computed.
    /// </summary>
    void SeparateSiblings()
    {
        separationsThisFrame = 0;

        for (int a = 0; a < chain.tips.Count; a++)
        for (int b = a + 1; b < chain.tips.Count; b++)
        {
            var ja = chain.tips[a];
            var jb = chain.tips[b];

            Vector3 d = jb.position - ja.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist >= minSeparation || dist < 1e-6f) continue;

            float wa = ja.effector.Weight;
            float wb = jb.effector.Weight;
            float total = wa + wb;
            if (total < 1e-6f) continue;

            Vector3 n = d / dist;
            float push = minSeparation - dist;

            ja.position -= n * push * (wb / total);   // heavier foot moves less
            jb.position += n * push * (wa / total);
            separationsThisFrame++;
        }
    }

    // ---- measurement and repair -------------------------------------------

    void MeasureAndRepair()
    {
        repairsThisFrame  = 0;
        maxPlantedErrorMm = WorstPlantedErrorMm();

        if (!repairEnabled || maxPlantedErrorMm <= contactToleranceMm) return;

        // A planted foot has been dragged off its contact point. Drop the lowest-priority
        // foot's claim on the junction for one frame and solve again. A foot sliding through
        // terrain costs more presence than a lifted sub-leg lagging behind.
        FootEffector lowest = null;
        float lowestW = float.MaxValue;
        foreach (var t in chain.tips)
            if (t.effector.Weight < lowestW) { lowestW = t.effector.Weight; lowest = t.effector; }

        if (lowest == null) return;

        ComputePull(lowest);
        BackwardPass();
        ForwardPass();
        repairsThisFrame  = 1;
        maxPlantedErrorMm = WorstPlantedErrorMm();
    }

    float WorstPlantedErrorMm()
    {
        float worst = 0f;
        foreach (var t in chain.tips)
        {
            if (!t.effector.isPlanted) continue;
            float mm = Vector3.Distance(t.position, t.effector.target.position) * 1000f;
            if (mm > worst) worst = mm;
        }
        return worst;
    }

    // ---- writing back ------------------------------------------------------

    /// <summary>
    /// Turn solved positions into bone rotations.
    ///
    /// Two things matter here and both were wrong in the first version.
    ///
    /// First, each bone is reset to its BIND local rotation before the solved swing is applied.
    /// Composing "rotate a bit further" onto last frame's rotation every frame lets twist
    /// accumulate: the bone still points the right way, but it has quietly rolled about its own
    /// axis, and the mesh skinned to it shears. Resetting makes the result a pure function of
    /// this frame's solved positions, with no history.
    ///
    /// Second, the swing is clamped. FABRIK solves positions, not poses, so nothing stops a
    /// bone folding backwards through the joint it hangs from if that is the shortest way to
    /// the target. Limiting the swing away from bind is the cheap form of the model constraints
    /// Aristidou, Chrysanthou and Lasenby (2016) add to FABRIK.
    ///
    /// Root first, so each bone reads a parent that has already been placed.
    /// </summary>
    void ApplyRotations()
    {
        foreach (var j in chain.rootFirst)
        {
            // Reset before measuring. A child's world POSITION depends on its parent's rotation
            // and its own local position, not on its own rotation, so resetting here is safe.
            j.bone.localRotation = j.bindLocalRotation;

            if (j.children.Count == 0) continue;

            Vector3 current = Vector3.zero;
            Vector3 wanted  = Vector3.zero;

            foreach (var c in j.children)
            {
                Vector3 cNow  = c.bone.position - j.bone.position;
                Vector3 cWant = c.position      - j.position;
                if (cNow.sqrMagnitude  > 1e-10f) current += cNow.normalized  * c.pull;
                if (cWant.sqrMagnitude > 1e-10f) wanted  += cWant.normalized * c.pull;
            }

            if (current.sqrMagnitude < 1e-10f || wanted.sqrMagnitude < 1e-10f) continue;

            Quaternion swing = Quaternion.FromToRotation(current.normalized, wanted.normalized);
            j.bone.rotation = ClampSwing(swing, maxBendDegrees) * j.bone.rotation;
        }
    }

    /// <summary>Limit a rotation to at most maxDegrees about its own axis.</summary>
    static Quaternion ClampSwing(Quaternion q, float maxDegrees)
    {
        q.ToAngleAxis(out float angle, out Vector3 axis);
        if (float.IsNaN(axis.x) || axis.sqrMagnitude < 1e-10f) return Quaternion.identity;

        if (angle > 180f) angle -= 360f;              // shortest way round
        angle = Mathf.Clamp(angle, -maxDegrees, maxDegrees);
        return Quaternion.AngleAxis(angle, axis.normalized);
    }

    // ---- scene view evidence ----------------------------------------------

    void OnDrawGizmos()
    {
        if (chain == null) return;
        foreach (var j in chain.rootFirst)
        {
            if (j.parent == null) continue;
            Gizmos.color = j.IsTip
                ? (j.effector.isPlanted ? Color.green : Color.yellow)
                : Color.cyan;
            Gizmos.DrawLine(j.parent.position, j.position);
            Gizmos.DrawWireSphere(j.position, 0.02f);
        }
    }
}
