using System.Collections.Generic;
using UnityEngine;

// Goes on an empty child of the scalpel with a Line Renderer.
// Draws a red loop on the patient showing where the cut will land, using the same
// edge/plane intersection the cut uses but without building any meshes.
[RequireComponent(typeof(LineRenderer))]
public class CutPreview : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private ScalpelTool scalpel;

    [Header("Look")]
    [SerializeField] private Color previewColour = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float lineWidth = 0.002f;

    // pushes the loop out from its own centre so it sits just outside the surface instead
    // of z-fighting with it
    [SerializeField] private float surfaceOffset = 0.0015f;

    private LineRenderer previewLine;

    // mesh.vertices allocates on every read and this runs every frame, so cache per mesh
    private Mesh cachedMesh;
    private Vector3[] vertices;
    private int[] triangles;

    private readonly List<Vector3> crossings = new List<Vector3>();
    private readonly List<Vector3> ring = new List<Vector3>();

    // the sort needs these, and a comparison method can't take extra arguments
    private Vector3 sortCentre;
    private Vector3 sortAcross;
    private Vector3 sortUp;

    // a fresh Line Renderer defaults to a metre-wide white ribbon, which fills the Scene
    // view until Play mode starts. clear it as soon as the component is added.
    private void Reset()
    {
        LineRenderer fresh = GetComponent<LineRenderer>();

        fresh.positionCount = 0;
        fresh.startWidth = lineWidth;
        fresh.endWidth = lineWidth;
        fresh.useWorldSpace = true;
        fresh.loop = true;
    }

    private void Awake()
    {
        previewLine = GetComponent<LineRenderer>();

        if (scalpel == null) scalpel = GetComponentInParent<ScalpelTool>();
        if (scalpel == null) Debug.LogWarning("CutPreview can't find a ScalpelTool - assign one in the Inspector.");

        previewLine.useWorldSpace = true;
        previewLine.loop = true;
        previewLine.startWidth = lineWidth;
        previewLine.endWidth = lineWidth;
        previewLine.startColor = previewColour;
        previewLine.endColor = previewColour;
    }

    // LateUpdate so the hand has finished moving the scalpel for this frame
    private void LateUpdate()
    {
        CuttableObject target = scalpel != null ? scalpel.FindTarget(out _) : null;

        if (target == null || target.Mesh == null)
        {
            previewLine.enabled = false;
            return;
        }

        CacheMesh(target.Mesh);

        // same local-space conversion the real cut uses, so the two can't drift apart
        Plane localPlane = target.ToLocal(scalpel.CutPlane);
        MeshCutter.FindCrossings(vertices, triangles, localPlane, crossings);

        // fewer than 3 means the blade is only clipping a corner
        if (crossings.Count < 3)
        {
            previewLine.enabled = false;
            return;
        }

        BuildRing(target.transform, localPlane.normal);

        previewLine.enabled = true;
        previewLine.positionCount = ring.Count;

        // one at a time, so we don't allocate an array every frame
        for (int i = 0; i < ring.Count; i++) previewLine.SetPosition(i, ring[i]);
    }

    private void CacheMesh(Mesh mesh)
    {
        if (mesh == cachedMesh) return;

        cachedMesh = mesh;
        vertices = mesh.vertices;
        triangles = mesh.triangles;
    }

    // the crossing points come out in triangle order, not loop order. sorting by angle
    // around the middle fixes that, for a convex cross-section.
    private void BuildRing(Transform target, Vector3 planeNormal)
    {
        sortCentre = Vector3.zero;
        for (int i = 0; i < crossings.Count; i++) sortCentre += crossings[i];
        sortCentre /= crossings.Count;

        // two directions lying in the plane, to measure the angle against
        sortAcross = Vector3.Cross(planeNormal, Mathf.Abs(planeNormal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        sortUp = Vector3.Cross(planeNormal, sortAcross);

        crossings.Sort(CompareByAngle);

        ring.Clear();
        Vector3 worldCentre = target.TransformPoint(sortCentre);

        for (int i = 0; i < crossings.Count; i++)
        {
            Vector3 world = target.TransformPoint(crossings[i]);

            // each point is found twice, once per triangle sharing that edge. after
            // sorting the copies are adjacent, so this drops them
            if (ring.Count > 0 && (world - ring[ring.Count - 1]).sqrMagnitude < 0.0000001f) continue;

            // nudge it outwards so the line sits on top of the surface, not inside it
            ring.Add(world + ((world - worldCentre).normalized * surfaceOffset));
        }
    }

    private int CompareByAngle(Vector3 a, Vector3 b)
    {
        float angleA = Mathf.Atan2(Vector3.Dot(a - sortCentre, sortUp), Vector3.Dot(a - sortCentre, sortAcross));
        float angleB = Mathf.Atan2(Vector3.Dot(b - sortCentre, sortUp), Vector3.Dot(b - sortCentre, sortAcross));

        return angleA.CompareTo(angleB);
    }
}
