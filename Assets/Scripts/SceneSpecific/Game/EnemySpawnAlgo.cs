using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

public enum Strategy {
    SmallUnits, // plein de petites units
    BigUnits, // peux d'units mais forte
    Boss, // boss avec ou sans units
    Aggressive, // max de dps
    Turtle, // max de défence
    Ranged // max d'attaque à distance
}

public class EnemySpawnAlgo : MonoBehaviour
{
    public static EnemySpawnAlgo instance;
    public int manaMax = 10;
    public int currentMana;
    public Strategy currentStrategy;
    public int maxEntity = 3;

    private HashSet<Vector2> occupiedGridPositions = new();

    public void Awake()
    {
        instance = this;
    }

    public void SpawnEnemies(float tileSize)
    {
        BugTracker.Info("[Enemy Spawn Algo] Start algo spawn enemy.");
        List<GameObject> allEntities = new();

        allEntities.Add(Resources.Load<GameObject>("CrocodilePrefab"));
        allEntities.Add(Resources.Load<GameObject>("TigerPrefab"));
        allEntities.Add(Resources.Load<GameObject>("Enemy_tmp"));
        
        int nbrEntities = 0;
        int safetyExit = 0;
        currentMana = manaMax;

        currentStrategy = Strategy.Aggressive;
        BugTracker.Info("[Enemy Spawn Algo] current algo strategy: '"+currentStrategy+"'.");

        List<GameObject> filtered = FilteredByStrategy(allEntities, currentStrategy);
        List<GameObject> affordableUnits = new();

        while (currentMana > 0 && safetyExit < 100 && nbrEntities < maxEntity) {
            safetyExit++;

            GameObject entity = filtered[Random.Range(0, filtered.Count)];
            if (entity.GetComponent<Units>().manaCost - currentMana <= 0) {
                currentMana -= entity.GetComponent<Units>().manaCost;
                affordableUnits.Add(entity);
                nbrEntities++;
                BugTracker.Info("[Enemy Spawn Algo] Entity '"+entity.name+"' add to the spawn list.");
            }
        }
        IntiateEntity(affordableUnits);
    }

    private void IntiateEntity(List<GameObject> entities)
    {
        if (entities.IsUnityNull()) {
            BugTracker.Error("[Enemy Spawn Algo] List of entities is null, failed to spawn entities.");
            return;
        }

        foreach (GameObject e in entities) {
            Vector2 vector2 = GetStrategicTilePosition(e.GetComponent<Units>().unitsClass);
            occupiedGridPositions.Add(vector2);

            GameObject entityInstance = Instantiate(e, new Vector3(vector2.x, 2f, vector2.y), Quaternion.identity);
            entityInstance.transform.Rotate(0, 180, 0);
            GameLoopManager.instance.RegisterUnit(entityInstance, false);

            BugTracker.Info("'" + entityInstance.name + "' spawn.");
        }
    }

