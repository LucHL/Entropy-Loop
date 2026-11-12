using System.Collections.Generic;
using UnityEngine;

// Shared flat-shaded meshes keep the procedural scenery inexpensive and consistent.
public static class EntropyLowPoly
{
    static Mesh crown;
    public static GameObject CreatePrimitive(PrimitiveType type)
    {
        var go = GameObject.CreatePrimitive(type);
        if (type != PrimitiveType.Sphere) return go;
        if (!crown) crown = CreateCrown();
        go.GetComponent<MeshFilter>().sharedMesh = crown;
        return go;
    }
    static Mesh CreateCrown()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        Vector3[] poles = { Vector3.up, Vector3.down };
        Vector3[] ring = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        for (int p = 0; p < 2; p++) for (int i = 0; i < 4; i++)
        {
            var a = poles[p]; var b = ring[i]; var c = ring[(i + 1) % 4];
            if (p == 1) { var temp = b; b = c; c = temp; }
            var ab = (a + b).normalized; var bc = (b + c).normalized; var ca = (c + a).normalized;
            Face(vertices, triangles, a, ab, ca); Face(vertices, triangles, ab, b, bc);
            Face(vertices, triangles, ca, bc, c); Face(vertices, triangles, ab, bc, ca);
        }
        var mesh = new Mesh { name = "Entropy faceted crown (32 faces)" };
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }
    static void Face(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
    {
        // Preserve the Unity sphere's diameter of one unit.
        if (Vector3.Dot(Vector3.Cross(b-a,c-a),a+b+c) < 0) { var temp=b; b=c; c=temp; }
        int n=vertices.Count; vertices.Add(a*.5f); vertices.Add(b*.5f); vertices.Add(c*.5f);
        triangles.Add(n); triangles.Add(n+1); triangles.Add(n+2);
    }
}
