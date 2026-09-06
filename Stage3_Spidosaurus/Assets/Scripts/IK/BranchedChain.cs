using System.Collections.Generic;
using UnityEngine;

// Scene-graph driven skeleton for the weighted-junction FABRIK solver.
// Uses the parent-child Transform relationships covered in Week 2/Week 3 (scene graph,
// local vs world space, Quaternion rotations) and the direction-vector magnitude work
// from Week 5/Week 6 to measure bone lengths at bind time.

public class IKJoint
{
    public Transform bone;
    public IKJoint parent;
    public readonly List<IKJoint> children = new List<IKJoint>();

    // Rest length to the parent bone, measured once from world-space positions.
    public float lengthFromParent;

    // Scratch position for the solve; never written straight back to the Transform.
    public Vector3 position;

    // Combined weight of every foot beneath this joint.
    public float pull;

    // Imported local rotation, restored each frame so error cannot accumulate.
    public Quaternion bindLocalRotation;

    public FootEffector effector;

    public bool IsTip => effector != null;
}

public class BranchedChain
{
    public IKJoint root;
    public readonly List<IKJoint> rootFirst = new List<IKJoint>();
    public readonly List<IKJoint> tips = new List<IKJoint>();

    public static BranchedChain Build(Transform rootBone, IList<FootEffector> effectors)
    {
        var chain = new BranchedChain();
        var feet = MapFeet(effectors);
        var spine = TraceUpwards(rootBone, feet.Keys);

        chain.root = chain.Grow(rootBone, spine, feet);
        chain.CollectTips();
        return chain;
    }

    // Longest possible reach of a foot from the hip, so foothold planning can be clamped.
    public float ReachTo(IKJoint tip)
    {
        float reach = 0f;
        var joint = tip;

        while (joint?.parent != null)
        {
            reach += joint.lengthFromParent;
            joint = joint.parent;
        }

        return reach;
    }

    static Dictionary<Transform, FootEffector> MapFeet(IList<FootEffector> effectors)
    {
        var feet = new Dictionary<Transform, FootEffector>();

        foreach (var e in effectors)
            if (e != null && e.tipBone != null)
                feet[e.tipBone] = e;

        return feet;
    }

    // Climb from each foot back towards the root, keeping only the bones in between.
    static HashSet<Transform> TraceUpwards(Transform rootBone, IEnumerable<Transform> feet)
    {
        var spine = new HashSet<Transform>();

        foreach (var foot in feet)
        {
            var bone = foot;
            while (bone != null && spine.Add(bone) && bone != rootBone)
                bone = bone.parent;
        }

        return spine;
    }

    // Iterative pre-order walk, so rootFirst comes out ordered without a second pass.
    IKJoint Grow(Transform rootBone, HashSet<Transform> spine, Dictionary<Transform, FootEffector> feet)
    {
        var start = MakeJoint(rootBone, null, feet);
        var pending = new Stack<IKJoint>();
        pending.Push(start);

        while (pending.Count > 0)
        {
            var joint = pending.Pop();
            rootFirst.Add(joint);

            if (joint.IsTip) continue;

            foreach (Transform child in joint.bone)
                if (spine.Contains(child))
                    joint.children.Add(MakeJoint(child, joint, feet));

            for (int i = joint.children.Count - 1; i >= 0; i--)
                pending.Push(joint.children[i]);
        }

        return start;
    }

    IKJoint MakeJoint(Transform bone, IKJoint parent, Dictionary<Transform, FootEffector> feet)
    {
        var joint = new IKJoint
        {
            bone = bone,
            parent = parent,
            bindLocalRotation = bone.localRotation
        };

        if (parent != null)
            joint.lengthFromParent = (bone.position - parent.bone.position).magnitude;

        if (feet.TryGetValue(bone, out var effector))
            joint.effector = effector;

        return joint;
    }

    void CollectTips()
    {
        foreach (var joint in rootFirst)
            if (joint.IsTip)
                tips.Add(joint);
    }
}
