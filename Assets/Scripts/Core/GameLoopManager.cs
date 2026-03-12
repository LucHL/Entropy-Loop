using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager instance;
    public GameObject popupEndGame;

    // the bool define if the entity is still alive
    private Dictionary<GameObject, bool> playerUnits = new();
    private Dictionary<GameObject, bool> enemyUnits = new();

    void Awake()
    {
        instance = this;
    }

    public void RegisterUnit(GameObject unit, bool isPlayer)
    {
        if (isPlayer)
            playerUnits.Add(unit, true);
        else
            enemyUnits.Add(unit, true);
        
        BugTracker.Info("New entity add in the list: '" + unit.name + "'");
    }

    public void UnitDied(GameObject unit)
    {
        try {
            playerUnits[unit] = false; // TODO change this code hahaha
        } catch {
            enemyUnits[unit] = false;
        }
        CheckVictory();

        BugTracker.Info("Entity remove from list of entities alive: " + unit.name);
    }

    public void RemoveEntity(GameObject unit)
    {
        try {
            playerUnits.Remove(unit); // TODO change this code hahaha
        } catch {
            enemyUnits.Remove(unit);
        }
    }

    private void EndGame(bool isPlayerVictorious)
    {
        if (isPlayerVictorious) {
            popupEndGame.GetComponentInChildren<TextMeshPro>().text = "You win";
        }
        popupEndGame.SetActive(true);
    }

    private void CheckVictory()
    {
        int countPlayer = 0;
        int countEnemy = 0;

        foreach (KeyValuePair<GameObject, bool> p in playerUnits) {
            if (p.Value)
                countPlayer++;
        }
        foreach (KeyValuePair<GameObject, bool> e in enemyUnits) {
            if (e.Value)
                countEnemy++;
        }

        if (countPlayer == 0) {
            Debug.Log("Enemy wins");
            EndGame(false);
        }
        else if (countEnemy == 0) {
            Debug.Log("Player wins");
            EndGame(true);
        }
    }

    public void RestartGame()
    {
        foreach (GameObject key in playerUnits.Keys) {
            playerUnits[key] = true;
            key.GetComponent<Units>().ResetUnit();
        }
        foreach (GameObject key in enemyUnits.Keys) {
            enemyUnits[key] = true;
            key.GetComponent<Units>().ResetUnit();
        }
    }
}
