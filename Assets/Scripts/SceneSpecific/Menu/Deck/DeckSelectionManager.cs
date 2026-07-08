using UnityEngine;
using System.Collections.Generic;

public class DeckSelectionManager : MonoBehaviour
{
    public static DeckSelectionManager instance;

    [Header("UI")]
    public Transform decksContainer;
    public GameObject deckPreviewPrefab;

    private List<DeckData> availableDecks = new();

    void Awake()
    {
        instance = this;
    }

    void LoadDecks()
    {
        DeckData[] loaded = Resources.LoadAll<DeckData>("Decks");
        availableDecks = new List<DeckData>(loaded);
        BugTracker.Info($"[DeckSelection] {availableDecks.Count} decks load.");
    }

    public void OpenSelection()
    {
        LoadDecks();
        RefreshDecks();
    }

    public void CloseSelection()
    {
        gameObject.SetActive(false);
    }

    void RefreshDecks()
    {
        foreach (Transform child in decksContainer)
            Destroy(child.gameObject);

        foreach (DeckData deck in availableDecks) {
            GameObject obj = Instantiate(deckPreviewPrefab, decksContainer);
            DeckPreview preview = obj.GetComponent<DeckPreview>();
            preview.Setup(deck, this);
        }
    }

    public void SelectDeck(DeckData deck)
    {
        GameModeManager.selectedDeck = deck;

        if (DeckManager.instance != null)
            DeckManager.instance.SetDeck();

        BugTracker.Info($"[DeckSelection] Deck '{deck.deckName}' selected.");
        CloseSelection();
    }
}
