using System.Collections.Generic;
using UnityEngine;

// Goes on an empty "Spidosaur Rig" object. Builds a marker and spring per bone,
// then drives the bones from the markers each frame.
public class SpidosaurRig : MonoBehaviour
{
    private class DrivenBone
    {
        public Transform bone;
        public BoneMarker marker;

        // the first child that kept a marker, and the bind-pose direction to it
        // in this bone's own space
        public BoneMarker childMarker;
        public Vector3 aimDirection;
    }

    [Header("Rig")]
    [SerializeField] private Transform rootBone;

    [Header("Templates")]
    [SerializeField] private GameObject markerTemplate;
    [SerializeField] private GameObject springTemplate;

    [Header("Markers")]
    [SerializeField] private float markerScale = 0.03f;

    [Header("Bones")]
    // off: bones are only translated, so no joint ever hinges
    [SerializeField] private bool aimBonesAtChildren = true;

    [Header("Springs")]
    [SerializeField] private float stiffness = 30f;
    [SerializeField] private float springLineWidth = 0.004f;
    [SerializeField] private bool showSprings = true;

    // on: stops limbs folding flat, but reaches past a joint so the deformation
    // is less local
    [SerializeField] private bool addBracingSprings = false;
    [SerializeField] private float bracingStiffness = 10f;

    [Header("Bone Filtering")]
    // a spring with a rest length near zero has no direction to pull along
    [SerializeField] private float minimumBoneLength = 0.02f;

    // parent before child
    private readonly List<DrivenBone> drivenBones = new List<DrivenBone>();
    private readonly List<BoneMarker> markers = new List<BoneMarker>();

    private int springCount;
    private int skippedBones;

    private void Start()
    {
        BuildRig();
    }

    public void BuildRig()
    {
        if (rootBone == null || markerTemplate == null || springTemplate == null)
        {
            Debug.LogWarning("SpidosaurRig is missing a Root Bone, Marker Template or Spring Template reference in the Inspector.");
            return;
        }

        if (markerTemplate.GetComponent<BoneMarker>() == null || springTemplate.GetComponent<BoneSpring>() == null)
        {
            Debug.LogWarning("SpidosaurRig needs a Marker Template carrying a BoneMarker component and a Spring Template carrying a BoneSpring component.");
            return;
        }

        // the base is pinned, or the springs drag the creature off its spot
        DrivenBone root = AddDrivenBone(rootBone, CreateMarker(rootBone, true));

        foreach (Transform child in rootBone)
        {
            AddBone(child, root, null);
        }

        Debug.Log("Spidosaur rig built: " + drivenBones.Count + " markers, " + springCount +
                  " springs, " + skippedBones + " bones skipped as shorter than " + minimumBoneLength + "m.");
    }

    // parent is the nearest bone above this one that kept a marker, not
    // necessarily its direct parent
    private void AddBone(Transform bone, DrivenBone parent, DrivenBone grandparent)
    {
        DrivenBone parentForChildren = parent;
        DrivenBone grandparentForChildren = grandparent;

        if (Vector3.Distance(bone.position, parent.marker.transform.position) >= minimumBoneLength)
        {
            DrivenBone driven = AddDrivenBone(bone, CreateMarker(bone, false));

            CreateSpring(parent.marker, driven.marker, stiffness);

            if (addBracingSprings && grandparent != null)
            {
                CreateSpring(grandparent.marker, driven.marker, bracingStiffness);
            }

            // at a junction only the first branch steers the bone above it
            if (parent.childMarker == null)
            {
                parent.childMarker = driven.marker;
                parent.aimDirection = Quaternion.Inverse(parent.bone.rotation) *
                                      (bone.position - parent.bone.position).normalized;
            }

            parentForChildren = driven;
            grandparentForChildren = parent;
        }
        else
        {
            skippedBones++;
        }

        foreach (Transform child in bone)
        {
            AddBone(child, parentForChildren, grandparentForChildren);
        }
    }

    private DrivenBone AddDrivenBone(Transform bone, BoneMarker marker)
    {
        DrivenBone driven = new DrivenBone { bone = bone, marker = marker };

        drivenBones.Add(driven);
        markers.Add(marker);

        return driven;
    }

    private BoneMarker CreateMarker(Transform bone, bool anchored)
    {
        GameObject markerObject = Instantiate(markerTemplate, bone.position, Quaternion.identity, transform);
        markerObject.name = "Marker (" + bone.name + ")";
        markerObject.transform.localScale = Vector3.one * markerScale;

        BoneMarker marker = markerObject.GetComponent<BoneMarker>();
        marker.Initialise(anchored);

        return marker;
    }

    private void CreateSpring(BoneMarker first, BoneMarker second, float springStiffness)
    {
        GameObject springObject = Instantiate(springTemplate, transform);
        springObject.name = "Spring (" + first.name + " to " + second.name + ")";

        BoneSpring spring = springObject.GetComponent<BoneSpring>();
        spring.Initialise(first.transform, second.transform, springStiffness, springLineWidth, showSprings);

        first.AddSpring(spring);
        second.AddSpring(spring);

        springCount++;
    }

    // fixed timestep, so the springs don't behave differently with frame rate
    private void FixedUpdate()
    {
        for (int i = 0; i < markers.Count; i++)
        {
            markers[i].DoDynamics(Time.fixedDeltaTime);
        }
    }

    // late, because XR moves a held marker during Update. parent before child,
    // each finished before the next starts, since moving a bone drags its subtree.
    private void LateUpdate()
    {
        for (int i = 0; i < drivenBones.Count; i++)
        {
            DrivenBone driven = drivenBones[i];

            driven.bone.position = driven.marker.transform.position;

            // the root is skipped, or the whole creature swings round
            if (!aimBonesAtChildren || i == 0 || driven.childMarker == null) continue;

            Vector3 wanted = driven.childMarker.transform.position - driven.marker.transform.position;

            if (wanted.sqrMagnitude < 0.000001f) continue;

            Vector3 facing = driven.bone.rotation * driven.aimDirection;

            driven.bone.rotation = Quaternion.FromToRotation(facing, wanted.normalized) * driven.bone.rotation;
        }
    }
}
