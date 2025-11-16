using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public CardUI selectedCard;

    private bool isPaused = false;
    [SerializeField] private GameObject pauseMenu;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }

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
}
