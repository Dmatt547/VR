using System.Collections.Generic;
using UnityEngine;

// Unit material this builds on: scene graph and parent-child transforms, local vs world space
// (Week 2, Week 3); quaternions for rotation, avoiding Euler gimbal lock (Week 3); direction
// vectors, magnitude and normalisation (Week 5, Week 6); kinematic, script-driven motion rather
// than Rigidbody forces (Week 7).

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
    [Tooltip("Maximum swing away from the bind pose, per bone, in degrees. FABRIK solves positions " +
             "rather than poses, so without a limit a bone will fold backwards through its own joint " +
             "to reach a target. Aristidou, Chrysanthou and Lasenby (2016) add model constraints to " +
             "FABRIK for the same reason.")]
    [Range(5f, 180f)] public float maxBendDegrees = 65f;

    [Header("Contact repair")]
    public bool  repairEnabled = true;
    [Tooltip("Perceptual threshold for world-locked content (Guan et al., 2023).")]
    public float contactToleranceMm = 15f;

    [Header("Readouts (leave alone, these are measurements)")]
    public float maxPlantedErrorMm;
    public int   repairsThisFrame;
    public int   separationsThisFrame;

    const float Degenerate = 1e-10f;

    BranchedChain chain;
    List<IKJoint> joints;
    List<IKJoint> feet;

    void Start()
    {
        if (rootBone == null)
        {
            Debug.LogError("[IK] rootBone not assigned.");
            enabled = false;
            return;
        }

        chain  = BranchedChain.Build(rootBone, effectors);
        joints = chain.rootFirst;
        feet   = chain.tips;

        Debug.Log($"[IK] Built chain: {joints.Count} joints, {feet.Count} feet.");
        foreach (var foot in feet) ReportReach(foot);
    }

    // A target starting beyond full extension can never be reached, so warn at startup
    // instead of leaving a permanently large planted error to explain later.
    void ReportReach(IKJoint foot)
    {
        float reach    = chain.ReachTo(foot);
        float toTarget = Vector3.Distance(rootBone.position, foot.effector.target.position);

        Debug.Log($"[IK] {foot.bone.name}: reach {reach:F2} m, target is {toTarget:F2} m from hip.");

        if (toTarget > reach)
            Debug.LogWarning($"[IK] {foot.effector.name} target is OUT OF REACH " +
                             $"({toTarget:F2} m > {reach:F2} m). Move the target near the foot.");
    }

    // Full extension of the chain ending at this foot, used by the gait planner to reject
    // footholds the leg could never get to.
    public float ReachOf(FootEffector e)
    {
        if (chain == null) return 0f;

        for (int i = 0; i < feet.Count; i++)
            if (feet[i].effector == e) return chain.ReachTo(feet[i]);

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
        RestoreBindPose();
        AccumulatePull(chain.root, null);

        for (int pass = 0; pass < iterations; pass++)
        {
            PullTowardsFeet();
            RebuildFromHip();
            if (separationEnabled) SeparateSiblings();
        }

        RebuildFromHip();   // separation stretches bones, so lengths are restored once at the end
        MeasureAndRepair();
        ApplyRotations();
    }

    // ---- passes -----------------------------------------------------------

    // Seeding from last frame's clamped pose makes the solve depend on its own output, which
    // with a fixed pass count alternates between two poses and reads as a twitch. Starting from
    // bind every frame makes the result a pure function of the targets.
    // Root-first order matters: a bone's world position is only read once its ancestors are back
    // at bind, and a bone's own rotation does not move its own pivot.
    void RestoreBindPose()
    {
        foreach (var j in joints)
        {
            j.bone.localRotation = j.bindLocalRotation;
            j.position = j.bone.position;
        }
    }

    // Weight of every foot beneath a joint. A suppressed foot contributes nothing for one solve.
    float AccumulatePull(IKJoint j, FootEffector suppressed)
    {
        if (j.IsTip)
            return j.pull = j.effector == suppressed ? 0f : j.effector.Weight;

        float sum = 0f;
        foreach (var c in j.children) sum += AccumulatePull(c, suppressed);

        return j.pull = sum;
    }

    // Feet toward the hip: each junction lands on the weighted average of its children's demands.
    void PullTowardsFeet()
    {
        for (int i = joints.Count - 1; i >= 0; i--)
        {
            var j = joints[i];

            if (j.IsTip)
            {
                j.position = j.effector.target.position;
                continue;
            }

            Vector3 sum    = Vector3.zero;
            float   weight = 0f;

            foreach (var c in j.children)
            {
                Vector3 dir = j.position - c.position;
                if (dir.sqrMagnitude < Degenerate) dir = j.bone.position - c.bone.position;
                if (dir.sqrMagnitude < Degenerate) dir = Vector3.up;

                sum    += (c.position + dir.normalized * c.lengthFromParent) * c.pull;
                weight += c.pull;
            }

            if (weight > 1e-6f) j.position = sum / weight;
        }
    }

    // Hip back down to the feet, re-imposing every cached bone length.
    void RebuildFromHip()
    {
        chain.root.position = chain.root.bone.position;   // the body owns the hip

        for (int i = 1; i < joints.Count; i++)
        {
            var j = joints[i];

            Vector3 dir = j.position - j.parent.position;
            if (dir.sqrMagnitude < Degenerate) dir = Vector3.down;

            j.position = j.parent.position + dir.normalized * j.lengthFromParent;
        }
    }

    // Feet under one junction can converge on the same patch of ground. The correction splits
    // inversely to plant weight, so the planted foot barely moves and the lifted one absorbs it.
    void SeparateSiblings()
    {
        separationsThisFrame = 0;

        for (int a = 0; a < feet.Count - 1; a++)
        {
            var first = feet[a];

            for (int b = a + 1; b < feet.Count; b++)
            {
                var second = feet[b];

                Vector3 gap = second.position - first.position;
                gap.y = 0f;

                float dist = gap.magnitude;
                if (dist >= minSeparation || dist < 1e-6f) continue;

                float wa    = first.effector.Weight;
                float wb    = second.effector.Weight;
                float total = wa + wb;
                if (total < 1e-6f) continue;

                Vector3 n    = gap / dist;
                float   push = minSeparation - dist;

                first.position  -= n * push * (wb / total);
                second.position += n * push * (wa / total);
                separationsThisFrame++;
            }
        }
    }

    // ---- measurement and repair -------------------------------------------

    void MeasureAndRepair()
    {
        repairsThisFrame  = 0;
        maxPlantedErrorMm = WorstPlantedErrorMm();

        if (!repairEnabled || maxPlantedErrorMm <= contactToleranceMm) return;

        // A planted foot has been dragged off its contact point. Sliding through terrain costs
        // more presence than a lifted sub-leg lagging, so the lightest foot loses its claim on
        // the junction for one extra solve.
        var lightest = LightestFoot();
        if (lightest == null) return;

        AccumulatePull(chain.root, lightest);
        PullTowardsFeet();
        RebuildFromHip();

        repairsThisFrame  = 1;
        maxPlantedErrorMm = WorstPlantedErrorMm();
    }

    FootEffector LightestFoot()
    {
        FootEffector lightest = null;
        float lowest = float.MaxValue;

        foreach (var foot in feet)
            if (foot.effector.Weight < lowest)
            {
                lowest   = foot.effector.Weight;
                lightest = foot.effector;
            }

        return lightest;
    }

    float WorstPlantedErrorMm()
    {
        float worst = 0f;

        foreach (var foot in feet)
        {
            if (!foot.effector.isPlanted) continue;

            float mm = Vector3.Distance(foot.position, foot.effector.target.position) * 1000f;
            if (mm > worst) worst = mm;
        }

        return worst;
    }

    // ---- writing back ------------------------------------------------------

    // Solved positions become bone rotations. Each bone returns to its bind local rotation first,
    // so no twist accumulates frame over frame and the skinned mesh cannot shear, and the swing
    // away from bind is clamped because FABRIK has no notion of an anatomically possible pose.
    void ApplyRotations()
    {
        foreach (var j in joints)
        {
            j.bone.localRotation = j.bindLocalRotation;
            if (j.children.Count == 0) continue;

            Vector3 current = Vector3.zero;
            Vector3 wanted  = Vector3.zero;

            foreach (var c in j.children)
            {
                Vector3 now  = c.bone.position - j.bone.position;
                Vector3 goal = c.position      - j.position;

                if (now.sqrMagnitude  > Degenerate) current += now.normalized  * c.pull;
                if (goal.sqrMagnitude > Degenerate) wanted  += goal.normalized * c.pull;
            }

            if (current.sqrMagnitude < Degenerate || wanted.sqrMagnitude < Degenerate) continue;

            j.bone.rotation = SwingTowards(current.normalized, wanted.normalized, maxBendDegrees)
                              * j.bone.rotation;
        }
    }

    // Cross and Angle are taken straight off the direction vectors. Building the rotation with
    // FromToRotation and pulling an axis back out of it with ToAngleAxis is unstable near 180
    // degrees, where every axis is a valid answer and the bone is thrown somewhere new each frame.
    static Quaternion SwingTowards(Vector3 from, Vector3 to, float maxDegrees)
    {
        Vector3 axis = Vector3.Cross(from, to);
        if (axis.sqrMagnitude < 1e-8f) return Quaternion.identity;   // parallel or anti-parallel

        float angle = Vector3.Angle(from, to);
        if (angle < 1e-4f) return Quaternion.identity;

        return Quaternion.AngleAxis(Mathf.Min(angle, maxDegrees), axis.normalized);
    }

    // ---- scene view evidence ----------------------------------------------

    void OnDrawGizmos()
    {
        if (chain == null) return;

        foreach (var j in joints)
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
