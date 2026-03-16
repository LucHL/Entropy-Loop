using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnAlgo : MonoBehaviour
{
    public static EnemySpawnAlgo instance;
    public int numberEnemy = 3;

    public GameObject enemyPrefab;

    public void Awake()
    {
        instance = this;
        enemyPrefab = Resources.Load<GameObject>("Enemy_tmp");
    }

    public void SpawnEnemies()
    {
        GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");

        List<GameObject> spawnTiles = new();

        int maxY = int.MinValue;

        foreach (GameObject tile in tiles)
        {
            string[] parts = tile.name.Split('_');
            int y = int.Parse(parts[2]);

            if (y > maxY)
                maxY = y;
        }

        foreach (GameObject tile in tiles)
        {
            string[] parts = tile.name.Split('_');
            int y = int.Parse(parts[2]);

            if (y >= maxY - 3)
                spawnTiles.Add(tile);
        }

        for (int i = 0; i < numberEnemy && spawnTiles.Count > 0; i++)
        {
            int index = Random.Range(0, spawnTiles.Count);
            GameObject tile = spawnTiles[index];
            spawnTiles.RemoveAt(index);

            Vector3 spawnPos = tile.transform.position + Vector3.up * 2f;

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            enemy.transform.Rotate(0, 180, 0);

            enemy.GetComponent<NavMeshAgent>().enabled = false;

            GameLoopManager.instance.RegisterUnit(enemy, false);
        }
        // // TMP Create a Prefab
        // GameObject prefabEnemy = Resources.Load<GameObject>("Enemy_tmp");
        // GameObject enemyInstance = Instantiate(prefabEnemy, new Vector3(0f, 2f, 0f), Quaternion.identity);
        // enemyInstance.transform.Rotate(0, 180, 0);

        // // Movements are NOT managed by the navmesh
        // enemyInstance.GetComponent<NavMeshAgent>().enabled = false;

        // GameLoopManager.instance.RegisterUnit(enemyInstance, false);
    }




    // public interface UnitsOnBoard
    // {
    //     Units units;
    //     int tileNumber;
    // }

    // public Vector2 chessboardSize = new( 6, 6 );
    // public Vector2 chessTileSize;
    // public List<Units> enemyListAvailable;
    // public List<Units> enemyListToPlaceOnBoard;
    // public List<UnitsOnBoard> unitsOnBoards;
    // public int DifficultyLevel = 1;

    // private List board = new [
    //     [0, 0, 0, 0, 0, 0],
    //     [0, 0, 0, 0, 0, 0],
    //     [0, 0, 0, 0, 0, 0],
    //     // do not use more, this is the player board
    //     [0, 0, 0, 0, 0, 0],
    //     [0, 0, 0, 0, 0, 0],
    //     [0, 0, 0, 0, 0, 0],
    // ]; // create board form 'chessboardSize'

    // private void DefineNumberOfEnemy()
    // {
    //     int sizeBoardEnemy = chessboardSize.Y / 2;
    //     int nbrEnemy = sizeBoardEnemy / 4; // change this by using a tree with afinity for each units

    //     for (int i = 0; i < nbrEnemy; i++) {
    //         EnemyListToPlaceOnBoard.Append(EnemyListAvailable[Random.Next(EnemyListAvailable.Count)]);
    //     }
    // }

    // private void PutEnemyInBoard()
    // {
    //     int sizeBoardEnemy = chessboardSize.Y / 2;

    //     for (int i = 0; i < EnemyListToPlaceOnBoard.Count; ++i) {
    //         bool skip = false;
    //         int randomRow = Random.Next(chessboardSize.X);
    //         int randomColumn = Random.Next(chessboardSize.Y / 2);
    //         int tileNbr = randomColumn * chessboardSize.Y + randomRow;

    //         for (int e = 0; unitsOnBoards.Count; e++) {
    //             if (unitsOnBoards[i].tileNumber == tileNbr) {
    //                 skip = true;
    //                 i--;
    //                 break;
    //             }
    //         }
    //         if (skip)
    //             continue;
    //         UnitsOnBoard newUnits = { EnemyListToPlaceOnBoard[i], tileNbr };
    //         unitsOnBoards.Add();
    //     }
    // }

    // public void ClearEnemyListToPlaceOnBoard()
    // {
    //     EnemyListToPlaceOnBoard.Clear();
    // }

    // public void SelectEnemyDeck(String EnemyFolder)
    // {

    // }
}
