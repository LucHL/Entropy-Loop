using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation; // AI Navigation package

[ExecuteAlways]
[RequireComponent(typeof(NavMeshSurface))]
public class VoidbornMapGeneratorHybrid : MonoBehaviour
{
    [Header("Seed & Phase")]
    public int seed = 12345;
    [Range(0, 4)] public int phase = 0; // 0..4

    [Header("Island")]
    public float islandRadius = 60f;
    public float islandEdgeFalloff = 8f;
    public float islandMaxHeight = 6f;
    public int islandResolution = 140;
    public int islandVisualResolution = 200;   // mesh visuel plus fin
    public bool useGPUIslandVisual = true;     // active le rendu GPU

    [Header("Park & Arena (center)")]
    public float parkRadius = 12f;
    public float chessboardSize = 8f; // meters
    public float chessTile = 1f;
    public float arenaRaise = 0.6f;

    [Header("Forest")]
    public int forestCount = 220;
    public Vector2 treeHeightRange = new Vector2(4f, 7f);

    [Header("Village")]
    public float villageRadiusPhase1 = 14f;
    public float villageRadiusPhase2 = 20f;
    public float villageRadiusPhase3 = 26f;
    public float villageRadiusPhase4 = 30f;
    public int housesPhase1 = 10;
    public int housesPhase2 = 20;
    public int housesPhase3 = 35;
    public int housesPhase4 = 45;

    [Header("Roads")]
    public float roadWidth = 2.2f; // human scale
    public float roadY = 0.02f;

    [Header("Lake (phase >= 2)")]
    public float lakeRadius = 6f;
    public Vector2 lakeOffset = new Vector2(10f, 6f);

    [Header("Castle (phase >= 3)")]
    public float castleOuterRadius = 18f;
    public float castleWallHeight = 4.5f;
    public int castleTowers = 6;
    public float towerRadius = 1.6f;

    [Header("House Proportions")]
    public Vector2 houseFootprintRange = new Vector2(2.4f, 3.4f);
    public Vector2 houseFloorHeightRange = new Vector2(2.4f, 2.9f);
    public Vector2Int houseFloorsRange = new Vector2Int(1, 2);
    public float roofHeightFactor = 0.45f;

    [Header("Generation")]
    public bool generateOnPlay = true;
    public bool autoClearBeforeGenerate = true;
    public bool rebuildNavMeshAfterGenerate = true;

    [Header("GPU Island Visual")]
    public Shader islandGPUShader; // Shader "Void/IslandGPU" de dessous

    // Runtime materials
    Material matGrass, matDirt, matRock, matWater, matWood, matRoofDark, matStone, matBoardLight, matBoardDark, matPath;

    // Internal
    System.Random rng;
    Transform root;
    NavMeshSurface navSurface;

    // ------------------------------------------------------------------ //

    void Start()
    {
        navSurface = GetComponent<NavMeshSurface>();
        if (Application.isPlaying && generateOnPlay)
            Generate();
    }

    [ContextMenu("Generate Now")]
    public void Generate()
    {
        rng = new System.Random(seed);
        if (autoClearBeforeGenerate) ClearChildren();
        EnsureRoot();
        BuildMaterials();

        // ISLAND (CPU + optionnel GPU visuel)
        var islandGroup = BuildIslandHybrid();
        islandGroup.name = "Island";

        // Park + arena
        var park = BuildParkAndArena();
        park.name = "ParkAndArena";

        // Forest
        int forestN = Mathf.Max(0, forestCount - phase * 30);
        var forest = BuildForest(forestN);
        forest.name = "Forest";

        // Village
        if (phase >= 1)
        {
            var village = BuildVillage();
            village.name = "Village";
        }

        // Lake
        if (phase >= 2)
        {
            var lake = BuildLake();
            lake.name = "Lake";
        }

        // Castle
        if (phase >= 3)
        {
            var castle = BuildCastle();
            castle.name = "Castle";
        }

        // Roads
        if (phase >= 1)
        {
            var roads = BuildRoads();
            roads.name = "Roads";
        }

        root.transform.position = Vector3.zero;

        // NAVMESH BUILD
        if (navSurface == null)
            navSurface = GetComponent<NavMeshSurface>();

        if (navSurface != null && rebuildNavMeshAfterGenerate)
        {
            navSurface.BuildNavMesh();
        }
    }

    // ------------------------------------------------------------------ //
    // infra
    // ------------------------------------------------------------------ //

