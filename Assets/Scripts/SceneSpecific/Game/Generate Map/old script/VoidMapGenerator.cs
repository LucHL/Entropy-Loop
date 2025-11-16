using UnityEngine;
using UnityEditor;
using System.IO;

public class VoidMapGenerator : MonoBehaviour
{
    [Header("Room Settings")]
    public int roomWidth = 6;
    public int roomHeight = 6;
    public float spacing = 1.2f;

    [Header("Assets")]
    public GameObject tilePrefab;
    public GameObject portalPrefab;

    private const string TILE_PATH = "Assets/Prefabs/VoidTile.prefab";
    private const string PORTAL_PATH = "Assets/Prefabs/VoidPortal.prefab";
    private const string MATERIAL_PATH = "Assets/Materials/Void_Mat.mat";

    void Start()
    {
#if UNITY_EDITOR
        InitAssets();
#endif
        LoadAssets();
        GenerateRoom();
    }

    void InitAssets()
    {
        CreateFolders();
        CreateVoidMaterial();
        CreateVoidTilePrefab();
        CreatePortalPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("🌀 Assets initialized");
    }

    void CreateFolders()
    {
        string[] folders = { "Assets/Prefabs", "Assets/Materials", "Assets/Models" };
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }
    }

    void CreateVoidMaterial()
    {
        if (!File.Exists(MATERIAL_PATH))
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.name = "Void_Mat";
            mat.color = new Color(0.3f, 0.0f, 0.5f); // violet
            mat.SetFloat("_Glossiness", 0.2f);
            AssetDatabase.CreateAsset(mat, MATERIAL_PATH);
        }
    }

    void CreateVoidTilePrefab()
    {
        if (!File.Exists(TILE_PATH))
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "VoidTile";
            tile.transform.localScale = new Vector3(1, 0.2f, 1);

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            tile.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tile, TILE_PATH);
            GameObject.DestroyImmediate(tile);
        }
    }

    void CreatePortalPrefab()
    {
        if (!File.Exists(PORTAL_PATH))
        {
            GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portal.name = "VoidPortal";
            portal.transform.localScale = new Vector3(0.6f, 0.05f, 0.6f);

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            portal.GetComponent<Renderer>().sharedMaterial = mat;

            // Add effect or visual
            Light light = portal.AddComponent<Light>();
            light.color = new Color(0.5f, 0f, 1f);
            light.intensity = 3f;
            light.range = 5f;
            light.type = LightType.Point;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(portal, PORTAL_PATH);
            GameObject.DestroyImmediate(portal);
        }
    }

    void LoadAssets()
    {
#if UNITY_EDITOR
        tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TILE_PATH);
        portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PORTAL_PATH);
#endif
    }

    void GenerateRoom()
    {
        if (tilePrefab == null || portalPrefab == null)
        {
            Debug.LogError("❌ Missing prefabs.");
            return;
        }

        for (int x = 0; x < roomWidth; x++)
        {
            for (int z = 0; z < roomHeight; z++)
            {
                Vector3 pos = new Vector3(x * spacing, 0, z * spacing);
                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{z}";

                // Portail aux bords
                bool isEdge = x == 0 || x == roomWidth - 1 || z == 0 || z == roomHeight - 1;
                if (isEdge && Random.value < 0.25f)
                {
                    Vector3 portalPos = pos + Vector3.up * 0.3f;
                    GameObject portal = Instantiate(portalPrefab, portalPos, Quaternion.identity, tile.transform);
                    portal.name = "Portal";
                }
            }
        }

        Debug.Log($"✅ Room generated ({roomWidth} x {roomHeight})");
    }
}
