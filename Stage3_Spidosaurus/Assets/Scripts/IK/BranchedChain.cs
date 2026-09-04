using System.Collections.Generic;
using UnityEngine;

/// <summary>One bone in the solve. Plain class, not a component: no per-frame allocation.</summary>
public class IKJoint
{
    public Transform bone;
    public IKJoint   parent;
    public readonly List<IKJoint> children = new List<IKJoint>();

    /// <summary>Rest distance to the parent bone, cached once at bind time.</summary>
    public float lengthFromParent;

    /// <summary>Working position during the solve. Never written back directly.</summary>
    public Vector3 position;

    /// <summary>Summed weight of every foot below this joint. Drives the weighted average.</summary>
    public float pull;

    /// <summary>
    /// The bone's local rotation in the imported bind pose. Every frame the bone is reset to
    /// this before the solved rotation is applied, so error cannot accumulate frame over frame.
    /// </summary>
    public Quaternion bindLocalRotation;

    /// <summary>Non-null only on tip bones.</summary>
    public FootEffector effector;

    public bool IsTip => effector != null;
}

/// <summary>
/// Builds a joint tree from Unity's Transform hierarchy, keeping only the bones that
/// actually lie on a path from the root down to one of the feet. Unity's hierarchy is
/// already the branching structure this solver needs, so no parallel data structure is built.
/// </summary>
public class BranchedChain
{
    public IKJoint root;
    public readonly List<IKJoint> rootFirst = new List<IKJoint>();
    public readonly List<IKJoint> tips      = new List<IKJoint>();

    public static BranchedChain Build(Transform rootBone, IList<FootEffector> effectors)
    {
        var chain  = new BranchedChain();
        var onPath = new HashSet<Transform>();

        // Walk up from every tip to the root, marking bones we care about.
        foreach (var e in effectors)
        {
            if (e == null || e.tipBone == null) continue;
            var t = e.tipBone;
            while (t != null)
            {
                onPath.Add(t);
                if (t == rootBone) break;
                t = t.parent;
            }
        }

        chain.root = chain.AddRecursive(rootBone, null, onPath, effectors);
        chain.Flatten(chain.root);
        chain.CacheLengths();
        return chain;
    }

    IKJoint AddRecursive(Transform bone, IKJoint parent, HashSet<Transform> onPath,
                         IList<FootEffector> effectors)
    {
        var j = new IKJoint { bone = bone, parent = parent };
        parent?.children.Add(j);

        foreach (var e in effectors)
            if (e != null && e.tipBone == bone) { j.effector = e; tips.Add(j); }

        if (!j.IsTip)
            foreach (Transform child in bone)
                if (onPath.Contains(child))
                    AddRecursive(child, j, onPath, effectors);

        return j;
    }

    void Flatten(IKJoint j)
    {
        rootFirst.Add(j);
        foreach (var c in j.children) Flatten(c);
    }

    /// <summary>
    /// Total bone length from the root down to one tip. This is the furthest that foot can
    /// possibly get from the hip, so it is the hard limit on where a foothold may be planned.
    /// </summary>
    public float ReachTo(IKJoint tip)
    {
        float total = 0f;
        for (var j = tip; j != null && j.parent != null; j = j.parent)
            total += j.lengthFromParent;
        return total;
    }

    void CacheLengths()
    {
        foreach (var j in rootFirst)
        {
            j.bindLocalRotation = j.bone.localRotation;
            if (j.parent != null)
                j.lengthFromParent = Vector3.Distance(j.bone.position, j.parent.bone.position);
        }
    }
}
