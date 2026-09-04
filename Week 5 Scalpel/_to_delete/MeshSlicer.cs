using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// the geometry half of the scalpel. no Unity scene knowledge at all - it takes a
// mesh and a plane (both in the SAME space, which is the mesh's local space) and
// hands back two new meshes.
//
// keeping it a static class with no MonoBehaviour makes it testable and means the
// same code could slice anything, not just the patient object.
//
// the algorithm, in the order the code runs:
//   1. walk every triangle and work out which side of the plane each of its
//      3 vertices is on (signed distance from the plane)
//   2. triangles fully on one side get copied across unchanged
//   3. triangles that straddle the plane get split into 3 smaller triangles -
//      1 on the lone vertex's side, 2 on the other side. the new corner points sit
//      exactly on the plane and are found by lerping along the two crossing edges
//   4. every split records the little line segment it created on the plane. those
//      segments together form the outline of the cut face
//   5. that outline is filled in with a triangle fan from its centre, once for each
//      half, with the normals flipped so both caps face outwards
public static class MeshSlicer
{
    // anything closer to the plane than this counts as "on" the plane. stops us
    // generating zero-area slivers when the blade clips a vertex exactly
    private const float PlaneTolerance = 1e-5f;

    /// <summary>
    /// Splits <paramref name="sourceMesh"/> with <paramref name="localPlane"/>.
    /// The plane must already be expressed in the mesh's local space.
    /// Returns false if the plane misses the mesh (nothing ended up on one side).
    /// </summary>
    /// <param name="capPoints">optional - filled with the points on the cut outline,
    /// in local space, so the caller can drop debug markers on them.</param>
    public static bool Slice(Mesh sourceMesh, Plane localPlane, bool generateCap,
                             out Mesh positiveMesh, out Mesh negativeMesh,
                             List<Vector3> capPoints = null)
    {
        positiveMesh = null;
        negativeMesh = null;

        if (sourceMesh == null || !sourceMesh.isReadable)
        {
            Debug.LogError("MeshSlicer: mesh is null or Read/Write is not enabled on it.");
            return false;
        }

        SourceMesh source = new SourceMesh(sourceMesh);
        MeshSide positive = new MeshSide(source);
        MeshSide negative = new MeshSide(source);

        // each entry is one edge of the cut outline: two points that both sit on the plane
        List<CutEdge> cutEdges = new List<CutEdge>();

        int[] triangles = source.Triangles;

        for (int t = 0; t < triangles.Length; t += 3)
        {
            int i0 = triangles[t];
            int i1 = triangles[t + 1];
            int i2 = triangles[t + 2];

            // signed distance - positive means the vertex is on the side the plane
            // normal points towards
            float d0 = localPlane.GetDistanceToPoint(source.Vertices[i0]);
            float d1 = localPlane.GetDistanceToPoint(source.Vertices[i1]);
            float d2 = localPlane.GetDistanceToPoint(source.Vertices[i2]);

            bool s0 = d0 >= -PlaneTolerance;
            bool s1 = d1 >= -PlaneTolerance;
            bool s2 = d2 >= -PlaneTolerance;

            // easy case: all three corners agree, so the whole triangle goes to one side
            if (s0 == s1 && s1 == s2)
            {
                MeshSide side = s0 ? positive : negative;
                side.AddSurfaceTriangle(side.AddSourceVertex(i0),
                                        side.AddSourceVertex(i1),
                                        side.AddSourceVertex(i2));
                continue;
            }

            // hard case: the plane cuts through this triangle. rotate the corners so
            // the odd one out is first. rotating (not swapping) keeps the winding
            // order, which keeps the triangle facing the right way
            int a, b, c;
            float da, db, dc;
            bool loneIsPositive;

            if (s0 != s1 && s0 != s2)
            {
                a = i0; b = i1; c = i2; da = d0; db = d1; dc = d2; loneIsPositive = s0;
            }
            else if (s1 != s0 && s1 != s2)
            {
                a = i1; b = i2; c = i0; da = d1; db = d2; dc = d0; loneIsPositive = s1;
            }
            else
            {
                a = i2; b = i0; c = i1; da = d2; db = d0; dc = d1; loneIsPositive = s2;
            }

            // where along edge a->b and edge c->a does the plane sit?
            // da and db always have opposite signs here, so the divide is safe
            float tAB = da / (da - db);
            float tCA = dc / (dc - da);

            MeshSide lone = loneIsPositive ? positive : negative;
            MeshSide bulk = loneIsPositive ? negative : positive;

            // the lone corner keeps one small triangle
            int loneA = lone.AddSourceVertex(a);
            int loneAB = lone.AddInterpolatedVertex(a, b, tAB);
            int loneCA = lone.AddInterpolatedVertex(c, a, tCA);
            lone.AddSurfaceTriangle(loneA, loneAB, loneCA);

            // the other two corners get a quad, which has to be two triangles
            int bulkB = bulk.AddSourceVertex(b);
            int bulkC = bulk.AddSourceVertex(c);
            int bulkAB = bulk.AddInterpolatedVertex(a, b, tAB);
            int bulkCA = bulk.AddInterpolatedVertex(c, a, tCA);
            bulk.AddSurfaceTriangle(bulkAB, bulkB, bulkC);
            bulk.AddSurfaceTriangle(bulkAB, bulkC, bulkCA);

            // remember the new edge that was just carved on the plane
            cutEdges.Add(new CutEdge(
                Vector3.Lerp(source.Vertices[a], source.Vertices[b], tAB),
                Vector3.Lerp(source.Vertices[c], source.Vertices[a], tCA)));
        }

        // if either half came back empty the blade never actually went through the
        // object - it only grazed it, so report "no cut" and let the caller bail out
        if (positive.SurfaceTriangles.Count == 0 || negative.SurfaceTriangles.Count == 0)
        {
            return false;
        }

        if (capPoints != null)
        {
            for (int i = 0; i < cutEdges.Count; i++)
            {
                capPoints.Add(cutEdges[i].Start);
                capPoints.Add(cutEdges[i].End);
            }
        }

        if (generateCap && cutEdges.Count >= 3)
        {
            // the two caps are the same shape but face opposite ways. the positive
            // half's cap looks back down the normal, the negative half's looks along it
            BuildCap(positive, cutEdges, -localPlane.normal);
            BuildCap(negative, cutEdges, localPlane.normal);
        }

        positiveMesh = positive.Build(sourceMesh.name + "_A");
        negativeMesh = negative.Build(sourceMesh.name + "_B");
        return true;
    }

