using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

[ExecuteAlways]
[RequireComponent(typeof(NavMeshSurface))]
public class VoidMapGeneratorGPU : MonoBehaviour
{
    [Header("Seed & Phase")]
    public int seed = 12345;
    [Range(0, 5)] public int phase = 0;

    [Header("Island")]
    public float islandRadius          = 38f;
    public float islandEdgeFalloff     = 10f;
    public float islandMaxHeight       = 8f;
    public int   islandResolution      = 128;
    public int   islandVisualResolution = 200;
    public bool  useGPUIslandVisual    = false;

    [Header("Planet Underside (ventre de l'île)")]
    public bool  buildUnderside      = true;
    public float undersideDepth      = 14f;
    public int   undersideResolution = 80;

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
    public float chessTile      = 4.0f;
    public float arenaRaise     = 0.06f;

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
    private Material matUnderRock;
    // === EAU ===
    private Material matWater;
    private Material matWaterDeep;
    // === VÉGÉTATION ===
    private Material matWood;
    private Material matWoodDark;
    private Material matLeaf;
    private Material matLeafLight;
    private Material matLeafDark;
    private Material matLeafAutumn;
    private Material matLeafYellow;
    private Material matBirchBark;
    private Material matBlossom;
    private Material matBlossomLight;
    // === NÉANT (sol & végétation corrompus) ===
    private Material matVoidWood;
    private Material matVoidLeaf;
    private Material matVoidGlowLeaf;
    private Material matVoidGround;
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

    // Données partagées avec le générateur de props (VoidMapPropsGPU)
    [HideInInspector] public List<Vector3> lastHousePositions = new List<Vector3>();
    [HideInInspector] public List<Vector2> lastRoadBranches   = new List<Vector2>(); // (angle°, longueur)
    // Morsures du néant : (x, z, rayon, profondeur) — la planète part en morceaux
    [HideInInspector] public List<Vector4> voidBites          = new List<Vector4>();

    public static VoidMapGeneratorGPU instance;

    void Awake()
    {
        chessTile  = 1.0f;
        instance   = this;
        navSurface = GetComponent<NavMeshSurface>();
    }

    void Start()
    {
        if (Application.isPlaying && generateOnPlay)
            StartCoroutine(GenerateAsync());
    }

    public void SetSeed(int newSeed)
    {
        seed = newSeed;
        StartCoroutine(GenerateAsync());
    }

    public void SetPhase(int newPhase)
    {
        phase = newPhase;
        StartCoroutine(GenerateAsync());
    }

    System.Collections.IEnumerator GenerateAsync()
    {
        rng = new System.Random(seed);
        lastHousePositions.Clear();
        lastRoadBranches.Clear();
        if (autoClearBeforeGenerate) ClearChildren();
        EnsureRoot();
        BuildMaterials();
        ComputeVoidBites();
        yield return null;

        var islandGroup = BuildIslandHybrid();
        islandGroup.name = "Island";
        yield return null;

        var park = BuildParkAndArena();
        park.name = "ParkAndArena";
        yield return null;

        if (phase >= 1)
        {
            var village = BuildVillage();
            village.name = "Village";
            yield return null;
        }

        int forestN = Mathf.Max(0, forestCount - phase * 30);
        var forest = BuildForest(forestN);
        forest.name = "Forest";
        yield return null;

        if (phase >= 2)
        {
            var lake = BuildLake();
            lake.name = "Lake";
            yield return null;
        }
        if (phase >= 3)
        {
            var castle = BuildCastle();
            castle.name = "Castle";
            yield return null;
        }
        if (phase >= 1)
        {
            var roads = BuildRoads();
            roads.name = "Roads";
            yield return null;
        }

        VoidMapPropsGPU props = GetComponent<VoidMapPropsGPU>();
        if (props == null) props = gameObject.AddComponent<VoidMapPropsGPU>();
        var propsGroup = props.GenerateProps(this, root, rng);
        propsGroup.name = "Props";
        yield return null;

        root.transform.position = Vector3.zero;

        if (navSurface == null) navSurface = GetComponent<NavMeshSurface>();
        if (navSurface != null && rebuildNavMeshAfterGenerate)
        {
            navSurface.collectObjects = CollectObjects.Volume;
            navSurface.center = Vector3.zero;
            navSurface.size   = new Vector3(islandRadius * 2f + 4f,
                                            islandMaxHeight * 2f + 8f,
                                            islandRadius * 2f + 4f);
            navSurface.BuildNavMesh();
        }

        if (EnemySpawnAlgo.instance != null)
            EnemySpawnAlgo.instance.SpawnEnemies(chessTile);

        ApplyVoidAtmosphere();
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
        lastHousePositions.Clear();
        lastRoadBranches.Clear();
        if (autoClearBeforeGenerate) ClearChildren();
        EnsureRoot();
        BuildMaterials();
        ComputeVoidBites();   // avant le mesh : TerrainHeight creuse à ces endroits

        GameObject islandGroup = BuildIslandHybrid();
        islandGroup.name = "Island";

        GameObject park = BuildParkAndArena();
        park.name = "ParkAndArena";

        // Le village se génère AVANT la forêt pour que les arbres
        // puissent éviter les maisons (fini les troncs dans les salons)
        if (phase >= 1)
        {
            Transform village = BuildVillage();
            village.name = "Village";
        }

        int forestN = Mathf.Max(0, forestCount - phase * 30);
        Transform forest = BuildForest(forestN);
        forest.name = "Forest";
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

        // Props décoratifs (buissons, fleurs, sapins, lanternes, ponton…)
        VoidMapPropsGPU props = GetComponent<VoidMapPropsGPU>();
        if (props == null) props = gameObject.AddComponent<VoidMapPropsGPU>();
        Transform propsGroup = props.GenerateProps(this, root, rng);
        propsGroup.name = "Props";

        root.transform.position = Vector3.zero;

        if (navSurface == null) navSurface = GetComponent<NavMeshSurface>();
        if (navSurface != null && rebuildNavMeshAfterGenerate && Application.isPlaying)
        {
            // Limiter le bake au volume de l'île uniquement : sans ça, les
            // nuages (y≈27) et les îles flottantes du vide agrandissent
            // énormément le volume voxelisé et font exploser la RAM du bake.
            navSurface.collectObjects = CollectObjects.Volume;
            navSurface.center = Vector3.zero;
            navSurface.size   = new Vector3(islandRadius * 2f + 4f,
                                            islandMaxHeight * 2f + 8f,
                                            islandRadius * 2f + 4f);
            navSurface.BuildNavMesh();
        }

        if (Application.isPlaying && EnemySpawnAlgo.instance != null)
            EnemySpawnAlgo.instance.SpawnEnemies(chessTile);

        // Ambiance du néant : le ciel et le brouillard se corrompent avec la phase
        ApplyVoidAtmosphere();
    }

