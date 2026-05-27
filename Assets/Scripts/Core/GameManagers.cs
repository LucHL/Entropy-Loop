using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public CardUI selectedCard;
    public ManaManager manaManager;
    public GameObject settings;
    public DeckData selectedDeck;
    public LevelData currentLevelData;

    private bool isPaused = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    // ---- SETTINGS ----

    public void PauseGame()
    {
        isPaused = true;
        settings.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        settings.SetActive(false);
        Time.timeScale = 1f;
    }

    // ---- CARD ----

    public void SetSelectedCard(CardUI card)
    {
        if (selectedCard != null && selectedCard != card)
        {
            selectedCard.DeselectCard();
        }

        selectedCard = card;
        Debug.Log("Carte sélectionnée : " + card.cardData.cardName);
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


    // ---- LEVELS ----

    public void SaveLevelConfig(LevelData levelData)
    {
        BugTracker.Info("Save current level data, current level: '" + levelData.currentlevel + "'.");
        currentLevelData = levelData;
    }
}
