using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager instance;

    [Header("Popup End Game")]
    [SerializeField] GameObject popupEndGame;
    [SerializeField] Button buttonNextOrRestart;

    [Header("Deck & Mana manager")]
    public CardUI selectedCard;
    public ManaManager manaManager;
    [SerializeField] GameObject settings;

    public bool isGameRunning = false;
    private List<GameObject> playerUnits = new();
    private List<CardData> playerCardData = new();
    private List<GameObject> enemyUnits = new();

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // if (GameManager.instance.currentLevelData.currentlevel < 3)
        //     VoidMapGeneratorGPU.instance.SetPhase(0);
        // if (GameManager.instance.currentLevelData.currentlevel == 3)
        //     VoidMapGeneratorGPU.instance.SetPhase(1);
        // if (GameManager.instance.currentLevelData.currentlevel == 4)
        //     VoidMapGeneratorGPU.instance.SetPhase(2);
        // if (GameManager.instance.currentLevelData.currentlevel == 5)
        //     VoidMapGeneratorGPU.instance.SetPhase(3);
        VoidMapGeneratorGPU.instance.SetSeed(VoidMapGeneratorGPU.instance.seed + GameManager.instance.currentLevelData.currentlevel);


        LevelInformationFadeTextManager.instance.DisplayTextWithFade(
            GameManager.instance.currentLevelData.currentlevel.ToString(),
            GameManager.instance.currentLevelData.spawnAlgo.difficulty
        );

        if (GameModeManager.selectedDeck == null)
            DeckSelectionManager.instance.OpenSelection();
        else
            DeckSelectionManager.instance.CloseSelection();
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

                if (clickedObject.GetComponentInChildren<EntityTeam>() != null
                && clickedObject.GetComponentInChildren<EntityTeam>().CompareTag("Champion")) {
                    RemoveUnit(clickedObject);
                }
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
        CardData card = null;
        foreach (CardData cardData in playerCardData) {
            if (cardData != null && cardData.unitPrefab != null) {
                if (unit.name.StartsWith(cardData.unitPrefab.name)) {
                    card = cardData;
                    break;
                }
            }
        }

        if (playerUnits.Contains(unit)) {
            playerUnits.Remove(unit);
            ManaManager.instance.AddMana(unit.GetComponent<Units>().manaCost);
            Destroy(unit);

            BugTracker.Info("'" + unit.name + "' has been remove from the playerUnits list.");
            DeckManager.instance.AddCardInDeck(card);
            playerCardData.Remove(card);
            return;
        }
        if (enemyUnits.Contains(unit)) {
            enemyUnits.Remove(unit);
            Destroy(unit);

            BugTracker.Info("'" + unit.name + "' has been remove from the enemyUnits list.");
            playerCardData.Remove(card);
            return;
        }
    }

    public void EndGame(bool isPlayerVictorious)
    {
        isGameRunning = false;

        BugTracker.Info("End of the game, player win: '" + isPlayerVictorious + "'.");

        if (isPlayerVictorious && GameManager.instance.currentLevelData.chaptersAfterGame != "" && GameManager.instance.currentLevelData != null) {
            GameManager.instance.nextStory = GameManager.instance.currentLevelData.chaptersAfterGame;
            LoadingScene.Instance.LoadStory();
            return;
        }

        popupEndGame.SetActive(true);
        if (isPlayerVictorious) {
            popupEndGame.GetComponentInChildren<TextMeshProUGUI>().text = "You win";

            buttonNextOrRestart.GetComponentInChildren<TextMeshProUGUI>().text = "Next";
            buttonNextOrRestart.onClick.AddListener(GameManager.instance.SetNextLevel);
            return;
        }

        buttonNextOrRestart.onClick.AddListener(RestartGame);
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
        BugTracker.Info("Game is restarting...");

        popupEndGame.SetActive(false);

        foreach (GameObject key in playerUnits)
            key.GetComponent<Units>().ResetUnit();

        foreach (GameObject key in enemyUnits)
            key.GetComponent<Units>().ResetUnit();

        StartOrStopCombat(false);

        ManaManager.instance.AddMana(3);
    }

    public void StartOrStopCombat(bool enabled)
    {
        if (!playerUnits.Any() || isGameRunning)
            enabled = !enabled;

        isGameRunning = enabled;

        GameObject[] entities = GameObject.FindGameObjectsWithTag("Entities");
        if (entities == null)
            return;

        foreach (GameObject e in entities) {
            e.GetComponentInChildren<Units>().isGameRunning = enabled;
        }
        BugTracker.Info("Start Game: '"+isGameRunning+"'.");
    }

    private IEnumerator DesableEntityAfterDeath(GameObject entity)
    {
        yield return new WaitForSeconds(1f);
        if (isGameRunning)
            entity.SetActive(false);
    }


    // ---- CARD ----

    public void SetSelectedCard(CardUI card)
    {
        if (selectedCard != null && selectedCard != card)
            selectedCard.DeselectCard();

        selectedCard = card;
    }

    public void DeselectCard()
    {
        if (selectedCard != null) {
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
        playerCardData.Add(selectedCard.cardData);

        return selectedCard != null ? selectedCard.cardData.unitPrefab : null;
    }
}