    // ──────────────────────────────────────────────────────────────────
    Vector3 GetValidNavMeshPosition(Vector3 wantedPos, float maxDistance)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(wantedPos, out hit, maxDistance, NavMesh.AllAreas))
            return hit.position;
        return wantedPos;
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
        Shader waterSh = Shader.Find("Void/WaterAnimated");
        Shader std     = Shader.Find("Standard");
        Shader chosen  = std;

        matGrass      = NewMat(chosen, new Color(0.27f, 0.52f, 0.20f), 1f, 0.10f);
        matGrassLight = NewMat(chosen, new Color(0.38f, 0.62f, 0.28f), 1f, 0.08f);
        matDirt       = NewMat(chosen, new Color(0.40f, 0.28f, 0.18f), 1f, 0.12f);
        matRock       = NewMat(chosen, new Color(0.55f, 0.53f, 0.50f), 1f, 0.30f);
        matRockDark   = NewMat(chosen, new Color(0.35f, 0.34f, 0.32f), 1f, 0.22f);
        matSand       = NewMat(chosen, new Color(0.84f, 0.76f, 0.55f), 1f, 0.06f);
        matSandWet    = NewMat(chosen, new Color(0.65f, 0.58f, 0.42f), 1f, 0.20f);
        matUnderRock  = NewMat(chosen, new Color(0.24f, 0.20f, 0.19f), 1f, 0.16f);

        Shader wSh   = waterSh != null ? waterSh : chosen;
        matWater     = NewMat(wSh,    new Color(0.14f, 0.46f, 0.68f, 0.60f), 0.60f, 0.92f, true);
        matWaterDeep = NewMat(chosen, new Color(0.08f, 0.22f, 0.45f, 0.72f), 0.72f, 0.95f, true);

        matWood      = NewMat(chosen, new Color(0.38f, 0.24f, 0.14f), 1f, 0.14f);
        matWoodDark  = NewMat(chosen, new Color(0.24f, 0.14f, 0.08f), 1f, 0.10f);
        matLeaf      = NewMat(chosen, new Color(0.18f, 0.46f, 0.15f), 1f, 0.10f);
        matLeafLight = NewMat(chosen, new Color(0.35f, 0.58f, 0.22f), 1f, 0.08f);
        matLeafDark  = NewMat(chosen, new Color(0.10f, 0.30f, 0.10f), 1f, 0.12f);
        matLeafAutumn   = NewMat(chosen, new Color(0.80f, 0.42f, 0.12f), 1f, 0.10f);
        matLeafYellow   = NewMat(chosen, new Color(0.86f, 0.68f, 0.18f), 1f, 0.08f);
        matBirchBark    = NewMat(chosen, new Color(0.88f, 0.86f, 0.80f), 1f, 0.10f);
        matBlossom      = NewMat(chosen, new Color(0.93f, 0.62f, 0.74f), 1f, 0.12f);
        matBlossomLight = NewMat(chosen, new Color(0.98f, 0.80f, 0.86f), 1f, 0.12f);

        matRoofDark  = NewMat(chosen, new Color(0.18f, 0.06f, 0.04f), 1f, 0.12f);
        matRoofRed   = NewMat(chosen, new Color(0.55f, 0.18f, 0.10f), 1f, 0.15f);
        matRoofBrown = NewMat(chosen, new Color(0.32f, 0.18f, 0.10f), 1f, 0.14f);

        matStone      = NewMat(chosen, new Color(0.68f, 0.65f, 0.60f), 1f, 0.22f);
        matStoneDark  = NewMat(chosen, new Color(0.32f, 0.30f, 0.28f), 1f, 0.18f);
        matPlaster    = NewMat(chosen, new Color(0.88f, 0.84f, 0.76f), 1f, 0.12f);
        matPlasterWarm= NewMat(chosen, new Color(0.82f, 0.72f, 0.58f), 1f, 0.10f);
        matPlasterGrey= NewMat(chosen, new Color(0.70f, 0.70f, 0.68f), 1f, 0.12f);

        matBoardLight = NewMat(chosen, new Color(0.94f, 0.94f, 0.92f), 1f, 0.25f);
        matBoardDark  = NewMat(chosen, new Color(0.06f, 0.06f, 0.06f), 1f, 0.25f);
        matPath       = NewMat(chosen, new Color(0.60f, 0.52f, 0.40f), 1f, 0.06f);
        matPathDark   = NewMat(chosen, new Color(0.38f, 0.30f, 0.22f), 1f, 0.05f);
        matWindow     = NewMat(chosen, new Color(0.55f, 0.75f, 0.90f, 0.55f), 0.55f, 0.95f, true);
        matDoor       = NewMat(chosen, new Color(0.28f, 0.16f, 0.08f), 1f, 0.18f);

        // --- Néant : palette officielle Entropy Loop (violet profond + accents
        // or/crème). Le sol et le feuillage reçoivent une texture de ciel étoilé
        // (voir GetVoidStarfield) : le vide spatial violet transparaît, avec des
        // étoiles dorées — voulu, et plus un aplat qui passait pour un glitch.
        matVoidWood     = NewMat(chosen, new Color(0.12f, 0.07f, 0.17f), 1f, 0.10f);  // #1f132b
        matVoidLeaf     = NewMat(chosen, new Color(0.16f, 0.10f, 0.27f), 1f, 0.05f);
        matVoidGround   = NewMat(chosen, new Color(0.12f, 0.07f, 0.17f), 1f, 0.05f);  // #1f132b
        // Lueur des cristaux : lavande signature #8e5dbc, émission douce pour ne
        // pas « cramer » en blanc-rose (réservée aux petits accents)
        matVoidGlowLeaf = NewMat(chosen, new Color(0.50f, 0.32f, 0.68f), 1f, 0.25f);
        matVoidGlowLeaf.EnableKeyword("_EMISSION");
        if (matVoidGlowLeaf.HasProperty("_EmissionColor"))
            matVoidGlowLeaf.SetColor("_EmissionColor", new Color(0.56f, 0.36f, 0.74f) * 0.9f);

        ApplyStarfield(matVoidGround, 1.6f);   // sol = espace étoilé violet
        ApplyStarfield(matVoidLeaf,   1.0f);   // canopée corrompue = ciel de nuit

        // Corruption globale : tout le décor sombre vers le violet profond du
        // néant (#1f132b) à mesure que la phase avance (fini le pastel/bleu)
        float corr = phase < 2 ? 0f : Mathf.Pow((phase - 1) / 4f, 1.7f);
        if (corr > 0f)
        {
            Color voidC = new Color(0.12f, 0.07f, 0.17f);
            TintToVoid(matGrass,        voidC, corr * 0.80f);
            TintToVoid(matGrassLight,   voidC, corr * 0.78f);
            TintToVoid(matDirt,         voidC, corr * 0.65f);
            TintToVoid(matSand,         voidC, corr * 0.55f);
            TintToVoid(matSandWet,      voidC, corr * 0.55f);
            TintToVoid(matLeaf,         voidC, corr * 0.72f);
            TintToVoid(matLeafLight,    voidC, corr * 0.70f);
            TintToVoid(matLeafDark,     voidC, corr * 0.78f);
            TintToVoid(matLeafAutumn,   voidC, corr * 0.72f);
            TintToVoid(matLeafYellow,   voidC, corr * 0.72f);
            TintToVoid(matBlossom,      voidC, corr * 0.85f);
            TintToVoid(matBlossomLight, voidC, corr * 0.85f);
        }

        if (islandGPUShader == null)
            islandGPUShader = Shader.Find("Void/IslandGPU");
    }

    // ── Texture procédurale "espace étoilé" partagée par les surfaces du néant.
    // Fond bleu-nuit très sombre + nébuleuse diffuse (Perlin basse fréquence) +
    // petites étoiles ponctuelles (surtout faibles, quelques brillantes). Les
    // étoiles servent aussi de carte d'émission pour qu'elles scintillent.
    private Texture2D _voidStarTex;
    Texture2D GetVoidStarfield()
    {
        if (_voidStarTex != null) return _voidStarTex;
        int size = 40;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        System.Random s = new System.Random(seed * 13 + 2);
        Color[] px = new Color[size * size];

        // Fond violet profond (#1f132b) + nébuleuse lavande/bleue (#3b2665,
        // #8e5dbc, touche de #3735d7) — palette officielle Entropy Loop
        float ox = (float)s.NextDouble() * 100f, oy = (float)s.NextDouble() * 100f;
        Color deep = new Color(0.07f, 0.04f, 0.11f);                     // ~#1f132b assombri
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float n  = Mathf.PerlinNoise(ox + x * 0.012f, oy + y * 0.012f);
            float n2 = Mathf.PerlinNoise(ox + x * 0.05f + 50f, oy + y * 0.05f + 50f);
            Color neb = Color.Lerp(new Color(0.23f, 0.15f, 0.40f),       // #3b2665
                                   new Color(0.30f, 0.20f, 0.55f), n2);  // lavande-bleu
            px[y * size + x] = Color.Lerp(deep, neb, n * n);
        }

        int stars = size * size / 90;
        for (int i = 0; i < stars; i++)
        {
            int x = s.Next(size), y = s.Next(size);
            float b  = Mathf.Pow((float)s.NextDouble(), 2f);     // beaucoup de faibles, peu de brillantes
            float br = Mathf.Lerp(0.25f, 1f, b);
            // Étoiles surtout dorées/crème (accent or de la DA), quelques lavande
            // et de rares bleues
            Color tint = new Color(0.96f, 0.89f, 0.76f);                 // crème #f4e2c2
            double r = s.NextDouble();
            if      (r < 0.35) tint = new Color(0.85f, 0.71f, 0.49f);    // or #dab47c
            else if (r < 0.55) tint = new Color(0.56f, 0.36f, 0.74f);    // lavande #8e5dbc
            else if (r < 0.62) tint = new Color(0.45f, 0.43f, 0.95f);    // bleu #3735d7
            Color star = tint * br;
            StarSetMax(px, size, x, y, star);
            if (b > 0.85f)   // halo des étoiles brillantes
            {
                Color h = star * 0.5f;
                StarSetMax(px, size, x + 1, y, h); StarSetMax(px, size, x - 1, y, h);
                StarSetMax(px, size, x, y + 1, h); StarSetMax(px, size, x, y - 1, h);
            }
        }

        tex.SetPixels(px);
        tex.Apply(true);
        _voidStarTex = tex;
        return tex;
    }

    void StarSetMax(Color[] px, int size, int x, int y, Color c)
    {
        x = ((x % size) + size) % size;
        y = ((y % size) + size) % size;
        int idx = y * size + x;
        Color o = px[idx];
        px[idx] = new Color(Mathf.Max(o.r, c.r), Mathf.Max(o.g, c.g), Mathf.Max(o.b, c.b), 1f);
    }

    // Applique la texture étoilée en albédo + émission (les étoiles brillent)
    void ApplyStarfield(Material m, float tiling)
    {
        Texture2D t = GetVoidStarfield();
        Vector2 sc = new Vector2(tiling, tiling);
        if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", t); m.SetTextureScale("_MainTex", sc); }
        if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", t); m.SetTextureScale("_BaseMap", sc); }
        if (m.HasProperty("_Color"))     m.SetColor("_Color", Color.white);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        if (m.HasProperty("_EmissionMap"))   { m.SetTexture("_EmissionMap", t); m.SetTextureScale("_EmissionMap", sc); }
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.white * 1.4f);
    }

    void TintToVoid(Material m, Color target, float t)
    {
        Color c = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
        c = Color.Lerp(c, target, Mathf.Clamp01(t));
        if (m.HasProperty("_Color"))     m.SetColor("_Color", c);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
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
    public float TerrainHeight(float x, float z)
    {
        float r  = new Vector2(x, z).magnitude;
        float sf = (float)seed;

        float flatZone = parkRadius + 2.5f;
        if (r <= flatZone) return 0f;

        float edgeStart = islandRadius - islandEdgeFalloff;
        float edgeMask  = 1f - Mathf.SmoothStep(edgeStart, islandRadius, r);
        float t = Mathf.Clamp01((r - flatZone) / Mathf.Max(0.1f, edgeStart - flatZone));
        float rise = t * t * islandMaxHeight;

        float n1 = Mathf.PerlinNoise((x + sf * 0.11f) * largeNoiseScale,       (z - sf * 0.17f) * largeNoiseScale);
        float n2 = Mathf.PerlinNoise((x - sf * 0.07f) * mediumNoiseScale,      (z + sf * 0.13f) * mediumNoiseScale);
        float n3 = Mathf.PerlinNoise((x + sf * 0.23f) * smallNoiseScale,       (z - sf * 0.31f) * smallNoiseScale);
        float n4 = Mathf.PerlinNoise((x - sf * 0.19f) * smallNoiseScale * 2.2f,(z + sf * 0.27f) * smallNoiseScale * 2.2f);

        float fbm         = n1 * 0.50f + n2 * 0.28f + n3 * 0.14f + n4 * 0.08f;
        float noiseSigned = (fbm - 0.5f) * 2f;
        float noiseAmp    = t * islandMaxHeight * 0.45f;

        float h = (rise + noiseSigned * noiseAmp) * edgeMask;
        h = Mathf.Max(0f, h);

        // Morsures du néant (phase >= 2) : le terrain est creusé en
        // gouffres qui plongent sous le niveau 0 — la planète se brise
        for (int i = 0; i < voidBites.Count; i++)
        {
            float dx = x - voidBites[i].x;
            float dz = z - voidBites[i].y;
            float d2 = dx * dx + dz * dz;
            float br = voidBites[i].z;
            if (d2 >= br * br) continue;
            float tt = 1f - Mathf.Sqrt(d2) / br;
            h -= tt * tt * voidBites[i].w;
        }
        return h;
    }

    Material ChooseTerrainMaterial(float x, float z, float h)
    {
        // Le néant ronge le sol : au-delà d'un seuil de corruption la terre,
        // l'herbe et le sable virent à la matière du néant (sombre violacée)
        float corr = CorruptionAt(new Vector3(x, 0f, z));
        if (corr > 0.65f) return matVoidGround;

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
        Mesh colliderMesh = GenerateIslandMesh(islandResolution);
        mf.sharedMesh     = colliderMesh;
        mc.sharedMesh     = colliderMesh;
        mr.sharedMaterial = matGrass;
        mr.enabled        = !useGPUIslandVisual;

        // Ventre rocheux : transforme la « crêpe » plate en morceau de planète
        if (buildUnderside)
            BuildIslandUnderside(group.transform);

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
            matGPU.SetColor("_BaseColor",         new Color(0.31f, 0.55f, 0.28f));
            matGPU.SetFloat("_IslandRadius",      islandRadius);
            matGPU.SetFloat("_IslandEdgeFalloff", islandEdgeFalloff);
            matGPU.SetFloat("_IslandMaxHeight",   islandMaxHeight);
            matGPU.SetFloat("_IslandSeed",        seed);
            mrVis.sharedMaterial = matGPU;
        }
        return group;
    }

    void BuildIslandColorOverlay(Transform parent)
    {
        for (int i = 0; i < 250; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Sqrt((float)rng.NextDouble()) * (islandRadius - 3f);
            float x   = Mathf.Cos(ang) * rad;
            float z   = Mathf.Sin(ang) * rad;
            if (new Vector2(x, z).magnitude < parkRadius + 1f) continue;

            float gy     = TerrainHeight(x, z);
            Material mat = ChooseTerrainMaterial(x, z, gy);

            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = "TerrainTint";
            patch.transform.SetParent(parent);
            patch.layer = gameObject.layer;
            float sx = Mathf.Lerp(1.0f, 5.0f, (float)rng.NextDouble());
            float sz = Mathf.Lerp(1.0f, 5.0f, (float)rng.NextDouble());
            patch.transform.position   = new Vector3(x, gy + 0.005f, z);
            patch.transform.rotation   = Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f);
            patch.transform.localScale = new Vector3(sx, 0.01f, sz);
            patch.GetComponent<MeshRenderer>().sharedMaterial = mat;
            SafeRemoveCollider(patch.GetComponent<BoxCollider>());
        }

        AddScatterRocks(parent);
        AddVoidGroundPatches(parent);
    }

    // Le néant gagne le sol : plaques de matière du néant et veines
    // luminescentes se répandent dans les zones corrompues. La densité et la
    // taille suivent CorruptionAt — autour des morsures et des bords c'est
    // quasiment intégral, l'arène centrale reste épargnée le plus longtemps.
    void AddVoidGroundPatches(Transform parent)
    {
        if (phase < 2) return;
        int attempts = 102 + phase * 13;
        for (int i = 0; i < attempts; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Sqrt((float)rng.NextDouble()) * (islandRadius - 2f);
            float x   = Mathf.Cos(ang) * rad;
            float z   = Mathf.Sin(ang) * rad;
            if (new Vector2(x, z).magnitude < parkRadius + 1f) continue;

            float corr = CorruptionAt(new Vector3(x, 0f, z));
            // Probabilité de pose ∝ corruption (les zones saines restent vertes)
            if (corr < 0.12f || (float)rng.NextDouble() > corr) continue;

            float gy = TerrainHeight(x, z);
            float sx = Mathf.Lerp(1.0f, 5.0f, (float)rng.NextDouble()) * Mathf.Lerp(0.7f, 1.3f, corr);
            float sz = Mathf.Lerp(1.0f, 5.0f, (float)rng.NextDouble()) * Mathf.Lerp(0.7f, 1.3f, corr);

            // Plaque de sol corrompu, sombre et mate : se fond comme une terre
            // calcinée (légèrement épaisse + surélevée pour éviter le z-fighting
            // qui donnait l'impression d'un bug de texture)
            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = "VoidGround";
            patch.transform.SetParent(parent);
            patch.layer = gameObject.layer;
            patch.transform.position   = new Vector3(x, gy + 0.02f, z);
            patch.transform.rotation   = Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f);
            patch.transform.localScale = new Vector3(sx, 0.04f, sz);
            patch.GetComponent<MeshRenderer>().sharedMaterial = matVoidGround;
            SafeRemoveCollider(patch.GetComponent<BoxCollider>());

            // Au cœur de la corruption, de petits éclats de cristal du néant
            // percent le sol : un VOLUME 3D luminescent lit beaucoup mieux qu'une
            // décalque plate (qui ressemblait à un glitch)
            if (corr > 0.6f && (float)rng.NextDouble() < 0.22f)
            {
                float cs = Mathf.Lerp(0.12f, 0.32f, (float)rng.NextDouble());
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "VoidShard";
                shard.transform.SetParent(parent);
                shard.layer = gameObject.layer;
                shard.transform.position   = new Vector3(x, gy + cs * 0.8f, z);
                shard.transform.rotation   = Quaternion.Euler((float)rng.NextDouble() * 40f - 20f,
                                                              (float)rng.NextDouble() * 360f,
                                                              (float)rng.NextDouble() * 40f - 20f);
                shard.transform.localScale = new Vector3(cs, cs * Mathf.Lerp(2f, 3.5f, (float)rng.NextDouble()), cs);
                shard.GetComponent<MeshRenderer>().sharedMaterial = matVoidGlowLeaf;
                SafeRemoveCollider(shard.GetComponent<BoxCollider>());
            }
        }
    }

    void AddScatterRocks(Transform parent)
    {
        int rockCount = 80;
        for (int i = 0; i < rockCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Sqrt((float)rng.NextDouble()) * (islandRadius - 1f);
            float x   = Mathf.Cos(ang) * rad;
            float z   = Mathf.Sin(ang) * rad;
            float h   = TerrainHeight(x, z);
            if (rad < parkRadius + 4f) continue;
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
        => GenerateDiscMesh(Mathf.Clamp(resolution / 2, 24, 160),
                            Mathf.Clamp(resolution, 48, 320), false);

    // Mesh en disque (anneaux × segments) : épouse exactement le contour
    // circulaire de l'île — fini la « crêpe » carrée plate autour de la côte.
    // underside = true génère le ventre rocheux (profondeurs inversées)
    Mesh GenerateDiscMesh(int rings, int segments, bool underside)
    {
        int vCount = 1 + rings * segments;
        Vector3[] verts = new Vector3[vCount];
        Vector2[] uvs   = new Vector2[vCount];
        verts[0] = new Vector3(0f, underside ? -UndersideDepth(0f, 0f) : TerrainHeight(0f, 0f), 0f);
        uvs[0]   = new Vector2(0.5f, 0.5f);
        for (int ri = 1; ri <= rings; ri++)
        for (int s = 0; s < segments; s++)
        {
            float rad = islandRadius * ri / rings;
            float a   = (float)s / segments * Mathf.PI * 2f;
            float x   = Mathf.Cos(a) * rad;
            float z   = Mathf.Sin(a) * rad;
            int   idx = 1 + (ri - 1) * segments + s;
            verts[idx] = new Vector3(x, underside ? -UndersideDepth(x, z) : TerrainHeight(x, z), z);
            uvs[idx]   = new Vector2(0.5f + x / (islandRadius * 2f), 0.5f + z / (islandRadius * 2f));
        }

        int[] tris = new int[segments * 3 + (rings - 1) * segments * 6];
        int t = 0;
        // Éventail central
        for (int s = 0; s < segments; s++)
        {
            int a2 = 1 + s;
            int b2 = 1 + (s + 1) % segments;
            if (underside) { tris[t++] = 0; tris[t++] = a2; tris[t++] = b2; }
            else           { tris[t++] = 0; tris[t++] = b2; tris[t++] = a2; }
        }
        // Quads entre anneaux
        for (int ri = 1; ri < rings; ri++)
        for (int s = 0; s < segments; s++)
        {
            int i0 = 1 + (ri - 1) * segments + s;
            int i1 = 1 + (ri - 1) * segments + (s + 1) % segments;
            int i2 = 1 + ri * segments + s;
            int i3 = 1 + ri * segments + (s + 1) % segments;
            if (underside)
            {
                tris[t++] = i0; tris[t++] = i2; tris[t++] = i3;
                tris[t++] = i0; tris[t++] = i3; tris[t++] = i1;
            }
            else
            {
                tris[t++] = i0; tris[t++] = i3; tris[t++] = i2;
                tris[t++] = i0; tris[t++] = i1; tris[t++] = i3;
            }
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
    // Profondeur du ventre rocheux sous l'île : 0 au bord (rejoint la côte),
    // maximum au centre, avec du bruit pour un relief rocheux naturel
    public float UndersideDepth(float x, float z)
    {
        float rr = new Vector2(x, z).magnitude / Mathf.Max(1f, islandRadius);
        if (rr >= 1f) return 0f;
        float t  = 1f - rr * rr;
        float sf = (float)seed;
        float n1 = Mathf.PerlinNoise((x + sf * 0.31f) * 0.045f, (z - sf * 0.21f) * 0.045f);
        float n2 = Mathf.PerlinNoise((x - sf * 0.13f) * 0.11f,  (z + sf * 0.37f) * 0.11f);
        float noise = n1 * 0.7f + n2 * 0.3f;
        return undersideDepth * Mathf.Pow(t, 0.75f) * (0.55f + 0.45f * noise);
    }

    void BuildIslandUnderside(Transform parent)
    {
        GameObject under = new GameObject("Island_Underside");
        under.transform.SetParent(parent);
        under.transform.localPosition = Vector3.zero;
        under.layer = gameObject.layer;
        MeshFilter   mf = under.AddComponent<MeshFilter>();
        MeshRenderer mr = under.AddComponent<MeshRenderer>();
        mf.sharedMesh     = GenerateDiscMesh(Mathf.Clamp(undersideResolution, 16, 128),
                                             Mathf.Clamp(undersideResolution * 2, 32, 256), true);
        mr.sharedMaterial = matUnderRock;
        // Le ventre ne participe jamais au NavMesh
        NavMeshModifier mod = under.AddComponent<NavMeshModifier>();
        mod.ignoreFromBuild = true;

        AddUndersideSpikes(under.transform);
    }

    // Stalactites rocheuses qui pendent sous l'île + grande racine centrale
    // (la silhouette iconique d'île volante à la Outer Wilds)
    void AddUndersideSpikes(Transform parent)
    {
        System.Random srng = new System.Random(seed * 397 + 7);
        int spikes = 14;
        for (int i = 0; i < spikes; i++)
        {
            float ang = (float)srng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Sqrt((float)srng.NextDouble()) * islandRadius * 0.75f;
            float x = Mathf.Cos(ang) * rad;
            float z = Mathf.Sin(ang) * rad;
            float baseY = -UndersideDepth(x, z);
            float len = Mathf.Lerp(1.8f, 5.5f, (float)srng.NextDouble())
                      * Mathf.Lerp(1f, 0.45f, rad / islandRadius);
            float w    = len * Mathf.Lerp(0.20f, 0.32f, (float)srng.NextDouble());
            float tilt = ((float)srng.NextDouble() - 0.5f) * 14f;
            Quaternion rot = Quaternion.Euler(tilt, (float)srng.NextDouble() * 360f, tilt);

            // Stalactite en deux segments (corps + pointe plus fine)
            AddTreePart(parent, PrimitiveType.Cube, "UnderSpike",
                new Vector3(x, baseY + len * 0.10f - len * 0.50f, z),
                new Vector3(w, len, w), rot, matUnderRock);
            AddTreePart(parent, PrimitiveType.Cube, "UnderSpikeTip",
                new Vector3(x, baseY + len * 0.10f - len * 0.95f, z),
                new Vector3(w * 0.45f, len * 0.6f, w * 0.45f), rot, matRockDark);
        }

        // Racine centrale : trois blocs empilés de plus en plus fins
        float d = UndersideDepth(0f, 0f);
        AddTreePart(parent, PrimitiveType.Cube, "UnderCore_A",
            new Vector3(0f, -d * 0.85f, 0f), new Vector3(6.0f, d * 0.9f, 6.0f),
            Quaternion.Euler(0f, 20f, 0f), matUnderRock);
        AddTreePart(parent, PrimitiveType.Cube, "UnderCore_B",
            new Vector3(0f, -d * 1.35f, 0f), new Vector3(3.4f, d * 0.5f, 3.4f),
            Quaternion.Euler(0f, 50f, 0f), matUnderRock);
        AddTreePart(parent, PrimitiveType.Cube, "UnderCore_C",
            new Vector3(0f, -d * 1.70f, 0f), new Vector3(1.8f, d * 0.4f, 1.8f),
            Quaternion.Euler(0f, 80f, 0f), matRockDark);
    }

    // ──────────────────────────────────────────────────────────────────
    GameObject BuildParkAndArena()
    {
        GameObject group = new GameObject("ParkAndArena");
        group.transform.SetParent(root);
        group.layer = gameObject.layer;

        float cy = TerrainHeight(0f, 0f);

        int   tiles     = 8;
        float boardSize = tiles * chessTile;
        float start     = -boardSize * 0.5f + chessTile * 0.5f;

        GameObject boardPad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boardPad.name = "BoardPad";
        boardPad.transform.SetParent(group.transform);
        boardPad.layer = gameObject.layer;
        boardPad.transform.localScale    = new Vector3(boardSize + 0.6f, 0.08f, boardSize + 0.6f);
        boardPad.transform.localPosition = new Vector3(0f, cy + arenaRaise + 0.02f, 0f);
        boardPad.GetComponent<MeshRenderer>().sharedMaterial = matDirt;
        SafeRemoveCollider(boardPad.GetComponent<BoxCollider>());

        Material[] stoneVariants = new Material[]
        {
            NewMat(Shader.Find("Standard"), new Color(0.62f, 0.58f, 0.50f), 1f, 0.18f),
            NewMat(Shader.Find("Standard"), new Color(0.52f, 0.50f, 0.46f), 1f, 0.14f),
            NewMat(Shader.Find("Standard"), new Color(0.70f, 0.64f, 0.54f), 1f, 0.20f),
        };

        for (int yy = 0; yy < tiles; yy++)
        for (int xx = 0; xx < tiles; xx++)
        {
            int   variant   = (xx * 3 + yy * 7 + seed) % 3;
            float jitterX   = ((xx * 17 + yy * 11 + seed) % 100 / 100f - 0.5f) * chessTile * 0.08f;
            float jitterZ   = ((xx * 13 + yy * 19 + seed) % 100 / 100f - 0.5f) * chessTile * 0.08f;
            float sizeJit   = 1f - ((xx * 7  + yy * 5  + seed) % 100 / 100f) * 0.12f;
            float rotJit    = ((xx * 11 + yy * 13 + seed) % 100 / 100f - 0.5f) * 8f;
            float heightJit = ((xx * 5  + yy * 17 + seed) % 100 / 100f) * 0.015f;

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
        }

        AddPathJoints(group.transform, cy + arenaRaise, boardSize);
        AddArenaOrganic(group.transform, cy + arenaRaise, boardSize);

        return group;
    }

    void AddPathJoints(Transform parent, float y, float boardSize)
    {
        float half = boardSize * 0.5f + 0.1f;
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
        float half   = boardWorldSize * 0.5f + 0.15f;
        float thick  = 0.18f;
        float frameY = y + 0.09f;

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
        float innerR = boardWorldSize * 0.5f + 0.5f;

        int bushCount = 18;
        for (int i = 0; i < bushCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(innerR, innerR + 2.0f, (float)rng.NextDouble());
            float x   = Mathf.Cos(ang) * rad;
            float z   = Mathf.Sin(ang) * rad;
            float gy  = TerrainHeight(x, z);
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

        int rockCount = 12;
        for (int i = 0; i < rockCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(innerR, innerR + 1.5f, (float)rng.NextDouble());
            float x   = Mathf.Cos(ang) * rad;
            float z   = Mathf.Sin(ang) * rad;
            float gy  = TerrainHeight(x, z);
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

        int patchCount = 30;
        for (int i = 0; i < patchCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(innerR, innerR + 2.5f, (float)rng.NextDouble());
            float x   = Mathf.Cos(ang) * rad;
            float z   = Mathf.Sin(ang) * rad;
            float gy  = TerrainHeight(x, z);
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
            if (pos.magnitude < parkRadius + 2f)  continue;
            if (IsInsideVillageRadius(pos))        continue;
            if (IsInsideLake(pos))                 continue;
            if (pos.magnitude > islandRadius - 3f) continue;
            if (IsNearHouse(pos, 3.0f))            continue;
            if (IsInVoidBite(pos))                 continue;
            PlaceTree(g, pos);
            placed++;
        }

        float inner = parkRadius + 2f;
        float outer = phase >= 1
            ? Mathf.Min(GetVillageRadius() - 1.5f, parkRadius + 14f)
            : parkRadius + 14f;
        if (outer > inner + 0.5f)
        {
            int ringTrees = Mathf.Min(Mathf.RoundToInt((outer - inner) * 24f), Mathf.Max(0, count - placed));
            for (int i = 0; i < ringTrees; i++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float rad = Mathf.Lerp(inner, outer, (float)rng.NextDouble());
                Vector3 pos = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
                if (IsInsideLake(pos))      continue;
                if (IsNearHouse(pos, 3.0f)) continue;
                PlaceTree(g, pos);
            }
        }
        return g;
    }

    bool IsInsideVillageRadius(Vector3 pos)
        => (phase >= 1) && pos.magnitude <= GetVillageRadius() + 1.5f;

    public bool IsInsideLake(Vector3 pos)
    {
        if (phase < 2) return false;
        Vector2 p = new Vector2(pos.x, pos.z) - lakeOffset;
        return p.magnitude <= lakeRadius + 1f;
    }

    bool IsNearHouse(Vector3 pos, float dist)
    {
        for (int i = 0; i < lastHousePositions.Count; i++)
        {
            float dx = pos.x - lastHousePositions[i].x;
            float dz = pos.z - lastHousePositions[i].z;
            if (dx * dx + dz * dz < dist * dist) return true;
        }
        return false;
    }

    // Calcule les morsures du néant : déterministe (seed + phase), et
    // calculé AVANT le mesh de l'île puisque TerrainHeight s'en sert
    void ComputeVoidBites()
    {
        voidBites.Clear();
        if (phase < 2) return;
        // Morsures volontairement modérées : le trou noir aspire l'atmosphère et
        // des morceaux, mais l'île doit rester largement intacte (pas de planète
        // en miettes). Cratères peu nombreux et peu profonds.
        int   count = phase == 2 ? 1 : phase == 3 ? 2 : phase == 4 ? 3 : 4;
        float rMin  = phase == 2 ? 2.5f : 3.5f;
        float rMax  = phase == 2 ? 4f : phase == 3 ? 5f : 6f;
        float depth = phase == 2 ? 2f : phase == 3 ? 3.5f : phase == 4 ? 5f : 6.5f;
        System.Random brng = new System.Random(seed * 7919 + phase * 131);
        for (int i = 0; i < count; i++)
        {
            float ang = (float)brng.NextDouble() * Mathf.PI * 2f;
            float rad = islandRadius - Mathf.Lerp(2f, islandEdgeFalloff + 6f, (float)brng.NextDouble());
            voidBites.Add(new Vector4(
                Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad,
                Mathf.Lerp(rMin, rMax, (float)brng.NextDouble()),
                depth * Mathf.Lerp(0.7f, 1.3f, (float)brng.NextDouble())));
        }
    }

    public bool IsInVoidBite(Vector3 pos)
    {
        for (int i = 0; i < voidBites.Count; i++)
        {
            float dx = pos.x - voidBites[i].x;
            float dz = pos.z - voidBites[i].y;
            float r  = voidBites[i].z + 1.5f;
            if (dx * dx + dz * dz < r * r) return true;
        }
        return false;
    }

    // Intensité de la corruption du néant en un point (0 = sain, 1 = consumé).
    // Monte avec la phase, plus forte près des morsures et du bord de l'île,
    // l'arène centrale reste relativement épargnée le plus longtemps possible
    public float CorruptionAt(Vector3 pos)
    {
        if (phase < 2) return 0f;
        // Montée volontairement lente : faible en phase 2, modérée en phase 3,
        // forte en phase 4, chaos total seulement en phase 5
        // (0.25→0.09, 0.50→0.31, 0.75→0.61, 1.0→1.0)
        float baseC = Mathf.Pow((phase - 1) / 4f, 1.7f);
        float r = new Vector2(pos.x, pos.z).magnitude;
        float edge = Mathf.InverseLerp(parkRadius + 2f, islandRadius - 2f, r);
        float c = baseC * Mathf.Lerp(0.35f, 1.0f, edge);
        // L'essentiel de la corruption se concentre autour des morsures du vide
        for (int i = 0; i < voidBites.Count; i++)
        {
            float dx = pos.x - voidBites[i].x;
            float dz = pos.z - voidBites[i].y;
            float d  = Mathf.Sqrt(dx * dx + dz * dz) - voidBites[i].z;
            if (d < 7f) c += (1f - Mathf.Max(0f, d) / 7f) * 0.55f;
        }
        return Mathf.Clamp01(c);
    }

    // Brouillard et lumière virent au violet sombre à mesure que la
    // planète se désagrège (phase 2 → 5)
    void ApplyVoidAtmosphere()
    {
        if (phase < 2) return;
        float t = Mathf.InverseLerp(2f, 5f, phase);
        RenderSettings.fog        = true;
        RenderSettings.fogMode    = FogMode.ExponentialSquared;
        RenderSettings.fogColor   = Color.Lerp(new Color(0.22f, 0.16f, 0.30f), new Color(0.05f, 0.03f, 0.09f), t);
        RenderSettings.fogDensity = Mathf.Lerp(0.012f, 0.030f, t);
        RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = Color.Lerp(new Color(0.20f, 0.16f, 0.30f), new Color(0.10f, 0.06f, 0.18f), t);
        RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.14f, 0.11f, 0.20f), new Color(0.07f, 0.04f, 0.12f), t);
        RenderSettings.ambientGroundColor  = Color.Lerp(new Color(0.05f, 0.04f, 0.07f), new Color(0.03f, 0.02f, 0.05f), t);

        // Le ciel clair tue l'angoisse : on le remplace par un ciel du néant
        // (violet quasi noir, sans soleil) de plus en plus sombre avec la phase
        Shader skyProc = Shader.Find("Skybox/Procedural");
        if (skyProc != null)
        {
            Material sky = new Material(skyProc);
            sky.SetFloat("_SunSize", 0f);
            sky.SetFloat("_SunSizeConvergence", 1f);
            sky.SetFloat("_AtmosphereThickness", 0.35f);
            sky.SetColor("_SkyTint",     Color.Lerp(new Color(0.12f, 0.09f, 0.20f), new Color(0.05f, 0.03f, 0.10f), t));
            sky.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.04f));
            sky.SetFloat("_Exposure",    Mathf.Lerp(0.45f, 0.15f, t));
            RenderSettings.skybox = sky;
        }
        DynamicGI.UpdateEnvironment();
    }

    // Place un arbre : essence tirée au sort pour une forêt variée et vivante
    void PlaceTree(Transform parent, Vector3 pos)
    {
        float groundY = TerrainHeight(pos.x, pos.z);
        float h       = Mathf.Lerp(treeHeightRange.x, treeHeightRange.y, (float)rng.NextDouble());

        GameObject tree = new GameObject("Tree");
        tree.transform.SetParent(parent);
        tree.layer = gameObject.layer;
        tree.transform.position = new Vector3(pos.x, groundY, pos.z);
        tree.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        tree.AddComponent<TreeOccluder>();

        // La corruption consume la faune : plus on est près d'une morsure (ou
        // tard dans les phases), plus l'arbre a de chances d'être remplacé par
        // un arbre du néant tordu et luminescent
        float corruption = CorruptionAt(pos);
        if (corruption > 0.05f && (float)rng.NextDouble() < corruption)
        {
            tree.name = "VoidTree";
            BuildVoidTree(tree.transform, h, corruption);
            return;
        }

        // 35% chêne, 25% feuillu étagé, 15% bouleau, 15% automne, 10% cerisier
        float roll = (float)rng.NextDouble();
        if      (roll < 0.35f) BuildOakTree(tree.transform, h, matLeaf, matLeafLight);
        else if (roll < 0.60f) BuildLayeredTree(tree.transform, h);
        else if (roll < 0.75f) BuildBirchTree(tree.transform, h);
        else if (roll < 0.90f) BuildOakTree(tree.transform, h, matLeafAutumn, matLeafYellow);
        else                   BuildBlossomTree(tree.transform, h);
    }

    GameObject AddTreePart(Transform parent, PrimitiveType type, string n,
                           Vector3 lPos, Vector3 lScale, Quaternion lRot, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name  = n;
        go.transform.SetParent(parent);
        go.layer = parent.gameObject.layer;
        go.transform.localPosition = lPos;
        go.transform.localRotation = lRot;
        go.transform.localScale    = lScale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        SafeRemoveCollider(go.GetComponent<Collider>());
        return go;
    }

    // CHÊNE : tronc légèrement penché, racines apparentes, branches portant des
    // bouquets, canopée en grappe de blobs irréguliers (la version automne
    // réutilise la même silhouette avec des feuillages orange/jaune)
    void BuildOakTree(Transform tree, float h, Material leafMain, Material leafAccent)
    {
        float trunkH = h * 0.42f;
        float trunkR = h * 0.045f;
        float lean   = ((float)rng.NextDouble() - 0.5f) * 9f;

        AddTreePart(tree, PrimitiveType.Cylinder, "Trunk",
            new Vector3(0f, trunkH * 0.5f, 0f),
            new Vector3(trunkR * 2f, trunkH * 0.5f, trunkR * 2f),
            Quaternion.Euler(lean, 0f, 0f), matWood);

        // Racines apparentes au pied
        int roots = 3 + (int)(rng.NextDouble() * 2f);
        for (int i = 0; i < roots; i++)
        {
            float ang   = i * (Mathf.PI * 2f / roots) + (float)rng.NextDouble() * 0.8f;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            AddTreePart(tree, PrimitiveType.Cylinder, "Root",
                dir * trunkR * 1.6f + Vector3.up * (trunkH * 0.08f),
                new Vector3(trunkR, trunkH * 0.15f, trunkR),
                Quaternion.AngleAxis(30f, Vector3.Cross(Vector3.up, dir)), matWood);
        }

        // Branches avec petit bouquet de feuilles au bout
        int branches = 2 + (int)(rng.NextDouble() * 2f);
        for (int i = 0; i < branches; i++)
        {
            float ang   = (float)rng.NextDouble() * Mathf.PI * 2f;
            float baseY = trunkH * Mathf.Lerp(0.55f, 0.85f, (float)rng.NextDouble());
            float bLen  = h * Mathf.Lerp(0.16f, 0.24f, (float)rng.NextDouble());
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            AddTreePart(tree, PrimitiveType.Cylinder, "Branch",
                dir * bLen * 0.45f + Vector3.up * (baseY + bLen * 0.30f),
                new Vector3(trunkR * 0.9f, bLen * 0.5f, trunkR * 0.9f),
                Quaternion.AngleAxis(Mathf.Lerp(40f, 65f, (float)rng.NextDouble()),
                                     Vector3.Cross(Vector3.up, dir)), matWood);
            float bs = h * 0.17f;
            AddTreePart(tree, PrimitiveType.Sphere, "BranchLeaves",
                dir * bLen * 0.95f + Vector3.up * (baseY + bLen * 0.62f),
                new Vector3(bs, bs * 0.8f, bs), Quaternion.identity, leafAccent);
        }

        // Canopée : gros cœur + couronne de blobs décalés
        float canopyR = h * 0.30f;
        float cy = trunkH + canopyR * 0.55f;
        AddTreePart(tree, PrimitiveType.Sphere, "CanopyCore",
            new Vector3(0f, cy + canopyR * 0.15f, 0f),
            new Vector3(canopyR * 2.1f, canopyR * 1.6f, canopyR * 2.1f),
            Quaternion.identity, leafMain);
        int blobs = 5 + (int)(rng.NextDouble() * 2f);
        for (int i = 0; i < blobs; i++)
        {
            float ang = i * (Mathf.PI * 2f / blobs) + (float)rng.NextDouble() * 0.6f;
            float rad = canopyR * Mathf.Lerp(0.55f, 0.90f, (float)rng.NextDouble());
            float s   = canopyR * Mathf.Lerp(0.95f, 1.35f, (float)rng.NextDouble());
            AddTreePart(tree, PrimitiveType.Sphere, "CanopyBlob",
                new Vector3(Mathf.Cos(ang) * rad,
                            cy + ((float)rng.NextDouble() - 0.35f) * canopyR * 0.55f,
                            Mathf.Sin(ang) * rad),
                new Vector3(s, s * 0.78f, s), Quaternion.identity,
                rng.NextDouble() < 0.65 ? leafMain : leafAccent);
        }
        // Blob clair au sommet : accroche la lumière
        AddTreePart(tree, PrimitiveType.Sphere, "CanopyTop",
            new Vector3(0f, cy + canopyR * 0.85f, 0f),
            new Vector3(canopyR, canopyR * 0.7f, canopyR),
            Quaternion.identity, leafAccent);
    }

    // FEUILLU ÉTAGÉ : silhouette classique en couches, avec jitter organique
    void BuildLayeredTree(Transform tree, float h)
    {
        float trunk = h * 0.28f;
        float width = h * 0.12f;
        int style = (int)(rng.NextDouble() * 3);
        Material trunkMat = style == 2 ? matWoodDark : matWood;
        Material leafMat  = style == 0 ? matLeafDark : style == 1 ? matLeaf : matLeafLight;

        AddTreePart(tree, PrimitiveType.Cylinder, "Trunk",
            new Vector3(0f, trunk * 0.5f, 0f),
            new Vector3(width * 0.40f, trunk * 0.5f, width * 0.40f),
            Quaternion.identity, trunkMat);

        float[] crownSizes   = { 2.0f, 1.50f, 1.10f, 0.70f };
        float[] crownHeights = { 0.38f, 0.28f, 0.20f, 0.14f };
        float[] crownOffsets = { 0.30f, 0.62f, 0.84f, 1.02f };
        for (int layer = 0; layer < crownSizes.Length; layer++)
        {
            float jit = width * 0.45f;
            float sj  = Mathf.Lerp(0.85f, 1.15f, (float)rng.NextDouble());
            AddTreePart(tree, PrimitiveType.Sphere, "Crown_" + layer,
                new Vector3(((float)rng.NextDouble() - 0.5f) * jit,
                            trunk + h * crownOffsets[layer] * 0.55f,
                            ((float)rng.NextDouble() - 0.5f) * jit),
                new Vector3(width * crownSizes[layer] * 1.7f * sj,
                            h * crownHeights[layer] * sj,
                            width * crownSizes[layer] * 1.7f * sj),
                Quaternion.identity,
                layer == crownSizes.Length - 1 && rng.NextDouble() < 0.5 ? matLeafLight : leafMat);
        }
    }

    // BOULEAU : tronc blanc strié de bandes sombres, feuillage léger et haut
    void BuildBirchTree(Transform tree, float h)
    {
        float trunkH = h * 0.55f;
        float trunkR = h * 0.028f;

        AddTreePart(tree, PrimitiveType.Cylinder, "Trunk",
            new Vector3(0f, trunkH * 0.5f, 0f),
            new Vector3(trunkR * 2f, trunkH * 0.5f, trunkR * 2f),
            Quaternion.identity, matBirchBark);

        // Stries sombres caractéristiques
        int bands = 4 + (int)(rng.NextDouble() * 3f);
        for (int i = 0; i < bands; i++)
        {
            float by = trunkH * Mathf.Lerp(0.12f, 0.88f, (float)rng.NextDouble());
            AddTreePart(tree, PrimitiveType.Cylinder, "BirchBand",
                new Vector3(0f, by, 0f),
                new Vector3(trunkR * 2.12f, 0.025f, trunkR * 2.12f),
                Quaternion.identity, matWoodDark);
        }

        // Feuillage : 3 blobs verticaux, le sommet tire vers le jaune-vert
        float cy = trunkH + h * 0.06f;
        for (int i = 0; i < 3; i++)
        {
            float s = h * Mathf.Lerp(0.22f, 0.30f, (float)rng.NextDouble());
            AddTreePart(tree, PrimitiveType.Sphere, "BirchLeaves",
                new Vector3(((float)rng.NextDouble() - 0.5f) * h * 0.12f,
                            cy + i * h * 0.10f,
                            ((float)rng.NextDouble() - 0.5f) * h * 0.12f),
                new Vector3(s, s * 1.35f, s), Quaternion.identity,
                i == 2 ? matLeafYellow : matLeafLight);
        }
    }

    // CERISIER EN FLEURS : tronc tortueux sombre, canopée rose, pétales au sol
    void BuildBlossomTree(Transform tree, float h)
    {
        float trunkH = h * 0.38f;
        float trunkR = h * 0.040f;

        AddTreePart(tree, PrimitiveType.Cylinder, "Trunk",
            new Vector3(0f, trunkH * 0.5f, 0f),
            new Vector3(trunkR * 2f, trunkH * 0.5f, trunkR * 2f),
            Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 12f, 0f,
                             ((float)rng.NextDouble() - 0.5f) * 12f), matWoodDark);

        int branches = 3;
        for (int i = 0; i < branches; i++)
        {
            float ang   = i * (Mathf.PI * 2f / branches) + (float)rng.NextDouble() * 0.7f;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            float bLen  = h * 0.20f;
            AddTreePart(tree, PrimitiveType.Cylinder, "Branch",
                dir * bLen * 0.40f + Vector3.up * (trunkH * 0.85f + bLen * 0.25f),
                new Vector3(trunkR * 0.8f, bLen * 0.5f, trunkR * 0.8f),
                Quaternion.AngleAxis(50f, Vector3.Cross(Vector3.up, dir)), matWoodDark);
        }

        // Canopée rose en grappe
        float canopyR = h * 0.28f;
        float cy = trunkH + canopyR * 0.50f;
        AddTreePart(tree, PrimitiveType.Sphere, "BlossomCore",
            new Vector3(0f, cy, 0f),
            new Vector3(canopyR * 2.0f, canopyR * 1.5f, canopyR * 2.0f),
            Quaternion.identity, matBlossom);
        int blobs = 4 + (int)(rng.NextDouble() * 2f);
        for (int i = 0; i < blobs; i++)
        {
            float ang = i * (Mathf.PI * 2f / blobs) + (float)rng.NextDouble() * 0.5f;
            float s   = canopyR * Mathf.Lerp(0.8f, 1.15f, (float)rng.NextDouble());
            AddTreePart(tree, PrimitiveType.Sphere, "BlossomBlob",
                new Vector3(Mathf.Cos(ang) * canopyR * 0.70f,
                            cy + ((float)rng.NextDouble() - 0.3f) * canopyR * 0.5f,
                            Mathf.Sin(ang) * canopyR * 0.70f),
                new Vector3(s, s * 0.8f, s), Quaternion.identity,
                rng.NextDouble() < 0.7 ? matBlossom : matBlossomLight);
        }

        // Tapis de pétales au pied
        int petals = 4 + (int)(rng.NextDouble() * 4f);
        for (int i = 0; i < petals; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(0.3f, canopyR * 1.6f, (float)rng.NextDouble());
            AddTreePart(tree, PrimitiveType.Cube, "Petal",
                new Vector3(Mathf.Cos(ang) * rad, 0.012f, Mathf.Sin(ang) * rad),
                new Vector3(0.12f, 0.012f, 0.12f),
                Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), matBlossomLight);
        }
    }

    // ARBRE DU NÉANT : squelette d'arbre mort tordu, écorce noir-violacé,
    // branches nues hérissées de cristaux luminescents et canopée de feuillage
    // du néant. Plus la corruption est forte, plus le feuillage devient
    // luminescent et clairsemé (l'arbre se dissout dans le vide).
    void BuildVoidTree(Transform tree, float h, float corruption)
    {
        float trunkH = h * 0.48f;
        float trunkR = h * 0.05f;
        float lean   = ((float)rng.NextDouble() - 0.5f) * 20f;   // bien plus tordu qu'un arbre sain

        AddTreePart(tree, PrimitiveType.Cylinder, "VoidTrunk",
            new Vector3(0f, trunkH * 0.5f, 0f),
            new Vector3(trunkR * 2f, trunkH * 0.5f, trunkR * 2f),
            Quaternion.Euler(lean, 0f, lean * 0.5f), matVoidWood);

        // Quelques racines noueuses au pied
        int roots = 2 + (int)(rng.NextDouble() * 2f);
        for (int i = 0; i < roots; i++)
        {
            float ang   = i * (Mathf.PI * 2f / roots) + (float)rng.NextDouble();
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            AddTreePart(tree, PrimitiveType.Cylinder, "VoidRoot",
                dir * trunkR * 1.5f + Vector3.up * (trunkH * 0.06f),
                new Vector3(trunkR * 0.8f, trunkH * 0.14f, trunkR * 0.8f),
                Quaternion.AngleAxis(35f, Vector3.Cross(Vector3.up, dir)), matVoidWood);
        }

        // Branches nues qui partent en tous sens (silhouette d'arbre mort)
        int branches = 3 + (int)(rng.NextDouble() * 3f);
        for (int i = 0; i < branches; i++)
        {
            float ang   = (float)rng.NextDouble() * Mathf.PI * 2f;
            float baseY = trunkH * Mathf.Lerp(0.45f, 0.95f, (float)rng.NextDouble());
            float bLen  = h * Mathf.Lerp(0.18f, 0.32f, (float)rng.NextDouble());
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            AddTreePart(tree, PrimitiveType.Cylinder, "VoidBranch",
                dir * bLen * 0.45f + Vector3.up * (baseY + bLen * 0.35f),
                new Vector3(trunkR * 0.65f, bLen * 0.5f, trunkR * 0.65f),
                Quaternion.AngleAxis(Mathf.Lerp(35f, 72f, (float)rng.NextDouble()),
                                     Vector3.Cross(Vector3.up, dir)), matVoidWood);

            // Cristal luminescent au bout de certaines branches
            if (rng.NextDouble() < 0.6)
            {
                float cs = h * Mathf.Lerp(0.05f, 0.11f, (float)rng.NextDouble());
                AddTreePart(tree, PrimitiveType.Cube, "VoidCrystal",
                    dir * bLen * 0.95f + Vector3.up * (baseY + bLen * 0.68f),
                    new Vector3(cs, cs * 2.2f, cs),
                    Quaternion.Euler((float)rng.NextDouble() * 60f,
                                     (float)rng.NextDouble() * 360f,
                                     (float)rng.NextDouble() * 60f), matVoidGlowLeaf);
            }
        }

        // Canopée du néant : amas de sphères texturées « galaxie » (matVoidLeaf).
        // Plus de gros blobs roses lumineux : seuls de petits cristaux aux
        // branches gardent une lueur lavande discrète.
        float canopyR = h * 0.26f;
        float cy      = trunkH + canopyR * 0.4f;
        int   blobs   = 3 + (int)(rng.NextDouble() * 3f);
        for (int i = 0; i < blobs; i++)
        {
            float ang = i * (Mathf.PI * 2f / blobs) + (float)rng.NextDouble() * 0.6f;
            float rad = canopyR * Mathf.Lerp(0.30f, 0.85f, (float)rng.NextDouble());
            float s   = canopyR * Mathf.Lerp(0.65f, 1.15f, (float)rng.NextDouble());
            AddTreePart(tree, PrimitiveType.Sphere, "VoidCanopy",
                new Vector3(Mathf.Cos(ang) * rad,
                            cy + ((float)rng.NextDouble() - 0.3f) * canopyR * 0.5f,
                            Mathf.Sin(ang) * rad),
                new Vector3(s, s * 0.8f, s), Quaternion.identity, matVoidLeaf);
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
            if (pos.magnitude > vr)                continue;
            if (pos.magnitude < parkRadius + 1.5f) continue;
            if (IsInsideLake(pos))                 continue;
            if (IsInVoidBite(pos))                 continue;
            GameObject house = BuildHouse(pos);
            house.transform.SetParent(g);
            house.layer = gameObject.layer;
            lastHousePositions.Add(new Vector3(pos.x, 0f, pos.z));
            placed++;
        }
        return g;
    }

    public float GetVillageRadius()
    {
        switch (phase)
        {
            default:
            case 1: return villageRadiusPhase1;
            case 2: return villageRadiusPhase2;
            case 3: return villageRadiusPhase3;
            case 4:
            case 5: return villageRadiusPhase4;
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
            case 4:
            case 5: return housesPhase4;
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

        // Soubassement en pierre : assoit la maison sur les pentes
        AddTreePart(house.transform, PrimitiveType.Cube, "Foundation",
            new Vector3(0f, -0.05f, 0f),
            new Vector3(width + 0.16f, 0.50f, depth + 0.16f),
            Quaternion.identity, matStoneDark);

        // Corps
        AddTreePart(house.transform, PrimitiveType.Cube, "Body",
            new Vector3(0f, totalH * 0.5f, 0f),
            new Vector3(width, totalH, depth), Quaternion.identity, wallMat);

        AddHouseTimbers(house.transform, width, depth, totalH, floors, floorH);

        float roofH = Mathf.Max(0.9f, totalH * roofHeightFactor);
        BuildHouseRoof(house.transform, width, depth, totalH, roofH, roofMat, wallMat);
        AddHouseDoorAndWindows(house.transform, width, depth, floors, floorH, roofMat);
        AddChimney(house.transform, depth, totalH + roofH);
        return house;
    }

    // Cheminée plantée sur le faîtage : elle traverse le toit au lieu de
    // flotter à côté d'un pan
    void AddChimney(Transform parent, float depth, float ridgeY)
    {
        float cz = Mathf.Lerp(-depth * 0.28f, depth * 0.28f, (float)rng.NextDouble());
        AddTreePart(parent, PrimitiveType.Cube, "Chimney",
            new Vector3(0f, ridgeY + 0.12f, cz),
            new Vector3(0.32f, 0.85f, 0.32f), Quaternion.identity, matStoneDark);
        AddTreePart(parent, PrimitiveType.Cube, "ChimneyCap",
            new Vector3(0f, ridgeY + 0.58f, cz),
            new Vector3(0.44f, 0.09f, 0.44f), Quaternion.identity, matRockDark);
    }

    // Colombages sur les 4 faces : poteaux d'angle, ceinture à chaque étage,
    // diagonales de contreventement sur les murs latéraux
    void AddHouseTimbers(Transform parent, float width, float depth, float totalH, int floors, float floorH)
    {
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sz = -1; sz <= 1; sz += 2)
            AddTreePart(parent, PrimitiveType.Cube, "CornerPost",
                new Vector3(sx * (width * 0.5f - 0.02f), totalH * 0.5f, sz * (depth * 0.5f - 0.02f)),
                new Vector3(0.15f, totalH, 0.15f), Quaternion.identity, matWoodDark);

        // Sablière sous le toit + ceinture à chaque plancher
        AddTimberRing(parent, width, depth, totalH - 0.06f);
        for (int f = 1; f < floors; f++)
            AddTimberRing(parent, width, depth, f * floorH);

        // Diagonales sur les murs latéraux (style maison à colombages)
        float dh = totalH - 0.5f;
        float dz = depth * 0.62f;
        float dl = Mathf.Sqrt(dh * dh + dz * dz);
        float dAng = Mathf.Atan2(dz, dh) * Mathf.Rad2Deg;
        for (int sx = -1; sx <= 1; sx += 2)
            AddTreePart(parent, PrimitiveType.Cube, "Brace",
                new Vector3(sx * (width * 0.5f + 0.005f), totalH * 0.5f, 0f),
                new Vector3(0.08f, dl, 0.08f),
                Quaternion.Euler(sx * dAng, 0f, 0f), matWoodDark);
    }

    void AddTimberRing(Transform parent, float width, float depth, float y)
    {
        AddTreePart(parent, PrimitiveType.Cube, "TimberFront",
            new Vector3(0f, y, depth * 0.5f), new Vector3(width * 1.02f, 0.10f, 0.10f),
            Quaternion.identity, matWoodDark);
        AddTreePart(parent, PrimitiveType.Cube, "TimberBack",
            new Vector3(0f, y, -depth * 0.5f), new Vector3(width * 1.02f, 0.10f, 0.10f),
            Quaternion.identity, matWoodDark);
        AddTreePart(parent, PrimitiveType.Cube, "TimberLeft",
            new Vector3(-width * 0.5f, y, 0f), new Vector3(0.10f, 0.10f, depth * 1.02f),
            Quaternion.identity, matWoodDark);
        AddTreePart(parent, PrimitiveType.Cube, "TimberRight",
            new Vector3(width * 0.5f, y, 0f), new Vector3(0.10f, 0.10f, depth * 1.02f),
            Quaternion.identity, matWoodDark);
    }

    void AddHouseDoorAndWindows(Transform parent, float width, float depth, int floors, float floorH, Material roofMat)
    {
        float zF = depth * 0.5f;

        // Porte : battant, montants, linteau, poignée, auvent et marche
        AddTreePart(parent, PrimitiveType.Cube, "Door",
            new Vector3(0f, floorH * 0.40f, zF + 0.02f),
            new Vector3(0.85f, floorH * 0.80f, 0.10f), Quaternion.identity, matDoor);
        AddTreePart(parent, PrimitiveType.Cube, "DoorJambL",
            new Vector3(-0.48f, floorH * 0.42f, zF + 0.02f),
            new Vector3(0.10f, floorH * 0.86f, 0.13f), Quaternion.identity, matWoodDark);
        AddTreePart(parent, PrimitiveType.Cube, "DoorJambR",
            new Vector3(0.48f, floorH * 0.42f, zF + 0.02f),
            new Vector3(0.10f, floorH * 0.86f, 0.13f), Quaternion.identity, matWoodDark);
        AddTreePart(parent, PrimitiveType.Cube, "DoorLintel",
            new Vector3(0f, floorH * 0.88f, zF + 0.02f),
            new Vector3(1.10f, 0.13f, 0.13f), Quaternion.identity, matWoodDark);
        AddTreePart(parent, PrimitiveType.Cube, "DoorKnob",
            new Vector3(0.26f, floorH * 0.40f, zF + 0.075f),
            new Vector3(0.06f, 0.06f, 0.04f), Quaternion.identity, matStoneDark);
        AddTreePart(parent, PrimitiveType.Cube, "DoorAwning",
            new Vector3(0f, floorH * 1.04f, zF + 0.24f),
            new Vector3(1.25f, 0.06f, 0.55f), Quaternion.Euler(-20f, 0f, 0f), roofMat);
        AddTreePart(parent, PrimitiveType.Cube, "DoorStep",
            new Vector3(0f, 0.06f, zF + 0.28f),
            new Vector3(1.15f, 0.12f, 0.50f), Quaternion.identity, matStone);

        // Fenêtres sur les 4 faces : avant, arrière et pignons latéraux
        for (int f = 0; f < floors; f++)
        {
            float wY = f * floorH + floorH * 0.60f;
            AddWindow(parent, new Vector3(-width * 0.32f, wY, zF + 0.005f), false);
            AddWindow(parent, new Vector3( width * 0.32f, wY, zF + 0.005f), false);
            if (f > 0) AddWindow(parent, new Vector3(0f, wY, zF + 0.005f), false);

            AddWindow(parent, new Vector3(-width * 0.30f, wY, -zF - 0.005f), false);
            AddWindow(parent, new Vector3( width * 0.30f, wY, -zF - 0.005f), false);

            AddWindow(parent, new Vector3(-width * 0.5f - 0.005f, wY, 0f), true);
            AddWindow(parent, new Vector3( width * 0.5f + 0.005f, wY, 0f), true);
        }
    }

    void AddWindow(Transform parent, Vector3 localPos, bool side)
    {
        // Direction vers l'extérieur du mur (pour décoller vitre/croisillons)
        float ox = side ? Mathf.Sign(localPos.x) : 0f;
        float oz = side ? 0f : Mathf.Sign(localPos.z);
        Vector3 outDir = new Vector3(ox, 0f, oz);

        AddTreePart(parent, PrimitiveType.Cube, "WindowFrame", localPos,
            side ? new Vector3(0.10f, 0.72f, 0.62f) : new Vector3(0.62f, 0.72f, 0.10f),
            Quaternion.identity, matWoodDark);
        AddTreePart(parent, PrimitiveType.Cube, "WindowGlass",
            localPos + outDir * 0.015f,
            side ? new Vector3(0.08f, 0.56f, 0.46f) : new Vector3(0.46f, 0.56f, 0.08f),
            Quaternion.identity, matWindow);
        // Croisillons
        AddTreePart(parent, PrimitiveType.Cube, "WindowBarV",
            localPos + outDir * 0.03f,
            side ? new Vector3(0.10f, 0.58f, 0.05f) : new Vector3(0.05f, 0.58f, 0.10f),
            Quaternion.identity, matWoodDark);
        AddTreePart(parent, PrimitiveType.Cube, "WindowBarH",
            localPos + outDir * 0.03f,
            side ? new Vector3(0.10f, 0.05f, 0.48f) : new Vector3(0.48f, 0.05f, 0.10f),
            Quaternion.identity, matWoodDark);
        // Appui de fenêtre en pierre
        AddTreePart(parent, PrimitiveType.Cube, "WindowSill",
            localPos + outDir * 0.05f + Vector3.down * 0.41f,
            side ? new Vector3(0.16f, 0.08f, 0.74f) : new Vector3(0.74f, 0.08f, 0.16f),
            Quaternion.identity, matStone);
    }

    // Toit à deux pans calculé géométriquement : les pans se rejoignent
    // exactement au faîtage et débordent aux égouts ; les pignons sont
    // remplis par des lattes couleur mur (plus de barres qui dépassent)
    void BuildHouseRoof(Transform parent, float width, float depth, float bodyTopY, float roofH, Material roofMat, Material wallMat)
    {
        float halfW    = width * 0.5f;
        float overhang = 0.24f;
        float thick    = 0.11f;
        float baseLen  = Mathf.Sqrt(halfW * halfW + roofH * roofH);
        float ux = halfW / baseLen, uy = roofH / baseLen;   // direction égout → faîte
        float slabLen   = baseLen + overhang;
        float angDeg    = Mathf.Atan2(roofH, halfW) * Mathf.Rad2Deg;
        float depthOver = depth + overhang * 2f;

        GameObject roofRoot = new GameObject("Roof");
        roofRoot.transform.SetParent(parent);
        roofRoot.transform.localPosition = new Vector3(0f, bodyTopY, 0f);
        roofRoot.transform.localRotation = Quaternion.identity;
        roofRoot.transform.localScale    = Vector3.one;
        roofRoot.layer = parent.gameObject.layer;

        // Pan gauche : du débord d'égout jusqu'au faîte
        Vector3 cL = new Vector3((-halfW - ux * overhang) * 0.5f,
                                 (roofH - uy * overhang) * 0.5f, 0f)
                   + new Vector3(-uy, ux, 0f) * (thick * 0.5f);
        AddTreePart(roofRoot.transform, PrimitiveType.Cube, "Roof_L", cL,
            new Vector3(slabLen, thick, depthOver),
            Quaternion.Euler(0f, 0f, angDeg), roofMat);

        // Pan droit (miroir)
        Vector3 cR = new Vector3((halfW + ux * overhang) * 0.5f,
                                 (roofH - uy * overhang) * 0.5f, 0f)
                   + new Vector3(uy, ux, 0f) * (thick * 0.5f);
        AddTreePart(roofRoot.transform, PrimitiveType.Cube, "Roof_R", cR,
            new Vector3(slabLen, thick, depthOver),
            Quaternion.Euler(0f, 0f, -angDeg), roofMat);

        // Faîtage qui couvre la jonction des deux pans
        AddTreePart(roofRoot.transform, PrimitiveType.Cube, "Roof_Ridge",
            new Vector3(0f, roofH + thick * 0.55f, 0f),
            new Vector3(0.18f, 0.13f, depthOver + 0.04f),
            Quaternion.identity, matWoodDark);

        // Pignons pleins : lattes horizontales qui remplissent le triangle
        // sous le toit, dans la couleur du mur (façade fermée)
        int steps = 4;
        for (int side = -1; side <= 1; side += 2)
        for (int i = 0; i < steps; i++)
        {
            float t = (i + 0.5f) / steps;
            float w = Mathf.Max(0.12f, width * (1f - t));
            AddTreePart(roofRoot.transform, PrimitiveType.Cube, "Gable",
                new Vector3(0f, roofH * t, side * (depth * 0.5f - 0.06f)),
                new Vector3(w, roofH / steps + 0.02f, 0.12f),
                Quaternion.identity, wallMat);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    Transform BuildRoads()
    {
        Transform g = new GameObject("Roads").transform;
        g.SetParent(root);
        g.gameObject.layer = gameObject.layer;
        float vr        = GetVillageRadius();
        int   branches  = Mathf.Clamp(8 + phase * 2, 8, 14);
        float angleStep = 360f / branches;
        for (int i = 0; i < branches; i++)
        {
            float   ang = i * angleStep + (float)rng.NextDouble() * 10f;
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
            float   len = Mathf.Lerp(vr * 0.6f, vr, (float)rng.NextDouble());
            lastRoadBranches.Add(new Vector2(ang, len));
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
            tile.gameObject.layer = gameObject.layer;
            tile.transform.position   = new Vector3(p.x, gy, p.z);
            tile.transform.localScale = new Vector3(roadWidth, 0.05f, 1f);
            tile.transform.rotation   = Quaternion.LookRotation(dir);
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
        AddLakeVegetation(lakeRoot.transform, gy);
        return lakeRoot;
    }

    void AddLakeVegetation(Transform parent, float waterY)
    {
        int reedCount = Mathf.CeilToInt(lakeRadius * 5f);
        for (int i = 0; i < reedCount; i++)
        {
            float ang   = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad   = lakeRadius * Mathf.Lerp(0.85f, 1.15f, (float)rng.NextDouble());
            float rx    = lakeOffset.x + Mathf.Cos(ang) * rad;
            float rz    = lakeOffset.y + Mathf.Sin(ang) * rad;
            float reedH = Mathf.Lerp(0.55f, 1.20f, (float)rng.NextDouble());
            float reedR = Mathf.Lerp(0.025f, 0.05f, (float)rng.NextDouble());

            GameObject reed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            reed.name = "Reed";
            reed.transform.SetParent(parent);
            reed.gameObject.layer    = parent.gameObject.layer;
            reed.transform.position  = new Vector3(rx, waterY + reedH, rz);
            reed.transform.localScale = new Vector3(reedR, reedH * 0.5f, reedR);
            reed.transform.localRotation = Quaternion.Euler(
                ((float)rng.NextDouble() - 0.5f) * 14f, (float)rng.NextDouble() * 360f, 0f);
            reed.GetComponent<MeshRenderer>().sharedMaterial = matLeaf;
            SafeRemoveCollider(reed.GetComponent<Collider>());

            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tip.name = "ReedTip";
            tip.transform.SetParent(reed.transform);
            tip.gameObject.layer = parent.gameObject.layer;
            tip.transform.localScale    = new Vector3(2.2f, 0.16f, 2.2f);
            tip.transform.localPosition = new Vector3(0f, 1.12f, 0f);
            tip.GetComponent<MeshRenderer>().sharedMaterial = matRoofBrown;
            SafeRemoveCollider(tip.GetComponent<Collider>());
        }

        int lilyCount = Mathf.CeilToInt(lakeRadius * 2.5f);
        for (int i = 0; i < lilyCount; i++)
        {
            float ang  = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad  = lakeRadius * Mathf.Lerp(0.15f, 0.80f, (float)rng.NextDouble());
            float lx   = lakeOffset.x + Mathf.Cos(ang) * rad;
            float lz   = lakeOffset.y + Mathf.Sin(ang) * rad;
            float padR = Mathf.Lerp(0.22f, 0.42f, (float)rng.NextDouble());

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "LilyPad";
            pad.transform.SetParent(parent);
            pad.gameObject.layer    = parent.gameObject.layer;
            pad.transform.position  = new Vector3(lx, waterY + 0.02f, lz);
            pad.transform.localScale = new Vector3(padR * 2f, 0.025f, padR * 2f);
            pad.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            pad.GetComponent<MeshRenderer>().sharedMaterial = matLeafDark;
            SafeRemoveCollider(pad.GetComponent<Collider>());

            if (rng.NextDouble() < 0.55f)
            {
                GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.name = "LilyFlower";
                flower.transform.SetParent(pad.transform);
                flower.gameObject.layer = parent.gameObject.layer;
                flower.transform.localScale    = new Vector3(0.25f / padR, 1.4f, 0.25f / padR);
                flower.transform.localPosition = new Vector3(0f, 2.0f, 0f);
                flower.GetComponent<MeshRenderer>().sharedMaterial = rng.NextDouble() < 0.6f ? matPlaster : matRoofRed;
                SafeRemoveCollider(flower.GetComponent<Collider>());
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // CHÂTEAU : enceinte polygonale « réelle » — des courtines droites et
    // épaisses relient des tours d'angle, avec chemin de ronde crénelé, tours
    // plus hautes coiffées d'un toit conique, et une porte fortifiée. Fini
    // l'anneau de cubes pivotés qui se chevauchent.
    GameObject BuildCastle()
    {
        GameObject g = new GameObject("Castle");
        g.transform.SetParent(root);
        g.gameObject.layer = gameObject.layer;

        int sides = Mathf.Max(5, castleTowers);
        Vector3[] towers = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float ang = i * (Mathf.PI * 2f / sides);
            towers[i] = new Vector3(Mathf.Cos(ang) * castleOuterRadius, 0f, Mathf.Sin(ang) * castleOuterRadius);
        }

        int gateSide = ((seed % sides) + sides) % sides;   // une courtine porte l'entrée

        // Courtines entre tours consécutives
        for (int i = 0; i < sides; i++)
            BuildCurtainWall(g.transform, towers[i], towers[(i + 1) % sides], i == gateSide);

        // Tours d'angle dessinées après : elles coiffent proprement les jonctions
        for (int i = 0; i < sides; i++)
            BuildCastleTower(g.transform, towers[i]);

        return g;
    }

    // Une courtine : mur massif à base évasée, bandeaux de pierre, mâchicoulis
    // en encorbellement, créneaux + meurtrières sur le bord extérieur, et
    // parapet intérieur (chemin de ronde). isGate construit un corps de garde.
    void BuildCurtainWall(Transform parent, Vector3 a, Vector3 b, bool isGate)
    {
        Vector3 chord = b - a;
        float   len   = chord.magnitude;
        Vector3 dir   = chord / len;
        Vector3 mid   = (a + b) * 0.5f;
        Quaternion rot = Quaternion.LookRotation(dir);
        Vector3 perp     = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 outwardN = (Vector3.Dot(perp, new Vector3(mid.x, 0f, mid.z)) >= 0f) ? perp : -perp;

        // Base au point le plus bas du terrain, ancrée sous le sol (pentes)
        float baseY = Mathf.Min(TerrainHeight(a.x, a.z),
                      Mathf.Min(TerrainHeight(b.x, b.z), TerrainHeight(mid.x, mid.z)));
        float embed = 0.8f;
        float thick = 1.0f;
        float wallH = castleWallHeight;
        float baseH = wallH * 0.32f;
        float topY  = baseY + wallH;

        if (!isGate)
        {
            BuildWallSection(parent, mid, dir, outwardN, len, baseY, embed, thick, wallH, baseH);
            AddMachicolation(parent, mid, dir, outwardN, len, topY - 0.05f, thick);
            AddCrenellations(parent, mid, dir, outwardN, len, topY + 0.1f, thick, 0f);
            AddWallArrowSlits(parent, mid, dir, outwardN, len, baseY, wallH, thick);
            // Parapet intérieur du chemin de ronde
            AddTreePart(parent, PrimitiveType.Cube, "InnerParapet",
                new Vector3(mid.x, topY + 0.35f, mid.z) - outwardN * (thick * 0.5f - 0.09f),
                new Vector3(0.18f, 0.7f, len), rot, matStone);
        }
        else
        {
            BuildGatehouse(parent, mid, dir, outwardN, len, baseY, embed, thick, wallH, baseH);
        }
    }

    // Corps de mur : talus évasé + fût principal + bandeaux de transition
    void BuildWallSection(Transform parent, Vector3 mid, Vector3 dir, Vector3 outwardN,
                          float len, float baseY, float embed, float thick, float wallH, float baseH)
    {
        Quaternion rot = Quaternion.LookRotation(dir);
        AddTreePart(parent, PrimitiveType.Cube, "Curtain",
            new Vector3(mid.x, baseY + (wallH - embed) * 0.5f, mid.z),
            new Vector3(thick, wallH + embed, len), rot, matStone);
        // Talus (base évasée, plus large)
        AddTreePart(parent, PrimitiveType.Cube, "WallBatter",
            new Vector3(mid.x, baseY + (baseH - embed) * 0.5f, mid.z),
            new Vector3(thick + 0.5f, baseH + embed, len), rot, matStoneDark);
        // Bandeau au sommet du talus
        AddTreePart(parent, PrimitiveType.Cube, "WallString",
            new Vector3(mid.x, baseY + baseH, mid.z),
            new Vector3(thick + 0.18f, 0.16f, len), rot, matStone);
    }

    // Mâchicoulis : dalle en encorbellement vers l'extérieur, sur corbeaux
    void AddMachicolation(Transform parent, Vector3 mid, Vector3 dir, Vector3 outwardN,
                          float len, float y, float thick)
    {
        Quaternion rot = Quaternion.LookRotation(dir);
        AddTreePart(parent, PrimitiveType.Cube, "MachiSlab",
            new Vector3(mid.x, y, mid.z) + outwardN * (thick * 0.30f),
            new Vector3(thick * 0.7f, 0.22f, len), rot, matStoneDark);
        float step = 0.7f;
        int   nC   = Mathf.Max(1, Mathf.FloorToInt(len / step));
        float used = nC * step;
        for (int i = 0; i < nC; i++)
        {
            float d = -used * 0.5f + step * (i + 0.5f);
            Vector3 p = mid + dir * d + outwardN * (thick * 0.5f + 0.04f);
            AddTreePart(parent, PrimitiveType.Cube, "Corbel",
                new Vector3(p.x, y - 0.28f, p.z),
                new Vector3(0.26f, 0.32f, 0.20f), rot, matStone);
        }
    }

    // Créneaux (merlons espacés) sur le bord extérieur, avec meurtrières
    void AddCrenellations(Transform parent, Vector3 mid, Vector3 dir, Vector3 outwardN,
                          float len, float topY, float thick, float skipHalf)
    {
        float merlonW = 0.5f, gap = 0.45f, step = merlonW + gap, merlonH = 0.7f;
        int   n    = Mathf.Max(1, Mathf.FloorToInt(len / step));
        float used = n * step;
        Quaternion rot = Quaternion.LookRotation(dir);
        float outShift = thick * 0.22f;   // merlons sur la moitié extérieure
        for (int i = 0; i < n; i++)
        {
            float d = -used * 0.5f + step * (i + 0.5f);
            if (skipHalf > 0f && Mathf.Abs(d) < skipHalf) continue;
            Vector3 p = mid + dir * d + outwardN * outShift;
            AddTreePart(parent, PrimitiveType.Cube, "Merlon",
                new Vector3(p.x, topY + merlonH * 0.5f, p.z),
                new Vector3(thick * 0.5f, merlonH, merlonW), rot,
                (i % 2 == 0) ? matStone : matStoneDark);
            // Fente d'archer dans un merlon sur deux
            if (i % 2 == 0)
                AddTreePart(parent, PrimitiveType.Cube, "ArrowSlit",
                    new Vector3(p.x, topY + merlonH * 0.55f, p.z) + outwardN * (thick * 0.26f),
                    new Vector3(0.06f, merlonH * 0.5f, 0.10f), rot, matStoneDark);
        }
    }

    // Meurtrières verticales le long de la face extérieure de la courtine
    void AddWallArrowSlits(Transform parent, Vector3 mid, Vector3 dir, Vector3 outwardN,
                           float len, float baseY, float wallH, float thick)
    {
        Quaternion rot = Quaternion.LookRotation(dir);
        float step = 2.2f;
        int   n    = Mathf.Max(1, Mathf.FloorToInt(len / step));
        float used = n * step;
        for (int i = 0; i < n; i++)
        {
            float d = -used * 0.5f + step * (i + 0.5f);
            Vector3 p = mid + dir * d + outwardN * (thick * 0.5f - 0.02f);
            AddTreePart(parent, PrimitiveType.Cube, "ArrowSlit",
                new Vector3(p.x, baseY + wallH * 0.55f, p.z),
                new Vector3(0.10f, 0.8f, 0.14f), rot, matStoneDark);
        }
    }

    // Corps de garde : pans latéraux + deux tours-portes carrées encadrant une
    // entrée voûtée fermée par une herse et un double vantail de bois.
    void BuildGatehouse(Transform parent, Vector3 mid, Vector3 dir, Vector3 outwardN,
                        float len, float baseY, float embed, float thick, float wallH, float baseH)
    {
        Quaternion rot = Quaternion.LookRotation(dir);
        float gateW   = 3.0f;
        float sideLen = (len - gateW) * 0.5f;

        // Pans latéraux (mêmes détails que les courtines)
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 c = mid + dir * (s * (gateW + sideLen) * 0.5f);
            BuildWallSection(parent, c, dir, outwardN, sideLen, baseY, embed, thick, wallH, baseH);
            AddMachicolation(parent, c, dir, outwardN, sideLen, baseY + wallH - 0.05f, thick);
            AddCrenellations(parent, c, dir, outwardN, sideLen, baseY + wallH + 0.1f, thick, 0f);
        }

        // Deux tours-portes carrées, plus hautes que la courtine
        float gtH = wallH * 1.35f;
        float gtW = 1.7f;
        float boxX = thick + 0.7f, boxZ = gtW + 0.2f;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 c = mid + dir * (s * (gateW * 0.5f + gtW * 0.5f));
            AddTreePart(parent, PrimitiveType.Cube, "GateTower",
                new Vector3(c.x, baseY + (gtH - embed) * 0.5f, c.z),
                new Vector3(boxX, gtH + embed, boxZ), rot, matStone);
            AddBoxMachicolation(parent, c, rot, boxX, boxZ, baseY + gtH - 0.05f);
            AddBoxBattlements(parent, c, rot, boxX, boxZ, baseY + gtH + 0.1f);
        }

        // Mur plein reliant les tours au-dessus de la porte
        float doorH = wallH * 0.72f;
        AddTreePart(parent, PrimitiveType.Cube, "GateWallTop",
            new Vector3(mid.x, baseY + doorH + (wallH - doorH) * 0.5f, mid.z),
            new Vector3(thick, wallH - doorH, gateW), rot, matStone);
        // Mâchicoulis + créneaux défendant l'entrée
        AddMachicolation(parent, mid, dir, outwardN, gateW, baseY + wallH - 0.05f, thick);
        AddCrenellations(parent, mid, dir, outwardN, gateW, baseY + wallH + 0.1f, thick, 0f);

        // Arc de la porte (claveau-linteau sombre)
        AddTreePart(parent, PrimitiveType.Cube, "GateArch",
            new Vector3(mid.x, baseY + doorH, mid.z),
            new Vector3(thick + 0.1f, 0.32f, gateW + 0.3f), rot, matStoneDark);

        // Double vantail en bois
        for (int s = -1; s <= 1; s += 2)
            AddTreePart(parent, PrimitiveType.Cube, "GateDoor",
                new Vector3(mid.x, baseY + doorH * 0.5f, mid.z) + dir * (s * gateW * 0.25f),
                new Vector3(thick * 0.4f, doorH - 0.1f, gateW * 0.5f - 0.06f), rot, matWoodDark);

        // Herse (portcullis) côté extérieur
        Vector3 hc = new Vector3(mid.x, baseY, mid.z) + outwardN * (thick * 0.42f);
        int bars = 5;
        for (int i = 0; i < bars; i++)
        {
            float dd = Mathf.Lerp(-gateW * 0.42f, gateW * 0.42f, (float)i / (bars - 1));
            AddTreePart(parent, PrimitiveType.Cube, "PortcullisV",
                new Vector3(hc.x, baseY + doorH * 0.5f, hc.z) + dir * dd,
                new Vector3(0.08f, doorH, 0.08f), rot, matStoneDark);
        }
        for (int i = 0; i < 3; i++)
        {
            float yy = Mathf.Lerp(0.25f, doorH - 0.2f, (float)i / 2f);
            AddTreePart(parent, PrimitiveType.Cube, "PortcullisH",
                new Vector3(hc.x, baseY + yy, hc.z),
                new Vector3(0.08f, 0.08f, gateW * 0.85f), rot, matStoneDark);
        }
    }

    // Merlons sur le pourtour d'un sommet rectangulaire (tours-portes)
    void AddBoxBattlements(Transform parent, Vector3 center, Quaternion rot,
                           float sizeX, float sizeZ, float topY)
    {
        float merlonW = 0.42f, gap = 0.38f, step = merlonW + gap, mh = 0.55f;
        Vector3 right = rot * Vector3.right;
        Vector3 fwd   = rot * Vector3.forward;
        int   nz    = Mathf.Max(1, Mathf.FloorToInt(sizeZ / step));
        float usedz = nz * step;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int i = 0; i < nz; i++)
            {
                float d = -usedz * 0.5f + step * (i + 0.5f);
                Vector3 p = center + right * (sx * sizeX * 0.5f) + fwd * d;
                AddTreePart(parent, PrimitiveType.Cube, "Merlon",
                    new Vector3(p.x, topY + mh * 0.5f, p.z),
                    new Vector3(0.34f, mh, merlonW), rot,
                    ((i + sx) % 2 == 0) ? matStone : matStoneDark);
            }
        int   nx    = Mathf.Max(1, Mathf.FloorToInt(sizeX / step));
        float usedx = nx * step;
        for (int sz = -1; sz <= 1; sz += 2)
            for (int i = 0; i < nx; i++)
            {
                float d = -usedx * 0.5f + step * (i + 0.5f);
                Vector3 p = center + fwd * (sz * sizeZ * 0.5f) + right * d;
                AddTreePart(parent, PrimitiveType.Cube, "Merlon",
                    new Vector3(p.x, topY + mh * 0.5f, p.z),
                    new Vector3(merlonW, mh, 0.34f), rot,
                    ((i + sz) % 2 == 0) ? matStone : matStoneDark);
            }
    }

    // Dalle de mâchicoulis en encorbellement au sommet d'une tour carrée
    void AddBoxMachicolation(Transform parent, Vector3 center, Quaternion rot,
                             float sizeX, float sizeZ, float y)
    {
        AddTreePart(parent, PrimitiveType.Cube, "MachiSlab",
            new Vector3(center.x, y, center.z),
            new Vector3(sizeX + 0.4f, 0.22f, sizeZ + 0.4f), rot, matStoneDark);
    }

    // Tour d'angle : base évasée, bandeaux, meurtrières, mâchicoulis crénelé,
    // toit conique élancé et mât avec fanion
    void BuildCastleTower(Transform parent, Vector3 pos)
    {
        float gy     = TerrainHeight(pos.x, pos.z);
        float embed  = 0.8f;
        float towerH = castleWallHeight * 1.5f;
        float r      = towerRadius;

        // Talus évasé à la base
        float batH = towerH * 0.28f;
        AddTreePart(parent, PrimitiveType.Cylinder, "TowerBatter",
            new Vector3(pos.x, gy + (batH - embed) * 0.5f, pos.z),
            new Vector3(r * 2.4f, (batH + embed) * 0.5f, r * 2.4f),
            Quaternion.identity, matStoneDark);

        // Fût (hauteur Unity d'un cylindre = scale.y * 2)
        AddTreePart(parent, PrimitiveType.Cylinder, "Tower",
            new Vector3(pos.x, gy + (towerH - embed) * 0.5f, pos.z),
            new Vector3(r * 2f, (towerH + embed) * 0.5f, r * 2f),
            Quaternion.identity, matStone);

        // Bandeaux de pierre
        for (int b = 0; b < 2; b++)
            AddTreePart(parent, PrimitiveType.Cylinder, "TowerString",
                new Vector3(pos.x, gy + towerH * (0.36f + b * 0.30f), pos.z),
                new Vector3(r * 2.12f, 0.12f, r * 2.12f),
                Quaternion.identity, matStoneDark);

        // Meurtrières verticales (4 directions, 2 hauteurs)
        for (int i = 0; i < 4; i++)
        {
            float a = i * (Mathf.PI * 2f / 4f) + 0.4f;
            Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            for (int h = 0; h < 2; h++)
                AddTreePart(parent, PrimitiveType.Cube, "ArrowSlit",
                    new Vector3(pos.x, gy + towerH * (0.42f + h * 0.26f), pos.z) + d * (r * 0.99f),
                    new Vector3(0.10f, 0.75f, 0.14f),
                    Quaternion.LookRotation(d), matStoneDark);
        }

        // Mâchicoulis : couronne en encorbellement + corbeaux
        AddTreePart(parent, PrimitiveType.Cylinder, "TowerMachi",
            new Vector3(pos.x, gy + towerH - 0.10f, pos.z),
            new Vector3(r * 2.4f, 0.24f, r * 2.4f),
            Quaternion.identity, matStoneDark);
        int corb = Mathf.Max(10, Mathf.RoundToInt(r * 10f));
        for (int i = 0; i < corb; i++)
        {
            float a = (float)i / corb * Mathf.PI * 2f;
            Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            AddTreePart(parent, PrimitiveType.Cube, "Corbel",
                new Vector3(pos.x, gy + towerH - 0.34f, pos.z) + d * (r * 1.06f),
                new Vector3(0.20f, 0.30f, 0.24f), Quaternion.LookRotation(d), matStone);
        }

        // Couronne de créneaux (posée sur la dalle de mâchicoulis)
        int pieces = Mathf.Max(10, Mathf.RoundToInt(r * 8f));
        for (int i = 0; i < pieces; i++)
        {
            float a = (float)i / pieces * Mathf.PI * 2f;
            Vector3 c = new Vector3(pos.x + Mathf.Cos(a) * r * 1.12f,
                                    gy + towerH + 0.32f,
                                    pos.z + Mathf.Sin(a) * r * 1.12f);
            AddTreePart(parent, PrimitiveType.Cube, "TowerCrenel", c,
                new Vector3(0.34f, 0.55f, 0.34f),
                Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a))),
                (i % 2 == 0) ? matStone : matStoneDark);
        }

        // Toit conique élancé : disques décroissants empilés
        int   steps = 6;
        float roofH = r * 2.8f;
        for (int i = 0; i < steps; i++)
        {
            float t  = (float)i / steps;
            float rr = Mathf.Lerp(r * 1.18f, 0.06f, t);
            float yy = gy + towerH + 0.6f + t * roofH;
            AddTreePart(parent, PrimitiveType.Cylinder, "TowerRoof",
                new Vector3(pos.x, yy, pos.z),
                new Vector3(rr * 2f, roofH / steps * 0.6f, rr * 2f),
                Quaternion.identity, matRoofRed);
        }

        // Mât + fanion au sommet
        float poleTop = gy + towerH + 0.6f + roofH;
        AddTreePart(parent, PrimitiveType.Cylinder, "FlagPole",
            new Vector3(pos.x, poleTop + 0.4f, pos.z),
            new Vector3(0.05f, 0.4f, 0.05f), Quaternion.identity, matWoodDark);
        AddTreePart(parent, PrimitiveType.Cube, "Flag",
            new Vector3(pos.x + 0.28f, poleTop + 0.62f, pos.z),
            new Vector3(0.55f, 0.30f, 0.02f), Quaternion.identity, matRoofRed);
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
