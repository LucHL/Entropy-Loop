using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Built-in Render Pipeline. CPU generation; no compute shader, texture or post-process.
// Owns only its generated visual assets. Physics retains the original generator mesh.
[ExecuteAlways]
public sealed class VoidMapLowPolySurface : MonoBehaviour
{
    [SerializeField, HideInInspector] private Mesh terrain;
    [SerializeField, HideInInspector] private Material surfaceMaterial;
    [SerializeField, HideInInspector] private Mesh sphere;
    [SerializeField, HideInInspector] private Mesh cylinder;

    public static bool Create(VoidMapGeneratorGPU gen, Transform parent, Mesh source)
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            Debug.LogWarning("Low-poly surface targets Built-in rendering. Keeping the original surface.", gen);
            return false;
        }
        Shader shader = Resources.Load<Shader>("EntropyLoop/LowPolySurface");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning("Missing/unsupported LowPolySurface shader in Resources/EntropyLoop. Keeping original terrain.", gen);
            return false;
        }
        GameObject go = new GameObject("LowPolySurface");
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var owner = go.AddComponent<VoidMapLowPolySurface>();
        owner.terrain = BuildSurface(source, gen);
        owner.surfaceMaterial = new Material(shader) { name = "EntropyLoop_FacetedTerrain" };
        go.AddComponent<MeshFilter>().sharedMesh = owner.terrain;
        go.AddComponent<MeshRenderer>().sharedMaterial = owner.surfaceMaterial;
        // With RenderMeshes collection, this replaces the disabled CPU renderer
        // as NavMesh source; with PhysicsColliders, the original collider is used.
        return true;
    }

    static Mesh BuildSurface(Mesh source, VoidMapGeneratorGPU gen)
    {
        Vector3[] input = source.vertices;
        int[] indices = source.triangles;
        var vertices = new Vector3[indices.Length];
        var normals = new Vector3[indices.Length];
        var colors = new Color32[indices.Length];
        var outputIndices = new int[indices.Length];
        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3 a = input[indices[i]], b = input[indices[i + 1]], c = input[indices[i + 2]];
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            Color32 color = FaceColor((a + b + c) / 3f, normal, gen);
            vertices[i] = a; vertices[i + 1] = b; vertices[i + 2] = c;
            for (int j = 0; j < 3; j++)
            {
                normals[i + j] = normal;
                colors[i + j] = color;
                outputIndices[i + j] = i + j;
            }
        }
        Mesh mesh = new Mesh { name = "EntropyLoop_FlatTerrain" };
        mesh.indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors32 = colors;
        mesh.triangles = outputIndices;
        mesh.RecalculateBounds();
        return mesh;
    }

    static Color FaceColor(Vector3 p, Vector3 normal, VoidMapGeneratorGPU gen)
    {
        float offset = (gen.seed & 65535) * 0.031f;
        float patch = Mathf.PerlinNoise(p.x * 0.10f + offset, p.z * 0.10f - offset);
        float radius = new Vector2(p.x, p.z).magnitude;
        float height = Mathf.Clamp01(p.y / Mathf.Max(0.01f, gen.islandMaxHeight));
        Color moss = Color.Lerp(new Color(0.22f, 0.37f, 0.30f), new Color(0.48f, 0.60f, 0.36f), patch);
        Color stone = Color.Lerp(new Color(0.34f, 0.39f, 0.43f), new Color(0.57f, 0.61f, 0.61f), patch);
        float rock = Mathf.Max(Mathf.InverseLerp(0.30f, 0.62f, height),
            Mathf.InverseLerp(0.9f, 0.55f, normal.y));
        Color color = Color.Lerp(moss, stone, rock);
        float coast = 1f - Mathf.InverseLerp(0f, Mathf.Max(0.1f, gen.beachBand), gen.islandRadius - radius);
        color = Color.Lerp(color, new Color(0.68f, 0.66f, 0.49f), coast * 0.85f);
        float corruption = gen.CorruptionAt(p);
        Color violet = Color.Lerp(new Color(0.28f, 0.19f, 0.37f), new Color(0.15f, 0.10f, 0.23f), corruption);
        violet = Color.Lerp(violet, new Color(0.38f, 0.29f, 0.47f), rock * 0.45f);
        color = Color.Lerp(color, violet, corruption);
        // Tiny broad variation, not high-frequency random speckling.
        float facet = Mathf.PerlinNoise(p.x * 0.75f + 37f, p.z * 0.75f + offset);
        color *= 1f + (facet - 0.5f) * 2f * Mathf.Clamp01(gen.facetVariation);
        // Thin mineral seams encoded in alpha; no extra renderer or light.
        float seam = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.007f, 0.028f, Mathf.Abs(patch - 0.5f)));
        color.a = seam * Mathf.InverseLerp(0.35f, 0.95f, corruption) * 0.65f;
        return color;
    }

    public void FacetDecor(Transform root)
    {
        // All generated forest/props groups, including explicit additive generation.
        foreach (Transform child in root)
            if (child.name == "Forest" || child.name == "Props") FacetGroup(child);
    }

    void FacetGroup(Transform group)
    {
        foreach (MeshFilter filter in group.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;
            // Only Unity primitives, never imported models, board cells or collider meshes.
            if (mesh.name == "Sphere")
            {
                if (sphere == null) sphere = BuildSphere();
                filter.sharedMesh = sphere;
            }
            else if (mesh.name == "Cylinder")
            {
                if (cylinder == null) cylinder = BuildCylinder();
                filter.sharedMesh = cylinder;
            }
        }
    }

    static Vector3 SpherePoint(int ring, int segment)
    {
        float polar = Mathf.PI * ring / 4f;
        float angle = Mathf.PI * 2f * segment / 8f;
        return new Vector3(Mathf.Sin(polar) * Mathf.Cos(angle), Mathf.Cos(polar),
            Mathf.Sin(polar) * Mathf.Sin(angle)) * 0.5f;
    }

    static Mesh BuildSphere()
    {
        var v = new List<Vector3>(144);
        for (int ring = 0; ring < 4; ring++)
        for (int segment = 0; segment < 8; segment++)
        {
            Vector3 a = SpherePoint(ring, segment), b = SpherePoint(ring, segment + 1);
            Vector3 c = SpherePoint(ring + 1, segment), d = SpherePoint(ring + 1, segment + 1);
            if (ring > 0) Face(v, a, b, c);
            if (ring < 3) Face(v, b, d, c);
        }
        return FinishPrimitive(v, "EntropyLoop_Sphere48");
    }

    static Mesh BuildCylinder()
    {
        var v = new List<Vector3>(144);
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI * 2f / 12f, b = (i + 1) * Mathf.PI * 2f / 12f;
            Vector3 bottomA = new Vector3(Mathf.Cos(a) * 0.5f, -1f, Mathf.Sin(a) * 0.5f);
            Vector3 bottomB = new Vector3(Mathf.Cos(b) * 0.5f, -1f, Mathf.Sin(b) * 0.5f);
            Vector3 topA = bottomA + Vector3.up * 2f, topB = bottomB + Vector3.up * 2f;
            Face(v, bottomA, bottomB, topA); Face(v, topA, bottomB, topB);
            Face(v, Vector3.up, topA, topB); Face(v, Vector3.down, bottomB, bottomA);
        }
        return FinishPrimitive(v, "EntropyLoop_Cylinder48");
    }

    static void Face(List<Vector3> vertices, Vector3 a, Vector3 b, Vector3 c)
    {
        // Convex primitives centered at origin: ensure outward-facing triangles.
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f)
        { Vector3 swap = b; b = c; c = swap; }
        vertices.Add(a); vertices.Add(b); vertices.Add(c);
    }

    static Mesh FinishPrimitive(List<Vector3> vertices, string name)
    {
        var indices = new int[vertices.Count];
        var uv = new Vector2[vertices.Count];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = i;
            Vector3 v = vertices[i];
            uv[i] = new Vector2(Mathf.Atan2(v.z, v.x) / (2f * Mathf.PI) + 0.5f, v.y * 0.5f + 0.5f);
        }
        Mesh mesh = new Mesh { name = name };
        mesh.SetVertices(vertices); mesh.uv = uv; mesh.triangles = indices;
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    void OnDestroy()
    {
        Release(terrain); Release(surfaceMaterial); Release(sphere); Release(cylinder);
    }

    static void Release(Object asset)
    {
        if (asset == null) return;
        if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset);
    }
}
