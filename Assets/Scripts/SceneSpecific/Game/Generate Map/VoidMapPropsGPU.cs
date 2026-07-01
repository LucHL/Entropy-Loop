using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

// ============================================================================
// Générateur de props décoratifs — compagnon de VoidMapGeneratorGPU.
// Même philosophie que le générateur principal :
//   - primitives Unity + sharedMaterial (=> batching GPU automatique)
//   - aucun collider sur le décor (NavMesh non pollué)
//   - génération déterministe : on réutilise le System.Random du seed
//   - StaticBatchingUtility.Combine en Play => quasi 0 draw call supplémentaire
// Le composant est auto-ajouté par VoidMapGeneratorGPU.Generate().
// ============================================================================
[ExecuteAlways]
public class VoidMapPropsGPU : MonoBehaviour
{
    [Header("Buissons")]
    public int     bushCount     = 130;
    public Vector2 bushSizeRange = new Vector2(0.35f, 0.85f);

    [Header("Fleurs")]
    public int        flowerClusterCount = 40;
    public Vector2Int flowersPerCluster  = new Vector2Int(3, 7);

    [Header("Champignons")]
    public int mushroomClusterCount = 22;

    [Header("Souches & Troncs couchés")]
    public int stumpCount = 16;
    public int logCount   = 12;

    [Header("Sapins (zones hautes)")]
    public int     pineCount       = 28;
    public Vector2 pineHeightRange = new Vector2(3.5f, 6.5f);

    [Header("Parc (centre)")]
    public int benchCount    = 5;
    public int campfireCount = 2;

    [Header("Village (phase >= 1)")]
    public bool buildWell             = true;
    public bool buildFences           = true;
    public bool buildBarrelsAndCrates = true;
    public int  hayBaleCount          = 10;

    [Header("Routes (phase >= 1)")]
    public bool  buildLanterns  = true;
    public float lanternSpacing = 6f;
    public bool  buildSignposts = true;

    [Header("Lac (phase >= 2)")]
    public bool buildPontoon     = true;
    public int  lilyPadCount     = 10;
    public int  reedClusterCount = 14;
    public bool buildLakePath    = true;

    [Header("Ambiance & détails")]
    public int  grassTuftCount      = 150;
    public int  boulderClusterCount = 10;
    public int  crystalClusterCount = 9;
    public int  ruinCount           = 2;
    public bool buildArenaDecor     = true;

    [Header("Props animés (lucioles, ciel, vide)")]
    public int fireflyClusterCount = 12;
    public int cloudCount          = 9;
    public int floatingIslandCount = 12;

    [Header("Néant / Trou noir (phase >= 2)")]
    public bool buildVoidTheme = true;

    // === BOIS ===
    private Material matWood;
    private Material matWoodDark;
    private Material matPlank;
    // === VÉGÉTATION ===
    private Material matLeaf;
    private Material matLeafLight;
    private Material matLeafDark;
    private Material matPineDark;
    // === FLEURS ===
    private Material matFlowerRed;
    private Material matFlowerYellow;
    private Material matFlowerWhite;
    private Material matFlowerViolet;
    // === CHAMPIGNONS ===
    private Material matMushroomRed;
    private Material matMushroomBrown;
    private Material matMushroomStem;
    // === PIERRE / SOL ===
    private Material matStone;
    private Material matStoneDark;
    private Material matPath;
    private Material matAsh;
    // === DIVERS ===
    private Material matHay;
    private Material matWellWater;
    private Material matLilyPad;
    private Material matLilyFlower;
    private Material matReed;
    private Material matReedTip;
    // === ÉMISSIFS ===
    private Material matLantern;
    private Material matFire;
    // === AMBIANCE ===
    private Material matMoss;
    private Material matRope;
    private Material matCloud;
    private Material matVoidRock;
    private Material matBannerRed;
    private Material matBannerBlue;
    private Material matGold;
    private Material matCrystalCyan;
    private Material matCrystalViolet;
    private Material matFirefly;
    // === NÉANT ===
    private Material matBlackHole;
    private Material matAccretion;
    private Material matAccretionHot;
    private Material matVoidGlow;
    private Material matGhostPurple;

    private VoidMapGeneratorGPU gen;
    private System.Random       rng;

    void OnValidate()
    {
        bushCount          = Mathf.Max(0, bushCount);
        flowerClusterCount = Mathf.Max(0, flowerClusterCount);
        mushroomClusterCount = Mathf.Max(0, mushroomClusterCount);
        stumpCount         = Mathf.Max(0, stumpCount);
        logCount           = Mathf.Max(0, logCount);
        pineCount          = Mathf.Max(0, pineCount);
        benchCount         = Mathf.Max(0, benchCount);
        campfireCount      = Mathf.Max(0, campfireCount);
        hayBaleCount       = Mathf.Max(0, hayBaleCount);
        lilyPadCount       = Mathf.Max(0, lilyPadCount);
        reedClusterCount   = Mathf.Max(0, reedClusterCount);
        lanternSpacing     = Mathf.Max(2f, lanternSpacing);
        grassTuftCount      = Mathf.Max(0, grassTuftCount);
        boulderClusterCount = Mathf.Max(0, boulderClusterCount);
        crystalClusterCount = Mathf.Max(0, crystalClusterCount);
        ruinCount           = Mathf.Max(0, ruinCount);
        fireflyClusterCount = Mathf.Max(0, fireflyClusterCount);
        cloudCount          = Mathf.Max(0, cloudCount);
        floatingIslandCount = Mathf.Max(0, floatingIslandCount);
    }

    // ──────────────────────────────────────────────────────────────────
    // Point d'entrée appelé par VoidMapGeneratorGPU.Generate()
    public Transform GenerateProps(VoidMapGeneratorGPU generator, Transform parent, System.Random random)
    {
        gen = generator;
        rng = random;
        BuildMaterials();

        Transform g = Group("Props", parent);

        // Le décor ne participe JAMAIS au NavMesh : on exclut tout le groupe
        // du bake (des milliers de petits meshes voxelisés = RAM et temps
        // de bake qui explosent pour rien).
        NavMeshModifier mod = g.gameObject.AddComponent<NavMeshModifier>();
        mod.ignoreFromBuild = true;

        BuildBushes(g);
        BuildFlowers(g);
        BuildMushrooms(g);
        BuildStumpsAndLogs(g);
        BuildPines(g);
        BuildBenches(g);
        BuildCampfires(g);
        BuildGrassTufts(g);
        BuildBoulders(g);
        BuildCrystals(g);
        BuildRuins(g);
        if (buildArenaDecor) BuildArenaDecor(g);

        if (gen.phase >= 1)
        {
            if (buildFences)           BuildFences(g);
            if (buildBarrelsAndCrates) BuildBarrelsAndCrates(g);
            if (buildWell)             BuildWell(g);
            if (buildLanterns)         BuildLanterns(g);
            if (buildSignposts)        BuildSignposts(g);
            BuildHayBales(g);
        }
        if (gen.phase >= 2)
        {
            if (buildPontoon)  BuildPontoon(g);
            BuildLilyPads(g);
            BuildReeds(g);
            if (buildLakePath) BuildLakePath(g);
        }

        // Props animés (houle légère via VoidPropFloat) : groupe séparé,
        // surtout PAS de static batching sinon ils ne bougeraient plus
        Transform gDyn = Group("PropsDynamic", parent);
        NavMeshModifier modDyn = gDyn.gameObject.AddComponent<NavMeshModifier>();
        modDyn.ignoreFromBuild = true;
        BuildFireflies(gDyn);
        BuildClouds(gDyn);
        BuildFloatingIslands(gDyn);

        // ── Thème du Néant : la planète se désagrège, le trou noir approche ──
        if (buildVoidTheme && gen.phase >= 2)
        {
            BuildCracks(g);
            BuildVoidDestruction(g, gDyn);
        }

        return g;
    }

    // ──────────────────────────────────────────────────────────────────
    void BuildMaterials()
    {
        Shader std = Shader.Find("Standard");

        // --- Bois ---
        matWood      = NewMat(std, new Color(0.38f, 0.24f, 0.14f), 0.14f);
        matWoodDark  = NewMat(std, new Color(0.24f, 0.14f, 0.08f), 0.10f);
        matPlank     = NewMat(std, new Color(0.52f, 0.36f, 0.20f), 0.16f);

        // --- Végétation ---
        matLeaf      = NewMat(std, new Color(0.18f, 0.46f, 0.15f), 0.10f);
        matLeafLight = NewMat(std, new Color(0.35f, 0.58f, 0.22f), 0.08f);
        matLeafDark  = NewMat(std, new Color(0.10f, 0.30f, 0.10f), 0.12f);
        matPineDark  = NewMat(std, new Color(0.07f, 0.27f, 0.14f), 0.10f);

        // --- Fleurs ---
        matFlowerRed    = NewMat(std, new Color(0.85f, 0.18f, 0.15f), 0.20f);
        matFlowerYellow = NewMat(std, new Color(0.95f, 0.80f, 0.20f), 0.20f);
        matFlowerWhite  = NewMat(std, new Color(0.95f, 0.95f, 0.90f), 0.20f);
        matFlowerViolet = NewMat(std, new Color(0.55f, 0.35f, 0.75f), 0.20f);

        // --- Champignons ---
        matMushroomRed   = NewMat(std, new Color(0.75f, 0.15f, 0.10f), 0.18f);
        matMushroomBrown = NewMat(std, new Color(0.55f, 0.38f, 0.20f), 0.14f);
        matMushroomStem  = NewMat(std, new Color(0.90f, 0.86f, 0.78f), 0.10f);

        // --- Pierre / sol ---
        matStone     = NewMat(std, new Color(0.68f, 0.65f, 0.60f), 0.22f);
        matStoneDark = NewMat(std, new Color(0.32f, 0.30f, 0.28f), 0.18f);
        matPath      = NewMat(std, new Color(0.60f, 0.52f, 0.40f), 0.06f);
        matAsh       = NewMat(std, new Color(0.15f, 0.14f, 0.13f), 0.05f);

        // --- Divers ---
        matHay        = NewMat(std, new Color(0.85f, 0.70f, 0.30f), 0.08f);
        matWellWater  = NewMat(std, new Color(0.10f, 0.30f, 0.50f), 0.85f);
        matLilyPad    = NewMat(std, new Color(0.20f, 0.55f, 0.25f), 0.15f);
        matLilyFlower = NewMat(std, new Color(0.95f, 0.55f, 0.75f), 0.25f);
        matReed       = NewMat(std, new Color(0.30f, 0.50f, 0.20f), 0.10f);
        matReedTip    = NewMat(std, new Color(0.45f, 0.30f, 0.15f), 0.10f);

        // --- Émissifs (lumières low-cost, sans Light) ---
        matLantern = NewMatEmissive(std, new Color(1.00f, 0.85f, 0.45f), 1.8f);
        matFire    = NewMatEmissive(std, new Color(1.00f, 0.45f, 0.10f), 2.2f);

        // --- Ambiance ---
        matMoss       = NewMat(std, new Color(0.25f, 0.45f, 0.20f), 0.08f);
        matRope       = NewMat(std, new Color(0.45f, 0.36f, 0.24f), 0.10f);
        matCloud      = NewMat(std, new Color(0.95f, 0.96f, 0.98f), 0.05f);
        matVoidRock   = NewMat(std, new Color(0.16f, 0.12f, 0.22f), 0.20f);   // roche du vide violacée
        matBannerRed  = NewMat(std, new Color(0.75f, 0.15f, 0.12f), 0.15f);
        matBannerBlue = NewMat(std, new Color(0.15f, 0.30f, 0.75f), 0.15f);
        matGold       = NewMat(std, new Color(0.95f, 0.78f, 0.25f), 0.55f);
        matCrystalCyan   = NewMatEmissive(std, new Color(0.30f, 0.90f, 1.00f), 1.6f);
        matCrystalViolet = NewMatEmissive(std, new Color(0.70f, 0.40f, 1.00f), 1.6f);
        matFirefly       = NewMatEmissive(std, new Color(0.75f, 1.00f, 0.35f), 2.5f);

        // --- Néant / trou noir ---
        matBlackHole    = NewMat(std, new Color(0.01f, 0.01f, 0.02f), 0.05f);
        matAccretion    = NewMatEmissive(std, new Color(1.00f, 0.55f, 0.15f), 2.2f);
        matAccretionHot = NewMatEmissive(std, new Color(1.00f, 0.92f, 0.70f), 3.0f);
        matVoidGlow     = NewMatEmissive(std, new Color(0.62f, 0.40f, 0.85f), 1.9f);   // lavande #8e5dbc
        matGhostPurple  = NewMatGhost(std, new Color(0.45f, 0.20f, 0.80f), 0.16f);

        // Corruption globale des props : tout le décor sombre vers le violet
        // profond du néant (#1f132b) à mesure que la phase avance, pour rester
        // cohérent avec le sol étoilé violet et le ciel corrompus.
        float corr = gen != null && gen.phase >= 2 ? Mathf.Pow((gen.phase - 1) / 4f, 1.7f) : 0f;
        if (corr > 0f)
        {
            Color voidC = new Color(0.12f, 0.07f, 0.17f);
            TintToVoid(matLeaf,         voidC, corr * 0.78f);
            TintToVoid(matLeafLight,    voidC, corr * 0.74f);
            TintToVoid(matLeafDark,     voidC, corr * 0.80f);
            TintToVoid(matPineDark,     voidC, corr * 0.78f);
            TintToVoid(matMoss,         voidC, corr * 0.78f);
            TintToVoid(matLilyPad,      voidC, corr * 0.74f);
            TintToVoid(matReed,         voidC, corr * 0.74f);
            TintToVoid(matReedTip,      voidC, corr * 0.70f);
            TintToVoid(matHay,          voidC, corr * 0.72f);
            TintToVoid(matFlowerRed,    voidC, corr * 0.78f);
            TintToVoid(matFlowerYellow, voidC, corr * 0.80f);
            TintToVoid(matFlowerWhite,  voidC, corr * 0.82f);
            TintToVoid(matFlowerViolet, voidC, corr * 0.78f);
            TintToVoid(matMushroomRed,  voidC, corr * 0.72f);
            TintToVoid(matMushroomBrown,voidC, corr * 0.72f);
            TintToVoid(matMushroomStem, voidC, corr * 0.70f);
            TintToVoid(matWood,         voidC, corr * 0.55f);
            TintToVoid(matWoodDark,     voidC, corr * 0.60f);
            TintToVoid(matPlank,        voidC, corr * 0.50f);
        }
    }