    // fills the hole left by the cut with a triangle fan from the middle of the
    // outline out to each edge.
    //
    // a fan is correct for any convex cross-section - sphere, capsule, cylinder,
    // cube - which covers everything in this scene. a concave cross-section (a
    // torus, or an L-shape) would need proper polygon triangulation instead, so
    // the fan would produce a cap that bulges over the dent.
    private static void BuildCap(MeshSide side, List<CutEdge> edges, Vector3 capNormal)
    {
        // centre of the outline
        Vector3 centre = Vector3.zero;
        for (int i = 0; i < edges.Count; i++)
        {
            centre += edges[i].Start + edges[i].End;
        }
        centre /= edges.Count * 2f;

        // a 2D coordinate system lying in the plane, used to give the cap sane UVs
        Vector3 uAxis = Vector3.Normalize(Vector3.Cross(capNormal,
            Mathf.Abs(capNormal.y) < 0.9f ? Vector3.up : Vector3.right));
        Vector3 vAxis = Vector3.Cross(capNormal, uAxis);
        Vector4 capTangent = new Vector4(uAxis.x, uAxis.y, uAxis.z, -1f);

        int centreIndex = side.AddRawVertex(centre, capNormal, new Vector2(0.5f, 0.5f), capTangent);

        for (int i = 0; i < edges.Count; i++)
        {
            Vector3 p0 = edges[i].Start;
            Vector3 p1 = edges[i].End;

            // the outline edges come out of the loop above in no particular
            // direction, so check the winding and flip the pair if this triangle
            // would end up facing backwards
            Vector3 faceNormal = Vector3.Cross(p0 - centre, p1 - centre);
            if (Vector3.Dot(faceNormal, capNormal) < 0f)
            {
                (p0, p1) = (p1, p0);
            }

            int a = side.AddRawVertex(p0, capNormal, CapUv(p0, centre, uAxis, vAxis), capTangent);
            int b = side.AddRawVertex(p1, capNormal, CapUv(p1, centre, uAxis, vAxis), capTangent);
            side.AddCapTriangle(centreIndex, a, b);
        }
    }

