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

    [Header("Deck & Mana manager")]
    public CardUI selectedCard;
    public ManaManager manaManager;
    [SerializeField] GameObject settings;
    public DeckData selectedDeck;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (GameManager.instance.isPaused) {
                GameManager.instance.ResumeGame();
                settings.SetActive(false);
            } else {
                GameManager.instance.PauseGame();
                settings.SetActive(true);
            }
        }

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

        if (GameManager.instance.currentLevelData.chaptersAfterGame != "" && GameManager.instance.currentLevelData != null) {
            GameManager.instance.nextStory = GameManager.instance.currentLevelData.chaptersAfterGame;
            LoadingScene.Instance.LoadStory();
            return;
        }

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
                countPlayer++;
            } else
                StartCoroutine(DesableEntityAfterDeath(p));
        }
        foreach (GameObject e in enemyUnits) {
            if (e.GetComponent<Units>().isAlive) {
                countEnemy++;
            } else
                StartCoroutine(DesableEntityAfterDeath(e));
        }

        if (countPlayer == 0) {
            EndGame(false);
        } else if (countEnemy == 0) {
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


    // ---- CARD ----

    public void SetSelectedCard(CardUI card)
    {
        if (selectedCard != null && selectedCard != card)
        {
            selectedCard.DeselectCard();
        }

        selectedCard = card;
    }

    public void DeselectCard()
    {
        if (selectedCard != null)
        {
            selectedCard.DeselectCard();
            selectedCard = null;
        }
    }

    public bool HasSelectedCard()
    {
        return selectedCard != null;
    }

    public GameObject GetSelectedUnitPrefab()
    {
        return selectedCard != null ? selectedCard.cardData.unitPrefab : null;
    }
}
