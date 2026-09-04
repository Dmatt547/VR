using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Goes on anything the scalpel can cut. MeshCutter does the geometry; this handles the
// Unity side - converting the plane into local space, then turning the two meshes it
// returns into real objects.
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CuttableObject : MonoBehaviour
{
    [Header("Cut Face")]
    [SerializeField] private Material crossSectionMaterial;

    [Header("Rules")]
    [SerializeField] private bool piecesStayCuttable = true;
    [SerializeField] private bool piecesAreGrabbable = true;

    // how hard the two halves push apart, so you can see the cut happen
    [SerializeField] private float separationImpulse = 0.4f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Rigidbody objectRigidbody;
    private XRGrabInteractable grabInteractable;

    // read by CutPreview so it can run the intersection on the same mesh the cut would
    public Mesh Mesh => meshFilter != null ? meshFilter.sharedMesh : null;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        objectRigidbody = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    // called by ScalpelTool. crossings comes back filled with world-space points
    // for the markers
    public bool TryCut(Plane worldPlane, out GameObject[] pieces, List<Vector3> crossings)
    {
        pieces = null;

        if (meshFilter.sharedMesh == null) return false;

        List<Vector3> localPoints = new List<Vector3>();

        if (!MeshCutter.Cut(meshFilter.sharedMesh, ToLocal(worldPlane), out Mesh frontMesh, out Mesh backMesh, localPoints))
        {
            return false;
        }

        if (crossings != null)
        {
            for (int i = 0; i < localPoints.Count; i++) crossings.Add(transform.TransformPoint(localPoints[i]));
        }

        // submesh 0 is the original outside, submesh 1 the newly exposed inside
        Material[] materials = { meshRenderer.sharedMaterial, crossSectionMaterial != null ? crossSectionMaterial : meshRenderer.sharedMaterial };

        GameObject frontPiece = CreatePiece(frontMesh, name + " (Front)", materials);
        GameObject backPiece = CreatePiece(backMesh, name + " (Back)", materials);

        // push them apart so the cut reads as a cut. the normal is already normalised
        frontPiece.GetComponent<Rigidbody>().AddForce(worldPlane.normal * separationImpulse, ForceMode.Impulse);
        backPiece.GetComponent<Rigidbody>().AddForce(-worldPlane.normal * separationImpulse, ForceMode.Impulse);

        RemoveOriginal();

        pieces = new GameObject[] { frontPiece, backPiece };
        return true;
    }

    // Moves the plane from world into this object's local space (slide 12 - vertices are
    // stored locally). The normal needs the inverse transpose, which world -> local works
    // out to the transpose of localToWorld; InverseTransformDirection would be wrong on a
    // non-uniformly scaled object. Public because CutPreview needs the same conversion.
    public Plane ToLocal(Plane worldPlane)
    {
        Vector3 normal = transform.localToWorldMatrix.transpose.MultiplyVector(worldPlane.normal).normalized;
        Vector3 point = transform.InverseTransformPoint(worldPlane.ClosestPointOnPlane(transform.position));

        return new Plane(normal, point);
    }

    private GameObject CreatePiece(Mesh mesh, string pieceName, Material[] materials)
    {
        GameObject piece = new GameObject(pieceName);

        // the mesh is still in the original's local space, so the piece has to sit on the
        // same transform. parent first, then copy the locals, or a scaled parent rescales it
        piece.transform.SetParent(transform.parent, false);
        piece.transform.localPosition = transform.localPosition;
        piece.transform.localRotation = transform.localRotation;
        piece.transform.localScale = transform.localScale;
        piece.layer = gameObject.layer;
        piece.tag = gameObject.tag;

        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
        piece.AddComponent<MeshRenderer>().sharedMaterials = materials;

        // convex is required for a moving rigidbody, and for XRI to grab it
        MeshCollider pieceCollider = piece.AddComponent<MeshCollider>();
        pieceCollider.sharedMesh = mesh;
        pieceCollider.convex = true;

        Rigidbody pieceRigidbody = piece.AddComponent<Rigidbody>();
        pieceRigidbody.mass = (objectRigidbody != null ? objectRigidbody.mass : 1f) * 0.5f;

        // after the collider - XRGrabInteractable collects its colliders in Awake, which
        // runs the moment the component is added
        if (piecesAreGrabbable) CopyGrabSettings(piece.AddComponent<XRGrabInteractable>());

        // after the interactable, so the piece can cache it and pass the settings on again
        if (piecesStayCuttable) piece.AddComponent<CuttableObject>().CopySettingsFrom(this);

        return piece;
    }

    // a piece is built in code, so its interactable starts on XRI's defaults. copy the
    // settings across so a half behaves like the object it came from
    private void CopyGrabSettings(XRGrabInteractable pieceGrab)
    {
        if (grabInteractable == null) return;

        pieceGrab.movementType = grabInteractable.movementType;
        pieceGrab.farAttachMode = grabInteractable.farAttachMode;
        pieceGrab.useDynamicAttach = grabInteractable.useDynamicAttach;
        pieceGrab.interactionLayers = grabInteractable.interactionLayers;

        // attachTransform is not copied - it points at a child the piece doesn't have
    }

    // the halves aren't prefabs, so the inspector values are handed down
    private void CopySettingsFrom(CuttableObject original)
    {
        crossSectionMaterial = original.crossSectionMaterial;
        piecesStayCuttable = original.piecesStayCuttable;
        piecesAreGrabbable = original.piecesAreGrabbable;
        separationImpulse = original.separationImpulse;
    }

    // if it was held when cut, tell XRI to let go first or the interactor is left holding
    // a destroyed object
    private void RemoveOriginal()
    {
        if (grabInteractable != null && grabInteractable.isSelected && grabInteractable.interactionManager != null)
        {
            grabInteractable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grabInteractable);
        }

        Destroy(gameObject);
    }
}
