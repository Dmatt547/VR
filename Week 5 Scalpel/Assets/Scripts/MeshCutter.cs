using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// static helper - cuts a mesh in half with a plane. the week 5 lecture as code:
//   slide 29/30  signed distance d = ax + by + cz + d says which side a point is on
//   slide 31     solving a ray against the plane gives the exact crossing point
//   slide 32     loop every vertex, sort by the sign of d, rebuild the triangles
//   slide 33     triangles that straddle the plane get re-cut, not deleted
// the lecture throws the d < 0 side away. we keep both halves, for the bonus challenge.
public static class MeshCutter
{
    // this close to the plane counts as on it, so clipping a vertex dead-on doesn't
    // produce zero-area triangles
    private const float Tolerance = 0.00001f;

    // slide 23 - a corner is a position, a normal AND a uv. keep the three together
    private struct Corner
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;
        public float Distance;
    }

    // plane must already be in the mesh's LOCAL space (slide 12). crossings is an empty
    // list that comes back filled with the crossing points, in pairs. false = missed.
    public static bool Cut(Mesh source, Plane plane, out Mesh frontMesh, out Mesh backMesh, List<Vector3> crossings)
    {
        frontMesh = null;
        backMesh = null;
        if (source == null) return false;

        if (!source.isReadable)
        {
            Debug.LogWarning("MeshCutter can't read '" + source.name + "'. Tick Read/Write Enabled on the model.");
            return false;
        }

        // these properties allocate a new array every time you touch them, so read once
        Vector3[] vertices = source.vertices;
        Vector3[] normals = source.normals;
        Vector2[] uvs = source.uv;

        MeshSide front = new MeshSide();   // the d >= 0 side, where the normal points
        MeshSide back = new MeshSide();
        List<Vector3> cuts = crossings != null ? crossings : new List<Vector3>();

        // submeshes are walked separately, not via mesh.triangles which flattens them. a
        // piece from an earlier cut already has 0 = skin, 1 = red cut face, and those must
        // stay apart or the red surface comes back wearing the skin material
        for (int submesh = 0; submesh < source.subMeshCount; submesh++)
        {
            bool isCutFace = submesh > 0;
            int[] triangles = source.GetTriangles(submesh);

            for (int t = 0; t < triangles.Length; t += 3)   // slide 32 - three at a time
            {
                Corner c0 = ReadCorner(triangles[t], vertices, normals, uvs, plane);
                Corner c1 = ReadCorner(triangles[t + 1], vertices, normals, uvs, plane);
                Corner c2 = ReadCorner(triangles[t + 2], vertices, normals, uvs, plane);
                bool f0 = c0.Distance >= -Tolerance;
                bool f1 = c1.Distance >= -Tolerance;
                bool f2 = c2.Distance >= -Tolerance;

                // all three corners agree, so the plane misses this triangle
                if (f0 == f1 && f1 == f2) (f0 ? front : back).Add(c0, c1, c2, isCutFace);
                else Split(c0, c1, c2, f0, f1, f2, front, back, cuts, isCutFace);
            }
        }

        // one side empty means the blade passed near the object, never through it
        if (front.TriangleCount == 0 || back.TriangleCount == 0) return false;

        // splitting leaves both halves hollow, so fill the hole. the two caps sit in the
        // same place but must face opposite ways or one gets culled (slide 13)
        if (cuts.Count >= 6)
        {
            BuildCap(front, cuts, -plane.normal);
            BuildCap(back, cuts, plane.normal);
        }

        frontMesh = front.Build(source.name + " Front");
        backMesh = back.Build(source.name + " Back");
        return true;
    }

    // the mesh arrays are allowed to be empty, hence the length checks
    private static Corner ReadCorner(int i, Vector3[] vertices, Vector3[] normals, Vector2[] uvs, Plane plane)
    {
        return new Corner
        {
            Position = vertices[i],
            Normal = i < normals.Length ? normals[i] : Vector3.up,
            Uv = i < uvs.Length ? uvs[i] : Vector2.zero,
            Distance = SignedDistance(vertices[i], plane)
        };
    }

    // slides 29/30 - the general form ax + by + cz + d, where (a, b, c) is the normal.
    // Plane.GetDistanceToPoint() is this exact line; writing it out shows what it does
    private static float SignedDistance(Vector3 p, Plane plane)
    {
        return (plane.normal.x * p.x) + (plane.normal.y * p.y) + (plane.normal.z * p.z) + plane.distance;
    }

    // slide 31 - where does the edge start->end cross the plane?
    //
    // the slide's formula is t = -(a*Ox + b*Oy + c*Oz + d) / (a*Dx + b*Dy + c*Dz). the
    // numerator is "the signed distance of the ray's origin", already stored on the corner.
    // the denominator is "the rate the ray approaches", which along a straight edge is just
    // how much that distance changed. so the whole thing collapses to this.
    private static float EdgeCrossing(Corner start, Corner end)
    {
        return start.Distance / (start.Distance - end.Distance);
    }

    // slide 33 - the new vertex on the plane. normal and uv get blended too, not just
    // position, or the shading and texture break along the seam (slides 15/16)
    private static Corner Lerp(Corner a, Corner b, float t)
    {
        return new Corner
        {
            Position = Vector3.Lerp(a.Position, b.Position, t),
            Normal = Vector3.Slerp(a.Normal, b.Normal, t).normalized,
            Uv = Vector2.Lerp(a.Uv, b.Uv, t)
        };
    }

    // slide 33 - re-triangulating a triangle the plane passes through. a plane always
    // leaves ONE corner alone and TWO together, so the result is always 1 triangle on the
    // lone side and a quad (2 triangles) on the other
    private static void Split(Corner c0, Corner c1, Corner c2, bool f0, bool f1, bool f2,
                              MeshSide front, MeshSide back, List<Vector3> cuts, bool isCutFace)
    {
        Corner lone, second, third;
        bool loneIsFront;

        // shuffle so the odd corner is first. this ROTATES rather than swaps - swapping a
        // pair reverses the winding order, and slide 13 said that decides which way the face
        // points, so the triangle would get back-face culled and turn invisible
        if (f0 != f1 && f0 != f2) { lone = c0; second = c1; third = c2; loneIsFront = f0; }
        else if (f1 != f0 && f1 != f2) { lone = c1; second = c2; third = c0; loneIsFront = f1; }
        else { lone = c2; second = c0; third = c1; loneIsFront = f2; }

        // the two edges that actually cross are lone->second and third->lone
        Corner crossA = Lerp(lone, second, EdgeCrossing(lone, second));
        Corner crossB = Lerp(third, lone, EdgeCrossing(third, lone));
        MeshSide loneSide = loneIsFront ? front : back;
        MeshSide otherSide = loneIsFront ? back : front;

        loneSide.Add(lone, crossA, crossB, isCutFace);
        otherSide.Add(crossA, second, third, isCutFace);
        otherSide.Add(crossA, third, crossB, isCutFace);

        // crossA -> crossB is one line lying on the plane. all of them make the outline
        cuts.Add(crossA.Position);
        cuts.Add(crossB.Position);
    }

    // fills the hole with a triangle fan from the middle of the outline. only correct for a
    // CONVEX cross-section (sphere, capsule, cylinder, cube), which is everything here -
    // something with a hole in it would need real polygon triangulation
    private static void BuildCap(MeshSide side, List<Vector3> cuts, Vector3 normal)
    {
        Vector3 centre = Vector3.zero;
        for (int i = 0; i < cuts.Count; i++) centre += cuts[i];
        centre /= cuts.Count;

        // flat 2d axes lying in the plane, so the cap gets sensible uvs (slide 15)
        Vector3 u = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        Vector3 v = Vector3.Cross(normal, u);
        Corner mid = CapCorner(centre, centre, normal, u, v);

        for (int i = 0; i < cuts.Count; i += 2)
        {
            Vector3 a = cuts[i];
            Vector3 b = cuts[i + 1];

            // the outline segments come out in no particular direction, so flip the pair if
            // this triangle would face backwards. cross product gives the face normal from
            // the winding order, same as slide 13
            if (Vector3.Dot(Vector3.Cross(a - centre, b - centre), normal) < 0f)
            {
                Vector3 swap = a;
                a = b;
                b = swap;
            }

            side.Add(mid, CapCorner(a, centre, normal, u, v), CapCorner(b, centre, normal, u, v), true);
        }
    }

    // every cap corner shares one normal because the cap is flat. slide 17 called this a
    // hard edge - it keeps the join with the curved outside sharp instead of smoothing it
    private static Corner CapCorner(Vector3 p, Vector3 centre, Vector3 normal, Vector3 u, Vector3 v)
    {
        Vector3 offset = p - centre;

        return new Corner
        {
            Position = p,
            Normal = normal,
            Uv = new Vector2(Vector3.Dot(offset, u), Vector3.Dot(offset, v)) + new Vector2(0.5f, 0.5f)
        };
    }

    // finds only the points where the plane crosses the mesh's edges - no meshes built, no
    // triangles sorted. same maths as Cut() uses, so CutPreview can draw the exact outline
    // the cut would leave, live, before you pull the trigger.
    //
    // takes the arrays rather than the Mesh because reading mesh.vertices allocates a fresh
    // copy every time, and this runs every frame
    public static void FindCrossings(Vector3[] vertices, int[] triangles, Plane plane, List<Vector3> crossings)
    {
        crossings.Clear();

        for (int t = 0; t < triangles.Length; t += 3)
        {
            Vector3 p0 = vertices[triangles[t]];
            Vector3 p1 = vertices[triangles[t + 1]];
            Vector3 p2 = vertices[triangles[t + 2]];
            float d0 = SignedDistance(p0, plane);
            float d1 = SignedDistance(p1, plane);
            float d2 = SignedDistance(p2, plane);

            AddCrossing(p0, p1, d0, d1, crossings);
            AddCrossing(p1, p2, d1, d2, crossings);
            AddCrossing(p2, p0, d2, d0, crossings);
        }
    }

    // an edge only crosses the plane if its two ends are on opposite sides (slide 32)
    private static void AddCrossing(Vector3 a, Vector3 b, float da, float db, List<Vector3> crossings)
    {
        if ((da >= 0f) == (db >= 0f)) return;

        crossings.Add(Vector3.Lerp(a, b, da / (da - db)));
    }

    // one half of the cut, built up triangle by triangle
    private class MeshSide
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> surface = new List<int>();   // submesh 0 - outside
        private readonly List<int> cutFace = new List<int>();   // submesh 1 - exposed inside

        public int TriangleCount => (surface.Count + cutFace.Count) / 3;

        public void Add(Corner a, Corner b, Corner c, bool isCutFace)
        {
            List<int> target = isCutFace ? cutFace : surface;
            target.Add(AddCorner(a));
            target.Add(AddCorner(b));
            target.Add(AddCorner(c));
        }

        // every corner gets its own entry rather than being shared. the original mesh
        // already duplicates positions wherever a hard edge or uv seam needs it - slide 22,
        // the blender cube stores 8 positions but 14 uvs
        private int AddCorner(Corner c)
        {
            vertices.Add(c.Position);
            normals.Add(c.Normal);
            uvs.Add(c.Uv);
            return vertices.Count - 1;
        }

        public Mesh Build(string name)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;

            // cutting adds vertices, so a mesh just under the 65,535 limit can tip over it
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(surface, 0);
            mesh.SetTriangles(cutFace, 1);

            // normals were interpolated by hand so must NOT be recalculated. tangents weren't
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