    void EnsureRoot()
    {
        var existing = transform.Find("_GEN");
        if (existing) root = existing;
        else
        {
            var go = new GameObject("_GEN");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.layer = gameObject.layer;
            root = go.transform;
        }
    }

    void ClearChildren()
    {
        var existing = transform.Find("_GEN");
        if (!existing) return;
        if (Application.isPlaying) Destroy(existing.gameObject);
        else DestroyImmediate(existing.gameObject);
    }

    void SafeRemoveCollider(Component c)
    {
        if (!c) return;
        if (Application.isPlaying) Destroy(c);
        else DestroyImmediate(c);
    }

    // ------------------------------------------------------------------ //
    // materials
    // ------------------------------------------------------------------ //

    void BuildMaterials()
    {
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        Shader std = Shader.Find("Standard");
        Shader chosen = urp != null ? urp : std;

        matGrass      = NewMat(chosen, new Color(0.31f, 0.55f, 0.28f));
        matDirt       = NewMat(chosen, new Color(0.40f, 0.30f, 0.20f));
        matRock       = NewMat(chosen, new Color(0.52f, 0.52f, 0.55f));
        matWater      = NewMat(chosen, new Color(0.15f, 0.35f, 0.55f, 0.5f), alpha: 0.5f, smooth: 0.7f, forceTransparent: true);
        matWood       = NewMat(chosen, new Color(0.45f, 0.32f, 0.22f));
        matRoofDark   = NewMat(chosen, new Color(0.12f, 0.12f, 0.12f));
        matStone      = NewMat(chosen, new Color(0.6f, 0.6f, 0.63f));
        matBoardLight = NewMat(chosen, new Color(0.92f, 0.92f, 0.92f));
        matBoardDark  = NewMat(chosen, new Color(0.08f, 0.08f, 0.08f));
        matPath       = NewMat(chosen, new Color(0.55f, 0.50f, 0.45f));

        // Si on a pas de shader GPU assigné dans l'inspector, on essaie de le trouver
        if (islandGPUShader == null)
            islandGPUShader = Shader.Find("Void/IslandGPU");
    }

