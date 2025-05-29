using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// gencity.cs:
/// • Grille de rues dense
/// • Subdivision 2×2 lots
/// • Bâtiments générés
/// • Bâtiments avec volumes Cube
/// • Parc central avec échiquier
/// </summary>
public class ProceduralCityGeneratorV3_NoTrees : MonoBehaviour
{
    [Header("Global Scale")] public float globalScale = 1f;
    private Transform cityParent;

    [Header("Dimensions Urbaines")]  
    public int cityWidthMeters = 400;
    public int cityDepthMeters = 400;
    public int streetGridSize  = 25;

    [Header("Subdivision des blocs")]  
    public int lotsPerBlock = 2;
    public float lotMargin   = 2f;

    [Header("Parc central")]  
    public int parkSizeMeters  = 80;
    public int chessboardSize  = 8;
    public float benchWidth     = 2f;

    [Header("Perlin Noise & Seuil")]  
    public float noiseScale        = 0.5f;
    public float buildingThreshold = 0.7f;
    public int   seed              = 12345;

    // Matériaux
    private Material roadMat, sidewalkMat, grassMat, buildingMat;
    private Material lampMat, benchMat, chessWhiteMat, chessBlackMat;

void Start()
{
    GameObject root = new GameObject("Cityv1");
    cityParent = root.transform;
    cityParent.localScale = Vector3.one * globalScale;

    Random.InitState(seed);
    GenerateMaterials();
    GenerateCity();

    //PositionCamera(); // désactivé
    FixCamera();       // version figée
}

void FixCamera()
{
    if (Camera.main == null) return;

    //valeurs depuis l’Inspector
    Vector3 fixedPos = new Vector3(200.6f, 7.8f, 183.3f);
    Vector3 fixedRot = new Vector3(26.415f, 2.853f, 0.768f);

    Camera.main.transform.position = fixedPos;
    Camera.main.transform.rotation = Quaternion.Euler(fixedRot);
}



    void GenerateMaterials()
    {
        roadMat       = CreateMaterial(new Color(0.2f, 0.2f, 0.2f));
        sidewalkMat   = CreateMaterial(Color.gray);
        grassMat      = CreateMaterial(new Color(0.3f, 0.8f, 0.3f));
        buildingMat   = CreateMaterial(new Color(0.8f, 0.8f, 0.8f));
        lampMat       = CreateMaterial(Color.yellow);
        benchMat      = CreateMaterial(new Color(0.4f, 0.2f, 0.1f));
        chessWhiteMat = CreateMaterial(Color.white);
        chessBlackMat = CreateMaterial(Color.black);
    }

    void GenerateCity()
    {
        GenerateRoadGrid();
        GenerateBuildingBlocks();
        GenerateCentralParkWithChess();
    }

    void PositionCamera()
    {
        if (Camera.main == null) return;
        float cx = cityWidthMeters * 0.5f;
        float cz = cityDepthMeters * 0.5f;
        Camera.main.transform.position = new Vector3(cx, cityWidthMeters * 0.6f, cz);
        Camera.main.transform.rotation = Quaternion.Euler(60f, 180f, 0f);
    }

    void GenerateRoadGrid()
{
    int cols = cityWidthMeters / streetGridSize;
    int rows = cityDepthMeters / streetGridSize;

    // Centre et demi-taille du parc
    float cx = cityWidthMeters * 0.5f;
    float cz = cityDepthMeters * 0.5f;
    float parkHalf = parkSizeMeters * 0.5f;

    // Routes verticales (axe X)
    for (int x = 0; x <= cols; x++)
    {
        float wx = x * streetGridSize;
        //saute toute route qui tomberait à l'intérieur du parc
        if (wx > cx - parkHalf && wx < cx + parkHalf)
            continue;

        CreateStrip(new Vector3(wx, 0, 0), cityDepthMeters, roadMat, sidewalkMat, false);
    }

    // Routes horizontales (axe Z)
    for (int z = 0; z <= rows; z++)
    {
        float wz = z * streetGridSize;
        //saute toute route qui tomberait à l'intérieur du parc
        if (wz > cz - parkHalf && wz < cz + parkHalf)
            continue;

        CreateStrip(new Vector3(0, 0, wz), cityWidthMeters, roadMat, sidewalkMat, true);
    }
}