    private Vector2 GetStrategicTilePosition(UnitsClass unitClass)
    {
        GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
        
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        // 1. Calcul de la taille du plateau
        foreach (GameObject t in tiles) {
            string[] parts = t.name.Split('_');
            int x = int.Parse(parts[1]);
            int y = int.Parse(parts[2]);

            if (y > maxY)
                maxY = y;
            if (x > maxX)
                maxX = x;
        }

        int halfBoard = maxY / 2; // Ex: Si maxY = 7, halfBoard = 3 (Zone ennemie = 0 à 3)

        int enemyZoneHeight = halfBoard + 1; // Ex: de 0 à 3, cela fait 4 lignes

        float slice = (float)enemyZoneHeight / 4f; // Ex: 4 / 4 = 1.0f

        int RearY = 0;
        int FrontY = halfBoard;

        switch (unitClass) {
            case UnitsClass.Healer:
            case UnitsClass.Buffer:
                RearY = 0;
                FrontY = Mathf.FloorToInt((slice * 1));
                break;

            case UnitsClass.Archer:
            case UnitsClass.Mage:
                RearY = Mathf.FloorToInt(slice * 1);
                FrontY = Mathf.FloorToInt((slice * 2));
                break;

            case UnitsClass.Dps:
            case UnitsClass.Assassin:
                RearY = Mathf.FloorToInt(slice * 2);
                FrontY = Mathf.FloorToInt((slice * 3));
                break;

            case UnitsClass.Tank:
                RearY = Mathf.FloorToInt(slice * 3);
                FrontY = halfBoard;
                break;
        }

        RearY = Mathf.Clamp(RearY, 0, halfBoard);
        FrontY = Mathf.Clamp(FrontY, 0, halfBoard);

        Debug.Log($"[{unitClass}] Zone cible Y : de la ligne {RearY} à {FrontY}");

        // // 3. Filtrer pour ne garder que les cases valides ET libres
        // List<GameObject> validTiles = new List<GameObject>();

        // foreach (GameObject t in tiles) {
        //     string[] parts = t.name.Split('_');
        //     int x = int.Parse(parts[1]);
        //     int y = int.Parse(parts[2]);

        //     if (x <= maxX && y >= targetMinY && y <= targetMaxY) {
        //         // On vérifie dans le HashSet que la case n'est pas déjà prise
        //         if (!occupiedGridPositions.Contains(new Vector2(x, y))) {
        //             validTiles.Add(t);
        //         }
        //     }
        // }

        // // 4. Plan B si la zone idéale est pleine (on cherche partout sur la moitié haute)
        // if (validTiles.Count == 0) {
        //     foreach (GameObject t in tiles) {
        //         string[] parts = t.name.Split('_');
        //         int x = int.Parse(parts[1]);
        //         int y = int.Parse(parts[2]);

        //         if (x <= maxX && y > halfBoard) {
        //             if (!occupiedGridPositions.Contains(new Vector2(x, y))) {
        //                 validTiles.Add(t);
        //             }
        //         }
        //     }
        // }

        // // 5. ON RETOURNE LA POSITION SPATIALE (Vector2) DE LA CASE CHOISIE
        // if (validTiles.Count > 0) {
        //     GameObject chosenTile = validTiles[Random.Range(0, validTiles.Count)];
            
        //     // On bloque la case dans le HashSet pour les prochains ennemis
        //     string[] parts = chosenTile.name.Split('_');
        //     int finalX = int.Parse(parts[1]);
        //     int finalY = int.Parse(parts[2]);
        //     occupiedGridPositions.Add(new Vector2(finalX, finalY));

        //     // On extrait la position X et Y du monde (exactement comme dans ton GetRandomTilePosition)
        //     return new Vector2(chosenTile.transform.position.x, chosenTile.transform.position.y);
        // }

        // // S'il n'y a plus aucune case sur le plateau, on retourne une valeur d'erreur
        // Debug.LogWarning("Tile doesn't exist ou le plateau est plein.");
        // return new Vector2(-1f, -1f); 

        return new Vector2(0f, 0f);
    }


    private List<GameObject> FilteredByStrategy(List<GameObject> allEntity, Strategy strategy)
    {
        switch (strategy) {
            case Strategy.SmallUnits:
                return allEntity.OrderBy(go => go.GetComponent<Units>().manaCost <= 3).ToList();

            case Strategy.BigUnits:
                return allEntity.OrderBy(go => go.GetComponent<Units>().manaCost >= 5).ToList();

            // case Strategy.Boss:
            //     return allEntity;

            case Strategy.Aggressive:
                return allEntity.Where(go => 
                    go.GetComponent<Units>().unitsClass == UnitsClass.Dps || 
                    go.GetComponent<Units>().unitsClass == UnitsClass.Assassin || 
                    go.GetComponent<Units>().unitsClass == UnitsClass.Buffer).ToList();

            case Strategy.Turtle:
                return allEntity.Where(go => 
                    go.GetComponent<Units>().unitsClass == UnitsClass.Tank || 
                    go.GetComponent<Units>().unitsClass == UnitsClass.Healer || 
                    go.GetComponent<Units>().unitsClass == UnitsClass.Buffer).ToList();

            case Strategy.Ranged:
                return allEntity.Where(go => 
                    go.GetComponent<Units>().unitsClass == UnitsClass.Mage || 
                    go.GetComponent<Units>().unitsClass == UnitsClass.Archer).ToList();

            default:
                return allEntity;
        }
    }


