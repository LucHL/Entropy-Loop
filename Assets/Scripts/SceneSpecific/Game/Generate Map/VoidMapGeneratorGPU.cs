using System.Collections.Generic;
using UnityEngine;
<<<<<<< Updated upstream
using Unity.AI.Navigation; // AI Navigation package
=======
using Unity.AI.Navigation;
using UnityEngine.AI;
>>>>>>> Stashed changes

[ExecuteAlways]
[RequireComponent(typeof(NavMeshSurface))]
public class VoidMapGeneratorGPU : MonoBehaviour
{
    [Header("Seed & Phase")]
    public int seed = 12345;
    [Range(0, 4)] public int phase = 0;

    [Header("Island")]
    public float islandRadius          = 38f;
    public float islandEdgeFalloff     = 10f;
    public float islandMaxHeight       = 8f;
    public int   islandResolution      = 256;
    public int   islandVisualResolution = 400;
    public bool  useGPUIslandVisual    = false;

    [Header("Advanced Terrain")]
    public float largeNoiseScale  = 0.022f;
    public float mediumNoiseScale = 0.05f;
    public float smallNoiseScale  = 0.12f;
    public float cliffStrength    = 1.6f;
    public float beachHeight      = 1.2f;
    public float beachBand        = 5f;

    [Header("Arena Surroundings")]
    public float arenaFlatZoneRadius      = 2f;
    public float surroundElevationStrength = 2.0f;
    public float surroundRingOffset        = 16f;

    [Header("Park & Arena (center)")]
    public float parkRadius     = 4f;
    public float chessboardSize = 3f;
    public float chessTile      = 0.6f;
    public float arenaRaise     = 0.06f;   // quasi ras du sol

    [Header("Forest")]
    public int     forestCount     = 220;
    public Vector2 treeHeightRange = new Vector2(4f, 8f);

    [Header("Village")]
    public float villageRadiusPhase1 = 9f;
    public float villageRadiusPhase2 = 13f;
    public float villageRadiusPhase3 = 17f;
    public float villageRadiusPhase4 = 20f;
    public int   housesPhase1 = 10;
    public int   housesPhase2 = 20;
    public int   housesPhase3 = 35;
    public int   housesPhase4 = 45;

    [Header("Roads")]
    public float roadWidth = 2.2f;
    public float roadY     = 0.03f;

    [Header("Lake (phase >= 2)")]
    public float   lakeRadius = 4f;
    public Vector2 lakeOffset = new Vector2(10f, 6f);

    [Header("Castle (phase >= 3)")]
    public float castleOuterRadius = 11f;
    public float castleWallHeight  = 4.5f;
    public int   castleTowers      = 6;
    public float towerRadius       = 1.6f;

    [Header("House Proportions")]
    public Vector2    houseWidthRange      = new Vector2(2.8f, 4.0f);
    public Vector2    houseDepthRange      = new Vector2(2.4f, 3.6f);
    public Vector2    houseFloorHeightRange = new Vector2(2.4f, 2.9f);
    public Vector2Int houseFloorsRange     = new Vector2Int(1, 2);
    public float      roofHeightFactor     = 0.45f;

    [Header("Generation")]
    public bool generateOnPlay             = true;
    public bool autoClearBeforeGenerate    = true;
    public bool rebuildNavMeshAfterGenerate = true;

    [Header("GPU Island Visual")]
    public Shader islandGPUShader;

    // === TERRAIN ===
    private Material matGrass;
    private Material matGrassLight;
    private Material matDirt;
    private Material matRock;
    private Material matRockDark;
    private Material matSand;
    private Material matSandWet;
    // === EAU ===
    private Material matWater;
    private Material matWaterDeep;
    // === VÉGÉTATION ===
    private Material matWood;
    private Material matWoodDark;
    private Material matLeaf;
    private Material matLeafLight;
    private Material matLeafDark;
    // === CONSTRUCTION ===
    private Material matRoofDark;
    private Material matRoofRed;
    private Material matRoofBrown;
    private Material matStone;
    private Material matStoneDark;
    private Material matPlaster;
    private Material matPlasterWarm;
    private Material matPlasterGrey;
    // === DIVERS ===
    private Material matBoardLight;
    private Material matBoardDark;
    private Material matPath;
    private Material matPathDark;
    private Material matWindow;
    private Material matDoor;

    private System.Random  rng;
    private Transform      root;
    private NavMeshSurface navSurface;

<<<<<<< Updated upstream
    // ------------------------------------------------------------------ //
=======
    public static VoidMapGeneratorGPU instance;

    void Awake()
    {
        instance   = this;
        navSurface = GetComponent<NavMeshSurface>();
    }
>>>>>>> Stashed changes

    void Start()
    {
        if (Application.isPlaying && generateOnPlay)
            Generate();
    }

    void OnValidate()
    {
        islandRadius        = Mathf.Max(10f,   islandRadius);
        islandEdgeFalloff   = Mathf.Max(1f,    islandEdgeFalloff);
        islandMaxHeight     = Mathf.Max(1f,    islandMaxHeight);
        islandResolution    = Mathf.Max(32,    islandResolution);
        islandVisualResolution = Mathf.Max(32, islandVisualResolution);
        parkRadius          = Mathf.Max(2f,    parkRadius);
        chessTile           = Mathf.Max(0.25f, chessTile);
        chessboardSize      = Mathf.Max(2f,    chessboardSize);
        forestCount         = Mathf.Max(0,     forestCount);
        roadWidth           = Mathf.Max(0.5f,  roadWidth);
        lakeRadius          = Mathf.Max(1f,    lakeRadius);
        castleOuterRadius   = Mathf.Max(4f,    castleOuterRadius);
        castleWallHeight    = Mathf.Max(1f,    castleWallHeight);
        castleTowers        = Mathf.Max(3,     castleTowers);
        towerRadius         = Mathf.Max(0.5f,  towerRadius);
    }

    // ──────────────────────────────────────────────────────────────────
    [ContextMenu("Generate Now")]
    public void Generate()
    {
        rng = new System.Random(seed);
        if (autoClearBeforeGenerate) ClearChildren();
        EnsureRoot();
        BuildMaterials();

        GameObject islandGroup = BuildIslandHybrid();
        islandGroup.name = "Island";

        GameObject park = BuildParkAndArena();
        park.name = "ParkAndArena";

        int forestN = Mathf.Max(0, forestCount - phase * 30);
        Transform forest = BuildForest(forestN);
        forest.name = "Forest";

        if (phase >= 1)
        {
            Transform village = BuildVillage();
            village.name = "Village";
        }
        if (phase >= 2)
        {
            GameObject lake = BuildLake();
            lake.name = "Lake";
        }
        if (phase >= 3)
        {
            GameObject castle = BuildCastle();
            castle.name = "Castle";
        }
        if (phase >= 1)
        {
            Transform roads = BuildRoads();
            roads.name = "Roads";
        }

        root.transform.position = Vector3.zero;

        if (navSurface == null) navSurface = GetComponent<NavMeshSurface>();
        if (navSurface != null && rebuildNavMeshAfterGenerate)
            navSurface.BuildNavMesh();
<<<<<<< Updated upstream
        }
=======

        if (Application.isPlaying && EnemySpawnAlgo.instance != null)
            EnemySpawnAlgo.instance.SpawnEnemies(chessTile);

        // Post-processing automatique
        AutoPostProcess pp = UnityEngine.Object.FindFirstObjectByType<AutoPostProcess>();
        if (pp == null)
        {
            GameObject ppGO = new GameObject("_PostProcess");
            ppGO.transform.SetParent(transform);
            pp = ppGO.AddComponent<AutoPostProcess>();
        }
        pp.Apply();
    }

    // ──────────────────────────────────────────────────────────────────
    Vector3 GetValidNavMeshPosition(Vector3 wantedPos, float maxDistance)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(wantedPos, out hit, maxDistance, NavMesh.AllAreas))
            return hit.position;
        return wantedPos;