    void GenerateBuildingBlocks()
{
    float halfGrid = streetGridSize * 0.5f;
    float lotSize  = (streetGridSize - lotMargin * 2f) / lotsPerBlock;

    int cols = cityWidthMeters / streetGridSize;
    int rows = cityDepthMeters / streetGridSize;

    float cx = cityWidthMeters * 0.5f;
    float cz = cityDepthMeters * 0.5f;
    float parkHalf = parkSizeMeters * 0.5f;

    for (int x = 0; x < cols; x++)
    for (int z = 0; z < rows; z++)
    {
        float blockCenterX = x * streetGridSize + halfGrid;
        float blockCenterZ = z * streetGridSize + halfGrid;
        if (Mathf.Abs(blockCenterX - cx) < parkHalf && Mathf.Abs(blockCenterZ - cz) < parkHalf)
            continue;

        for (int i = 0; i < lotsPerBlock; i++)
        for (int j = 0; j < lotsPerBlock; j++)
        {
            float lx = x * streetGridSize + lotMargin + lotSize * (i + 0.5f);
            float lz = z * streetGridSize + lotMargin + lotSize * (j + 0.5f);
            Vector3 lotCenter = new Vector3(lx, 0f, lz);

            float noise = Mathf.PerlinNoise((lotCenter.x + seed) * noiseScale, (lotCenter.z + seed) * noiseScale);
            if (noise > buildingThreshold)
                CreateDetailedBuilding(lotCenter, lotSize);
        }
    }
}

void CreateDetailedBuilding(Vector3 center, float size)
{
    // 1) Nombre d'étages et hauteur totale
    int floors   = Random.Range(3, 8);
    float floorH = 2.5f;
    float totalH = floors * floorH;

    // 2) Création du GameObject parent
    GameObject bld = new GameObject($"Bld_{center.x:F0}_{center.z:F0}");
    bld.transform.SetParent(cityParent);

    // 3) Positionnement centré en Y
    bld.transform.position = center + Vector3.up * (totalH * 0.5f);

    // 4) Cube principal (mur plein)
    var mainCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    mainCube.transform.SetParent(bld.transform);
    mainCube.transform.localScale = new Vector3(size, totalH, size);
    mainCube.transform.localPosition = Vector3.zero;
    mainCube.GetComponent<Renderer>().material = buildingMat;

    // 5) Paramètres des fenêtres
    float w     = size * 0.1f;           // largeur de chaque fenêtre
    float h     = floorH * 0.6f;         // hauteur de chaque fenêtre
    float depth = 0.05f;                 // épaisseur (saillie) de la fenêtre
    int cols    = Mathf.FloorToInt(size / (w * 1.5f)); // nb de fenêtres par rangée

    // 6) Création des fenêtres sur chaque façade
    for (int side = 0; side < 4; side++)
    {
        for (int i = 0; i < cols; i++)
        {
            for (int j = 0; j < floors; j++)
            {
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.transform.SetParent(bld.transform);
                win.transform.localScale = new Vector3(w, h, depth);
                win.GetComponent<Renderer>().material = CreateMaterial(Color.gray);

                // Calcul de l'offset local
                float xOff = -size / 2f + w + i * (w * 1.5f);
                float yOff = -totalH / 2f + floorH * j + floorH * 0.5f;
                Vector3 pos = Vector3.zero;
                Quaternion rot = Quaternion.identity;

                switch (side)
                {
                    case 0: // façade avant (+Z)
                        pos = new Vector3(xOff, yOff, size / 2f + depth / 2f + 0.001f);
                        break;
                    case 1: // façade droite (+X)
                        pos = new Vector3(size / 2f + depth / 2f + 0.001f, yOff, -xOff);
                        rot = Quaternion.Euler(0, 90f, 0);
                        break;
                    case 2: // façade arrière (-Z)
                        pos = new Vector3(-xOff, yOff, -size / 2f - depth / 2f - 0.001f);
                        rot = Quaternion.Euler(0, 180f, 0);
                        break;
                    case 3: // façade gauche (-X)
                        pos = new Vector3(-size / 2f - depth / 2f - 0.001f, yOff, xOff);
                        rot = Quaternion.Euler(0, 270f, 0);
                        break;
                }

                win.transform.localPosition = pos;
                win.transform.localRotation = rot;
            }
        }
    }
}