    public void SpawnEnemiesssssss(float tileSize)
    {
        Vector2 tilePos = GetRandomTilePosition();
        // x = tilePos.x + (tileSize / 2);
        // y = tilePos.y + (tileSize / 2);

        GameObject cocodilePrefab = Resources.Load<GameObject>("CrocodilePrefab");
        // GameObject cocodileInstance = Instantiate(cocodilePrefab, new Vector3(tilePos.x, 2f, tilePos.y), Quaternion.identity);
        GameObject cocodileInstance = Instantiate(cocodilePrefab, new Vector3(-2f, 2f, 0f), Quaternion.identity);
        cocodileInstance.transform.Rotate(0, 180, 0);

        // enemyInstance.GetComponent<NavMeshAgent>().enabled = false;

        GameLoopManager.instance.RegisterUnit(cocodileInstance, false);

        BugTracker.Info("CocodilePrefab spawn.");

        // TMP Create a Prefab
        while((tilePos = GetRandomTilePosition()) != tilePos);

        // x = tilePos.x + (tileSize / 2);
        // y = tilePos.y + (tileSize / 2);

        GameObject tigerPrefab = Resources.Load<GameObject>("TigerPrefab");
        // GameObject tigerInstance = Instantiate(tigerPrefab, new Vector3(tilePos.x, 2f, tilePos.y), Quaternion.identity);
        GameObject tigerInstance = Instantiate(tigerPrefab, new Vector3(0f, 2f, 0f), Quaternion.identity);
        tigerInstance.transform.Rotate(0, 180, 0);

        // enemyInstance.GetComponent<NavMeshAgent>().enabled = false;

        GameLoopManager.instance.RegisterUnit(tigerInstance, false);

        BugTracker.Info("TigerPrefab spawn.");
        // EnemySpawnAlgo.instance.SpawnEnemies();

        // TMP Create a Prefab
        while((tilePos = GetRandomTilePosition()) != tilePos);

        // x = tilePos.x + (tileSize / 2);
        // y = tilePos.y + (tileSize / 2);

        GameObject enemy_tmpPrefab = Resources.Load<GameObject>("Enemy_tmp");
        // GameObject enemy_tmpInstance = Instantiate(enemy_tmpPrefab, new Vector3(tilePos.x, 2f, tilePos.y), Quaternion.identity);
        GameObject enemy_tmpInstance = Instantiate(enemy_tmpPrefab, new Vector3(2f, 2f, 0f), Quaternion.identity);
        enemy_tmpInstance.transform.Rotate(0, 180, 0);

        // enemyInstance1.GetComponent<NavMeshAgent>().enabled = false;

        GameLoopManager.instance.RegisterUnit(enemy_tmpInstance, false);

        BugTracker.Info("Enemy_tmp spawn.");
    }

    private Vector2 GetRandomTilePosition()
    {
        GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
        
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (GameObject t in tiles) {
            string[] parts = t.name.Split('_');
            int x = int.Parse(parts[1]);
            int y = int.Parse(parts[2]);

            if (y > maxY)
                maxY = y;
            if (x > maxX)
                maxX = x;
        }

        int randomX = Random.Range(0, maxX);
        int randomY = Random.Range(0, maxY / 2); // only half of the board

        foreach (GameObject t in tiles) {
            if (t.name == ("Tile_" + randomX + "_" + randomY))
                return new Vector2(t.transform.position.x, t.transform.position.y);
        }
        Debug.Log("Tile doesn't exist.");
        return new Vector2(0, 0);
    }

    // --------------------------------------
    
    // public List<GameObject> enemyPrefabsPool; 

    // private HashSet<Vector2> occupiedTiles = new HashSet<Vector2>();

    // private int minY = 4;
    // private int maxY = 7;

    // public void GenerateVague(int totalBudget)
    // {
    //     occupiedTiles.Clear();
    //     int currentBudget = totalBudget;

    //     int safetyCheck = 0; 

    //     while (currentBudget > 0 && safetyCheck < 100) {
    //         safetyCheck++;

    //         GameObject randomPrefab = Resources.Load<GameObject>("Enemy_tmp");
            
    //         Units unitScript = randomPrefab.GetComponent<Units>();
            
    //         if (unitScript == null)
    //             continue;

    //         int unitCost = unitScript.manaCost;

    //         if (unitCost > currentBudget)
    //             continue; 

    //         // Vector2 spawnPos = GetStrategicPos(unitScript);

    //         // if (spawnPos != new Vector2(-1, -1)) {
    //         //     GameObject spawnedEnemy = Instantiate(randomPrefab, new Vector3(spawnPos.x, 2f, spawnPos.y), Quaternion.identity);
    //         //     occupiedTiles.Add(spawnPos);
    //         //     currentBudget -= unitCost;
    //         // } else
    //         //     break;
    //     }
    // }

    // private void GetStrategicPos(Units unitScript)
    // {
    //     List<Vector2> potentialTiles = new();

    //     int targetMinY = (unitScript.attackRange <= 1.5f) ? 4 : 6;
    //     int targetMaxY = (unitScript.attackRange <= 1.5f) ? 5 : 7;


    //     if (unitScript.unitsClass.Contains(UnitsClass.Tank) || unitScript.unitsClass.Contains(UnitsClass.Dps)) {
            
    //     }
    //     if (unitScript.unitsClass.Contains(UnitsClass.Archer) || unitScript.unitsClass.Contains(UnitsClass.Dps)) {
            
    //     }
    // }
}