    Material NewMat(Shader shader, Color color, float alpha = 1f, float smooth = 0.4f, bool forceTransparent = false)
    {
        var m = new Material(shader);
        Color c = new Color(color.r, color.g, color.b, alpha);

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))     m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);

        bool transparent = forceTransparent || alpha < 0.999f;
        if (transparent)
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // URP transparent
            m.renderQueue = 3000;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        return m;
    }

    // ------------------------------------------------------------------ //
    // terrain height helper
    // ------------------------------------------------------------------ //

    float TerrainHeight(float x, float z)
    {
        Vector2 p = new Vector2(x, z);
        float r = p.magnitude;
        float edge = Mathf.InverseLerp(islandRadius, islandRadius - islandEdgeFalloff, r);
        edge = Mathf.Clamp01(edge);
        float baseHeight = Mathf.PerlinNoise((x + seed) * 0.045f, (z - seed) * 0.045f) * islandMaxHeight;
        baseHeight *= 1f - edge * 0.95f;
        return baseHeight;
    }

    // ------------------------------------------------------------------ //
    // ISLAND HYBRID : CPU collider + GPU visual
    // ------------------------------------------------------------------ //

    GameObject BuildIslandHybrid()
    {
        var group = new GameObject("IslandGroup");
        group.transform.SetParent(root);
        group.layer = gameObject.layer;

        // --------- MESH CPU POUR COLLIDER + NAVMESH --------- //
        var islandColliderGO = new GameObject("Island_CPU");
        islandColliderGO.transform.SetParent(group.transform);
        islandColliderGO.layer = gameObject.layer;

        var mf = islandColliderGO.AddComponent<MeshFilter>();
        var mr = islandColliderGO.AddComponent<MeshRenderer>();
        var mc = islandColliderGO.AddComponent<MeshCollider>();

        Mesh colliderMesh = GenerateIslandMesh(islandResolution);
        mf.sharedMesh = colliderMesh;
        mc.sharedMesh = colliderMesh;

        // On peut garder ou cacher le rendu CPU
        mr.sharedMaterial = matGrass;
        mr.enabled = !useGPUIslandVisual; // si on a le GPU, on cache le rendu CPU

        // --------- MESH GPU VISUEL --------- //
        if (useGPUIslandVisual && islandGPUShader != null)
        {
            var islandVisualGO = new GameObject("Island_GPU");
            islandVisualGO.transform.SetParent(group.transform);
            islandVisualGO.layer = gameObject.layer;

            var mfVis = islandVisualGO.AddComponent<MeshFilter>();
            var mrVis = islandVisualGO.AddComponent<MeshRenderer>();

            Mesh visualMesh = GenerateIslandMesh(islandVisualResolution);
            mfVis.sharedMesh = visualMesh;

            var matGPU = new Material(islandGPUShader);
            matGPU.SetColor("_BaseColor", new Color(0.31f, 0.55f, 0.28f));
            matGPU.SetFloat("_IslandRadius", islandRadius);
            matGPU.SetFloat("_IslandEdgeFalloff", islandEdgeFalloff);
            matGPU.SetFloat("_IslandMaxHeight", islandMaxHeight);
            matGPU.SetFloat("_IslandSeed", seed);

            mrVis.sharedMaterial = matGPU;
        }

        return group;
    }

    Mesh GenerateIslandMesh(int resolution)
    {
        int n = Mathf.Max(32, resolution);
        float size = islandRadius * 2f;
        Vector3[] verts = new Vector3[n * n];
        Vector2[] uvs = new Vector2[n * n];
        int[] tris = new int[(n - 1) * (n - 1) * 6];

        float step = size / (n - 1);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int i = y * n + x;
            float wx = -islandRadius + x * step;
            float wz = -islandRadius + y * step;

            float h = TerrainHeight(wx, wz);
            verts[i] = new Vector3(wx, h, wz);
            uvs[i] = new Vector2((float)x / (n - 1), (float)y / (n - 1));
        }

        int t = 0;
        for (int y = 0; y < n - 1; y++)
        for (int x = 0; x < n - 1; x++)
        {
            int i = y * n + x;
            tris[t++] = i;
            tris[t++] = i + n + 1;
            tris[t++] = i + n;
            tris[t++] = i;
            tris[t++] = i + 1;
            tris[t++] = i + n + 1;
        }

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ------------------------------------------------------------------ //
    // park + arena
    // ------------------------------------------------------------------ //

    GameObject BuildParkAndArena()
    {
        var group = new GameObject("ParkAndArena");
        group.transform.SetParent(root);
        group.layer = gameObject.layer;

        float cy = TerrainHeight(0f, 0f);

        var park = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        park.name = "ParkPad";
        park.transform.SetParent(group.transform);
        park.layer = gameObject.layer;
        park.transform.localScale = new Vector3(parkRadius * 2f, 0.05f, parkRadius * 2f);
        park.transform.localPosition = new Vector3(0f, cy + arenaRaise, 0f);
        park.GetComponent<MeshRenderer>().sharedMaterial = matDirt;
        SafeRemoveCollider(park.GetComponent<Collider>());

        int tiles = Mathf.Clamp(Mathf.RoundToInt(chessboardSize / chessTile), 4, 12);
        if (tiles % 2 != 0) tiles += 1;
        float boardSize = tiles * chessTile;
        float start = -boardSize * 0.5f + chessTile * 0.5f;
        for (int y = 0; y < tiles; y++)
        for (int x = 0; x < tiles; x++)
        {
            bool dark = ((x + y) % 2 == 0);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            quad.name = $"Tile_{x}_{y}";
            quad.transform.SetParent(group.transform);
            quad.layer = gameObject.layer;
            quad.transform.localScale = new Vector3(chessTile, 0.06f, chessTile);
            quad.transform.localPosition = new Vector3(start + x * chessTile, cy + arenaRaise + 0.06f, start + y * chessTile);
            quad.AddComponent<GridCell>();
            quad.GetComponent<MeshRenderer>().sharedMaterial = dark ? matBoardDark : matBoardLight;
            // SafeRemoveCollider(quad.GetComponent<BoxCollider>());
        }
        return group;
    }

    // ------------------------------------------------------------------ //
    // forest
    // ------------------------------------------------------------------ //

    Transform BuildForest(int count)
    {
        var g = new GameObject("Forest").transform;
        g.SetParent(root);
        g.gameObject.layer = gameObject.layer;
        if (count <= 0) return g;

        var samples = PoissonDisk(new Vector2(islandRadius * 2, islandRadius * 2), 2.2f, 30);
        int placed = 0;
        foreach (var s in samples)
        {
            if (placed >= count) break;
            Vector3 pos = new Vector3(s.x - islandRadius, 0f, s.y - islandRadius);
            if (pos.magnitude < parkRadius + 2f) continue;
            if (IsInsideVillageRadius(pos)) continue;
            if (IsInsideLake(pos)) continue;
            PlaceTree(g, pos);
            placed++;
        }

        float inner = parkRadius + 2f;
        float outer = phase >= 1 ? Mathf.Min(GetVillageRadius() - 1.5f, parkRadius + 14f) : parkRadius + 14f;
        if (outer > inner + 0.5f)
        {
            int ringTrees = Mathf.RoundToInt((outer - inner) * 28f);
            for (int i = 0; i < ringTrees; i++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float rad = Mathf.Lerp(inner, outer, (float)rng.NextDouble());
                Vector3 pos = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
                if (IsInsideLake(pos)) continue;
                PlaceTree(g, pos);
            }
        }
        return g;
    }

    bool IsInsideVillageRadius(Vector3 pos)
    {
        float r = GetVillageRadius();
        return (phase >= 1) && pos.magnitude <= r + 1.5f;
    }

    bool IsInsideLake(Vector3 pos)
    {
        if (phase < 2) return false;
        Vector2 p = new Vector2(pos.x, pos.z) - lakeOffset;
        return p.magnitude <= lakeRadius + 1f;
    }

    void PlaceTree(Transform parent, Vector3 pos)
    {
        float groundY = TerrainHeight(pos.x, pos.z);
        float h = Mathf.Lerp(treeHeightRange.x, treeHeightRange.y, (float)rng.NextDouble());
        float trunk = h * 0.35f;
        float crown = h - trunk;

        var trunkGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunkGO.name = "Tree_Trunk";
        trunkGO.transform.SetParent(parent);
        trunkGO.gameObject.layer = gameObject.layer;
        trunkGO.transform.localScale = new Vector3(h * 0.12f, trunk * 0.5f, h * 0.12f);
        trunkGO.transform.localPosition = new Vector3(pos.x, groundY + trunk * 0.5f, pos.z);
        trunkGO.GetComponent<MeshRenderer>().sharedMaterial = matWood;

        var crownGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crownGO.name = "Tree_Crown";
        crownGO.transform.SetParent(parent);
        crownGO.gameObject.layer = gameObject.layer;
        crownGO.transform.localScale = new Vector3(crown * 0.7f, crown * 0.7f, crown * 0.7f);
        crownGO.transform.localPosition = new Vector3(pos.x, groundY + trunk + crown * 0.5f, pos.z);
        crownGO.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

        SafeRemoveCollider(trunkGO.GetComponent<Collider>());
        SafeRemoveCollider(crownGO.GetComponent<Collider>());
    }

    // ------------------------------------------------------------------ //
    // village
    // ------------------------------------------------------------------ //

    Transform BuildVillage()
    {
        var g = new GameObject("Village").transform;
        g.SetParent(root);
        g.gameObject.layer = gameObject.layer;

        float vr = GetVillageRadius();
        int count = GetHouseCount();

        var samples = PoissonDisk(new Vector2(vr * 2f, vr * 2f), 3.6f, 30);
        int placed = 0;
        foreach (var s in samples)
        {
            if (placed >= count) break;
            Vector3 pos = new Vector3(s.x - vr, 0f, s.y - vr);
            if (pos.magnitude > vr) continue;
            if (pos.magnitude < parkRadius + 1.5f) continue;
            if (IsInsideLake(pos)) continue;

            var house = BuildHouse(pos);
            house.transform.SetParent(g);
            house.layer = gameObject.layer;
            placed++;
        }
        return g;
    }

    float GetVillageRadius()
    {
        switch (phase)
        {
            default:
            case 1: return villageRadiusPhase1;
            case 2: return villageRadiusPhase2;
            case 3: return villageRadiusPhase3;
            case 4: return villageRadiusPhase4;
        }
    }

    int GetHouseCount()
    {
        switch (phase)
        {
            default:
            case 1: return housesPhase1;
            case 2: return housesPhase2;
            case 3: return housesPhase3;
            case 4: return housesPhase4;
        }
    }

    GameObject BuildHouse(Vector3 groundPos)
    {
        float groundY = TerrainHeight(groundPos.x, groundPos.z);
        float foot = Mathf.Lerp(houseFootprintRange.x, houseFootprintRange.y, (float)rng.NextDouble());
        int floors = Mathf.RoundToInt(Mathf.Lerp(houseFloorsRange.x, houseFloorsRange.y, (float)rng.NextDouble()));
        float floorH = Mathf.Lerp(houseFloorHeightRange.x, houseFloorHeightRange.y, (float)rng.NextDouble());
        float totalH = floors * floorH;

        var house = new GameObject("House");
        house.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        house.transform.position = new Vector3(groundPos.x, groundY, groundPos.z);
        house.layer = gameObject.layer;

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(house.transform);
        body.gameObject.layer = gameObject.layer;
        body.transform.localScale = new Vector3(foot, totalH, foot * 0.8f);
        body.transform.localPosition = new Vector3(0f, totalH * 0.5f, 0f);
        body.GetComponent<MeshRenderer>().sharedMaterial = matStone;
        SafeRemoveCollider(body.GetComponent<BoxCollider>());

        float roofH = totalH * roofHeightFactor;
        var roof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        roof.name = "Roof";
        roof.transform.SetParent(house.transform);
        roof.gameObject.layer = gameObject.layer;
        roof.transform.localScale = new Vector3(foot * 1.02f, roofH * 0.5f, (foot * 0.8f) * 1.02f);
        roof.transform.localPosition = new Vector3(0f, totalH + roofH * 0.5f, 0f);
        roof.GetComponent<MeshRenderer>().sharedMaterial = matRoofDark;
        SafeRemoveCollider(roof.GetComponent<Collider>());

        var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "DoorPad";
        pad.transform.SetParent(house.transform);
        pad.gameObject.layer = gameObject.layer;
        pad.transform.localScale = new Vector3(1.2f, 0.06f, 0.8f);
        pad.transform.localPosition = new Vector3(foot * 0.5f + 0.4f, 0.03f, 0f);
        pad.GetComponent<MeshRenderer>().sharedMaterial = matPath;
        SafeRemoveCollider(pad.GetComponent<BoxCollider>());

        return house;
    }

    // ------------------------------------------------------------------ //
    // roads
    // ------------------------------------------------------------------ //

    Transform BuildRoads()
    {
        var g = new GameObject("Roads").transform;
        g.SetParent(root);
        g.gameObject.layer = gameObject.layer;

        float vr = GetVillageRadius();
        int branches = Mathf.Clamp(8 + phase * 2, 8, 14);
        float angleStep = 360f / branches;
        for (int i = 0; i < branches; i++)
        {
            float ang = i * angleStep + (float)rng.NextDouble() * 10f;
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
            float len = Mathf.Lerp(vr * 0.6f, vr, (float)rng.NextDouble());
            BuildRoadStrip(g, Vector3.zero, dir, len);
        }

        BuildRoadRing(g, vr);
        return g;
    }

    void BuildRoadStrip(Transform parent, Vector3 start, Vector3 dir, float length)
    {
        int segments = Mathf.CeilToInt(length);
        for (int i = 0; i < segments; i++)
        {
            Vector3 p = start + dir.normalized * (i + 0.5f);
            float gy = TerrainHeight(p.x, p.z) + roadY;
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "RoadTile";
            tile.transform.SetParent(parent);
            tile.gameObject.layer = gameObject.layer;
            tile.transform.position = new Vector3(p.x, gy, p.z);
            tile.transform.localScale = new Vector3(roadWidth, 0.06f, 1f);
            tile.transform.rotation = Quaternion.LookRotation(dir);
            tile.GetComponent<MeshRenderer>().sharedMaterial = matPath;
            SafeRemoveCollider(tile.GetComponent<BoxCollider>());
        }
    }

    void BuildRoadRing(Transform parent, float radius)
    {
        int tiles = Mathf.CeilToInt(2f * Mathf.PI * radius);
        for (int i = 0; i < tiles; i++)
        {
            float t = (float)i / tiles;
            float ang = t * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
            float gy = TerrainHeight(pos.x, pos.z) + roadY;
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "RoadRingTile";
            tile.transform.SetParent(parent);
            tile.gameObject.layer = gameObject.layer;
            tile.transform.position = new Vector3(pos.x, gy, pos.z);
            tile.transform.localScale = new Vector3(roadWidth, 0.06f, 1f);
            tile.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang)));
            tile.GetComponent<MeshRenderer>().sharedMaterial = matPath;
            SafeRemoveCollider(tile.GetComponent<BoxCollider>());
        }
    }

    // ------------------------------------------------------------------ //
    // lake
    // ------------------------------------------------------------------ //

    GameObject BuildLake()
    {
        var lake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lake.transform.SetParent(root);
        lake.gameObject.layer = gameObject.layer;
        lake.transform.localScale = new Vector3(lakeRadius * 2f, 0.2f, lakeRadius * 2f);
        float gy = TerrainHeight(lakeOffset.x, lakeOffset.y) + 0.02f;
        lake.transform.localPosition = new Vector3(lakeOffset.x, gy, lakeOffset.y);
        lake.GetComponent<MeshRenderer>().sharedMaterial = matWater;
        SafeRemoveCollider(lake.GetComponent<Collider>());
        lake.name = "Lake";
        return lake;
    }

    // ------------------------------------------------------------------ //
    // castle
    // ------------------------------------------------------------------ //

    GameObject BuildCastle()
    {
        var g = new GameObject("Castle");
        g.transform.SetParent(root);
        g.gameObject.layer = gameObject.layer;

        int segments = Mathf.Max(24, Mathf.RoundToInt(castleOuterRadius * 4f));
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            float ang = t * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(ang) * castleOuterRadius, 0f, Mathf.Sin(ang) * castleOuterRadius);
            float gy = TerrainHeight(pos.x, pos.z);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(g.transform);
            wall.gameObject.layer = gameObject.layer;
            wall.transform.position = new Vector3(pos.x, gy + castleWallHeight * 0.5f, pos.z);
            wall.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang)));
            wall.transform.localScale = new Vector3(1.2f, castleWallHeight, 2.6f);
            wall.GetComponent<MeshRenderer>().sharedMaterial = matStone;
            SafeRemoveCollider(wall.GetComponent<BoxCollider>());
        }

        for (int i = 0; i < castleTowers; i++)
        {
            float ang = i * (360f / castleTowers);
            Vector3 pos = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * castleOuterRadius, 0f, Mathf.Sin(ang * Mathf.Deg2Rad) * castleOuterRadius);
            float gy = TerrainHeight(pos.x, pos.z);
            var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "Tower";
            tower.transform.SetParent(g.transform);
            tower.gameObject.layer = gameObject.layer;
            tower.transform.position = new Vector3(pos.x, gy + castleWallHeight * 0.5f, pos.z);
            tower.transform.localScale = new Vector3(towerRadius * 2f, castleWallHeight * 0.5f, towerRadius * 2f);
            tower.GetComponent<MeshRenderer>().sharedMaterial = matStone;
            SafeRemoveCollider(tower.GetComponent<Collider>());
        }

        return g;
    }

    // ------------------------------------------------------------------ //
    // Poisson sampling
    // ------------------------------------------------------------------ //

    List<Vector2> PoissonDisk(Vector2 area, float radius, int k)
    {
        float cell = radius / Mathf.Sqrt(2);
        int gridW = Mathf.CeilToInt(area.x / cell);
        int gridH = Mathf.CeilToInt(area.y / cell);
        Vector2[,] grid = new Vector2[gridW, gridH];
        for (int i = 0; i < gridW; i++)
            for (int j = 0; j < gridH; j++)
                grid[i, j] = new Vector2(-9999f, -9999f);

        List<Vector2> points = new List<Vector2>();
        List<Vector2> active = new List<Vector2>();

        Vector2 first = new Vector2((float)rng.NextDouble() * area.x, (float)rng.NextDouble() * area.y);
        points.Add(first);
        active.Add(first);
        grid[(int)(first.x / cell), (int)(first.y / cell)] = first;

        while (active.Count > 0)
        {
            int idx = rng.Next(active.Count);
            Vector2 p = active[idx];
            bool found = false;
            for (int i = 0; i < k; i++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float rad = radius * (1f + (float)rng.NextDouble());
                Vector2 q = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
                if (q.x < 0 || q.y < 0 || q.x >= area.x || q.y >= area.y) continue;

                int gx = (int)(q.x / cell);
                int gy = (int)(q.y / cell);

                bool ok = true;
                for (int ix = Mathf.Max(0, gx - 2); ix <= Mathf.Min(gridW - 1, gx + 2); ix++)
                for (int iy = Mathf.Max(0, gy - 2); iy <= Mathf.Min(gridH - 1, gy + 2); iy++)
                {
                    Vector2 r = grid[ix, iy];
                    if (r.x < -1000f) continue;
                    if ((r - q).magnitude < radius) { ok = false; break; }
                }
                if (!ok) continue;

                points.Add(q);
                active.Add(q);
                grid[gx, gy] = q;
                found = true;
                break;
            }
            if (!found) active.RemoveAt(idx);
        }
        return points;
    }
}