    void GenerateCentralParkWithChess()
    {
        Vector3 center = new Vector3(cityWidthMeters / 2f, 0f, cityDepthMeters / 2f);
        CreatePlane(center + Vector3.up * 0.01f, parkSizeMeters, parkSizeMeters, grassMat);

        float half = parkSizeMeters * 0.5f;
        float pw = benchWidth * 0.5f;
        CreatePlane(center + Vector3.up * 0.02f, parkSizeMeters, pw, CreateMaterial(new Color(0.5f, 0.4f, 0.3f)));
        CreatePlane(center + Vector3.up * 0.02f, pw, parkSizeMeters, CreateMaterial(new Color(0.5f, 0.4f, 0.3f)));

        float s = benchWidth;
        float total = s * chessboardSize;
        Vector3 board = center + Vector3.up * 0.03f;
        board.x -= total * 0.5f - s * 0.5f;
        board.z -= total * 0.5f - s * 0.5f;
        for (int i = 0; i < chessboardSize; i++) for (int j = 0; j < chessboardSize; j++)
            CreateCube(board + new Vector3(i * s, 0f, j * s), s, ((i + j) % 2 == 0) ? chessWhiteMat : chessBlackMat);

        for (int dir = 0; dir < 4; dir++)
        {
            Vector3 offset = dir == 0 ? Vector3.forward : dir == 1 ? Vector3.back : dir == 2 ? Vector3.left : Vector3.right;
            Vector3 pos = center + offset * (half - pw);
            PlaceBenchAndLamp(pos);
        }
    }

    void PlaceBenchAndLamp(Vector3 pos)
    {
        var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bench.transform.SetParent(cityParent);
        bench.transform.position = pos + Vector3.up * 0.2f;
        bench.transform.localScale = new Vector3(benchWidth, 0.3f, 0.5f);
        bench.GetComponent<Renderer>().material = benchMat;

        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(cityParent);
        pole.transform.position = pos + Vector3.up * 1.5f;
        pole.transform.localScale = new Vector3(0.1f, 1.5f, 0.1f);
        pole.GetComponent<Renderer>().material = buildingMat;
        var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.transform.SetParent(cityParent);
        bulb.transform.position = pos + Vector3.up * 3.1f;
        bulb.transform.localScale = Vector3.one * 0.2f;
        bulb.GetComponent<Renderer>().material = lampMat;
    }

    Material CreateMaterial(Color c)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", c);
        return mat;
    }

    void CreateStrip(Vector3 start, float length, Material road, Material sidewalk, bool horizontal)
    {
        float rw = 6f, sw = 3f;
        Vector3 rp = horizontal ? start + Vector3.right * length * 0.5f : start + Vector3.forward * length * 0.5f;
        CreatePlane(rp, horizontal ? length : rw, horizontal ? rw : length, road);
        Vector3 perp = horizontal ? Vector3.forward : Vector3.right;
        Vector3 off = perp * (rw * 0.5f + sw * 0.5f);
        CreatePlane(rp + off, horizontal ? length : sw, horizontal ? sw : length, sidewalk, 0.1f);
        CreatePlane(rp - off, horizontal ? length : sw, horizontal ? sw : length, sidewalk, 0.1f);
    }

    void CreatePlane(Vector3 cen, float w, float d, Material m, float elev = 0f)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.transform.SetParent(cityParent);
        q.transform.position = new Vector3(cen.x, elev, cen.z);
        q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        q.transform.localScale = new Vector3(w, d, 1f);
        q.GetComponent<Renderer>().material = m;
    }

    void CreateCube(Vector3 cen, float s, Material m)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.SetParent(cityParent);
        c.transform.position = cen;
        c.transform.localScale = Vector3.one * s;
        c.GetComponent<Renderer>().material = m;
    }
}
