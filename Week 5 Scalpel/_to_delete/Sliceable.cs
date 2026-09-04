using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// put this on anything the scalpel is allowed to cut (the "patient").
//
// MeshSlicer does the maths; this class does the Unity side of it - turning the
// world-space cut plane into the object's local space, spawning the two halves as
// real GameObjects with colliders, rigidbodies and grab interactables, then
// removing the original.
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Sliceable : MonoBehaviour
{
    [Header("Look of the cut face")]
    [Tooltip("Material applied to the newly exposed inside surface. Leave empty to reuse the outside material.")]
    [SerializeField] private Material crossSectionMaterial;

    [Header("Rules")]
    [Tooltip("Can the two halves be cut again?")]
    [SerializeField] private bool piecesStaySliceable = true;

    [Tooltip("Can the two halves be picked up?")]
    [SerializeField] private bool piecesAreGrabbable = true;

    [Tooltip("Local-space volume below which a piece is thrown away instead of spawned. Stops the blade shaving off invisible slivers.")]
    [SerializeField] private float minimumPieceVolume = 0.00005f;

    [Tooltip("How hard the two halves push apart, so you can see the cut happen.")]
    [SerializeField] private float separationImpulse = 0.4f;

    [Header("Feedback")]
    [SerializeField] private AudioClip cutClip;
    [SerializeField] private float cutVolume = 0.8f;

    /// <summary>
    /// Cuts this object with a plane given in WORLD space.
    /// </summary>
    /// <param name="pieces">the two new halves, or null if nothing was cut</param>
    /// <param name="worldCapPoints">optional - filled with the world-space points on
    /// the cut outline, for the debug markers</param>
    /// <returns>true if the object was actually split in two</returns>
    public bool TrySlice(Plane worldPlane, out GameObject[] pieces, List<Vector3> worldCapPoints = null)
    {
        pieces = null;

        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh sourceMesh = filter.sharedMesh;
        if (sourceMesh == null)
        {
            return false;
        }

        // quick reject before doing any real work - if the whole bounding box sits on
        // one side of the plane there is nothing to cut
        if (!BoundsStraddlePlane(GetComponent<Renderer>().bounds, worldPlane))
        {
            return false;
        }

        Plane localPlane = WorldPlaneToLocal(worldPlane);

        List<Vector3> localCapPoints = worldCapPoints != null ? new List<Vector3>() : null;

        if (!MeshSlicer.Slice(sourceMesh, localPlane, true,
                              out Mesh positiveMesh, out Mesh negativeMesh, localCapPoints))
        {
            return false;
        }

        // reject cuts that only shaved a sliver off the edge
        float positiveVolume = MeshSlicer.CalculateVolume(positiveMesh);
        float negativeVolume = MeshSlicer.CalculateVolume(negativeMesh);
        if (positiveVolume < minimumPieceVolume || negativeVolume < minimumPieceVolume)
        {
            Destroy(positiveMesh);
            Destroy(negativeMesh);
            return false;
        }

        if (worldCapPoints != null && localCapPoints != null)
        {
            for (int i = 0; i < localCapPoints.Count; i++)
            {
                worldCapPoints.Add(transform.TransformPoint(localCapPoints[i]));
            }
        }

        Material[] pieceMaterials = BuildMaterialArray();
        float totalVolume = positiveVolume + negativeVolume;

        GameObject positivePiece = CreatePiece(positiveMesh, name + " (A)", pieceMaterials,
                                               positiveVolume / totalVolume);
        GameObject negativePiece = CreatePiece(negativeMesh, name + " (B)", pieceMaterials,
                                               negativeVolume / totalVolume);

        // shove them apart along the plane normal so the cut reads clearly
        PushApart(positivePiece, worldPlane.normal);
        PushApart(negativePiece, -worldPlane.normal);

        PlayCutSound();
        RemoveOriginal();

        pieces = new[] { positivePiece, negativePiece };
        return true;
    }

    // a plane's normal does NOT transform the same way a direction does when the
    // object has non-uniform scale - it needs the inverse transpose. going from
    // world to local, that works out to the transpose of localToWorld
    private Plane WorldPlaneToLocal(Plane worldPlane)
    {
        Vector3 localNormal = transform.localToWorldMatrix.transpose
            .MultiplyVector(worldPlane.normal).normalized;

        Vector3 worldPointOnPlane = worldPlane.ClosestPointOnPlane(transform.position);
        Vector3 localPointOnPlane = transform.InverseTransformPoint(worldPointOnPlane);

        return new Plane(localNormal, localPointOnPlane);
    }

    private static bool BoundsStraddlePlane(Bounds bounds, Plane plane)
    {
        // the box crosses the plane if its corners disagree about which side they're on
        Vector3 extents = bounds.extents;
        float radius = Mathf.Abs(plane.normal.x) * extents.x
                     + Mathf.Abs(plane.normal.y) * extents.y
                     + Mathf.Abs(plane.normal.z) * extents.z;

        return Mathf.Abs(plane.GetDistanceToPoint(bounds.center)) < radius;
    }

    private Material[] BuildMaterialArray()
    {
        Material outside = GetComponent<MeshRenderer>().sharedMaterial;
        Material inside = crossSectionMaterial != null ? crossSectionMaterial : outside;
        return new[] { outside, inside };
    }

    private GameObject CreatePiece(Mesh mesh, string pieceName, Material[] materials, float volumeShare)
    {
        GameObject piece = new GameObject(pieceName);

        // the new mesh is still in the original's local space, so the piece has to
        // sit on exactly the same transform for it to line up
        piece.transform.SetPositionAndRotation(transform.position, transform.rotation);
        piece.transform.localScale = transform.localScale;
        piece.transform.SetParent(transform.parent, true);
        piece.layer = gameObject.layer;
        piece.tag = gameObject.tag;

        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
        piece.AddComponent<MeshRenderer>().sharedMaterials = materials;

        // convex is required both for a moving rigidbody and for XRI to grab it
        MeshCollider collider = piece.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = true;

        Rigidbody body = piece.AddComponent<Rigidbody>();
        Rigidbody originalBody = GetComponent<Rigidbody>();
        body.mass = Mathf.Max(0.01f, (originalBody != null ? originalBody.mass : 1f) * volumeShare);

        // the collider has to exist before the interactable is added, because
        // XRGrabInteractable collects its colliders in Awake
        if (piecesAreGrabbable)
        {
            piece.AddComponent<XRGrabInteractable>();
        }

        if (piecesStaySliceable)
        {
            Sliceable childSliceable = piece.AddComponent<Sliceable>();
            childSliceable.CopySettingsFrom(this);
        }

        return piece;
    }

    private void CopySettingsFrom(Sliceable other)
    {
        crossSectionMaterial = other.crossSectionMaterial;
        piecesStaySliceable = other.piecesStaySliceable;
        piecesAreGrabbable = other.piecesAreGrabbable;
        minimumPieceVolume = other.minimumPieceVolume;
        separationImpulse = other.separationImpulse;
        cutClip = other.cutClip;
        cutVolume = other.cutVolume;
    }

    private void PushApart(GameObject piece, Vector3 direction)
    {
        if (separationImpulse <= 0f)
        {
            return;
        }

        piece.GetComponent<Rigidbody>().AddForce(direction * separationImpulse, ForceMode.Impulse);
    }

    private void PlayCutSound()
    {
        if (cutClip == null)
        {
            return;
        }

        // played at a point in the world rather than on this object, because this
        // object is about to be destroyed and would cut the sound off
        AudioSource.PlayClipAtPoint(cutClip, transform.position, cutVolume);
    }

    // if the patient is being held when it gets cut, XRI still thinks it owns this
    // object. tell the manager to let go before the object disappears, otherwise the
    // interactor is left holding a destroyed reference
    private void RemoveOriginal()
    {
        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();

        if (grab != null && grab.isSelected && grab.interactionManager != null)
        {
            grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        }

        Destroy(gameObject);
    }
}
