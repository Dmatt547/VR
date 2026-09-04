using System.Collections.Generic;
using UnityEngine;

// goes on the Handle child of the scalpel
//
// unity has no cone primitive, and scaling a capsule stretches it evenly, so a primitive
// handle is always the same width all the way down. this builds the mesh instead: a stack
// of rings, each one slightly narrower than the last. that difference in radius IS the taper.
//
// same mesh structure the lecture described (slides 11-13) - a list of vertex positions and
// a list of triangles indexing into it, wound consistently so the faces point outwards
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TaperedHandle : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private float length = 0.11f;
    [SerializeField] private float backRadius = 0.008f;     // fat end, away from the blade
    [SerializeField] private float frontRadius = 0.0045f;   // narrow end, where the blade exits

    // squashes the cross-section on X, so the handle is a flattened oval like a real
    // scalpel rather than a round tube. 1 = perfectly round
    [Range(0.2f, 1f)]
    [SerializeField] private float flatten = 0.62f;

    // how much of the length is spent rounding the back end off, as a fraction.
    // small = a blunt dome, large = a long soft nose
    [Range(0.05f, 0.5f)]
    [SerializeField] private float roundedBack = 0.15f;

    [Header("Detail")]
    // slide 14 - poly count matters in XR, and this is a handle nobody looks closely at
    [Range(6, 32)] [SerializeField] private int sides = 14;
    [Range(4, 40)] [SerializeField] private int rings = 24;

    private Mesh mesh;

    private void OnEnable()
    {
        Build();
    }

    // rebuilds live while you drag the sliders, so you can shape it without pressing Play
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
            mesh.name = "Tapered Handle";
        }

        mesh.Clear();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        // one vertex at the very back, for the first ring to fan out from
        vertices.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0f));

        for (int r = 1; r <= rings; r++)
        {
            float t = (float)r / rings;

            // the taper: radius shrinks from back to front
            float radius = Mathf.Lerp(backRadius, frontRadius, t);

            // then round the back end off over the first `roundedBack` of the length.
            // this is a quarter circle, sqrt(1 - x^2), NOT a straight ramp - a straight
            // ramp would pull the back into a cone point instead of a dome
            float capProgress = Mathf.Clamp01(t / roundedBack);
            radius *= Mathf.Sqrt(1f - ((1f - capProgress) * (1f - capProgress)));

            for (int s = 0; s < sides; s++)
            {
                float angle = (float)s / sides * Mathf.PI * 2f;

                vertices.Add(new Vector3(Mathf.Cos(angle) * radius * flatten,
                                         Mathf.Sin(angle) * radius,
                                         t * length));
                uvs.Add(new Vector2((float)s / sides, t));
            }
        }

        // one vertex at the front, closing the narrow end off
        int frontCentre = vertices.Count;
        vertices.Add(new Vector3(0f, 0f, length));
        uvs.Add(new Vector2(0.5f, 1f));

        // fan the back point out to the first ring
        for (int s = 0; s < sides; s++)
        {
            triangles.Add(0);
            triangles.Add(1 + ((s + 1) % sides));
            triangles.Add(1 + s);
        }

        // then a band of quads between each pair of rings, two triangles each
        for (int r = 0; r < rings - 1; r++)
        {
            int lower = 1 + (r * sides);
            int upper = lower + sides;

            for (int s = 0; s < sides; s++)
            {
                int next = (s + 1) % sides;

                triangles.Add(lower + s);
                triangles.Add(lower + next);
                triangles.Add(upper + next);

                triangles.Add(lower + s);
                triangles.Add(upper + next);
                triangles.Add(upper + s);
            }
        }

        // fan the last ring in to the front point
        int lastRing = 1 + ((rings - 1) * sides);

        for (int s = 0; s < sides; s++)
        {
            triangles.Add(lastRing + s);
            triangles.Add(lastRing + ((s + 1) % sides));
            triangles.Add(frontCentre);
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);

        // the rings share vertices all the way round, so recalculating gives smooth
        // normals and the handle shades as a curved surface rather than a set of facets
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}