    private static Vector2 CapUv(Vector3 point, Vector3 centre, Vector3 uAxis, Vector3 vAxis)
    {
        Vector3 offset = point - centre;
        return new Vector2(Vector3.Dot(offset, uAxis), Vector3.Dot(offset, vAxis)) + new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// Approximate volume of a closed mesh, in local units cubed. Used to throw away
    /// paper-thin slivers when the blade only clips the very edge of an object.
    /// Works by summing the signed volume of the tetrahedron each triangle makes
    /// with the origin - the parts outside the surface cancel out.
    /// </summary>
    public static float CalculateVolume(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        float volume = 0f;

        for (int t = 0; t < triangles.Length; t += 3)
        {
            Vector3 p0 = vertices[triangles[t]];
            Vector3 p1 = vertices[triangles[t + 1]];
            Vector3 p2 = vertices[triangles[t + 2]];
            volume += Vector3.Dot(Vector3.Cross(p0, p1), p2) / 6f;
        }

        return Mathf.Abs(volume);
    }

    // ---------------------------------------------------------------------------
    // helpers
    // ---------------------------------------------------------------------------

    private readonly struct CutEdge
    {
        public readonly Vector3 Start;
        public readonly Vector3 End;

        public CutEdge(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }
    }

    // pulls the source arrays out of the Mesh once. reading mesh.vertices allocates
    // a fresh copy every single time you touch it, so caching matters here
    private class SourceMesh
    {
        public readonly Vector3[] Vertices;
        public readonly Vector3[] Normals;
        public readonly Vector2[] Uvs;
        public readonly Vector4[] Tangents;
        public readonly int[] Triangles;

        public SourceMesh(Mesh mesh)
        {
            Vertices = mesh.vertices;
            Normals = mesh.normals;
            Uvs = mesh.uv;
            Tangents = mesh.tangents;
            Triangles = mesh.triangles;
        }

        // a mesh is allowed to have no normals / uvs / tangents, so fall back to
        // something harmless instead of indexing off the end of an empty array
        public Vector3 NormalAt(int i) => Normals.Length > i ? Normals[i] : Vector3.up;
        public Vector2 UvAt(int i) => Uvs.Length > i ? Uvs[i] : Vector2.zero;
        public Vector4 TangentAt(int i) => Tangents.Length > i ? Tangents[i] : new Vector4(1f, 0f, 0f, -1f);
    }

    // one of the two halves being built up
    private class MeshSide
    {
        private readonly SourceMesh source;
        private readonly Dictionary<int, int> sourceToLocal = new Dictionary<int, int>();

        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<Vector4> Tangents = new List<Vector4>();

        // kept apart so the outer surface and the cut face can use different
        // materials - submesh 0 is skin, submesh 1 is the red inside
        public readonly List<int> SurfaceTriangles = new List<int>();
        public readonly List<int> CapTriangles = new List<int>();

        public MeshSide(SourceMesh source)
        {
            this.source = source;
        }

        // a vertex copied straight from the original mesh. the dictionary means a
        // vertex shared by several triangles is only stored once
        public int AddSourceVertex(int sourceIndex)
        {
            if (sourceToLocal.TryGetValue(sourceIndex, out int existing))
            {
                return existing;
            }

            int index = AddRawVertex(source.Vertices[sourceIndex],
                                     source.NormalAt(sourceIndex),
                                     source.UvAt(sourceIndex),
                                     source.TangentAt(sourceIndex));
            sourceToLocal.Add(sourceIndex, index);
            return index;
        }

        // a brand new vertex sitting on the cut plane, t of the way along a->b.
        // everything gets lerped, not just the position, otherwise the shading and
        // texturing break along the seam
        public int AddInterpolatedVertex(int a, int b, float t)
        {
            return AddRawVertex(
                Vector3.Lerp(source.Vertices[a], source.Vertices[b], t),
                Vector3.Slerp(source.NormalAt(a), source.NormalAt(b), t).normalized,
                Vector2.Lerp(source.UvAt(a), source.UvAt(b), t),
                Vector4.Lerp(source.TangentAt(a), source.TangentAt(b), t));
        }

        public int AddRawVertex(Vector3 position, Vector3 normal, Vector2 uv, Vector4 tangent)
        {
            Vertices.Add(position);
            Normals.Add(normal);
            Uvs.Add(uv);
            Tangents.Add(tangent);
            return Vertices.Count - 1;
        }

        public void AddSurfaceTriangle(int a, int b, int c)
        {
            SurfaceTriangles.Add(a);
            SurfaceTriangles.Add(b);
            SurfaceTriangles.Add(c);
        }

        public void AddCapTriangle(int a, int b, int c)
        {
            CapTriangles.Add(a);
            CapTriangles.Add(b);
            CapTriangles.Add(c);
        }

        public Mesh Build(string name)
        {
            Mesh mesh = new Mesh { name = name };

            // slicing adds vertices, so a mesh that was just under the 65k limit can
            // tip over it. 32-bit indices remove the ceiling
            mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, Uvs);
            mesh.SetTangents(Tangents);

            // always 2 submeshes so the renderer's material array is the same shape
            // on both halves, even if one of them somehow has no cap
            mesh.subMeshCount = 2;
            mesh.SetTriangles(SurfaceTriangles, 0);
            mesh.SetTriangles(CapTriangles, 1);

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
