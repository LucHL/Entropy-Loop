using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.Tilemaps;

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

    private HashSet<string> occupiedGridPositions = new();
    private float _tileSize;
    private int maxEntity = 10;

    public void Awake()
    {
        instance = this;
    }

    public void SpawnEnemies(float tileSize)
    {
        BugTracker.Info("[EnemySpawnAlgo] Start algo spawn enemy.");
        List<GameObject> allEntities = new();

        allEntities.Add(Resources.Load<GameObject>("CrocodilePrefab"));
        allEntities.Add(Resources.Load<GameObject>("TigerPrefab"));
        allEntities.Add(Resources.Load<GameObject>("Enemy_tmp"));
        
        currentStrategy = GameManager.instance.currentStrategy;
        manaMax = GameManager.instance.currentManaCost;

        _tileSize = tileSize;
        int nbrEntities = 0;
        int safetyExit = 0;
        currentMana = manaMax;

        BugTracker.Info("[EnemySpawnAlgo] current algo strategy: '"+currentStrategy+"'.");
        BugTracker.Info("[EnemySpawnAlgo] max mana: "+manaMax+".");

        List<GameObject> filtered = FilteredByStrategy(allEntities, currentStrategy);
        List<GameObject> affordableUnits = new();

        while (currentMana > 0 && safetyExit < 100 && nbrEntities <= maxEntity) {
            safetyExit++;

            GameObject entity = filtered[Random.Range(0, filtered.Count)];
            if (currentMana - entity.GetComponent<Units>().manaCost >= 0) {
                currentMana -= entity.GetComponent<Units>().manaCost;
                affordableUnits.Add(entity);
                nbrEntities++;
                BugTracker.Info("[EnemySpawnAlgo] Entity '"+entity.name+"' add to the spawn list.");
            }
        }
        IntiateEntity(affordableUnits);
    }

    private void IntiateEntity(List<GameObject> entities)
    {
        if (entities.IsUnityNull()) {
            BugTracker.Error("[EnemySpawnAlgo] List of entities is null, failed to spawn entities.");
            return;
        }

        foreach (GameObject e in entities) {
            Vector2 vector2 = GetStrategicTilePosition(e.GetComponent<Units>().unitsClass);

            if (vector2 == new Vector2(-1f, -1f)) {
                BugTracker.Error("'" + e.name + "' vector2 is -1f, failed to instantiate units.");
                continue;
            }

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

        foreach (GameObject t in tiles) {
            string[] parts = t.name.Split('_');
            int x = int.Parse(parts[1]);
            int y = int.Parse(parts[2]);

            if (y > maxY)
                maxY = y;
            if (x > maxX)
                maxX = x;
        }

        int halfBoard = maxY / 2;

        int enemyZoneHeight = halfBoard + 1;

        float slice = (float)enemyZoneHeight / 4f;

        int RearY = 0;
        int FrontY = halfBoard;

        switch (unitClass) {
            case UnitsClass.Tank:
                RearY = halfBoard + 1;
                FrontY = Mathf.FloorToInt(halfBoard + slice);
                break;

            case UnitsClass.Dps:
            case UnitsClass.Assassin:
            case UnitsClass.Support:
                RearY = Mathf.FloorToInt(halfBoard + slice);
                FrontY = Mathf.FloorToInt(halfBoard + (slice * 2));
                break;

            case UnitsClass.Archer:
            case UnitsClass.Mage:
                RearY = Mathf.FloorToInt(halfBoard + (slice * 2));
                FrontY = Mathf.FloorToInt(halfBoard + (slice * 3));
                break;

            case UnitsClass.Healer:
            case UnitsClass.Buffer:
                RearY = Mathf.FloorToInt(halfBoard + (slice * 3));
                FrontY = maxY;
                break;
        }

        RearY = Mathf.Clamp(RearY, halfBoard + 1, maxY);
        FrontY = Mathf.Clamp(FrontY, halfBoard + 1, maxY);

        List<GameObject> validTiles = new();

        foreach (GameObject t in tiles) {
            string[] parts = t.name.Split('_');
            int x = int.Parse(parts[1]);
            int y = int.Parse(parts[2]);

            if (x <= maxX && y >= RearY && y <= FrontY) {
                if (!validTiles.Contains(t) && !occupiedGridPositions.Contains(t.name)) {
                    validTiles.Add(t);
                }
            }
        }

        if (validTiles.Count > 0) {
            GameObject chosenTile = validTiles[Random.Range(0, validTiles.Count)];
            occupiedGridPositions.Add(chosenTile.name);

            return new Vector2(chosenTile.transform.position.x, chosenTile.transform.position.z);
        }
        return new Vector2(-1f, -1f);
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
}