>>>>>>> Stashed changes
    }

    void EnsureRoot()
    {
        Transform existing = transform.Find("_GEN");
        if (existing != null) { root = existing; return; }
        GameObject go = new GameObject("_GEN");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        go.layer = gameObject.layer;
        root = go.transform;
    }

    void ClearChildren()
    {
        Transform existing = transform.Find("_GEN");
        if (existing == null) return;
        if (Application.isPlaying) Destroy(existing.gameObject);
        else DestroyImmediate(existing.gameObject);
    }

    void SafeRemoveCollider(Component c)
    {
        if (c == null) return;
        if (Application.isPlaying) Destroy(c);
        else DestroyImmediate(c);
    }

    // ──────────────────────────────────────────────────────────────────
    void BuildMaterials()
    {
        // Préférer le shader terrain custom, sinon Standard Built-in
        Shader terrainSh = Shader.Find("Void/TerrainBlend");
        Shader waterSh   = Shader.Find("Void/WaterAnimated");
        Shader std       = Shader.Find("Standard");
        Shader chosen    = std;   // Standard pour tout sauf terrain/eau

        // --- Terrain ---
        matGrass      = NewMat(chosen, new Color(0.27f, 0.52f, 0.20f), 1f, 0.10f);
        matGrassLight = NewMat(chosen, new Color(0.38f, 0.62f, 0.28f), 1f, 0.08f);
        matDirt       = NewMat(chosen, new Color(0.40f, 0.28f, 0.18f), 1f, 0.12f);
        matRock       = NewMat(chosen, new Color(0.55f, 0.53f, 0.50f), 1f, 0.30f);
        matRockDark   = NewMat(chosen, new Color(0.35f, 0.34f, 0.32f), 1f, 0.22f);
        matSand       = NewMat(chosen, new Color(0.84f, 0.76f, 0.55f), 1f, 0.06f);
        matSandWet    = NewMat(chosen, new Color(0.65f, 0.58f, 0.42f), 1f, 0.20f);

        // --- Eau (shader animé si disponible) ---
        Shader wSh   = waterSh != null ? waterSh : chosen;
        matWater     = NewMat(wSh,   new Color(0.14f, 0.46f, 0.68f, 0.60f), 0.60f, 0.92f, true);
        matWaterDeep = NewMat(chosen, new Color(0.08f, 0.22f, 0.45f, 0.72f), 0.72f, 0.95f, true);

        // --- Bois & végétation ---
        matWood      = NewMat(chosen, new Color(0.38f, 0.24f, 0.14f), 1f, 0.14f);
        matWoodDark  = NewMat(chosen, new Color(0.24f, 0.14f, 0.08f), 1f, 0.10f);
        matLeaf      = NewMat(chosen, new Color(0.18f, 0.46f, 0.15f), 1f, 0.10f);
        matLeafLight = NewMat(chosen, new Color(0.35f, 0.58f, 0.22f), 1f, 0.08f);
        matLeafDark  = NewMat(chosen, new Color(0.10f, 0.30f, 0.10f), 1f, 0.12f);

        // --- Toitures ---
        matRoofDark  = NewMat(chosen, new Color(0.18f, 0.06f, 0.04f), 1f, 0.12f);
        matRoofRed   = NewMat(chosen, new Color(0.55f, 0.18f, 0.10f), 1f, 0.15f);
        matRoofBrown = NewMat(chosen, new Color(0.32f, 0.18f, 0.10f), 1f, 0.14f);

        // --- Pierre ---
        matStone      = NewMat(chosen, new Color(0.68f, 0.65f, 0.60f), 1f, 0.22f);
        matStoneDark  = NewMat(chosen, new Color(0.32f, 0.30f, 0.28f), 1f, 0.18f);
        matPlaster    = NewMat(chosen, new Color(0.88f, 0.84f, 0.76f), 1f, 0.12f);
        matPlasterWarm= NewMat(chosen, new Color(0.82f, 0.72f, 0.58f), 1f, 0.10f);
        matPlasterGrey= NewMat(chosen, new Color(0.70f, 0.70f, 0.68f), 1f, 0.12f);

        // --- Divers ---
        matBoardLight = NewMat(chosen, new Color(0.94f, 0.94f, 0.92f), 1f, 0.25f);
        matBoardDark  = NewMat(chosen, new Color(0.06f, 0.06f, 0.06f), 1f, 0.25f);
        matPath       = NewMat(chosen, new Color(0.60f, 0.52f, 0.40f), 1f, 0.06f);
        matPathDark   = NewMat(chosen, new Color(0.38f, 0.30f, 0.22f), 1f, 0.05f);
        matWindow     = NewMat(chosen, new Color(0.55f, 0.75f, 0.90f, 0.55f), 0.55f, 0.95f, true);
        matDoor       = NewMat(chosen, new Color(0.28f, 0.16f, 0.08f), 1f, 0.18f);

        if (islandGPUShader == null)
            islandGPUShader = Shader.Find("Void/IslandGPU");
    }

    Material NewMat(Shader shader, Color color, float alpha, float smooth, bool forceTransparent)
    {
        Material m = new Material(shader);
        Color c = new Color(color.r, color.g, color.b, alpha);
        if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))      m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        bool transparent = forceTransparent || alpha < 0.999f;
        if (transparent)
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            m.renderQueue = 3000;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            // Built-in pipeline transparency
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
        }
        return m;
    }

    Material NewMat(Shader shader, Color color, float alpha, float smooth)
        => NewMat(shader, color, alpha, smooth, false);

    // ──────────────────────────────────────────────────────────────────
    float TerrainHeight(float x, float z)
    {
        float r    = new Vector2(x, z).magnitude;
        float sf   = (float)seed;

        // ── Zone plate au centre (arène + marge de confort) ──────────
        float flatZone = parkRadius + 2.5f;   // ~6.5 unités plat garanti
        if (r <= flatZone) return 0f;

        // ── Masque de bord (l'île retombe à 0 sur la côte) ───────────
        float edgeStart = islandRadius - islandEdgeFalloff;
        float edgeMask  = 1f - Mathf.SmoothStep(edgeStart, islandRadius, r);

        // ── Montée progressive : t = 0 au bord de la zone plate,
        //    t = 1 au début du falloff côtier ─────────────────────────
        float t = Mathf.Clamp01((r - flatZone) / Mathf.Max(0.1f, edgeStart - flatZone));

        // Courbe quadratique : douce au début, plus marquée vers les bords
        float rise = t * t * islandMaxHeight;

        // ── FBM 3 octaves pour un relief naturel ─────────────────────
        // Octave 1 : grandes vagues (formes générales)
        float n1 = Mathf.PerlinNoise((x + sf * 0.11f) * largeNoiseScale,
                                      (z - sf * 0.17f) * largeNoiseScale);
        // Octave 2 : détails moyens (collines)
        float n2 = Mathf.PerlinNoise((x - sf * 0.07f) * mediumNoiseScale,
                                      (z + sf * 0.13f) * mediumNoiseScale);
        // Octave 3 : micro-relief (rochers, bosses)
        float n3 = Mathf.PerlinNoise((x + sf * 0.23f) * smallNoiseScale,
                                      (z - sf * 0.31f) * smallNoiseScale);
        // Octave 4 : très fine rugosité
        float n4 = Mathf.PerlinNoise((x - sf * 0.19f) * smallNoiseScale * 2.2f,
                                      (z + sf * 0.27f) * smallNoiseScale * 2.2f);

        float fbm = n1 * 0.50f + n2 * 0.28f + n3 * 0.14f + n4 * 0.08f;
        // fbm est dans [0,1], on centre autour de 0 → [-1, 1]
        float noiseSigned = (fbm - 0.5f) * 2f;

        // L'amplitude du bruit grossit avec t (peu de variation près du centre,
        // beaucoup vers les crêtes)
        float noiseAmp = t * islandMaxHeight * 0.45f;

        // ── Hauteur finale ────────────────────────────────────────────
        float h = (rise + noiseSigned * noiseAmp) * edgeMask;

        return Mathf.Max(0f, h);
    }

    Material ChooseTerrainMaterial(float x, float z, float h)
    {
        float r     = new Vector2(x, z).magnitude;
        float coast = islandRadius - r;
        if (coast < beachBand * 0.4f && h <= beachHeight * 0.7f) return matSandWet;
        if (coast < beachBand        && h <= beachHeight + 1.2f)  return matSand;
        if (h > islandMaxHeight * 0.80f) return matRockDark;
        if (h > islandMaxHeight * 0.60f) return matRock;
        if (h < beachHeight + 0.4f)      return matDirt;
        float n = Mathf.PerlinNoise(x * 0.08f + seed, z * 0.08f - seed);
        return n > 0.52f ? matGrassLight : matGrass;
    }

    // ──────────────────────────────────────────────────────────────────
    GameObject BuildIslandHybrid()
    {
        GameObject group = new GameObject("IslandGroup");
        group.transform.SetParent(root);
        group.layer = gameObject.layer;

        GameObject islandCPU = new GameObject("Island_CPU");
        islandCPU.transform.SetParent(group.transform);
        islandCPU.layer = gameObject.layer;
        MeshFilter   mf = islandCPU.AddComponent<MeshFilter>();
        MeshRenderer mr = islandCPU.AddComponent<MeshRenderer>();
        MeshCollider mc = islandCPU.AddComponent<MeshCollider>();
        Mesh colliderMesh    = GenerateIslandMesh(islandResolution);
        mf.sharedMesh        = colliderMesh;
        mc.sharedMesh        = colliderMesh;
        mr.sharedMaterial    = matGrass;
        mr.enabled           = !useGPUIslandVisual;

        BuildIslandColorOverlay(group.transform);

        if (useGPUIslandVisual && islandGPUShader != null)
        {
            GameObject islandVisualGO = new GameObject("Island_GPU");
            islandVisualGO.transform.SetParent(group.transform);
            islandVisualGO.layer = gameObject.layer;
            MeshFilter   mfVis = islandVisualGO.AddComponent<MeshFilter>();
            MeshRenderer mrVis = islandVisualGO.AddComponent<MeshRenderer>();
            Mesh visualMesh    = GenerateIslandMesh(islandVisualResolution);
            mfVis.sharedMesh   = visualMesh;
            Material matGPU    = new Material(islandGPUShader);
            matGPU.SetColor("_BaseColor", new Color(0.31f, 0.55f, 0.28f));
            matGPU.SetFloat("_IslandRadius",       islandRadius);
            matGPU.SetFloat("_IslandEdgeFalloff",  islandEdgeFalloff);
            matGPU.SetFloat("_IslandMaxHeight",    islandMaxHeight);
            matGPU.SetFloat("_IslandSeed",         seed);
            mrVis.sharedMaterial = matGPU;
        }
        return group;
    }

    void BuildIslandColorOverlay(Transform parent)
    {
        // Le mesh de l'île (Island_CPU) EST le sol — on n'ajoute que des décorations
        // ponctuelles par-dessus, pas de patches de couverture qui créent des incohérences.

        // Quelques grosses taches de couleur très plates (épaisseur 0.01)
        // pour casser l'uniformité du matériau du mesh — elles suivent exactement le sol
        for (int i = 0; i < 250; i++)
        {
            float ang  = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad  = Mathf.Sqrt((float)rng.NextDouble()) * (islandRadius - 3f);
            float x    = Mathf.Cos(ang) * rad;
            float z    = Mathf.Sin(ang) * rad;
            if (new Vector2(x, z).magnitude < parkRadius + 1f) continue;

            float gy     = TerrainHeight(x, z);
            Material mat = ChooseTerrainMaterial(x, z, gy);

            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = "TerrainTint";
            patch.transform.SetParent(parent);
            patch.layer = gameObject.layer;
            float sx = Mathf.Lerp(1.0f, 5.0f, (float)rng.NextDouble());
            float sz = Mathf.Lerp(1.0f, 5.0f, (float)rng.NextDouble());
            // On colle la tache exactement sur le mesh (épaisseur 1cm)
            patch.transform.position   = new Vector3(x, gy + 0.005f, z);
            patch.transform.rotation   = Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f);
            patch.transform.localScale = new Vector3(sx, 0.01f, sz);
            patch.GetComponent<MeshRenderer>().sharedMaterial = mat;
            SafeRemoveCollider(patch.GetComponent<BoxCollider>());
        }

        AddScatterRocks(parent);
    }

    void AddScatterRocks(Transform parent)
    {
        int rockCount = 80;
        for (int i = 0; i < rockCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Sqrt((float)rng.NextDouble()) * (islandRadius - 1f);
            float x = Mathf.Cos(ang) * rad;
            float z = Mathf.Sin(ang) * rad;
            float h = TerrainHeight(x, z);
            if (rad < parkRadius + 4f) continue;   // garder l'arène libre de rochers
            bool highZone  = h > islandMaxHeight * 0.50f;
            bool coastZone = (islandRadius - rad) < beachBand + 3f;
            if (!highZone && !coastZone) continue;
            float size        = Mathf.Lerp(0.25f, 0.90f, (float)rng.NextDouble());
            float heightScale = Mathf.Lerp(0.20f, 0.60f, (float)rng.NextDouble());
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock";
            rock.transform.SetParent(parent);
            rock.layer = gameObject.layer;
            rock.transform.position  = new Vector3(x, h + heightScale * 0.3f, z);
            rock.transform.rotation  = Quaternion.Euler(
                (float)rng.NextDouble() * 25f - 12f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 20f - 10f);
            rock.transform.localScale = new Vector3(size, heightScale, size * Mathf.Lerp(0.7f, 1.3f, (float)rng.NextDouble()));
            rock.GetComponent<MeshRenderer>().sharedMaterial = highZone ? matRockDark : matRock;
            SafeRemoveCollider(rock.GetComponent<BoxCollider>());
        }
    }

    Mesh GenerateIslandMesh(int resolution)
    {
        int   n    = Mathf.Max(32, resolution);
        float size = islandRadius * 2f;
        Vector3[] verts = new Vector3[n * n];
        Vector2[] uvs   = new Vector2[n * n];
        int[]     tris  = new int[(n - 1) * (n - 1) * 6];
        float step = size / (n - 1);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int   i  = y * n + x;
            float wx = -islandRadius + x * step;
            float wz = -islandRadius + y * step;
            verts[i] = new Vector3(wx, TerrainHeight(wx, wz), wz);
            uvs[i]   = new Vector2((float)x / (n - 1), (float)y / (n - 1));
        }
        int t = 0;
        for (int y = 0; y < n - 1; y++)
        for (int x = 0; x < n - 1; x++)
        {
            int i = y * n + x;
            tris[t++] = i;         tris[t++] = i + n + 1; tris[t++] = i + n;
            tris[t++] = i;         tris[t++] = i + 1;     tris[t++] = i + n + 1;
        }
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ──────────────────────────────────────────────────────────────────
    GameObject BuildParkAndArena()
    {
        GameObject group = new GameObject("ParkAndArena");
        group.transform.SetParent(root);
        group.layer = gameObject.layer;

        float cy = TerrainHeight(0f, 0f);

        // Socle en terre sous les dalles
        float boardWorldSize = Mathf.Ceil(chessboardSize / chessTile) * chessTile;
        GameObject boardPad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boardPad.name = "BoardPad";
        boardPad.transform.SetParent(group.transform);
        boardPad.layer = gameObject.layer;
        boardPad.transform.localScale    = new Vector3(boardWorldSize + 0.6f, 0.08f, boardWorldSize + 0.6f);
        boardPad.transform.localPosition = new Vector3(0f, cy + arenaRaise + 0.02f, 0f);
        boardPad.GetComponent<MeshRenderer>().sharedMaterial = matDirt;
        SafeRemoveCollider(boardPad.GetComponent<BoxCollider>());

        // Dalles de chemin irrégulières (remplace le damier)
        int tiles = Mathf.Clamp(Mathf.RoundToInt(chessboardSize / chessTile), 4, 12);
        if (tiles % 2 != 0) tiles += 1;
        float boardSize = tiles * chessTile;
<<<<<<< Updated upstream
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
            quad.GetComponent<MeshRenderer>().sharedMaterial = dark ? matBoardDark : matBoardLight;
            SafeRemoveCollider(quad.GetComponent<BoxCollider>());
=======
        float start     = -boardSize * 0.5f + chessTile * 0.5f;

        // 3 teintes de pierre pour variation naturelle
        Material[] stoneVariants = new Material[]
        {
            NewMat(Shader.Find("Standard"), new Color(0.62f, 0.58f, 0.50f), 1f, 0.18f), // pierre chaude
            NewMat(Shader.Find("Standard"), new Color(0.52f, 0.50f, 0.46f), 1f, 0.14f), // pierre froide
            NewMat(Shader.Find("Standard"), new Color(0.70f, 0.64f, 0.54f), 1f, 0.20f), // pierre claire
        };

        for (int yy = 0; yy < tiles; yy++)
        for (int xx = 0; xx < tiles; xx++)
        {
            // Variation pseudo-aléatoire déterministe basée sur position
            int   variant  = (xx * 3 + yy * 7 + seed) % 3;
            float jitterX  = ((xx * 17 + yy * 11 + seed) % 100 / 100f - 0.5f) * chessTile * 0.08f;
            float jitterZ  = ((xx * 13 + yy * 19 + seed) % 100 / 100f - 0.5f) * chessTile * 0.08f;
            float sizeJit  = 1f - ((xx * 7  + yy * 5  + seed) % 100 / 100f) * 0.12f; // 88–100%
            float rotJit   = ((xx * 11 + yy * 13 + seed) % 100 / 100f - 0.5f) * 8f;  // ±4°
            float heightJit= ((xx * 5  + yy * 17 + seed) % 100 / 100f) * 0.015f;      // micro-relief

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            quad.name = "Tile_" + xx + "_" + yy;
            quad.tag  = "Tile";
            quad.transform.SetParent(group.transform);
            quad.layer = gameObject.layer;
            quad.transform.localScale    = new Vector3(chessTile * sizeJit - 0.04f, 0.06f + heightJit, chessTile * sizeJit - 0.04f);
            quad.transform.localPosition = new Vector3(start + xx * chessTile + jitterX,
                                                       cy + arenaRaise + 0.07f + heightJit,
                                                       start + yy * chessTile + jitterZ);
            quad.transform.localRotation = Quaternion.Euler(0f, rotJit, 0f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = stoneVariants[variant];
            if (quad.GetComponent<GridCell>() == null)
                quad.AddComponent<GridCell>();
>>>>>>> Stashed changes
        }

        // Joints entre les dalles : fine couche de sable/terre
        AddPathJoints(group.transform, cy + arenaRaise, boardSize);

        // Décoration organique autour de l'échiquier
        AddArenaOrganic(group.transform, cy + arenaRaise, boardWorldSize);

        return group;
    }

    // Couche de sable entre les dalles pour simuler les joints
    void AddPathJoints(Transform parent, float y, float boardSize)
    {
        float half = boardSize * 0.5f + 0.1f;
        // Fond plat continu (joints = espace vide entre dalles laisse voir le socle dirt)
        // On ajoute juste quelques brins de mousse dans les coins
        int mossCount = 10;
        for (int i = 0; i < mossCount; i++)
        {
            float px = Mathf.Lerp(-half, half, (float)(i * 37 % 97) / 97f);
            float pz = Mathf.Lerp(-half, half, (float)(i * 53 % 97) / 97f);
            float sz = Mathf.Lerp(0.04f, 0.10f, (float)(i * 71 % 97) / 97f);
            GameObject moss = GameObject.CreatePrimitive(PrimitiveType.Cube);
            moss.name = "PathMoss";
            moss.transform.SetParent(parent);
            moss.layer = gameObject.layer;
            moss.transform.position   = new Vector3(px, y + 0.065f, pz);
            moss.transform.localScale = new Vector3(sz, 0.01f, sz);
            moss.GetComponent<MeshRenderer>().sharedMaterial = matGrassLight;
            SafeRemoveCollider(moss.GetComponent<BoxCollider>());
        }
    }

    void AddBoardFrame(Transform parent, float y, float boardWorldSize)
    {
        float half = boardWorldSize * 0.5f + 0.15f;
        float thick = 0.18f;
        float frameY = y + 0.09f;

        // 4 bordures
        string[] names = { "Frame_N", "Frame_S", "Frame_E", "Frame_W" };
        Vector3[] positions = {
            new Vector3(0f,    frameY, +half),
            new Vector3(0f,    frameY, -half),
            new Vector3(+half, frameY, 0f),
            new Vector3(-half, frameY, 0f)
        };
        Vector3[] scales = {
            new Vector3(boardWorldSize + thick * 2f + 0.02f, thick, thick),
            new Vector3(boardWorldSize + thick * 2f + 0.02f, thick, thick),
            new Vector3(thick, thick, boardWorldSize + 0.02f),
            new Vector3(thick, thick, boardWorldSize + 0.02f)
        };
        for (int i = 0; i < 4; i++)
        {
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = names[i];
            frame.transform.SetParent(parent);
            frame.layer = gameObject.layer;
            frame.transform.position   = positions[i];
            frame.transform.localScale = scales[i];
            frame.GetComponent<MeshRenderer>().sharedMaterial = matStoneDark;
            SafeRemoveCollider(frame.GetComponent<BoxCollider>());
        }
    }

    void AddArenaOrganic(Transform parent, float y, float boardWorldSize)
    {
        float innerR = boardWorldSize * 0.5f + 0.3f;

        // Petits buissons (sphères)
        int bushCount = 18;
        for (int i = 0; i < bushCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(innerR + 0.5f, parkRadius - 0.4f, (float)rng.NextDouble());
            float x = Mathf.Cos(ang) * rad;
            float z = Mathf.Sin(ang) * rad;
            float gy = TerrainHeight(x, z);
            float sz  = Mathf.Lerp(0.18f, 0.45f, (float)rng.NextDouble());

            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "Bush";
            bush.transform.SetParent(parent);
            bush.layer = gameObject.layer;
            bush.transform.position   = new Vector3(x, gy + sz * 0.5f, z);
            bush.transform.localScale = new Vector3(sz, sz * 0.75f, sz);
            bush.GetComponent<MeshRenderer>().sharedMaterial = matLeafDark;
            SafeRemoveCollider(bush.GetComponent<SphereCollider>());
        }

        // Petits rochers décoratifs
        int rockCount = 12;
        for (int i = 0; i < rockCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(innerR + 0.3f, parkRadius - 0.3f, (float)rng.NextDouble());
            float x = Mathf.Cos(ang) * rad;
            float z = Mathf.Sin(ang) * rad;
            float gy = TerrainHeight(x, z);
            float sz  = Mathf.Lerp(0.08f, 0.22f, (float)rng.NextDouble());

            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "ArenaRock";
            rock.transform.SetParent(parent);
            rock.layer = gameObject.layer;
            rock.transform.position   = new Vector3(x, gy + sz * 0.4f, z);
            rock.transform.rotation   = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 15f);
            rock.transform.localScale = new Vector3(sz, sz * 0.6f, sz * Mathf.Lerp(0.8f, 1.4f, (float)rng.NextDouble()));
            rock.GetComponent<MeshRenderer>().sharedMaterial = matRock;
            SafeRemoveCollider(rock.GetComponent<BoxCollider>());
        }

        // Patches d'herbe au sol
        int patchCount = 30;
        for (int i = 0; i < patchCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(innerR, parkRadius, (float)rng.NextDouble());
            float x = Mathf.Cos(ang) * rad;
            float z = Mathf.Sin(ang) * rad;
            float gy = TerrainHeight(x, z);
            float sx  = Mathf.Lerp(0.2f, 0.6f, (float)rng.NextDouble());
            float sz  = Mathf.Lerp(0.2f, 0.6f, (float)rng.NextDouble());

            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = "GrassPatch";
            patch.transform.SetParent(parent);
            patch.layer = gameObject.layer;
            patch.transform.position   = new Vector3(x, gy + 0.01f, z);
            patch.transform.rotation   = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            patch.transform.localScale = new Vector3(sx, 0.02f, sz);
            patch.GetComponent<MeshRenderer>().sharedMaterial = matGrassLight;
            SafeRemoveCollider(patch.GetComponent<BoxCollider>());
        }
    }

    // ──────────────────────────────────────────────────────────────────
    Transform BuildForest(int count)
    {
        Transform g = new GameObject("Forest").transform;
        g.SetParent(root);
        g.gameObject.layer = gameObject.layer;
        if (count <= 0) return g;

        List<Vector2> samples = PoissonDisk(new Vector2(islandRadius * 2f, islandRadius * 2f), 2.5f, 30);
        int placed = 0;
        for (int i = 0; i < samples.Count; i++)
        {
            if (placed >= count) break;
            Vector2 s   = samples[i];
            Vector3 pos = new Vector3(s.x - islandRadius, 0f, s.y - islandRadius);
            if (pos.magnitude < parkRadius + 2f)   continue;
            if (IsInsideVillageRadius(pos))         continue;
            if (IsInsideLake(pos))                  continue;
            if (pos.magnitude > islandRadius - 3f)  continue;
            PlaceTree(g, pos);
            placed++;
        }

        float inner = parkRadius + 2f;
        float outer = phase >= 1
            ? Mathf.Min(GetVillageRadius() - 1.5f, parkRadius + 14f)
            : parkRadius + 14f;
        if (outer > inner + 0.5f)
        {
            int ringTrees = Mathf.RoundToInt((outer - inner) * 24f);
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
        => (phase >= 1) && pos.magnitude <= GetVillageRadius() + 1.5f;

    bool IsInsideLake(Vector3 pos)
    {
        if (phase < 2) return false;
        Vector2 p = new Vector2(pos.x, pos.z) - lakeOffset;
        return p.magnitude <= lakeRadius + 1f;
    }

    void PlaceTree(Transform parent, Vector3 pos)
    {
        float groundY  = TerrainHeight(pos.x, pos.z);
        float h        = Mathf.Lerp(treeHeightRange.x, treeHeightRange.y, (float)rng.NextDouble());
        int   treeStyle = (int)(rng.NextDouble() * 3);
        float trunk    = h * 0.28f;
        float width    = h * 0.12f;

        GameObject tree = new GameObject("Tree");
        tree.transform.SetParent(parent);
        tree.layer = gameObject.layer;
        tree.transform.position = new Vector3(pos.x, groundY, pos.z);
        tree.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

        Material trunkMat = (treeStyle == 2) ? matWoodDark : matWood;
        GameObject trunkGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunkGO.name = "Tree_Trunk";
        trunkGO.transform.SetParent(tree.transform);
        trunkGO.gameObject.layer = gameObject.layer;
        trunkGO.transform.localScale    = new Vector3(width * 0.40f, trunk * 0.5f, width * 0.40f);
        trunkGO.transform.localPosition = new Vector3(0f, trunk * 0.5f, 0f);
        trunkGO.GetComponent<MeshRenderer>().sharedMaterial = trunkMat;
        SafeRemoveCollider(trunkGO.GetComponent<Collider>());

        Material leafMat = treeStyle == 0 ? matLeafDark : treeStyle == 1 ? matLeaf : matLeafLight;
        PlaceTreeCrown(tree.transform, width, h, trunk, leafMat);
    }

    void PlaceTreeCrown(Transform tree, float width, float h, float trunk, Material leafMat)
    {
        float[] crownSizes   = { 2.0f, 1.50f, 1.10f, 0.70f };
        float[] crownHeights = { 0.38f, 0.28f, 0.20f, 0.14f };
        float[] crownOffsets = { 0.30f, 0.62f, 0.84f, 1.02f };
        for (int layer = 0; layer < crownSizes.Length; layer++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.name = "Crown_" + layer;
            c.transform.SetParent(tree);
            c.gameObject.layer = tree.gameObject.layer;
            c.transform.localScale    = new Vector3(
                width * crownSizes[layer] * 1.7f,
                h * crownHeights[layer],
                width * crownSizes[layer] * 1.7f);
            c.transform.localPosition = new Vector3(0f, trunk + h * crownOffsets[layer] * 0.55f, 0f);
            c.GetComponent<MeshRenderer>().sharedMaterial = leafMat;
            SafeRemoveCollider(c.GetComponent<Collider>());
        }
    }

    // ──────────────────────────────────────────────────────────────────
    Transform BuildVillage()
    {
        Transform g = new GameObject("Village").transform;
        g.SetParent(root);
        g.gameObject.layer = gameObject.layer;
        float vr    = GetVillageRadius();
        int   count = GetHouseCount();
        List<Vector2> samples = PoissonDisk(new Vector2(vr * 2f, vr * 2f), 4.0f, 30);
        int placed = 0;
        for (int i = 0; i < samples.Count; i++)
        {
            if (placed >= count) break;
            Vector2 s   = samples[i];
            Vector3 pos = new Vector3(s.x - vr, 0f, s.y - vr);
            if (pos.magnitude > vr)              continue;
            if (pos.magnitude < parkRadius + 1.5f) continue;
            if (IsInsideLake(pos))               continue;
            GameObject house = BuildHouse(pos);
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
        float groundY  = TerrainHeight(groundPos.x, groundPos.z);
        float width    = Mathf.Lerp(houseWidthRange.x,      houseWidthRange.y,      (float)rng.NextDouble());
        float depth    = Mathf.Lerp(houseDepthRange.x,      houseDepthRange.y,      (float)rng.NextDouble());
        int   floors   = Mathf.RoundToInt(Mathf.Lerp(houseFloorsRange.x, houseFloorsRange.y, (float)rng.NextDouble()));
        float floorH   = Mathf.Lerp(houseFloorHeightRange.x, houseFloorHeightRange.y, (float)rng.NextDouble());
        float totalH   = floors * floorH;

        int wallVariant = (int)(rng.NextDouble() * 3);
        Material wallMat = wallVariant == 0 ? matPlaster : wallVariant == 1 ? matPlasterWarm : matPlasterGrey;
        int roofVariant  = (int)(rng.NextDouble() * 3);
        Material roofMat = roofVariant == 0 ? matRoofDark : roofVariant == 1 ? matRoofRed : matRoofBrown;

        GameObject house = new GameObject("House");
        house.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        house.transform.position = new Vector3(groundPos.x, groundY, groundPos.z);
        house.layer = gameObject.layer;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(house.transform);
        body.gameObject.layer = gameObject.layer;
        body.transform.localScale    = new Vector3(width, totalH, depth);
        body.transform.localPosition = new Vector3(0f, totalH * 0.5f, 0f);
        body.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
        SafeRemoveCollider(body.GetComponent<BoxCollider>());

        AddHouseTimbers(house.transform, width, depth, totalH);
        float roofH = Mathf.Max(0.8f, totalH * roofHeightFactor);
        BuildHouseRoof(house.transform, width, depth, totalH, roofH, roofMat);
        AddHouseDoorAndWindows(house.transform, width, depth, floors, floorH);

        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "DoorPad";
        pad.transform.SetParent(house.transform);
        pad.gameObject.layer = gameObject.layer;
        pad.transform.localScale    = new Vector3(1.3f, 0.05f, 0.9f);
        pad.transform.localPosition = new Vector3(0f, 0.03f, depth * 0.55f);
        pad.GetComponent<MeshRenderer>().sharedMaterial = matPath;
        SafeRemoveCollider(pad.GetComponent<BoxCollider>());

        AddChimney(house.transform, width, depth, totalH + roofH * 0.6f);
        return house;
    }

    void AddChimney(Transform parent, float width, float depth, float baseY)
    {
        float cx = Mathf.Lerp(-width * 0.25f, width * 0.25f, (float)rng.NextDouble());
        float cz = Mathf.Lerp(-depth * 0.25f, depth * 0.25f, (float)rng.NextDouble());
        GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chimney.name = "Chimney";
        chimney.transform.SetParent(parent);
        chimney.gameObject.layer = gameObject.layer;
        chimney.transform.localScale    = new Vector3(0.30f, 0.65f, 0.30f);
        chimney.transform.localPosition = new Vector3(cx, baseY + 0.30f, cz);
        chimney.GetComponent<MeshRenderer>().sharedMaterial = matStoneDark;
        SafeRemoveCollider(chimney.GetComponent<BoxCollider>());
        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.name = "ChimneyCap";
        cap.transform.SetParent(parent);
        cap.gameObject.layer = gameObject.layer;
        cap.transform.localScale    = new Vector3(0.42f, 0.08f, 0.42f);
        cap.transform.localPosition = new Vector3(cx, baseY + 0.68f, cz);
        cap.GetComponent<MeshRenderer>().sharedMaterial = matRockDark;
        SafeRemoveCollider(cap.GetComponent<BoxCollider>());
    }

    void AddHouseTimbers(Transform parent, float width, float depth, float totalH)
    {
        float trimY = totalH * 0.97f;
        AddTimber(parent, "TrimFront", new Vector3(0f, trimY,  depth * 0.5f), new Vector3(width * 1.03f, 0.09f, 0.09f));
        AddTimber(parent, "TrimBack",  new Vector3(0f, trimY, -depth * 0.5f), new Vector3(width * 1.03f, 0.09f, 0.09f));
        AddTimber(parent, "TrimLeft",  new Vector3(-width * 0.5f, trimY, 0f), new Vector3(0.09f, 0.09f, depth * 1.03f));
        AddTimber(parent, "TrimRight", new Vector3( width * 0.5f, trimY, 0f), new Vector3(0.09f, 0.09f, depth * 1.03f));
        float halfW = width * 0.5f;
        float midY  = totalH * 0.5f;
        AddTimber(parent, "TimberL",   new Vector3(-halfW + 0.04f, midY, depth * 0.5f), new Vector3(0.09f, totalH, 0.09f));
        AddTimber(parent, "TimberR",   new Vector3( halfW - 0.04f, midY, depth * 0.5f), new Vector3(0.09f, totalH, 0.09f));
        AddTimber(parent, "TimberMid", new Vector3(0f, totalH * 0.5f, depth * 0.5f),    new Vector3(width * 1.01f, 0.09f, 0.09f));
    }

    void AddTimber(Transform parent, string tName, Vector3 lPos, Vector3 lScale)
    {
        GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
        t.name = tName;
        t.transform.SetParent(parent);
        t.gameObject.layer     = parent.gameObject.layer;
        t.transform.localScale    = lScale;
        t.transform.localPosition = lPos;
        t.GetComponent<MeshRenderer>().sharedMaterial = matWoodDark;
        SafeRemoveCollider(t.GetComponent<BoxCollider>());
    }

    void AddHouseDoorAndWindows(Transform parent, float width, float depth, int floors, float floorH)
    {
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Door";
        door.transform.SetParent(parent);
        door.gameObject.layer = gameObject.layer;
        door.transform.localScale    = new Vector3(0.90f, floorH * 0.80f, 0.09f);
        door.transform.localPosition = new Vector3(0f, floorH * 0.40f, depth * 0.505f);
        door.GetComponent<MeshRenderer>().sharedMaterial = matDoor;
        SafeRemoveCollider(door.GetComponent<BoxCollider>());

        GameObject lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lintel.name = "DoorLintel";
        lintel.transform.SetParent(parent);
        lintel.gameObject.layer = gameObject.layer;
        lintel.transform.localScale    = new Vector3(1.05f, 0.12f, 0.09f);
        lintel.transform.localPosition = new Vector3(0f, floorH * 0.84f, depth * 0.505f);
        lintel.GetComponent<MeshRenderer>().sharedMaterial = matWoodDark;
        SafeRemoveCollider(lintel.GetComponent<BoxCollider>());

        for (int f = 0; f < floors; f++)
        {
            float wY = f * floorH + floorH * 0.58f;
            if (f > 0) AddWindow(parent, new Vector3(0f, wY, depth * 0.505f), false);
            AddWindow(parent, new Vector3(-width * 0.24f, wY, depth * 0.505f), false);
            AddWindow(parent, new Vector3( width * 0.24f, wY, depth * 0.505f), false);
        }
    }

    void AddWindow(Transform parent, Vector3 localPos, bool side)
    {
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "WindowFrame";
        frame.transform.SetParent(parent);
        frame.gameObject.layer = gameObject.layer;
        frame.transform.localScale    = side ? new Vector3(0.09f, 0.72f, 0.58f) : new Vector3(0.62f, 0.72f, 0.09f);
        frame.transform.localPosition = localPos;
        frame.GetComponent<MeshRenderer>().sharedMaterial = matWoodDark;
        SafeRemoveCollider(frame.GetComponent<BoxCollider>());

        GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glass.name = "WindowGlass";
        glass.transform.SetParent(parent);
        glass.gameObject.layer = gameObject.layer;
        Vector3 glassOffset = side ? Vector3.zero : new Vector3(0f, 0f, 0.02f);
        glass.transform.localScale    = side ? new Vector3(0.05f, 0.56f, 0.44f) : new Vector3(0.48f, 0.56f, 0.05f);
        glass.transform.localPosition = localPos + glassOffset;
        glass.GetComponent<MeshRenderer>().sharedMaterial = matWindow;
        SafeRemoveCollider(glass.GetComponent<BoxCollider>());
    }

    void BuildHouseRoof(Transform parent, float width, float depth, float bodyHeight, float roofHeight, Material roofMat = null)
    {
        if (roofMat == null) roofMat = matRoofDark;
        GameObject roofRoot = new GameObject("Roof");
        roofRoot.transform.SetParent(parent);
        roofRoot.transform.localPosition = new Vector3(0f, bodyHeight, 0f);
        roofRoot.transform.localRotation = Quaternion.identity;
        roofRoot.transform.localScale    = Vector3.one;
        roofRoot.layer = gameObject.layer;

        GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.name = "Roof_Left";
        left.transform.SetParent(roofRoot.transform);
        left.gameObject.layer = gameObject.layer;
        left.transform.localScale    = new Vector3(width * 0.60f, 0.14f, depth * 1.10f);
        left.transform.localPosition = new Vector3(-width * 0.22f, roofHeight * 0.45f, 0f);
        left.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        left.GetComponent<MeshRenderer>().sharedMaterial = roofMat;
        SafeRemoveCollider(left.GetComponent<BoxCollider>());

        GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
        right.name = "Roof_Right";
        right.transform.SetParent(roofRoot.transform);
        right.gameObject.layer = gameObject.layer;
        right.transform.localScale    = new Vector3(width * 0.60f, 0.14f, depth * 1.10f);
        right.transform.localPosition = new Vector3(width * 0.22f, roofHeight * 0.45f, 0f);
        right.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        right.GetComponent<MeshRenderer>().sharedMaterial = roofMat;
        SafeRemoveCollider(right.GetComponent<BoxCollider>());

        GameObject ridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ridge.name = "Roof_Ridge";
        ridge.transform.SetParent(roofRoot.transform);
        ridge.gameObject.layer = gameObject.layer;
        ridge.transform.localScale    = new Vector3(0.14f, 0.14f, depth * 1.08f);
        ridge.transform.localPosition = new Vector3(0f, roofHeight * 0.75f, 0f);
        ridge.GetComponent<MeshRenderer>().sharedMaterial = matWoodDark;
        SafeRemoveCollider(ridge.GetComponent<BoxCollider>());

        AddRoofGable(roofRoot.transform, width, roofHeight,  depth * 0.54f, roofMat);
        AddRoofGable(roofRoot.transform, width, roofHeight, -depth * 0.54f, roofMat);
    }

    void AddRoofGable(Transform roofRoot, float width, float roofH, float zOffset, Material mat)
    {
        GameObject gl = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gl.name = "Gable_L";
        gl.transform.SetParent(roofRoot);
        gl.gameObject.layer = roofRoot.gameObject.layer;
        gl.transform.localScale    = new Vector3(width * 0.52f, 0.12f, 0.12f);
        gl.transform.localPosition = new Vector3(-width * 0.19f, roofH * 0.42f, zOffset);
        gl.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        gl.GetComponent<MeshRenderer>().sharedMaterial = mat;
        SafeRemoveCollider(gl.GetComponent<BoxCollider>());

        GameObject gr = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gr.name = "Gable_R";
        gr.transform.SetParent(roofRoot);
        gr.gameObject.layer = roofRoot.gameObject.layer;
        gr.transform.localScale    = new Vector3(width * 0.52f, 0.12f, 0.12f);
        gr.transform.localPosition = new Vector3(width * 0.19f, roofH * 0.42f, zOffset);
        gr.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        gr.GetComponent<MeshRenderer>().sharedMaterial = mat;
        SafeRemoveCollider(gr.GetComponent<BoxCollider>());
    }

    // ──────────────────────────────────────────────────────────────────
    Transform BuildRoads()
    {
        Transform g = new GameObject("Roads").transform;
        g.SetParent(root);
        g.gameObject.layer = gameObject.layer;
        float vr       = GetVillageRadius();
        int   branches = Mathf.Clamp(8 + phase * 2, 8, 14);
        float angleStep = 360f / branches;
        for (int i = 0; i < branches; i++)
        {
            float   ang = i * angleStep + (float)rng.NextDouble() * 10f;
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
            float   len = Mathf.Lerp(vr * 0.6f, vr, (float)rng.NextDouble());
            BuildRoadStrip(g, Vector3.zero, dir, len);
        }
        BuildRoadRing(g, vr * 0.92f);
        return g;
    }

    void BuildRoadStrip(Transform parent, Vector3 start, Vector3 dir, float length)
    {
        int segments = Mathf.CeilToInt(length);
        for (int i = 0; i < segments; i++)
        {
            Vector3 p  = start + dir.normalized * (i + 0.5f);
            float   gy = TerrainHeight(p.x, p.z) + roadY;
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "RoadTile";
            tile.transform.SetParent(parent);
            tile.gameObject.layer  = gameObject.layer;
            tile.transform.position    = new Vector3(p.x, gy, p.z);
            tile.transform.localScale  = new Vector3(roadWidth, 0.05f, 1f);
            tile.transform.rotation    = Quaternion.LookRotation(dir);
            tile.GetComponent<MeshRenderer>().sharedMaterial = (i % 5 < 1) ? matPathDark : matPath;
            SafeRemoveCollider(tile.GetComponent<BoxCollider>());
        }
    }

    void BuildRoadRing(Transform parent, float radius)
    {
        int tiles = Mathf.CeilToInt(2f * Mathf.PI * radius);
        for (int i = 0; i < tiles; i++)
        {
            float   t   = (float)i / tiles;
            float   ang = t * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
            float   gy  = TerrainHeight(pos.x, pos.z) + roadY;
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "RoadRingTile";
            tile.transform.SetParent(parent);
            tile.gameObject.layer = gameObject.layer;
            tile.transform.position   = new Vector3(pos.x, gy, pos.z);
            tile.transform.localScale = new Vector3(roadWidth, 0.05f, 1f);
            tile.transform.rotation   = Quaternion.LookRotation(new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang)));
            tile.GetComponent<MeshRenderer>().sharedMaterial = (i % 5 < 1) ? matPathDark : matPath;
            SafeRemoveCollider(tile.GetComponent<BoxCollider>());
        }
    }

    // ──────────────────────────────────────────────────────────────────
    GameObject BuildLake()
    {
        GameObject lakeRoot = new GameObject("Lake");
        lakeRoot.transform.SetParent(root);
        lakeRoot.layer = gameObject.layer;

        float gy = TerrainHeight(lakeOffset.x, lakeOffset.y) + 0.02f;

        GameObject lakeDeep = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lakeDeep.name = "LakeDeep";
        lakeDeep.transform.SetParent(lakeRoot.transform);
        lakeDeep.gameObject.layer = gameObject.layer;
        lakeDeep.transform.localScale    = new Vector3(lakeRadius * 1.6f, 0.12f, lakeRadius * 1.6f);
        lakeDeep.transform.localPosition = new Vector3(lakeOffset.x, gy - 0.05f, lakeOffset.y);
        lakeDeep.GetComponent<MeshRenderer>().sharedMaterial = matWaterDeep;
        SafeRemoveCollider(lakeDeep.GetComponent<Collider>());

        GameObject lake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lake.name = "LakeSurface";
        lake.transform.SetParent(lakeRoot.transform);
        lake.gameObject.layer = gameObject.layer;
        lake.transform.localScale    = new Vector3(lakeRadius * 2f, 0.10f, lakeRadius * 2f);
        lake.transform.localPosition = new Vector3(lakeOffset.x, gy, lakeOffset.y);
        lake.GetComponent<MeshRenderer>().sharedMaterial = matWater;
        SafeRemoveCollider(lake.GetComponent<Collider>());

        int pieces = Mathf.CeilToInt(lakeRadius * 12f);
        for (int i = 0; i < pieces; i++)
        {
            float t   = (float)i / pieces;
            float ang = t * Mathf.PI * 2f;
            bool  wet = (i % 3 == 0);
            float shoreRad = lakeRadius + (wet ? 0.10f : 0.28f);
            GameObject shore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shore.name = "Shore";
            shore.transform.SetParent(lakeRoot.transform);
            shore.gameObject.layer = gameObject.layer;
            shore.transform.position   = new Vector3(
                lakeOffset.x + Mathf.Cos(ang) * shoreRad,
                gy - (wet ? 0.08f : 0.05f),
                lakeOffset.y + Mathf.Sin(ang) * shoreRad);
            shore.transform.rotation   = Quaternion.LookRotation(new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang)));
            shore.transform.localScale = new Vector3(wet ? 0.28f : 0.45f, 0.04f, wet ? 0.65f : 1.0f);
            shore.GetComponent<MeshRenderer>().sharedMaterial = wet ? matSandWet : matSand;
            SafeRemoveCollider(shore.GetComponent<BoxCollider>());
        }

        int rockCount = Mathf.CeilToInt(lakeRadius * 2f);
        for (int i = 0; i < rockCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = lakeRadius + Mathf.Lerp(0.3f, 1.2f, (float)rng.NextDouble());
            float sz  = Mathf.Lerp(0.20f, 0.55f, (float)rng.NextDouble());
            GameObject lakeRock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lakeRock.name = "LakeRock";
            lakeRock.transform.SetParent(lakeRoot.transform);
            lakeRock.gameObject.layer = gameObject.layer;
            lakeRock.transform.position   = new Vector3(
                lakeOffset.x + Mathf.Cos(ang) * rad,
                gy - 0.01f,
                lakeOffset.y + Mathf.Sin(ang) * rad);
            lakeRock.transform.rotation   = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            lakeRock.transform.localScale = new Vector3(sz, sz * 0.55f, sz * Mathf.Lerp(0.8f, 1.4f, (float)rng.NextDouble()));
            lakeRock.GetComponent<MeshRenderer>().sharedMaterial = matRock;
            SafeRemoveCollider(lakeRock.GetComponent<BoxCollider>());
        }
        return lakeRoot;
    }

    // ──────────────────────────────────────────────────────────────────
    GameObject BuildCastle()
    {
        GameObject g = new GameObject("Castle");
        g.transform.SetParent(root);
        g.gameObject.layer = gameObject.layer;

        int segments = Mathf.Max(24, Mathf.RoundToInt(castleOuterRadius * 4f));
        for (int i = 0; i < segments; i++)
        {
            float   t   = (float)i / segments;
            float   ang = t * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(ang) * castleOuterRadius, 0f, Mathf.Sin(ang) * castleOuterRadius);
            float   gy  = TerrainHeight(pos.x, pos.z);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(g.transform);
            wall.gameObject.layer = gameObject.layer;
            wall.transform.position   = new Vector3(pos.x, gy + castleWallHeight * 0.5f, pos.z);
            wall.transform.rotation   = Quaternion.LookRotation(new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang)));
            wall.transform.localScale = new Vector3(1.3f, castleWallHeight, 2.7f);
            wall.GetComponent<MeshRenderer>().sharedMaterial = (i % 4 == 0) ? matStoneDark : matStone;
            SafeRemoveCollider(wall.GetComponent<BoxCollider>());
            AddBattlementToWall(wall.transform);
        }

        for (int i = 0; i < castleTowers; i++)
        {
            float   ang = i * (360f / castleTowers);
            Vector3 pos = new Vector3(
                Mathf.Cos(ang * Mathf.Deg2Rad) * castleOuterRadius, 0f,
                Mathf.Sin(ang * Mathf.Deg2Rad) * castleOuterRadius);
            float gy = TerrainHeight(pos.x, pos.z);
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "Tower";
            tower.transform.SetParent(g.transform);
            tower.gameObject.layer = gameObject.layer;
            tower.transform.position   = new Vector3(pos.x, gy + castleWallHeight * 0.5f, pos.z);
            tower.transform.localScale = new Vector3(towerRadius * 2f, castleWallHeight * 0.55f, towerRadius * 2f);
            tower.GetComponent<MeshRenderer>().sharedMaterial = matStoneDark;
            SafeRemoveCollider(tower.GetComponent<Collider>());
            AddTowerCrenels(tower.transform);
            AddTowerRoof(g.transform, pos, gy + castleWallHeight, ang);
        }

        // Donjon central supprimé (v1)
        // AddKeep(g.transform);

        return g;
    }

    void AddTowerRoof(Transform parent, Vector3 basePos, float baseY, float angleDeg)
    {
        GameObject towerRoof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        towerRoof.name = "TowerRoof";
        towerRoof.transform.SetParent(parent);
        towerRoof.gameObject.layer = gameObject.layer;
        towerRoof.transform.position   = new Vector3(basePos.x, baseY + 0.55f, basePos.z);
        towerRoof.transform.localScale = new Vector3(towerRadius * 2.2f, 0.80f, towerRadius * 2.2f);
        towerRoof.GetComponent<MeshRenderer>().sharedMaterial = matRoofDark;
        SafeRemoveCollider(towerRoof.GetComponent<Collider>());
    }

    void AddBattlementToWall(Transform wall)
    {
        for (int i = -1; i <= 1; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "Battlement";
            c.transform.SetParent(wall);
            c.gameObject.layer = gameObject.layer;
            c.transform.localScale    = new Vector3(1.28f, 0.30f, 0.46f);
            c.transform.localPosition = new Vector3(0f, 0.58f, i * 0.75f);
            c.GetComponent<MeshRenderer>().sharedMaterial = matStone;
            SafeRemoveCollider(c.GetComponent<BoxCollider>());
        }
    }

    void AddTowerCrenels(Transform tower)
    {
        int pieces = 8;
        for (int i = 0; i < pieces; i++)
        {
            float ang = (float)i / pieces * Mathf.PI * 2f;
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "TowerCrenel";
            c.transform.SetParent(tower);
            c.gameObject.layer = gameObject.layer;
            c.transform.localPosition = new Vector3(Mathf.Cos(ang) * 0.95f, 0.55f, Mathf.Sin(ang) * 0.95f);
            c.transform.localRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)));
            c.transform.localScale    = new Vector3(0.36f, 0.24f, 0.36f);
            c.GetComponent<MeshRenderer>().sharedMaterial = matStoneDark;
            SafeRemoveCollider(c.GetComponent<BoxCollider>());
        }
    }

    // ──────────────────────────────────────────────────────────────────
    List<Vector2> PoissonDisk(Vector2 area, float radius, int k)
    {
        float cell  = radius / Mathf.Sqrt(2f);
        int   gridW = Mathf.CeilToInt(area.x / cell);
        int   gridH = Mathf.CeilToInt(area.y / cell);
        Vector2[,] grid = new Vector2[gridW, gridH];
        for (int gx = 0; gx < gridW; gx++)
        for (int gy = 0; gy < gridH; gy++)
            grid[gx, gy] = new Vector2(-9999f, -9999f);

        List<Vector2> points = new List<Vector2>();
        List<Vector2> active = new List<Vector2>();
        Vector2 first = new Vector2((float)rng.NextDouble() * area.x, (float)rng.NextDouble() * area.y);
        points.Add(first);
        active.Add(first);
        grid[(int)(first.x / cell), (int)(first.y / cell)] = first;

        while (active.Count > 0)
        {
            int     idx   = rng.Next(active.Count);
            Vector2 p     = active[idx];
            bool    found = false;
            for (int i = 0; i < k; i++)
            {
                float   ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float   rad = radius * (1f + (float)rng.NextDouble());
                Vector2 q   = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
                if (q.x < 0f || q.y < 0f || q.x >= area.x || q.y >= area.y) continue;
                int gx = (int)(q.x / cell);
                int gy = (int)(q.y / cell);
                bool ok = true;
                for (int ix = Mathf.Max(0, gx - 2); ix <= Mathf.Min(gridW - 1, gx + 2) && ok; ix++)
                for (int iy = Mathf.Max(0, gy - 2); iy <= Mathf.Min(gridH - 1, gy + 2) && ok; iy++)
                {
                    Vector2 r = grid[ix, iy];
                    if (r.x > -1000f && (r - q).sqrMagnitude < radius * radius)
                        ok = false;
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