    void TintToVoid(Material m, Color target, float t)
    {
        Color c = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
        c = Color.Lerp(c, target, Mathf.Clamp01(t));
        if (m.HasProperty("_Color"))     m.SetColor("_Color", c);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
    }

    Material NewMat(Shader shader, Color color, float smooth)
    {
        Material m = new Material(shader);
        if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color"))      m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        m.enableInstancing = true;
        return m;
    }

    Material NewMatEmissive(Shader shader, Color color, float intensity)
    {
        Material m = NewMat(shader, color, 0.4f);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * intensity);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return m;
    }

    // Matériau fantôme semi-transparent (halo du trou noir, rayon de siphon)
    Material NewMatGhost(Shader shader, Color color, float alpha)
    {
        Material m = new Material(shader);
        Color c = new Color(color.r, color.g, color.b, alpha);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))     m.SetColor("_Color", c);
        m.renderQueue = 3000;
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_ALPHABLEND_ON");
        m.enableInstancing = true;
        return m;
    }

    // ──────────────────────────────────────────────────────────────────
    // Helpers de placement
    float R01() => (float)rng.NextDouble();

    Vector3 RandomPoint(float minR, float maxR)
    {
        float ang = R01() * Mathf.PI * 2f;
        float rad = Mathf.Lerp(minR, maxR, Mathf.Sqrt(R01()));
        return new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
    }

    // Un emplacement est libre s'il évite l'arène, la côte, le lac,
    // l'enceinte du château, les routes et les maisons.
    bool IsFree(Vector3 pos)
    {
        float r = new Vector2(pos.x, pos.z).magnitude;
        if (r < gen.parkRadius + 1.2f)      return false;
        if (r > gen.islandRadius - 2.5f)    return false;
        if (gen.IsInsideLake(pos))          return false;
        if (gen.phase >= 3 && Mathf.Abs(r - gen.castleOuterRadius) < 2.2f) return false;
        if (IsOnRoad(pos))                  return false;
        if (NearHouse(pos, 2.4f))           return false;
        if (gen.IsInVoidBite(pos))          return false;
        return true;
    }

    bool IsOnRoad(Vector3 pos)
    {
        if (gen.phase < 1) return false;
        float half = gen.roadWidth * 0.5f + 0.35f;

        // Anneau du village
        float ringR = gen.GetVillageRadius() * 0.92f;
        float r = new Vector2(pos.x, pos.z).magnitude;
        if (Mathf.Abs(r - ringR) < half) return true;

        // Branches radiales
        for (int i = 0; i < gen.lastRoadBranches.Count; i++)
        {
            Vector2 branch = gen.lastRoadBranches[i];
            Vector3 dir    = Quaternion.Euler(0f, branch.x, 0f) * Vector3.forward;
            float   along  = Vector3.Dot(new Vector3(pos.x, 0f, pos.z), dir);
            if (along < 0f || along > branch.y) continue;
            Vector3 lateral = new Vector3(pos.x, 0f, pos.z) - dir * along;
            if (lateral.magnitude < half) return true;
        }
        return false;
    }

    bool NearHouse(Vector3 pos, float dist)
    {
        for (int i = 0; i < gen.lastHousePositions.Count; i++)
        {
            Vector3 h = gen.lastHousePositions[i];
            float dx = pos.x - h.x, dz = pos.z - h.z;
            if (dx * dx + dz * dz < dist * dist) return true;
        }
        return false;
    }

    Transform Group(string gName, Transform parent)
    {
        GameObject go = new GameObject(gName);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.layer = parent.gameObject.layer;
        return go.transform;
    }

    // Pivot vide posé au sol (pour les props composés de plusieurs primitives)
    Transform Pivot(string pName, Transform parent, Vector3 worldPos, float yaw)
    {
        GameObject go = new GameObject(pName);
        go.transform.SetParent(parent);
        go.layer = parent.gameObject.layer;
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        return go.transform;
    }

    GameObject PrimLocal(PrimitiveType type, string pName, Transform parent,
                         Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name  = pName;
        go.transform.SetParent(parent);
        go.layer = parent.gameObject.layer;
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale    = localScale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        SafeRemoveCollider(go.GetComponent<Collider>());
        return go;
    }

    GameObject Prim(PrimitiveType type, string pName, Transform parent,
                    Vector3 worldPos, Vector3 scale, Quaternion rot, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name  = pName;
        go.transform.SetParent(parent);
        go.layer = parent.gameObject.layer;
        go.transform.position   = worldPos;
        go.transform.rotation   = rot;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        SafeRemoveCollider(go.GetComponent<Collider>());
        return go;
    }

    void SafeRemoveCollider(Component c)
    {
        if (c == null) return;
        if (Application.isPlaying) Destroy(c);
        else DestroyImmediate(c);
    }

    // ──────────────────────────────────────────────────────────────────
    // BUISSONS : 1 à 3 sphères aplaties, parfois quelques baies rouges
    void BuildBushes(Transform parent)
    {
        Transform g = Group("Bushes", parent);
        int placed = 0, guard = bushCount * 8;
        while (placed < bushCount && guard-- > 0)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 1.5f, gen.islandRadius - 3f);
            if (!IsFree(pos)) continue;
            float gy   = gen.TerrainHeight(pos.x, pos.z);
            float size = Mathf.Lerp(bushSizeRange.x, bushSizeRange.y, R01());
            int   style = (int)(R01() * 3);
            Material leaf = style == 0 ? matLeafDark : style == 1 ? matLeaf : matLeafLight;

            Transform bush = Pivot("Bush", g, new Vector3(pos.x, gy, pos.z), R01() * 360f);

            int blobs = 1 + (int)(R01() * 3f);
            for (int b = 0; b < blobs; b++)
            {
                float bs = size * Mathf.Lerp(0.6f, 1f, R01());
                Vector3 off = new Vector3((R01() - 0.5f) * size * 0.8f, bs * 0.40f,
                                          (R01() - 0.5f) * size * 0.8f);
                PrimLocal(PrimitiveType.Sphere, "BushBlob", bush,
                          off, new Vector3(bs, bs * 0.75f, bs), Quaternion.identity, leaf);
            }

            // Quelques baies sur ~1 buisson sur 5
            if (R01() < 0.22f)
            {
                int berries = 3 + (int)(R01() * 3f);
                for (int b = 0; b < berries; b++)
                {
                    Vector3 off = new Vector3((R01() - 0.5f) * size * 0.7f,
                                              size * Mathf.Lerp(0.35f, 0.60f, R01()),
                                              (R01() - 0.5f) * size * 0.7f);
                    PrimLocal(PrimitiveType.Sphere, "Berry", bush,
                              off, Vector3.one * 0.06f, Quaternion.identity, matFlowerRed);
                }
            }
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // FLEURS : clusters tige (cylindre) + corolle (sphère colorée)
    void BuildFlowers(Transform parent)
    {
        Transform g = Group("Flowers", parent);
        Material[] petals = { matFlowerRed, matFlowerYellow, matFlowerWhite, matFlowerViolet };

        int placed = 0, guard = flowerClusterCount * 8;
        while (placed < flowerClusterCount && guard-- > 0)
        {
            Vector3 center = RandomPoint(gen.parkRadius + 1.5f, gen.islandRadius - 4f);
            if (!IsFree(center)) continue;
            // Les fleurs préfèrent les zones basses (herbe)
            if (gen.TerrainHeight(center.x, center.z) > gen.islandMaxHeight * 0.55f) continue;

            Material petal = petals[(int)(R01() * petals.Length) % petals.Length];
            Transform cluster = Pivot("FlowerCluster", g,
                new Vector3(center.x, 0f, center.z), 0f);

            int count = Mathf.RoundToInt(Mathf.Lerp(flowersPerCluster.x, flowersPerCluster.y, R01()));
            for (int f = 0; f < count; f++)
            {
                float fx = center.x + (R01() - 0.5f) * 1.2f;
                float fz = center.z + (R01() - 0.5f) * 1.2f;
                float gy = gen.TerrainHeight(fx, fz);
                float h  = Mathf.Lerp(0.14f, 0.24f, R01());

                Prim(PrimitiveType.Cylinder, "FlowerStem", cluster,
                     new Vector3(fx, gy + h * 0.5f, fz),
                     new Vector3(0.025f, h * 0.5f, 0.025f), Quaternion.identity, matLeaf);
                Prim(PrimitiveType.Sphere, "FlowerHead", cluster,
                     new Vector3(fx, gy + h + 0.03f, fz),
                     Vector3.one * Mathf.Lerp(0.07f, 0.11f, R01()), Quaternion.identity, petal);
            }
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // CHAMPIGNONS : pied crème + chapeau aplati rouge ou brun
    void BuildMushrooms(Transform parent)
    {
        Transform g = Group("Mushrooms", parent);
        int placed = 0, guard = mushroomClusterCount * 8;
        while (placed < mushroomClusterCount && guard-- > 0)
        {
            Vector3 center = RandomPoint(gen.parkRadius + 2.5f, gen.islandRadius - 4f);
            if (!IsFree(center)) continue;

            Material cap = R01() < 0.5f ? matMushroomRed : matMushroomBrown;
            Transform cluster = Pivot("MushroomCluster", g,
                new Vector3(center.x, 0f, center.z), 0f);

            int count = 2 + (int)(R01() * 3f);
            for (int m = 0; m < count; m++)
            {
                float mx = center.x + (R01() - 0.5f) * 0.7f;
                float mz = center.z + (R01() - 0.5f) * 0.7f;
                float gy = gen.TerrainHeight(mx, mz);
                float s  = Mathf.Lerp(0.6f, 1.2f, R01());

                Prim(PrimitiveType.Cylinder, "MushroomStem", cluster,
                     new Vector3(mx, gy + 0.06f * s, mz),
                     new Vector3(0.05f, 0.06f, 0.05f) * s, Quaternion.identity, matMushroomStem);
                Prim(PrimitiveType.Sphere, "MushroomCap", cluster,
                     new Vector3(mx, gy + 0.13f * s, mz),
                     new Vector3(0.17f, 0.09f, 0.17f) * s, Quaternion.identity, cap);
            }
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // SOUCHES & TRONCS couchés (forêt vécue)
    void BuildStumpsAndLogs(Transform parent)
    {
        Transform g = Group("StumpsAndLogs", parent);

        int placed = 0, guard = stumpCount * 8;
        while (placed < stumpCount && guard-- > 0)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 2.5f, gen.islandRadius - 4f);
            if (!IsFree(pos)) continue;
            float gy = gen.TerrainHeight(pos.x, pos.z);
            float s  = Mathf.Lerp(0.8f, 1.3f, R01());

            Transform stump = Pivot("Stump", g, new Vector3(pos.x, gy, pos.z), R01() * 360f);
            PrimLocal(PrimitiveType.Cylinder, "StumpBody", stump,
                      new Vector3(0f, 0.14f * s, 0f),
                      new Vector3(0.34f, 0.14f, 0.34f) * s, Quaternion.identity, matWood);
            // Disque plus clair sur le dessus (coupe)
            PrimLocal(PrimitiveType.Cylinder, "StumpTop", stump,
                      new Vector3(0f, 0.285f * s, 0f),
                      new Vector3(0.29f, 0.008f, 0.29f) * s, Quaternion.identity, matPlank);
            placed++;
        }

        placed = 0; guard = logCount * 8;
        while (placed < logCount && guard-- > 0)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 2.5f, gen.islandRadius - 4f);
            if (!IsFree(pos)) continue;
            float gy  = gen.TerrainHeight(pos.x, pos.z);
            float len = Mathf.Lerp(1.2f, 2.2f, R01());
            float yaw = R01() * 360f;

            Prim(PrimitiveType.Cylinder, "FallenLog", g,
                 new Vector3(pos.x, gy + 0.11f, pos.z),
                 new Vector3(0.22f, len * 0.5f, 0.22f),
                 Quaternion.Euler(90f, yaw, 0f),
                 R01() < 0.5f ? matWood : matWoodDark);
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // SAPINS : tronc + étages de cubes tournés à 45° (style low-poly)
    void BuildPines(Transform parent)
    {
        Transform g = Group("Pines", parent);
        int placed = 0, guard = pineCount * 14;
        while (placed < pineCount && guard-- > 0)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 3f, gen.islandRadius - 4f);
            if (!IsFree(pos)) continue;
            float gy = gen.TerrainHeight(pos.x, pos.z);
            // Les sapins poussent sur les hauteurs
            if (gy < gen.islandMaxHeight * 0.35f) continue;

            float h = Mathf.Lerp(pineHeightRange.x, pineHeightRange.y, R01());
            Transform pine = Pivot("Pine", g, new Vector3(pos.x, gy, pos.z), R01() * 360f);

            float trunkH = h * 0.22f;
            PrimLocal(PrimitiveType.Cylinder, "PineTrunk", pine,
                      new Vector3(0f, trunkH * 0.5f, 0f),
                      new Vector3(h * 0.05f, trunkH * 0.5f, h * 0.05f),
                      Quaternion.identity, matWoodDark);

            // 4 étages de feuillage, de plus en plus petits, tournés en quinconce
            float[] layerSizes = { 1.00f, 0.78f, 0.55f, 0.32f };
            for (int l = 0; l < layerSizes.Length; l++)
            {
                float ls = h * 0.34f * layerSizes[l];
                PrimLocal(PrimitiveType.Cube, "PineLayer_" + l, pine,
                          new Vector3(0f, trunkH + h * 0.17f * (l + 0.6f), 0f),
                          new Vector3(ls, h * 0.14f, ls),
                          Quaternion.Euler(0f, l * 45f, 0f), matPineDark);
            }
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // BANCS autour de l'arène, tournés vers le centre
    void BuildBenches(Transform parent)
    {
        if (benchCount <= 0) return;
        Transform g = Group("Benches", parent);
        float rad = gen.parkRadius - 0.7f;
        for (int i = 0; i < benchCount; i++)
        {
            float ang = i * (Mathf.PI * 2f / benchCount) + (R01() - 0.5f) * 0.35f;
            Vector3 pos = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
            float gy = gen.TerrainHeight(pos.x, pos.z);

            float yaw = Mathf.Atan2(-pos.x, -pos.z) * Mathf.Rad2Deg; // face au centre
            Transform bench = Pivot("Bench", g, new Vector3(pos.x, gy, pos.z), yaw);

            PrimLocal(PrimitiveType.Cube, "BenchSeat", bench,
                      new Vector3(0f, 0.40f, 0f), new Vector3(0.90f, 0.06f, 0.32f),
                      Quaternion.identity, matPlank);
            PrimLocal(PrimitiveType.Cube, "BenchLegL", bench,
                      new Vector3(-0.36f, 0.19f, 0f), new Vector3(0.07f, 0.38f, 0.30f),
                      Quaternion.identity, matWoodDark);
            PrimLocal(PrimitiveType.Cube, "BenchLegR", bench,
                      new Vector3(0.36f, 0.19f, 0f), new Vector3(0.07f, 0.38f, 0.30f),
                      Quaternion.identity, matWoodDark);
            PrimLocal(PrimitiveType.Cube, "BenchBack", bench,
                      new Vector3(0f, 0.66f, -0.16f), new Vector3(0.90f, 0.32f, 0.05f),
                      Quaternion.Euler(-8f, 0f, 0f), matPlank);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // FEUX DE CAMP : cercle de pierres + bûches croisées + braise émissive
    void BuildCampfires(Transform parent)
    {
        if (campfireCount <= 0) return;
        Transform g = Group("Campfires", parent);
        int placed = 0, guard = campfireCount * 30;
        while (placed < campfireCount && guard-- > 0)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 5f, gen.islandRadius - 8f);
            if (!IsFree(pos)) continue;
            float gy = gen.TerrainHeight(pos.x, pos.z);
            Transform fire = Pivot("Campfire", g, new Vector3(pos.x, gy, pos.z), R01() * 360f);

            // Cendres au sol
            PrimLocal(PrimitiveType.Cylinder, "FireAsh", fire,
                      new Vector3(0f, 0.01f, 0f), new Vector3(0.55f, 0.012f, 0.55f),
                      Quaternion.identity, matAsh);

            // Cercle de pierres
            int rocks = 7;
            for (int r = 0; r < rocks; r++)
            {
                float a  = (float)r / rocks * Mathf.PI * 2f;
                float rs = Mathf.Lerp(0.10f, 0.16f, R01());
                PrimLocal(PrimitiveType.Cube, "FireRock", fire,
                          new Vector3(Mathf.Cos(a) * 0.48f, rs * 0.4f, Mathf.Sin(a) * 0.48f),
                          new Vector3(rs, rs * 0.8f, rs),
                          Quaternion.Euler(0f, R01() * 360f, 0f), matStone);
            }

            // Deux bûches croisées
            PrimLocal(PrimitiveType.Cylinder, "FireLog1", fire,
                      new Vector3(0f, 0.10f, 0f), new Vector3(0.09f, 0.34f, 0.09f),
                      Quaternion.Euler(90f, 25f, 0f), matWoodDark);
            PrimLocal(PrimitiveType.Cylinder, "FireLog2", fire,
                      new Vector3(0f, 0.15f, 0f), new Vector3(0.09f, 0.34f, 0.09f),
                      Quaternion.Euler(90f, 115f, 0f), matWoodDark);

            // Braise + flamme stylisée (émissif, pas de Light => gratuit)
            PrimLocal(PrimitiveType.Sphere, "FireEmber", fire,
                      new Vector3(0f, 0.16f, 0f), new Vector3(0.30f, 0.16f, 0.30f),
                      Quaternion.identity, matFire);
            PrimLocal(PrimitiveType.Sphere, "FireFlame", fire,
                      new Vector3(0f, 0.30f, 0f), new Vector3(0.16f, 0.26f, 0.16f),
                      Quaternion.identity, matLantern);
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // CLÔTURES : petits potagers à côté d'une maison sur trois
    void BuildFences(Transform parent)
    {
        Transform g = Group("Fences", parent);
        float half = 1.1f;

        for (int i = 0; i < gen.lastHousePositions.Count; i += 3)
        {
            Vector3 house = gen.lastHousePositions[i];
            float ang = R01() * Mathf.PI * 2f;
            Vector3 center = house + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 3.4f;
            // Le potager peut être proche de SA maison, mais pas sur une route/lac
            float r = new Vector2(center.x, center.z).magnitude;
            if (r < gen.parkRadius + 1.5f || r > gen.islandRadius - 3f) continue;
            if (gen.IsInsideLake(center) || IsOnRoad(center))           continue;

            float gy = gen.TerrainHeight(center.x, center.z);
            Transform fence = Pivot("GardenFence", g, new Vector3(center.x, gy, center.z), 0f);

            // 8 poteaux : coins + milieux de côtés
            for (int px = -1; px <= 1; px++)
            for (int pz = -1; pz <= 1; pz++)
            {
                if (px == 0 && pz == 0) continue;
                PrimLocal(PrimitiveType.Cube, "FencePost", fence,
                          new Vector3(px * half, 0.28f, pz * half),
                          new Vector3(0.07f, 0.56f, 0.07f), Quaternion.identity, matWood);
            }

            // 2 traverses par côté
            for (int side = 0; side < 4; side++)
            {
                bool  alongX = side < 2;
                float sign   = (side % 2 == 0) ? 1f : -1f;
                Vector3 railScale = alongX
                    ? new Vector3(half * 2f + 0.10f, 0.05f, 0.05f)
                    : new Vector3(0.05f, 0.05f, half * 2f + 0.10f);
                Vector3 railPos = alongX
                    ? new Vector3(0f, 0f, sign * half)
                    : new Vector3(sign * half, 0f, 0f);
                PrimLocal(PrimitiveType.Cube, "FenceRailLow", fence,
                          railPos + Vector3.up * 0.24f, railScale, Quaternion.identity, matWood);
                PrimLocal(PrimitiveType.Cube, "FenceRailHigh", fence,
                          railPos + Vector3.up * 0.44f, railScale, Quaternion.identity, matWood);
            }

            // Rangées de légumes à l'intérieur
            for (int row = -1; row <= 1; row++)
            for (int col = -1; col <= 1; col++)
            {
                if (R01() < 0.25f) continue;
                float vs = Mathf.Lerp(0.14f, 0.22f, R01());
                PrimLocal(PrimitiveType.Cube, "Crop", fence,
                          new Vector3(col * 0.55f, vs * 0.5f, row * 0.55f),
                          new Vector3(vs * 1.4f, vs, vs * 1.4f),
                          Quaternion.Euler(0f, R01() * 90f, 0f), matLeafLight);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // TONNEAUX & CAISSES contre une maison sur deux
    void BuildBarrelsAndCrates(Transform parent)
    {
        Transform g = Group("BarrelsAndCrates", parent);
        for (int i = 1; i < gen.lastHousePositions.Count; i += 2)
        {
            Vector3 house = gen.lastHousePositions[i];
            int items = 1 + (int)(R01() * 3f);
            for (int n = 0; n < items; n++)
            {
                float ang = R01() * Mathf.PI * 2f;
                Vector3 pos = house + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang))
                                      * Mathf.Lerp(2.5f, 3.1f, R01());
                float r = new Vector2(pos.x, pos.z).magnitude;
                if (r < gen.parkRadius + 1.5f || r > gen.islandRadius - 3f) continue;
                if (gen.IsInsideLake(pos) || IsOnRoad(pos))                 continue;

                float gy = gen.TerrainHeight(pos.x, pos.z);
                if (R01() < 0.5f)
                {
                    // Tonneau : cylindre + deux cerclages sombres
                    Transform barrel = Pivot("Barrel", g, new Vector3(pos.x, gy, pos.z), R01() * 360f);
                    PrimLocal(PrimitiveType.Cylinder, "BarrelBody", barrel,
                              new Vector3(0f, 0.32f, 0f), new Vector3(0.45f, 0.32f, 0.45f),
                              Quaternion.identity, matWood);
                    PrimLocal(PrimitiveType.Cylinder, "BarrelRingLow", barrel,
                              new Vector3(0f, 0.15f, 0f), new Vector3(0.47f, 0.02f, 0.47f),
                              Quaternion.identity, matWoodDark);
                    PrimLocal(PrimitiveType.Cylinder, "BarrelRingHigh", barrel,
                              new Vector3(0f, 0.50f, 0f), new Vector3(0.47f, 0.02f, 0.47f),
                              Quaternion.identity, matWoodDark);
                }
                else
                {
                    // Caisse : cube + couvercle légèrement débordant
                    float s = Mathf.Lerp(0.42f, 0.58f, R01());
                    Transform crate = Pivot("Crate", g, new Vector3(pos.x, gy, pos.z), R01() * 360f);
                    PrimLocal(PrimitiveType.Cube, "CrateBody", crate,
                              new Vector3(0f, s * 0.5f, 0f), new Vector3(s, s, s),
                              Quaternion.identity, matPlank);
                    PrimLocal(PrimitiveType.Cube, "CrateLid", crate,
                              new Vector3(0f, s + 0.025f, 0f), new Vector3(s * 1.06f, 0.05f, s * 1.06f),
                              Quaternion.identity, matWoodDark);
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // PUITS au cœur du village
    void BuildWell(Transform parent)
    {
        float vr = gen.GetVillageRadius();
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 2.5f, Mathf.Max(gen.parkRadius + 3.5f, vr - 1.5f));
            if (!IsFree(pos)) continue;
            if (NearHouse(pos, 2.8f)) continue;

            float gy = gen.TerrainHeight(pos.x, pos.z);
            Transform well = Pivot("Well", Group("Well", parent),
                                   new Vector3(pos.x, gy, pos.z), R01() * 360f);

            // Margelle en pierre + eau sombre
            PrimLocal(PrimitiveType.Cylinder, "WellBase", well,
                      new Vector3(0f, 0.35f, 0f), new Vector3(1.30f, 0.35f, 1.30f),
                      Quaternion.identity, matStone);
            PrimLocal(PrimitiveType.Cylinder, "WellWater", well,
                      new Vector3(0f, 0.71f, 0f), new Vector3(1.00f, 0.01f, 1.00f),
                      Quaternion.identity, matWellWater);

            // Couronne de pierres sombres sur la margelle
            int stones = 8;
            for (int s = 0; s < stones; s++)
            {
                float a = (float)s / stones * Mathf.PI * 2f;
                PrimLocal(PrimitiveType.Cube, "WellRim", well,
                          new Vector3(Mathf.Cos(a) * 0.60f, 0.76f, Mathf.Sin(a) * 0.60f),
                          new Vector3(0.24f, 0.14f, 0.24f),
                          Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f), matStoneDark);
            }

            // Montants + traverse + petit toit
            PrimLocal(PrimitiveType.Cube, "WellPostL", well,
                      new Vector3(-0.65f, 1.15f, 0f), new Vector3(0.08f, 0.90f, 0.08f),
                      Quaternion.identity, matWoodDark);
            PrimLocal(PrimitiveType.Cube, "WellPostR", well,
                      new Vector3(0.65f, 1.15f, 0f), new Vector3(0.08f, 0.90f, 0.08f),
                      Quaternion.identity, matWoodDark);
            PrimLocal(PrimitiveType.Cylinder, "WellAxle", well,
                      new Vector3(0f, 1.38f, 0f), new Vector3(0.06f, 0.66f, 0.06f),
                      Quaternion.Euler(0f, 0f, 90f), matWood);
            PrimLocal(PrimitiveType.Cube, "WellRoofL", well,
                      new Vector3(-0.28f, 1.78f, 0f), new Vector3(0.75f, 0.05f, 1.05f),
                      Quaternion.Euler(0f, 0f, 30f), matWoodDark);
            PrimLocal(PrimitiveType.Cube, "WellRoofR", well,
                      new Vector3(0.28f, 1.78f, 0f), new Vector3(0.75f, 0.05f, 1.05f),
                      Quaternion.Euler(0f, 0f, -30f), matWoodDark);
            return;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // LANTERNES le long des routes radiales, en quinconce
    void BuildLanterns(Transform parent)
    {
        Transform g = Group("Lanterns", parent);
        float sideOffset = gen.roadWidth * 0.5f + 0.45f;

        for (int b = 0; b < gen.lastRoadBranches.Count; b++)
        {
            Vector2 branch = gen.lastRoadBranches[b];
            Vector3 dir  = Quaternion.Euler(0f, branch.x, 0f) * Vector3.forward;
            Vector3 perp = new Vector3(dir.z, 0f, -dir.x);
            int k = 0;
            for (float d = 3.5f; d < branch.y - 0.5f; d += lanternSpacing, k++)
            {
                float side = (k % 2 == 0) ? 1f : -1f;
                Vector3 pos = dir * d + perp * side * sideOffset;
                if (gen.IsInsideLake(pos)) continue;
                if (pos.magnitude > gen.islandRadius - 3f) continue;
                if (NearHouse(pos, 1.4f)) continue;   // pas de lanterne dans un mur

                float gy = gen.TerrainHeight(pos.x, pos.z);
                Transform lantern = Pivot("Lantern", g, new Vector3(pos.x, gy, pos.z), branch.x);

                PrimLocal(PrimitiveType.Cylinder, "LanternPost", lantern,
                          new Vector3(0f, 0.62f, 0f), new Vector3(0.07f, 0.62f, 0.07f),
                          Quaternion.identity, matWoodDark);
                PrimLocal(PrimitiveType.Cube, "LanternGlow", lantern,
                          new Vector3(0f, 1.34f, 0f), Vector3.one * 0.20f,
                          Quaternion.Euler(0f, 45f, 0f), matLantern);
                PrimLocal(PrimitiveType.Cube, "LanternCap", lantern,
                          new Vector3(0f, 1.48f, 0f), new Vector3(0.27f, 0.05f, 0.27f),
                          Quaternion.Euler(0f, 45f, 0f), matWoodDark);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // PANNEAUX indicateurs sur les premières branches de route
    void BuildSignposts(Transform parent)
    {
        Transform g = Group("Signposts", parent);
        float vr = gen.GetVillageRadius();
        int count = Mathf.Min(3, gen.lastRoadBranches.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2 branch = gen.lastRoadBranches[i];
            Vector3 dir  = Quaternion.Euler(0f, branch.x, 0f) * Vector3.forward;
            Vector3 perp = new Vector3(dir.z, 0f, -dir.x);
            float d = Mathf.Min(branch.y - 1f, vr * 0.6f);
            Vector3 pos = dir * d + perp * (gen.roadWidth * 0.5f + 0.5f);
            if (gen.IsInsideLake(pos)) continue;
            if (NearHouse(pos, 1.6f))  continue;

            float gy = gen.TerrainHeight(pos.x, pos.z);
            Transform sign = Pivot("Signpost", g, new Vector3(pos.x, gy, pos.z), branch.x + 90f);

            PrimLocal(PrimitiveType.Cylinder, "SignPost", sign,
                      new Vector3(0f, 0.60f, 0f), new Vector3(0.07f, 0.60f, 0.07f),
                      Quaternion.identity, matWoodDark);
            PrimLocal(PrimitiveType.Cube, "SignBoardTop", sign,
                      new Vector3(0.18f, 1.05f, 0f), new Vector3(0.72f, 0.14f, 0.04f),
                      Quaternion.Euler(0f, (R01() - 0.5f) * 30f, 0f), matPlank);
            PrimLocal(PrimitiveType.Cube, "SignBoardBottom", sign,
                      new Vector3(-0.14f, 0.85f, 0f), new Vector3(0.62f, 0.13f, 0.04f),
                      Quaternion.Euler(0f, 180f + (R01() - 0.5f) * 30f, 0f), matPlank);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // BOTTES DE FOIN dans les champs autour du village
    void BuildHayBales(Transform parent)
    {
        if (hayBaleCount <= 0) return;
        Transform g = Group("HayBales", parent);
        float vr = gen.GetVillageRadius();
        int placed = 0, guard = hayBaleCount * 12;
        while (placed < hayBaleCount && guard-- > 0)
        {
            Vector3 pos = RandomPoint(vr + 2f, Mathf.Min(vr + 8f, gen.islandRadius - 4f));
            if (!IsFree(pos)) continue;
            float gy = gen.TerrainHeight(pos.x, pos.z);
            Prim(PrimitiveType.Cylinder, "HayBale", g,
                 new Vector3(pos.x, gy + 0.28f, pos.z),
                 new Vector3(0.55f, 0.40f, 0.55f),
                 Quaternion.Euler(90f, R01() * 360f, 0f), matHay);
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // PONTON en bois sur la rive du lac (côté arène)
    void BuildPontoon(Transform parent)
    {
        Transform g = Group("Pontoon", parent);
        Vector3 lakeC = new Vector3(gen.lakeOffset.x, 0f, gen.lakeOffset.y);
        float   wy    = gen.TerrainHeight(gen.lakeOffset.x, gen.lakeOffset.y) + 0.02f;

        Vector3 dirToCenter = (-lakeC).normalized;             // vers le centre de l'île
        Vector3 shore       = lakeC + dirToCenter * gen.lakeRadius;
        Vector3 dirIn       = -dirToCenter;                    // vers le large
        Quaternion deckRot  = Quaternion.LookRotation(dirIn);

        int planks = Mathf.CeilToInt(gen.lakeRadius * 0.55f / 0.5f);
        for (int i = 0; i < planks; i++)
        {
            Vector3 p = shore + dirIn * (0.25f + i * 0.5f);
            Prim(PrimitiveType.Cube, "PontoonPlank", g,
                 new Vector3(p.x, wy + 0.16f, p.z),
                 new Vector3(0.90f, 0.06f, 0.46f), deckRot, matPlank);

            // Pilotis tous les 2 planches
            if (i % 2 == 0)
            {
                Vector3 perp = new Vector3(dirIn.z, 0f, -dirIn.x);
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 postP = p + perp * 0.42f * s;
                    Prim(PrimitiveType.Cylinder, "PontoonPost", g,
                         new Vector3(postP.x, wy - 0.06f, postP.z),
                         new Vector3(0.08f, 0.38f, 0.08f), Quaternion.identity, matWoodDark);
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // NÉNUPHARS sur le lac, certains avec une fleur rose
    void BuildLilyPads(Transform parent)
    {
        if (lilyPadCount <= 0) return;
        Transform g = Group("LilyPads", parent);
        Vector3 lakeC = new Vector3(gen.lakeOffset.x, 0f, gen.lakeOffset.y);
        float   wy    = gen.TerrainHeight(gen.lakeOffset.x, gen.lakeOffset.y) + 0.02f;

        for (int i = 0; i < lilyPadCount; i++)
        {
            float ang = R01() * Mathf.PI * 2f;
            float rad = Mathf.Sqrt(R01()) * gen.lakeRadius * 0.75f;
            Vector3 pos = lakeC + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
            float s = Mathf.Lerp(0.30f, 0.60f, R01());

            Prim(PrimitiveType.Cylinder, "LilyPad", g,
                 new Vector3(pos.x, wy + 0.115f, pos.z),
                 new Vector3(s, 0.012f, s),
                 Quaternion.Euler(0f, R01() * 360f, 0f), matLilyPad);

            if (R01() < 0.35f)
                Prim(PrimitiveType.Sphere, "LilyFlower", g,
                     new Vector3(pos.x, wy + 0.17f, pos.z),
                     Vector3.one * 0.10f, Quaternion.identity, matLilyFlower);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // ROSEAUX en touffes sur la rive du lac
    void BuildReeds(Transform parent)
    {
        if (reedClusterCount <= 0) return;
        Transform g = Group("Reeds", parent);
        Vector3 lakeC = new Vector3(gen.lakeOffset.x, 0f, gen.lakeOffset.y);
        float   wy    = gen.TerrainHeight(gen.lakeOffset.x, gen.lakeOffset.y) + 0.02f;

        for (int i = 0; i < reedClusterCount; i++)
        {
            float clusterAng = R01() * Mathf.PI * 2f;
            float clusterRad = gen.lakeRadius + Mathf.Lerp(0.15f, 0.60f, R01());
            Vector3 center = lakeC + new Vector3(Mathf.Cos(clusterAng) * clusterRad, 0f,
                                                 Mathf.Sin(clusterAng) * clusterRad);

            int reeds = 3 + (int)(R01() * 4f);
            for (int rIdx = 0; rIdx < reeds; rIdx++)
            {
                float rx = center.x + (R01() - 0.5f) * 0.5f;
                float rz = center.z + (R01() - 0.5f) * 0.5f;
                float baseY = Mathf.Max(wy, gen.TerrainHeight(rx, rz));
                float h = Mathf.Lerp(0.55f, 1.0f, R01());
                Quaternion tilt = Quaternion.Euler((R01() - 0.5f) * 12f, R01() * 360f,
                                                   (R01() - 0.5f) * 12f);

                Prim(PrimitiveType.Cylinder, "Reed", g,
                     new Vector3(rx, baseY + h * 0.5f, rz),
                     new Vector3(0.025f, h * 0.5f, 0.025f), tilt, matReed);
                // Quenouille brune au sommet
                Prim(PrimitiveType.Cylinder, "ReedTip", g,
                     new Vector3(rx, baseY + h + 0.06f, rz),
                     new Vector3(0.05f, 0.08f, 0.05f), tilt, matReedTip);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // CHEMIN DE PIERRES sinueux reliant l'anneau du village au ponton
    void BuildLakePath(Transform parent)
    {
        Transform g = Group("LakePath", parent);
        Vector3 lakeC   = new Vector3(gen.lakeOffset.x, 0f, gen.lakeOffset.y);
        Vector3 lakeDir = lakeC.normalized;

        Vector3 start = lakeDir * (gen.GetVillageRadius() * 0.92f);
        Vector3 end   = lakeC - lakeDir * (gen.lakeRadius + 0.4f);
        Vector3 perp  = new Vector3(lakeDir.z, 0f, -lakeDir.x);
        float   dist  = (end - start).magnitude;
        if (dist < 1.5f) return;

        int steps = Mathf.CeilToInt(dist / 0.7f);
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 pos = Vector3.Lerp(start, end, t)
                        + perp * Mathf.Sin(t * Mathf.PI * 3f) * 0.5f;
            // On ne pose pas de pierre sur la route ni dans l'eau
            if (IsOnRoad(pos)) continue;
            if ((new Vector2(pos.x, pos.z) - new Vector2(lakeC.x, lakeC.z)).magnitude
                < gen.lakeRadius - 0.1f) continue;

            float gy = gen.TerrainHeight(pos.x, pos.z);
            Prim(PrimitiveType.Cube, "SteppingStone", g,
                 new Vector3(pos.x, gy + 0.035f, pos.z),
                 new Vector3(Mathf.Lerp(0.45f, 0.62f, R01()), 0.05f,
                             Mathf.Lerp(0.35f, 0.50f, R01())),
                 Quaternion.Euler(0f, R01() * 360f, 0f),
                 (i % 4 == 0) ? matStoneDark : matPath);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // TOUFFES D'HERBE : 3 lamelles croisées, densifient le sol à peu de frais
    void BuildGrassTufts(Transform parent)
    {
        if (grassTuftCount <= 0) return;
        Transform g = Group("GrassTufts", parent);
        int placed = 0, guard = grassTuftCount * 6;
        while (placed < grassTuftCount && guard-- > 0)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 1.2f, gen.islandRadius - 3f);
            if (!IsFree(pos)) continue;
            float gy = gen.TerrainHeight(pos.x, pos.z);
            float s  = Mathf.Lerp(0.7f, 1.3f, R01());
            Material leaf = R01() < 0.5f ? matLeaf : matLeafLight;
            Transform tuft = Pivot("GrassTuft", g, new Vector3(pos.x, gy, pos.z), R01() * 360f);
            for (int b = 0; b < 3; b++)
                PrimLocal(PrimitiveType.Cube, "Blade", tuft,
                          new Vector3(0f, 0.11f * s, 0f),
                          new Vector3(0.18f * s, 0.24f * s, 0.02f),
                          Quaternion.Euler((R01() - 0.5f) * 14f, b * 60f, (R01() - 0.5f) * 14f),
                          leaf);
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // GROS ROCHERS en amas, coiffés de mousse
    void BuildBoulders(Transform parent)
    {
        if (boulderClusterCount <= 0) return;
        Transform g = Group("Boulders", parent);
        int placed = 0, guard = boulderClusterCount * 12;
        while (placed < boulderClusterCount && guard-- > 0)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 4f, gen.islandRadius - 4f);
            if (!IsFree(pos)) continue;
            Transform cluster = Pivot("BoulderCluster", g,
                new Vector3(pos.x, gen.TerrainHeight(pos.x, pos.z), pos.z), R01() * 360f);

            int rocks = 2 + (int)(R01() * 3f);
            float biggest = 0f; Vector3 biggestPos = Vector3.zero;
            for (int r = 0; r < rocks; r++)
            {
                float s = Mathf.Lerp(0.5f, 1.3f, R01());
                Vector3 off = new Vector3((R01() - 0.5f) * 1.6f, s * 0.28f, (R01() - 0.5f) * 1.6f);
                PrimLocal(PrimitiveType.Cube, "Boulder", cluster, off,
                          new Vector3(s, s * 0.75f, s * Mathf.Lerp(0.8f, 1.2f, R01())),
                          Quaternion.Euler((R01() - 0.5f) * 24f, R01() * 360f, (R01() - 0.5f) * 24f),
                          R01() < 0.5f ? matStone : matStoneDark);
                if (s > biggest) { biggest = s; biggestPos = off; }
            }
            // Coiffe de mousse sur le plus gros rocher
            PrimLocal(PrimitiveType.Cube, "BoulderMoss", cluster,
                      biggestPos + Vector3.up * biggest * 0.38f,
                      new Vector3(biggest * 0.85f, 0.06f, biggest * 0.80f),
                      Quaternion.Euler(0f, R01() * 360f, 0f), matMoss);
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // CRISTAUX ÉMISSIFS : éclats cyan/violet sur socle sombre (ambiance rogue-lite)
    void BuildCrystals(Transform parent)
    {
        if (crystalClusterCount <= 0) return;
        Transform g = Group("Crystals", parent);
        int placed = 0, guard = crystalClusterCount * 14;
        while (placed < crystalClusterCount && guard-- > 0)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 5f, gen.islandRadius - 4f);
            if (!IsFree(pos)) continue;
            float gy = gen.TerrainHeight(pos.x, pos.z);
            Material crystal = R01() < 0.5f ? matCrystalCyan : matCrystalViolet;
            Transform cluster = Pivot("CrystalCluster", g, new Vector3(pos.x, gy, pos.z), R01() * 360f);

            // Socle rocheux sombre
            PrimLocal(PrimitiveType.Cube, "CrystalBase", cluster,
                      new Vector3(0f, 0.10f, 0f), new Vector3(0.85f, 0.30f, 0.85f),
                      Quaternion.Euler((R01() - 0.5f) * 10f, R01() * 360f, (R01() - 0.5f) * 10f),
                      matVoidRock);

            int shards = 3 + (int)(R01() * 4f);
            for (int s = 0; s < shards; s++)
            {
                float w   = Mathf.Lerp(0.10f, 0.22f, R01());
                float len = Mathf.Lerp(0.40f, 1.10f, R01());
                PrimLocal(PrimitiveType.Cube, "CrystalShard", cluster,
                          new Vector3((R01() - 0.5f) * 0.55f, len * 0.42f, (R01() - 0.5f) * 0.55f),
                          new Vector3(w, len, w),
                          Quaternion.Euler((R01() - 0.5f) * 38f, R01() * 360f, (R01() - 0.5f) * 38f),
                          crystal);
            }
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // RUINES : cercle de colonnes brisées, débris, mousse et cristal central
    void BuildRuins(Transform parent)
    {
        if (ruinCount <= 0) return;
        Transform g = Group("Ruins", parent);
        int placed = 0, guard = ruinCount * 30;
        while (placed < ruinCount && guard-- > 0)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 7f, gen.islandRadius - 7f);
            if (!IsFree(pos)) continue;
            float gy = gen.TerrainHeight(pos.x, pos.z);
            Transform ruin = Pivot("Ruin", g, new Vector3(pos.x, gy, pos.z), R01() * 360f);

            // Dalle circulaire effondrée
            PrimLocal(PrimitiveType.Cylinder, "RuinFloor", ruin,
                      new Vector3(0f, 0.04f, 0f), new Vector3(2.6f, 0.04f, 2.6f),
                      Quaternion.identity, matStoneDark);

            // Cercle de colonnes brisées à hauteurs variées
            int pillars = 6;
            for (int p = 0; p < pillars; p++)
            {
                float a  = (float)p / pillars * Mathf.PI * 2f;
                float ph = Mathf.Lerp(0.35f, 1.9f, R01());
                Vector3 basePos = new Vector3(Mathf.Cos(a) * 1.15f, 0f, Mathf.Sin(a) * 1.15f);
                PrimLocal(PrimitiveType.Cylinder, "RuinPillar", ruin,
                          basePos + Vector3.up * ph * 0.5f,
                          new Vector3(0.30f, ph * 0.5f, 0.30f),
                          Quaternion.Euler((R01() - 0.5f) * 6f, 0f, (R01() - 0.5f) * 6f), matStone);
                // Chapiteau sur les colonnes encore hautes
                if (ph > 1.4f)
                    PrimLocal(PrimitiveType.Cube, "RuinCapital", ruin,
                              basePos + Vector3.up * (ph + 0.07f),
                              new Vector3(0.44f, 0.14f, 0.44f), Quaternion.identity, matStone);
                // Mousse au pied
                if (R01() < 0.6f)
                    PrimLocal(PrimitiveType.Cube, "RuinMoss", ruin,
                              basePos + new Vector3((R01() - 0.5f) * 0.3f, 0.085f, (R01() - 0.5f) * 0.3f),
                              new Vector3(0.30f, 0.015f, 0.30f),
                              Quaternion.Euler(0f, R01() * 360f, 0f), matMoss);
            }

            // Colonne effondrée + débris épars
            PrimLocal(PrimitiveType.Cylinder, "RuinFallen", ruin,
                      new Vector3(0.4f, 0.16f, -0.3f), new Vector3(0.28f, 0.8f, 0.28f),
                      Quaternion.Euler(90f, R01() * 360f, 0f), matStone);
            for (int d = 0; d < 4; d++)
                PrimLocal(PrimitiveType.Cube, "RuinDebris", ruin,
                          new Vector3((R01() - 0.5f) * 2.2f, 0.08f, (R01() - 0.5f) * 2.2f),
                          Vector3.one * Mathf.Lerp(0.12f, 0.28f, R01()),
                          Quaternion.Euler(R01() * 30f, R01() * 360f, R01() * 30f), matStoneDark);

            // Un cristal au centre : point d'intérêt lumineux
            PrimLocal(PrimitiveType.Cube, "RuinCrystal", ruin,
                      new Vector3(0f, 0.55f, 0f), new Vector3(0.20f, 0.85f, 0.20f),
                      Quaternion.Euler(8f, R01() * 360f, -6f),
                      R01() < 0.5f ? matCrystalCyan : matCrystalViolet);
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // DÉCOR D'ARÈNE : braseros aux coins du plateau, bannières de tournoi
    // rouge/bleu et cordon de poteaux — l'ambiance auto chess
    void BuildArenaDecor(Transform parent)
    {
        Transform g = Group("ArenaDecor", parent);
        float cy = gen.TerrainHeight(0f, 0f);
        float boardWorld = Mathf.Ceil(gen.chessboardSize / gen.chessTile) * gen.chessTile;
        float half = boardWorld * 0.5f + 0.7f;

        // Braseros aux 4 coins du plateau
        for (int cx = -1; cx <= 1; cx += 2)
        for (int cz = -1; cz <= 1; cz += 2)
        {
            Transform brazier = Pivot("Brazier", g, new Vector3(cx * half, cy, cz * half), 0f);
            PrimLocal(PrimitiveType.Cylinder, "BrazierBase", brazier,
                      new Vector3(0f, 0.14f, 0f), new Vector3(0.34f, 0.14f, 0.34f),
                      Quaternion.identity, matStoneDark);
            PrimLocal(PrimitiveType.Cylinder, "BrazierColumn", brazier,
                      new Vector3(0f, 0.42f, 0f), new Vector3(0.18f, 0.16f, 0.18f),
                      Quaternion.identity, matStone);
            PrimLocal(PrimitiveType.Cylinder, "BrazierBowl", brazier,
                      new Vector3(0f, 0.62f, 0f), new Vector3(0.42f, 0.07f, 0.42f),
                      Quaternion.identity, matStoneDark);
            PrimLocal(PrimitiveType.Sphere, "BrazierFire", brazier,
                      new Vector3(0f, 0.74f, 0f), new Vector3(0.28f, 0.20f, 0.28f),
                      Quaternion.identity, matFire);
            PrimLocal(PrimitiveType.Sphere, "BrazierFlame", brazier,
                      new Vector3(0f, 0.86f, 0f), new Vector3(0.14f, 0.20f, 0.14f),
                      Quaternion.identity, matLantern);
        }

        // Bannières rouge/bleu autour du parc (ambiance tournoi)
        int banners = 6;
        for (int i = 0; i < banners; i++)
        {
            float ang = i * (Mathf.PI * 2f / banners) + 0.26f;
            float rad = gen.parkRadius + 0.6f;
            Vector3 pos = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
            if (NearHouse(pos, 1.9f)) continue;   // pas de bannière collée à une maison
            float gy  = gen.TerrainHeight(pos.x, pos.z);
            float yaw = Mathf.Atan2(-pos.x, -pos.z) * Mathf.Rad2Deg;
            Transform banner = Pivot("Banner", g, new Vector3(pos.x, gy, pos.z), yaw);
            Material cloth = (i % 2 == 0) ? matBannerRed : matBannerBlue;

            PrimLocal(PrimitiveType.Cylinder, "BannerPole", banner,
                      new Vector3(0f, 0.95f, 0f), new Vector3(0.06f, 0.95f, 0.06f),
                      Quaternion.identity, matWoodDark);
            PrimLocal(PrimitiveType.Cube, "BannerCrossbar", banner,
                      new Vector3(0.20f, 1.80f, 0f), new Vector3(0.50f, 0.05f, 0.05f),
                      Quaternion.identity, matWoodDark);
            PrimLocal(PrimitiveType.Cube, "BannerCloth", banner,
                      new Vector3(0.24f, 1.45f, 0f), new Vector3(0.40f, 0.66f, 0.035f),
                      Quaternion.identity, cloth);
            PrimLocal(PrimitiveType.Sphere, "BannerFinial", banner,
                      new Vector3(0f, 1.96f, 0f), Vector3.one * 0.11f,
                      Quaternion.identity, matGold);
        }

        // Cordon de poteaux reliés autour de l'arène
        int posts = 20;
        float ropeR = gen.parkRadius + 0.1f;
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= posts; i++)
        {
            float ang = (float)i / posts * Mathf.PI * 2f;
            Vector3 p = new Vector3(Mathf.Cos(ang) * ropeR, 0f, Mathf.Sin(ang) * ropeR);
            p.y = gen.TerrainHeight(p.x, p.z);
            if (i < posts)
                Prim(PrimitiveType.Cylinder, "RopePost", g,
                     p + Vector3.up * 0.22f, new Vector3(0.06f, 0.22f, 0.06f),
                     Quaternion.identity, matWood);
            if (i > 0)
            {
                Vector3 a   = prev + Vector3.up * 0.36f;
                Vector3 b   = p + Vector3.up * 0.36f;
                Vector3 d   = b - a;
                Prim(PrimitiveType.Cylinder, "Rope", g, (a + b) * 0.5f,
                     new Vector3(0.022f, d.magnitude * 0.5f, 0.022f),
                     Quaternion.FromToRotation(Vector3.up, d.normalized), matRope);
            }
            prev = p;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // LUCIOLES : essaims émissifs qui flottent et orbitent doucement
    void BuildFireflies(Transform parent)
    {
        if (fireflyClusterCount <= 0) return;
        Transform g = Group("Fireflies", parent);
        int placed = 0, guard = fireflyClusterCount * 10;
        while (placed < fireflyClusterCount && guard-- > 0)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 3f, gen.islandRadius - 5f);
            if (!IsFree(pos)) continue;
            float gy = gen.TerrainHeight(pos.x, pos.z);
            Transform cluster = Pivot("FireflyCluster", g, new Vector3(pos.x, gy, pos.z), 0f);

            int flies = 4 + (int)(R01() * 3f);
            for (int f = 0; f < flies; f++)
                PrimLocal(PrimitiveType.Sphere, "Firefly", cluster,
                          new Vector3((R01() - 0.5f) * 1.4f,
                                      Mathf.Lerp(0.35f, 1.30f, R01()),
                                      (R01() - 0.5f) * 1.4f),
                          Vector3.one * Mathf.Lerp(0.035f, 0.06f, R01()),
                          Quaternion.identity, matFirefly);

            VoidPropFloat fl = cluster.gameObject.AddComponent<VoidPropFloat>();
            fl.bobAmplitude = 0.18f;
            fl.bobSpeed     = Mathf.Lerp(0.8f, 1.6f, R01());
            fl.rotSpeed     = Mathf.Lerp(-14f, 14f, R01());
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // NUAGES low-poly qui dérivent lentement au-dessus de l'île
    void BuildClouds(Transform parent)
    {
        if (cloudCount <= 0) return;
        Transform g = Group("Clouds", parent);
        for (int i = 0; i < cloudCount; i++)
        {
            float ang = R01() * Mathf.PI * 2f;
            float rad = R01() * (gen.islandRadius + 12f);
            float y   = Mathf.Lerp(16f, 27f, R01());
            Transform cloud = Pivot("Cloud", g,
                new Vector3(Mathf.Cos(ang) * rad, y, Mathf.Sin(ang) * rad), R01() * 360f);

            int   puffs = 3 + (int)(R01() * 3f);
            float baseS = Mathf.Lerp(2.2f, 4.2f, R01());
            for (int p = 0; p < puffs; p++)
            {
                float s = baseS * Mathf.Lerp(0.45f, 1f, R01());
                PrimLocal(PrimitiveType.Cube, "CloudPuff", cloud,
                          new Vector3((p - puffs * 0.5f) * baseS * 0.42f + (R01() - 0.5f) * 0.8f,
                                      (R01() - 0.5f) * 0.7f,
                                      (R01() - 0.5f) * 1.4f),
                          new Vector3(s, s * 0.30f, s * 0.62f),
                          Quaternion.identity, matCloud);
            }

            VoidPropFloat fl = cloud.gameObject.AddComponent<VoidPropFloat>();
            fl.bobAmplitude   = 0.5f;
            fl.bobSpeed       = 0.12f;
            fl.driftDir       = new Vector3(Mathf.Cos(ang + 1.6f), 0f, Mathf.Sin(ang + 1.6f));
            fl.driftAmplitude = Mathf.Lerp(4f, 9f, R01());
            fl.driftSpeed     = 0.05f;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // ÎLES FLOTTANTES dans le vide autour de l'île : rocher sombre, herbe
    // sur le dessus, parfois un arbuste ou un cristal — elles ondulent
    void BuildFloatingIslands(Transform parent)
    {
        if (floatingIslandCount <= 0) return;
        Transform g = Group("FloatingIslands", parent);

        // Gros blocs de planète arrachés, GARANTIS (indépendants de l'inspecteur)
        // et de plus en plus nombreux avec la phase : c'est eux qui matérialisent
        // le vide. Certains flottent au-dessus, beaucoup plongent SOUS l'île.
        int giants = 4 + gen.phase * 2;
        int total  = floatingIslandCount + giants;
        for (int i = 0; i < total; i++)
        {
            bool giant = i < giants;
            float ang = R01() * Mathf.PI * 2f;
            float rad = giant ? gen.islandRadius + Mathf.Lerp(5f, 24f, R01())
                              : gen.islandRadius + Mathf.Lerp(3f, 14f, R01());
            // Large étalement vertical : au-dessus ET bien en-dessous dans le vide
            float y = giant ? Mathf.Lerp(-14f, 11f, R01())
                            : Mathf.Lerp(-7f, 8f, R01());
            float s = giant ? Mathf.Lerp(4f, 9f, R01())
                            : Mathf.Lerp(1.4f, 3.2f, R01());
            BuildFloatingChunk(g, new Vector3(Mathf.Cos(ang) * rad, y, Mathf.Sin(ang) * rad), s, giant);
        }
    }

    // Un morceau d'île qui flotte : roche du vide, longue pointe dessous, herbe
    // sur le dessus, végétation/cristaux, satellites et houle lente (les gros
    // blocs dérivent plus lentement, question de masse)
    void BuildFloatingChunk(Transform g, Vector3 pos, float s, bool giant)
    {
        Transform isle = Pivot("FloatingIsland", g, pos, R01() * 360f);

        // Corps + pointe(s) sous l'île (silhouette d'île volante)
        PrimLocal(PrimitiveType.Cube, "IslandRock", isle,
                  Vector3.zero, new Vector3(s, s * 0.7f, s * Mathf.Lerp(0.85f, 1.15f, R01())),
                  Quaternion.Euler((R01() - 0.5f) * 10f, R01() * 360f, (R01() - 0.5f) * 10f),
                  matVoidRock);
        PrimLocal(PrimitiveType.Cube, "IslandSpike", isle,
                  new Vector3(0f, -s * 0.7f, 0f), new Vector3(s * 0.55f, s * 0.95f, s * 0.55f),
                  Quaternion.Euler(45f, R01() * 360f, 45f), matVoidRock);
        if (giant)
            PrimLocal(PrimitiveType.Cube, "IslandSpikeTip", isle,
                      new Vector3(0f, -s * 1.3f, 0f), new Vector3(s * 0.28f, s * 0.7f, s * 0.28f),
                      Quaternion.Euler(40f, R01() * 360f, 40f), matVoidRock);
        // Herbe sur le dessus
        PrimLocal(PrimitiveType.Cube, "IslandGrass", isle,
                  new Vector3(0f, s * 0.37f, 0f), new Vector3(s * 0.95f, 0.10f, s * 0.90f),
                  Quaternion.identity, matMoss);

        float topY = s * 0.42f;
        if (giant)
        {
            // Décor de surface sur les gros blocs : quelques arbres + cristaux
            int trees = 1 + (int)(R01() * 3f);
            for (int t = 0; t < trees; t++)
            {
                float ox = (R01() - 0.5f) * s * 0.6f;
                float oz = (R01() - 0.5f) * s * 0.6f;
                float th = Mathf.Lerp(0.4f, 0.9f, R01());
                PrimLocal(PrimitiveType.Cylinder, "IsleTrunk", isle,
                          new Vector3(ox, topY + th * 0.5f, oz), new Vector3(0.12f, th * 0.5f, 0.12f),
                          Quaternion.identity, matWoodDark);
                PrimLocal(PrimitiveType.Sphere, "IsleLeaves", isle,
                          new Vector3(ox, topY + th + 0.3f, oz),
                          new Vector3(0.8f, 0.65f, 0.8f), Quaternion.identity, matLeaf);
            }
            int crystals = 1 + (int)(R01() * 2f);
            for (int c = 0; c < crystals; c++)
                PrimLocal(PrimitiveType.Cube, "IsleCrystal", isle,
                          new Vector3((R01() - 0.5f) * s * 0.7f, topY + 0.35f, (R01() - 0.5f) * s * 0.7f),
                          new Vector3(0.18f, Mathf.Lerp(0.6f, 1.1f, R01()), 0.18f),
                          Quaternion.Euler(8f, R01() * 360f, -8f),
                          R01() < 0.5f ? matCrystalCyan : matCrystalViolet);
        }
        else if (s > 1.8f)
        {
            PrimLocal(PrimitiveType.Cylinder, "IsleTrunk", isle,
                      new Vector3(0f, topY + 0.22f, 0f), new Vector3(0.09f, 0.22f, 0.09f),
                      Quaternion.identity, matWoodDark);
            PrimLocal(PrimitiveType.Sphere, "IsleLeaves", isle,
                      new Vector3(0f, topY + 0.55f, 0f), new Vector3(0.55f, 0.42f, 0.55f),
                      Quaternion.identity, matLeaf);
        }
        else if (R01() < 0.4f)
            PrimLocal(PrimitiveType.Cube, "IsleCrystal", isle,
                      new Vector3(0f, topY + 0.25f, 0f), new Vector3(0.14f, 0.5f, 0.14f),
                      Quaternion.Euler(6f, R01() * 360f, -8f),
                      R01() < 0.5f ? matCrystalCyan : matCrystalViolet);

        // Rochers satellites qui gravitent autour (plus nombreux pour les géants)
        int sats = giant ? 3 + (int)(R01() * 5f) : (int)(R01() * 3f);
        for (int sIdx = 0; sIdx < sats; sIdx++)
            PrimLocal(PrimitiveType.Cube, "IslandSatellite", isle,
                      new Vector3((R01() - 0.5f) * s * 3f,
                                  (R01() - 0.5f) * s * 1.4f,
                                  (R01() - 0.5f) * s * 3f),
                      Vector3.one * Mathf.Lerp(0.20f, giant ? 0.9f : 0.50f, R01()),
                      Quaternion.Euler(R01() * 360f, R01() * 360f, R01() * 360f), matVoidRock);

        VoidPropFloat fl = isle.gameObject.AddComponent<VoidPropFloat>();
        fl.bobAmplitude = giant ? Mathf.Lerp(0.35f, 0.8f, R01()) : Mathf.Lerp(0.25f, 0.55f, R01());
        fl.bobSpeed     = giant ? Mathf.Lerp(0.12f, 0.3f, R01())  : Mathf.Lerp(0.25f, 0.6f, R01());
        fl.rotSpeed     = giant ? Mathf.Lerp(-3f, 3f, R01())      : Mathf.Lerp(-6f, 6f, R01());
    }

    // ──────────────────────────────────────────────────────────────────
    // NÉANT — fissures violettes lumineuses qui lézardent le sol
    void BuildCracks(Transform parent)
    {
        Transform g = Group("VoidCracks", parent);
        int count = gen.phase == 2 ? 7 : gen.phase == 3 ? 12 : gen.phase == 4 ? 18 : 26;
        int placed = 0, guard = count * 8;
        while (placed < count && guard-- > 0)
        {
            Vector3 pos = RandomPoint(gen.parkRadius + 2f, gen.islandRadius - 4f);
            if (gen.IsInsideLake(pos) || NearHouse(pos, 1.5f)) continue;
            float ang = R01() * 360f;
            int segs = 3 + (int)(R01() * 3f);
            for (int s = 0; s < segs; s++)
            {
                float len = Mathf.Lerp(1.0f, 2.0f, R01());
                Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                Vector3 mid = pos + dir * len * 0.5f;
                float gy = gen.TerrainHeight(mid.x, mid.z);
                Prim(PrimitiveType.Cube, "Crack", g,
                     new Vector3(mid.x, gy + 0.025f, mid.z),
                     new Vector3(Mathf.Lerp(0.10f, 0.22f, R01()), 0.03f, len * 1.05f),
                     Quaternion.Euler(0f, ang, 0f), matVoidGlow);
                pos += dir * len;
                ang += (R01() - 0.5f) * 70f;
            }
            placed++;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // NÉANT — orchestration de la destruction selon la phase :
    //   2 : lueurs au fond des petites morsures
    //   3 : + morceaux arrachés flottants + trou noir au loin
    //   4 : + débris aspirés vers le ciel + morceaux éparpillés + trou noir proche
    //   5 : + cataclysme final (ceinture orbitale, rayon de siphonnage)
    void BuildVoidDestruction(Transform stat, Transform dyn)
    {
        for (int i = 0; i < gen.voidBites.Count; i++)
        {
            Vector4 b = gen.voidBites[i];
            float floorY = gen.TerrainHeight(b.x, b.y);

            // Lueur du néant au fond de la morsure
            Prim(PrimitiveType.Cylinder, "VoidPool", stat,
                 new Vector3(b.x, floorY + 0.10f, b.y),
                 new Vector3(b.z * 1.05f, 0.03f, b.z * 1.05f),
                 Quaternion.identity, matVoidGlow);

            // Morceaux arrachés qui flottent près de la morsure
            if (gen.phase >= 3)
            {
                int chunks = gen.phase >= 4 ? 2 : 1;
                for (int c = 0; c < chunks; c++)
                {
                    Vector2 dirOut = new Vector2(b.x, b.y).normalized;
                    Vector3 p = new Vector3(
                        b.x + dirOut.x * b.z * Mathf.Lerp(0.4f, 1.3f, R01()) + (R01() - 0.5f) * 2f,
                        Mathf.Lerp(2.5f, 7f, R01()),
                        b.y + dirOut.y * b.z * Mathf.Lerp(0.4f, 1.3f, R01()) + (R01() - 0.5f) * 2f);
                    BuildVoidChunk(dyn, p, b.z * Mathf.Lerp(0.22f, 0.40f, R01()));
                }
            }

            // Colonne de débris aspirés vers le ciel
            if (gen.phase >= 4)
            {
                Transform column = Pivot("UpdraftDebris", dyn, new Vector3(b.x, 0f, b.y), R01() * 360f);
                int bits = 6 + (int)(R01() * 3f);
                for (int d = 0; d < bits; d++)
                {
                    float t = (float)d / bits;
                    PrimLocal(PrimitiveType.Cube, "Debris", column,
                              new Vector3((R01() - 0.5f) * b.z * (1f - t),
                                          Mathf.Lerp(1.5f, 11f, t),
                                          (R01() - 0.5f) * b.z * (1f - t)),
                              Vector3.one * Mathf.Lerp(0.35f, 0.08f, t),
                              Quaternion.Euler(R01() * 360f, R01() * 360f, R01() * 360f),
                              R01() < 0.3f ? matVoidGlow : matVoidRock);
                }
                VoidPropFloat fc = column.gameObject.AddComponent<VoidPropFloat>();
                fc.bobAmplitude = 0.5f;
                fc.bobSpeed     = 0.5f;
                fc.rotSpeed     = Mathf.Lerp(4f, 10f, R01());
            }
        }

        if (gen.phase >= 3)
        {
            BuildBlackHole(dyn);
            BuildAtmosphereSiphon(dyn, BlackHolePos());   // l'atmosphère et des morceaux sont aspirés
        }
        if (gen.phase >= 4) BuildScatteredChunks(dyn);
        if (gen.phase >= 5) BuildFinalCataclysm(dyn);
    }

    // L'atmosphère et des morceaux de la map s'arrachent du sol et spiralent en
    // continu vers le trou noir supermassif (composant VoidSiphon), rétrécissant
    // jusqu'à disparaître dedans — puis bouclent. Aspiration permanente, mais
    // ce ne sont que des bouts qui s'envolent : l'île reste intacte.
    void BuildAtmosphereSiphon(Transform parent, Vector3 bhPos)
    {
        Transform g = Group("AtmosphereSiphon", parent);
        int n = gen.phase >= 5 ? 130 : gen.phase >= 4 ? 95 : 65;
        for (int i = 0; i < n; i++)
        {
            float ang = R01() * Mathf.PI * 2f;
            float rad = Mathf.Sqrt(R01()) * gen.islandRadius * 0.95f;
            Vector3 start = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
            start.y = gen.TerrainHeight(start.x, start.z) + Mathf.Lerp(0.5f, 6f, R01());

            float roll = R01();
            GameObject go;
            if (roll < 0.52f)        // volute d'atmosphère étirée
                go = Prim(PrimitiveType.Cube, "AtmoWisp", g, start,
                          new Vector3(0.18f, 0.18f, Mathf.Lerp(1.5f, 4.5f, R01())),
                          Quaternion.identity, matGhostPurple);
            else if (roll < 0.85f)   // morceau de matière aspiré
                go = Prim(PrimitiveType.Cube, "SiphonDebris", g, start,
                          Vector3.one * Mathf.Lerp(0.3f, 1.2f, R01()),
                          Quaternion.Euler(R01() * 360f, R01() * 360f, R01() * 360f), matVoidRock);
            else                     // étincelle lumineuse
                go = Prim(PrimitiveType.Cube, "SiphonGlow", g, start,
                          Vector3.one * Mathf.Lerp(0.15f, 0.4f, R01()),
                          Quaternion.identity, matVoidGlow);

            VoidSiphon vs   = go.AddComponent<VoidSiphon>();
            vs.startPos     = start;
            vs.target       = bhPos;
            vs.speed        = Mathf.Lerp(0.04f, 0.12f, R01());
            vs.arc          = Mathf.Lerp(5f, 14f, R01());
            vs.swirl        = Mathf.Lerp(1.2f, 3.5f, R01());
            vs.swirlRadius  = Mathf.Lerp(4f, 13f, R01());
            vs.baseScale    = go.transform.localScale;
            vs.faceTarget   = roll < 0.52f;   // les volutes s'étirent vers le trou noir
            vs.spin         = roll >= 0.52f && roll < 0.85f;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // Un morceau de planète arraché : roche du vide, herbe dessus,
    // pointe dessous, parfois un arbuste ou un cristal — il flotte
    void BuildVoidChunk(Transform parent, Vector3 pos, float size)
    {
        Transform chunk = Pivot("VoidChunk", parent, pos, 0f);
        chunk.localRotation = Quaternion.Euler((R01() - 0.5f) * 24f, R01() * 360f, (R01() - 0.5f) * 24f);

        PrimLocal(PrimitiveType.Cube, "ChunkRock", chunk, Vector3.zero,
                  new Vector3(size, size * 0.7f, size * Mathf.Lerp(0.8f, 1.2f, R01())),
                  Quaternion.identity, matVoidRock);
        PrimLocal(PrimitiveType.Cube, "ChunkSpike", chunk, new Vector3(0f, -size * 0.5f, 0f),
                  Vector3.one * size * 0.5f, Quaternion.Euler(45f, R01() * 360f, 45f), matVoidRock);
        PrimLocal(PrimitiveType.Cube, "ChunkGrass", chunk, new Vector3(0f, size * 0.38f, 0f),
                  new Vector3(size * 0.95f, 0.09f, size * 0.9f), Quaternion.identity, matMoss);

        if (size > 1.6f && R01() < 0.5f)
        {
            PrimLocal(PrimitiveType.Cylinder, "ChunkTrunk", chunk,
                      new Vector3(0f, size * 0.42f + 0.2f, 0f), new Vector3(0.08f, 0.2f, 0.08f),
                      Quaternion.identity, matWoodDark);
            PrimLocal(PrimitiveType.Sphere, "ChunkLeaves", chunk,
                      new Vector3(0f, size * 0.42f + 0.5f, 0f), new Vector3(0.5f, 0.38f, 0.5f),
                      Quaternion.identity, matLeaf);
        }
        else if (R01() < 0.35f)
            PrimLocal(PrimitiveType.Cube, "ChunkCrystal", chunk,
                      new Vector3(0f, size * 0.42f + 0.22f, 0f), new Vector3(0.12f, 0.45f, 0.12f),
                      Quaternion.Euler(7f, R01() * 360f, -7f), matCrystalViolet);

        VoidPropFloat fl = chunk.gameObject.AddComponent<VoidPropFloat>();
        fl.bobAmplitude = Mathf.Lerp(0.2f, 0.5f, R01());
        fl.bobSpeed     = Mathf.Lerp(0.3f, 0.7f, R01());
        fl.rotSpeed     = Mathf.Lerp(-8f, 8f, R01());
    }

    // ──────────────────────────────────────────────────────────────────
    // Position du trou noir : déterministe (seed), il se rapproche et
    // grossit au fil des phases
    Vector3 BlackHolePos()
    {
        System.Random brng = new System.Random(gen.seed * 911 + 13);
        float ang  = (float)brng.NextDouble() * Mathf.PI * 2f;
        // Supermassif : assez loin pour ne pas chevaucher l'île, assez haut pour
        // la surplomber de façon écrasante
        float dist = gen.phase >= 5 ? gen.islandRadius * 2.2f
                   : gen.phase >= 4 ? gen.islandRadius * 2.7f
                   : gen.islandRadius * 3.3f;
        float y    = gen.phase >= 5 ? 44f : gen.phase >= 4 ? 36f : 28f;
        return new Vector3(Mathf.Cos(ang) * dist, y, Mathf.Sin(ang) * dist);
    }

    // TROU NOIR (forme à disque plein, style Saturne/Gargantua) : sphère sombre
    // cernée d'un anneau de photons blanc et d'un disque d'accrétion plein qui
    // va de l'orange au blanc-or brûlant. Supermassif mais le disque reste fin
    // et net (pas le « blob »). Halo de distorsion violet, spirale de matière.
    void BuildBlackHole(Transform parent)
    {
        float D = gen.phase >= 5 ? 32f : gen.phase >= 4 ? 23f : 16f;
        Transform bh = Pivot("BlackHole", parent, BlackHolePos(), R01() * 360f);
        Quaternion diskTilt = Quaternion.Euler(16f, 0f, 9f);
        Quaternion haloRot  = Quaternion.Euler(106f, 0f, 9f);

        // Horizon des événements (sphère sombre)
        PrimLocal(PrimitiveType.Sphere, "EventHorizon", bh, Vector3.zero,
                  Vector3.one * D, Quaternion.identity, matBlackHole);

        // Disque d'accrétion : anneaux pleins, orange aux bords -> blanc-or au coeur
        PrimLocal(PrimitiveType.Cylinder, "AccretionOuter", bh, Vector3.zero,
                  new Vector3(D * 2.6f, D * 0.012f, D * 2.6f), diskTilt, matAccretion);
        PrimLocal(PrimitiveType.Cylinder, "AccretionMid", bh, Vector3.zero,
                  new Vector3(D * 2.0f, D * 0.015f, D * 2.0f), diskTilt, matAccretion);
        PrimLocal(PrimitiveType.Cylinder, "AccretionInner", bh, Vector3.zero,
                  new Vector3(D * 1.55f, D * 0.020f, D * 1.55f), diskTilt, matAccretionHot);

        // Anneau de photons : un fin liseré blanc dans le plan du disque + un
        // vertical (la lentille gravitationnelle façon Gargantua)
        PrimLocal(PrimitiveType.Cylinder, "PhotonRing", bh, Vector3.zero,
                  new Vector3(D * 1.18f, D * 0.010f, D * 1.18f), diskTilt, matAccretionHot);
        PrimLocal(PrimitiveType.Cylinder, "PhotonRingV", bh, Vector3.zero,
                  new Vector3(D * 1.24f, D * 0.008f, D * 1.24f), haloRot, matAccretionHot);

        // Halo de distorsion violet (donne la teinte violacée à la sphère)
        PrimLocal(PrimitiveType.Sphere, "LensHalo", bh, Vector3.zero,
                  Vector3.one * D * 1.45f, Quaternion.identity, matGhostPurple);

        // Spirale de matière aspirée dans le plan du disque
        int bits = 34;
        for (int i = 0; i < bits; i++)
        {
            float t   = (float)i / bits;
            float a   = t * Mathf.PI * 4f + R01() * 0.2f;
            float rad = D * Mathf.Lerp(2.4f, 1.2f, t);
            Vector3 p = diskTilt * new Vector3(Mathf.Cos(a) * rad,
                                               (R01() - 0.5f) * D * 0.03f,
                                               Mathf.Sin(a) * rad);
            PrimLocal(PrimitiveType.Cube, "AccretionDebris", bh, p,
                      Vector3.one * (D * Mathf.Lerp(0.045f, 0.012f, t)),
                      Quaternion.Euler(R01() * 360f, R01() * 360f, R01() * 360f),
                      t > 0.55f ? matAccretionHot : matAccretion);
        }

        // Lentille gravitationnelle : coquille qui COURBE réellement l'arrière-
        // plan autour du trou noir (shader GrabPass Void/BlackHoleLens)
        Shader lensSh = Shader.Find("Void/BlackHoleLens");
        if (lensSh != null)
        {
            Material lensMat = new Material(lensSh);
            lensMat.SetFloat("_Strength", 0.17f);
            lensMat.SetFloat("_Swirl", 1.1f);
            lensMat.SetFloat("_Power", 2.3f);
            PrimLocal(PrimitiveType.Sphere, "LensDistortion", bh, Vector3.zero,
                      Vector3.one * D * 1.5f, Quaternion.identity, lensMat);
        }

        // Illumination : une lumière chaude émane du disque et éclaire les
        // alentours (le trou noir « rayonne » au lieu d'être un aplat sombre)
        GameObject lightGO = new GameObject("BlackHoleLight");
        lightGO.transform.SetParent(bh);
        lightGO.transform.localPosition = Vector3.zero;
        lightGO.layer = bh.gameObject.layer;
        Light bl = lightGO.AddComponent<Light>();
        bl.type      = LightType.Point;
        bl.color     = new Color(1.00f, 0.66f, 0.42f);   // or chaud du disque
        bl.intensity = gen.phase >= 5 ? 5.5f : 3.8f;
        bl.range     = D * 8f;
        bl.shadows   = LightShadows.None;

        VoidPropFloat fl = bh.gameObject.AddComponent<VoidPropFloat>();
        fl.bobAmplitude = 0.5f;
        fl.bobSpeed     = 0.10f;
        fl.rotSpeed     = gen.phase >= 5 ? 12f : 7f;
    }

    // ──────────────────────────────────────────────────────────────────
    // Morceaux de planète éparpillés tout autour de l'île (phase >= 4)
    void BuildScatteredChunks(Transform parent)
    {
        Transform g = Group("ScatteredChunks", parent);
        int count = gen.phase >= 5 ? 24 : 16;
        for (int i = 0; i < count; i++)
        {
            float ang = R01() * Mathf.PI * 2f;
            float rad = gen.islandRadius * Mathf.Lerp(1.10f, 1.95f, R01());
            float y   = Mathf.Lerp(-5f, 16f, R01());
            BuildVoidChunk(g, new Vector3(Mathf.Cos(ang) * rad, y, Mathf.Sin(ang) * rad),
                           Mathf.Lerp(0.8f, 3.0f, R01()));
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // PHASE 5 — LE CATACLYSME : ceinture de débris en orbite autour de
    // l'île et rayon de siphonnage qui vide la plus grosse morsure vers
    // le trou noir — la planète est en train de mourir
    void BuildFinalCataclysm(Transform parent)
    {
        // Ceinture de débris inclinée qui orbite lentement
        Transform belt = Pivot("DebrisBelt", parent, new Vector3(0f, 9f, 0f), 0f);
        belt.localRotation = Quaternion.Euler(11f, 0f, 4f);
        int beltBits = 44;
        for (int i = 0; i < beltBits; i++)
        {
            float a = (float)i / beltBits * Mathf.PI * 2f + R01() * 0.1f;
            float r = gen.islandRadius * Mathf.Lerp(1.25f, 1.45f, R01());
            PrimLocal(PrimitiveType.Cube, "BeltDebris", belt,
                      new Vector3(Mathf.Cos(a) * r, (R01() - 0.5f) * 2.5f, Mathf.Sin(a) * r),
                      Vector3.one * Mathf.Lerp(0.18f, 0.75f, R01()),
                      Quaternion.Euler(R01() * 360f, R01() * 360f, R01() * 360f),
                      R01() < 0.18f ? matVoidGlow : matVoidRock);
        }
        VoidPropFloat beltFl = belt.gameObject.AddComponent<VoidPropFloat>();
        beltFl.bobAmplitude = 0.3f;
        beltFl.bobSpeed     = 0.08f;
        beltFl.rotSpeed     = 2.2f;

        // Rayon de siphonnage : la plus grosse morsure se vide vers le trou noir
        if (gen.voidBites.Count == 0) return;
        Vector4 biggest = gen.voidBites[0];
        for (int i = 1; i < gen.voidBites.Count; i++)
            if (gen.voidBites[i].z > biggest.z) biggest = gen.voidBites[i];

        Vector3 from = new Vector3(biggest.x, gen.TerrainHeight(biggest.x, biggest.y) + 1f, biggest.y);
        Vector3 to   = BlackHolePos();
        Vector3 d    = to - from;
        Quaternion beamRot = Quaternion.FromToRotation(Vector3.up, d.normalized);

        Prim(PrimitiveType.Cylinder, "SiphonBeam", parent,
             from + d * 0.5f, new Vector3(1.8f, d.magnitude * 0.5f, 1.8f), beamRot, matGhostPurple);
        Prim(PrimitiveType.Cylinder, "SiphonCore", parent,
             from + d * 0.5f, new Vector3(0.40f, d.magnitude * 0.5f, 0.40f), beamRot, matVoidGlow);

        // Débris emportés en spirale le long du rayon
        Vector3 perp = Vector3.Cross(d.normalized, Vector3.up).normalized;
        int sBits = 16;
        for (int i = 0; i < sBits; i++)
        {
            float t = (i + 0.5f) / sBits;
            Vector3 p = from + d * t
                      + perp * Mathf.Sin(t * Mathf.PI * 5f) * 1.6f
                      + Vector3.up * Mathf.Sin(t * Mathf.PI * 3f + 1f) * 1.2f;
            Transform bit = Pivot("SiphonDebris", parent, p, R01() * 360f);
            PrimLocal(PrimitiveType.Cube, "Bit", bit, Vector3.zero,
                      Vector3.one * Mathf.Lerp(0.6f, 0.15f, t),
                      Quaternion.Euler(R01() * 360f, R01() * 360f, R01() * 360f),
                      R01() < 0.3f ? matVoidGlow : matVoidRock);
            VoidPropFloat bf = bit.gameObject.AddComponent<VoidPropFloat>();
            bf.bobAmplitude = 0.4f;
            bf.bobSpeed     = Mathf.Lerp(0.6f, 1.4f, R01());
            bf.rotSpeed     = Mathf.Lerp(-25f, 25f, R01());
        }
    }
}

// ============================================================================
// Petit composant d'animation pour les props flottants (îles, nuages,
// lucioles). Volontairement minimaliste : un sinus, zéro allocation,
// ne tourne qu'en Play mode.
// ============================================================================
public class VoidPropFloat : MonoBehaviour
{
    public float   bobAmplitude   = 0.3f;
    public float   bobSpeed       = 0.5f;
    public float   rotSpeed       = 0f;
    public Vector3 driftDir       = Vector3.zero;
    public float   driftAmplitude = 0f;
    public float   driftSpeed     = 0f;

    private Vector3 basePos;
    private float   phase;

    void Start()
    {
        basePos = transform.position;
        phase   = basePos.x * 0.73f + basePos.z * 1.31f;
    }

    void Update()
    {
        float t = Time.time;
        Vector3 p = basePos + Vector3.up * (Mathf.Sin(t * bobSpeed + phase) * bobAmplitude);
        if (driftAmplitude > 0f)
            p += driftDir * (Mathf.Sin(t * driftSpeed + phase) * driftAmplitude);
        transform.position = p;
        if (rotSpeed != 0f)
            transform.Rotate(0f, rotSpeed * Time.deltaTime, 0f, Space.World);
    }
}

// ============================================================================
// Aspiration vers le trou noir supermassif : l'objet remonte du sol en spirale
// vers la cible, accélère et rétrécit en approchant (spaghettification), puis
// boucle à son point de départ — flux continu de matière « avalée ».
// ============================================================================
public class VoidSiphon : MonoBehaviour
{
    public Vector3 startPos;
    public Vector3 target;
    public float   speed       = 0.1f;
    public float   arc         = 8f;
    public float   swirl       = 2f;
    public float   swirlRadius = 8f;
    public Vector3 baseScale   = Vector3.one;
    public bool    faceTarget  = false;
    public bool    spin        = false;

    private float   t;
    private float   ph;
    private Vector3 spinAxis;

    void Start()
    {
        ph = Mathf.Repeat(startPos.x * 0.13f + startPos.z * 0.29f, 1f);
        t  = ph;
        spinAxis = new Vector3(Random.value - 0.5f, Random.value - 0.5f, Random.value - 0.5f).normalized;
    }

    void Update()
    {
        t += Time.deltaTime * speed;
        if (t >= 1f) t -= 1f;

        float e = t * t;                                   // accélère vers la fin
        Vector3 p = Vector3.Lerp(startPos, target, e);
        p.y += Mathf.Sin(t * Mathf.PI) * arc;              // arc bombé vers le ciel
        float a = t * swirl * Mathf.PI * 2f + ph * 6.2832f;
        float r = swirlRadius * (1f - t);                  // spirale qui se resserre
        p += new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
        transform.position = p;

        transform.localScale = baseScale * Mathf.Lerp(1f, 0.08f, t);   // avalé = rétrécit

        if (faceTarget)
        {
            Vector3 dir = target - transform.position;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
        }
        else if (spin)
        {
            transform.Rotate(spinAxis * (160f * Time.deltaTime), Space.Self);
        }
    }
}
