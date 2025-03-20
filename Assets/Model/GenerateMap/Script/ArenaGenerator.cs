using UnityEngine;

public class ArenaGenerator : MonoBehaviour
{
    // 📌 Noms des modèles à charger depuis Resources/
    public string floorName = "Floor";
    public string grassPrefabName = "GrassPrefab";  // ✅ Vérifier qu'il est bien dans Resources/
    public string wallName = "wall";
    public string treeName = "tree1";
    public string rock1Name = "rock1";
    public string rock2Name = "rock2";
    public string lanternName = "lantern";

    // 📌 Paramètres de la map
    public float mapSize = 80f; // Taille totale de la map
    public Vector3 floorScale = new Vector3(40, 1, 40); // 🎯 Taille du Floor (sera écrasée par les nouvelles valeurs)
    public Vector3 floorPosition = new Vector3(22, 1, 20); // 🎯 Position du Floor (sera écrasée par les nouvelles valeurs)
    public int gridSize = 10; // Nombre de tuiles d’herbe (augmenter si besoin)

    void Start()
    {
        Debug.Log("📂 Vérification des fichiers disponibles dans Resources/...");
        Object[] allObjects = Resources.LoadAll("", typeof(GameObject));
        foreach (Object obj in allObjects)
        {
            Debug.Log("🔹 Trouvé : " + obj.name);
        }

        Debug.Log("🏗 Lancement de la génération de l'arène...");
        GenerateArena();
    }

    void GenerateArena()
    {
        Debug.Log("📢 Début de la génération de l’arène...");

        GenerateGrassFloor();
        Debug.Log("✅ Sol généré !");

        // ✅ Modification ici : Application des réglages du Floor
        GameObject floor = LoadModel(floorName, new Vector3(5.6f, 0.4f, 2.7f), Quaternion.identity, new Vector3(4000, 1, 4000));
        if (floor == null)
        {
            Debug.LogError("❌ Floor non généré !");
            return;
        }
        Debug.Log("✅ Floor placé en " + floor.transform.position + " avec échelle : " + floor.transform.localScale);

        GenerateWalls();
        Debug.Log("✅ Murs générés !");

        GenerateLanterns();
        Debug.Log("✅ Lanternes générées !");

        GenerateSurroundingElements();
        Debug.Log("✅ Objets générés autour du Floor !");

        Debug.Log("✅ Arène terminée !");
    }

    void GenerateGrassFloor()
    {
        Debug.Log("🌱 Début de la génération du sol en herbe (GrassPrefab)...");

        float tileSize = 10f;
        Vector3 grassScale = new Vector3(10, 1, 10);

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 position = new Vector3(x * tileSize, 0, z * tileSize);

                // ⚠ Ne pas placer de l’herbe sous Floor
                if (position.x >= floorPosition.x - floorScale.x / 2 && position.x <= floorPosition.x + floorScale.x / 2 &&
                    position.z >= floorPosition.z - floorScale.z / 2 && position.z <= floorPosition.z + floorScale.z / 2)
                    continue;

                GameObject grass = LoadModel(grassPrefabName, position, Quaternion.identity, grassScale);

                if (grass == null)
                {
                    Debug.LogError("❌ GrassPrefab introuvable ! Vérifie son nom et son emplacement dans Resources/.");
                    return;
                }
                DisableShadows(grass);
            }
        }
        Debug.Log("✅ Sol en herbe (GrassPrefab) généré avec succès !");
    }

    void GenerateSurroundingElements()
    {
        float minDist = floorScale.x / 2 + 5f;

        GenerateRandomObjects(treeName, 10, minDist);
        GenerateRandomObjects(rock1Name, 5, minDist);
        GenerateRandomObjects(rock2Name, 5, minDist);
    }

    void GenerateRandomObjects(string modelName, int count, float minDist)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos;
            do
            {
                randomPos = new Vector3(
                    Random.Range(0, mapSize),
                    0.01f,
                    Random.Range(0, mapSize)
                );
            }
            while (randomPos.x >= floorPosition.x - minDist && randomPos.x <= floorPosition.x + minDist &&
                   randomPos.z >= floorPosition.z - minDist && randomPos.z <= floorPosition.z + minDist);

            GameObject obj = LoadModel(modelName, randomPos, Quaternion.Euler(0, Random.Range(0, 360), 0), new Vector3(3, 3, 3));
            if (obj != null)
            {
                DisableShadows(obj);
            }
        }
        Debug.Log("🌿 Objets aléatoires (" + modelName + ") placés autour du Floor !");
    }

    void GenerateWalls()
    {
        float wallHeight = 5f;

        LoadModel(wallName, new Vector3(mapSize / 2, wallHeight / 2, mapSize), Quaternion.identity, new Vector3(15, 6, 1));
        LoadModel(wallName, new Vector3(mapSize / 2, wallHeight / 2, 0), Quaternion.identity, new Vector3(15, 6, 1));
        LoadModel(wallName, new Vector3(mapSize, wallHeight / 2, mapSize / 2), Quaternion.Euler(0, 90, 0), new Vector3(15, 6, 1));
        LoadModel(wallName, new Vector3(0, wallHeight / 2, mapSize / 2), Quaternion.Euler(0, 90, 0), new Vector3(15, 6, 1));

        Debug.Log("🧱 Grands murs générés !");
    }

    void GenerateLanterns()
    {
        float height = 2f;
        Vector3[] lanternPositions = {
            new Vector3(floorPosition.x + floorScale.x / 2, height, floorPosition.z + floorScale.z / 2),
            new Vector3(floorPosition.x - floorScale.x / 2, height, floorPosition.z + floorScale.z / 2),
            new Vector3(floorPosition.x + floorScale.x / 2, height, floorPosition.z - floorScale.z / 2),
            new Vector3(floorPosition.x - floorScale.x / 2, height, floorPosition.z - floorScale.z / 2)
        };

        foreach (Vector3 pos in lanternPositions)
        {
            GameObject lantern = LoadModel(lanternName, pos, Quaternion.identity, new Vector3(2, 2, 2));
            if (lantern != null)
            {
                DisableShadows(lantern);
            }
        }
        Debug.Log("🏮 Lanternes placées autour du Floor !");
    }

    GameObject LoadModel(string modelName, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Debug.Log("🔍 Chargement du modèle : " + modelName);

        GameObject prefab = Resources.Load<GameObject>(modelName);
        if (prefab == null)
        {
            Debug.LogError("⚠️ Modèle " + modelName + " NON TROUVÉ dans Resources/ !");
            return null;
        }

        GameObject obj = Instantiate(prefab, position, rotation);
        obj.transform.localScale = scale;
        obj.name = modelName;

        DisableShadows(obj);
        return obj;
    }

    void DisableShadows(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
