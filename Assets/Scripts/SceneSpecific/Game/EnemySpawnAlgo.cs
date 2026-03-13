using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class EnemySpawnAlgo : MonoBehaviour
{
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
