using UnityEngine;
using System.Collections.Generic;

public class DeckSelectionManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject selectionPanel;
    public Transform decksContainer;
    public GameObject deckPreviewPrefab;

    private List<DeckData> availableDecks = new();

    void Start()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }

    void LoadDecks()
    {
        DeckData[] loaded = Resources.LoadAll<DeckData>("Decks");
        availableDecks = new List<DeckData>(loaded);
        BugTracker.Info($"[DeckSelection] {availableDecks.Count} decks chargés.");
    }

    public void OpenSelection()
    {
        LoadDecks();
        selectionPanel.SetActive(true);
        RefreshDecks();
    }

    public void CloseSelection()
    {
        selectionPanel.SetActive(false);
    }

    void RefreshDecks()
    {
        foreach (Transform child in decksContainer)
            Destroy(child.gameObject);

        foreach (DeckData deck in availableDecks)
        {
            GameObject obj = Instantiate(deckPreviewPrefab, decksContainer);
            DeckPreview preview = obj.GetComponent<DeckPreview>();
            preview.Setup(deck, this);
        }
    }

    public void SelectDeck(DeckData deck)
    {
        GameModeManager.selectedDeck = deck;
        BugTracker.Info($"[DeckSelection] Deck '{deck.deckName}' sélectionné.");
        CloseSelection();
    }
}