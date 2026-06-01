using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager instance;
    public GameObject popupEndGame;
    public bool isGameRunning { get; set; } = false;

    private List<GameObject> playerUnits = new();
    private List<GameObject> enemyUnits = new();

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (isGameRunning)
            return;

        if (Input.GetMouseButtonDown(1)) { // right click
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit)) {
                GameObject clickedObject = hit.collider.gameObject;

                if (clickedObject.GetComponentInChildren<EntityTeam>() != null && clickedObject.GetComponentInChildren<EntityTeam>().CompareTag("Champion"))
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
        
        BugTracker.Info("New entity add in the list: '" + unit.name + "'.");
    }

    private void RemoveUnit(GameObject unit)
    {
        if (playerUnits.Contains(unit)) {
            playerUnits.Remove(unit);
            ManaManager.instance.AddMana(unit.GetComponent<Units>().manaCost);
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
        isGameRunning = false;
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
            if (p.GetComponent<Units>().isAlive) {
                Debug.Log(p.name + ": player");
                countPlayer++;
            } else
                StartCoroutine(DesableEntityAfterDeath(p));
        }
        foreach (GameObject e in enemyUnits) {
            if (e.GetComponent<Units>().isAlive) {
                Debug.Log(e.name + ": enemy");
                countEnemy++;
            } else
                StartCoroutine(DesableEntityAfterDeath(e));
        }

        if (countPlayer == 0) {
            Debug.Log("Enemy wins");
            EndGame(false);
        } else if (countEnemy == 0) {
            Debug.Log("Player wins");
            EndGame(true);
        }
    }

    public void RestartGame()
    {
        popupEndGame.SetActive(false);
        foreach (GameObject key in playerUnits) {
            key.SetActive(true);
            key.GetComponent<Units>().ResetUnit();
        }
        foreach (GameObject key in enemyUnits) {
            key.SetActive(true);
            key.GetComponent<Units>().ResetUnit();
        }
        StartOrStopCombat(false);

        ManaManager.instance.AddMana(3);
    }

    public void StartOrStopCombat(bool enabled = true)
    {
        isGameRunning = enabled;

        GameObject[] entities = GameObject.FindGameObjectsWithTag("Entities");
        if (entities == null)
            return;

        foreach (GameObject e in entities) {
            e.GetComponentInChildren<Units>().isGameRunning = enabled;
        }
    }

    private IEnumerator DesableEntityAfterDeath(GameObject entity)
    {
        yield return new WaitForSeconds(1f);
        entity.SetActive(false);
    }
}
