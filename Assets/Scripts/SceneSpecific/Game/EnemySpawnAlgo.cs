using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnAlgo : MonoBehaviour
{
    public static EnemySpawnAlgo instance;
    public int numberEnemy = 3;

    public void Awake()
    {
        instance = this;
    }

    public void SpawnEnemies(float tileSize)
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

        foreach (GameObject t in tiles)
        {
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
