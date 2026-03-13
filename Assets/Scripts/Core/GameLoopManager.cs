using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager instance;
    public GameObject popupEndGame;

    // the bool define if the entity is still alive
    private List<GameObject> playerUnits = new();
    private List<GameObject> enemyUnits = new();

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) { // right click
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) {
                GameObject clickedObject = hit.collider.gameObject;

                RemoveUnit(clickedObject);
            }
        }
    }

    public void RegisterUnit(GameObject unit, bool isPlayer = false)
    {
        if (isPlayer)
            playerUnits.Add(unit);
        else
            enemyUnits.Add(unit);
        
        BugTracker.Info("New entity add in the list: '" + unit.name + "'");
    }

    private void RemoveUnit(GameObject unit)
    {
        if (playerUnits.Contains(unit)) {
            playerUnits.Remove(unit);
            Destroy(unit);

            BugTracker.Info("'" + unit.name + "' has been remove from the playerUnits list.");
            return;
        }
        if (enemyUnits.Contains(unit)) {
            enemyUnits.Remove(unit);
            Destroy(unit);

            BugTracker.Info("'" + unit.name + "' has been remove from the enemyUnits list.");
            return;
        }
    }

    private void EndGame(bool isPlayerVictorious)
    {
        popupEndGame.SetActive(true);

        if (isPlayerVictorious) {
            popupEndGame.GetComponentInChildren<TextMeshProUGUI>().text = "You win";
        }
    }

    public void CheckVictory()
    {
        int countPlayer = 0;
        int countEnemy = 0;

        foreach (GameObject p in playerUnits) {
            if (p.GetComponent<Units>().isAlive)
                countPlayer++;
        }
        foreach (GameObject e in enemyUnits) {
            if (e.GetComponent<Units>().isAlive)
                countEnemy++;
        }

        if (countPlayer == 0) {
            Debug.Log("Player wins");
            EndGame(true);
        }
        else if (countEnemy == 0) {
            Debug.Log("Enemy wins");
            EndGame(false);
        }
    }

    public void RestartGame()
    {
        popupEndGame.SetActive(false);
        foreach (GameObject key in playerUnits) {
            key.GetComponent<Units>().ResetUnit();
        }
        foreach (GameObject key in enemyUnits) {
            key.GetComponent<Units>().ResetUnit();
        }
        StartOrStopCombat(false);
    }

    public void StartOrStopCombat(bool enabled = true)
    {
        GameObject[] entities = GameObject.FindGameObjectsWithTag("Entities");
        if (entities == null)
            return;

        foreach (GameObject e in entities) {
            e.GetComponentInChildren<Units>().enabled = enabled;
        }
    }
}
