using System.Collections.Generic;
using UnityEngine;

// goes on the Blade child of the scalpel
//
// you can't cut a corner off a cube, so the blade is built as a mesh too. it's a flat
// four-sided outline drawn in the YZ plane, then given thickness by making a copy of it
// shifted along X and joining the two with side faces.
//
// the outline, from the back of the blade forwards:
//   P0  back bottom          P1  back top
//   P2  where the spine starts sloping down
//   P3  the point
//
// slide 13 - the outline is drawn in one consistent direction so every face ends up wound
// the same way. get one backwards and it vanishes when you look at that side
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PointedBlade : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private float length = 0.05f;
    [SerializeField] private float height = 0.011f;
    [SerializeField] private float thickness = 0.0012f;

    // how far along before the top edge starts sloping down towards the point.
    // low = a long slender point, high = a stubby one
    [Range(0.1f, 0.9f)]
    [SerializeField] private float spineLength = 0.5f;

    // how high the tip sits. 0 puts the point on the cutting edge at the bottom
    [Range(0f, 0.5f)]
    [SerializeField] private float tipHeight = 0f;

    private Mesh mesh;

    private void OnEnable()
    {
        Build();
    }

    private void OnValidate()
    {
        Build();
    }

    [ContextMenu("Rebuild")]
    private void Build()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Pointed Blade";
        }

        mesh.Clear();

        // the flat outline, in the blade's own plane. z runs forward, y runs up
        Vector2[] outline =
        {
            new Vector2(0f, 0f),
            new Vector2(height, 0f),
            new Vector2(height, length * spineLength),
            new Vector2(height * tipHeight, length)
        };

        float half = thickness * 0.5f;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        // the two flat faces. each gets its own copy of the vertices so the edges stay
        // sharp - slide 17, a hard edge needs the same position stored twice with
        // different normals rather than one shared vertex
        AddFace(vertices, normals, triangles, outline, half, Vector3.right, false);
        AddFace(vertices, normals, triangles, outline, -half, Vector3.left, true);

        // then a strip round the rim joining the two faces
        for (int i = 0; i < outline.Length; i++)
        {
            Vector2 a = outline[i];
            Vector2 b = outline[(i + 1) % outline.Length];

            AddRimQuad(vertices, normals, triangles, a, b, half);
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    // one flat side of the blade, as a fan from the first outline point
    private static void AddFace(List<Vector3> vertices, List<Vector3> normals, List<int> triangles,
                                Vector2[] outline, float x, Vector3 normal, bool reversed)
    {
        int start = vertices.Count;

        for (int i = 0; i < outline.Length; i++)
        {
            vertices.Add(new Vector3(x, outline[i].x, outline[i].y));
            normals.Add(normal);
        }

        for (int i = 1; i < outline.Length - 1; i++)
        {
            triangles.Add(start);
            triangles.Add(start + (reversed ? i + 1 : i));
            triangles.Add(start + (reversed ? i : i + 1));
        }
    }

    // one quad of the rim, running along the outline edge a -> b
    private static void AddRimQuad(List<Vector3> vertices, List<Vector3> normals, List<int> triangles,
                                   Vector2 a, Vector2 b, float half)
    {
        int start = vertices.Count;

        Vector3 frontA = new Vector3(half, a.x, a.y);
        Vector3 backA = new Vector3(-half, a.x, a.y);
        Vector3 backB = new Vector3(-half, b.x, b.y);
        Vector3 frontB = new Vector3(half, b.x, b.y);

        // the cross product of two edges gives the face normal from the winding order,
        // the same relationship the cut caps in MeshCutter rely on
        Vector3 normal = Vector3.Cross(backA - frontA, backB - frontA).normalized;

        vertices.Add(frontA);
        vertices.Add(backA);
        vertices.Add(backB);
        vertices.Add(frontB);

        for (int i = 0; i < 4; i++)
        {
            normals.Add(normal);
        }

        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);

        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }
}
