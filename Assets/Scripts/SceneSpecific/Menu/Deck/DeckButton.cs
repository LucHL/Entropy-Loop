using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckButton : MonoBehaviour
{
    public TextMeshProUGUI deckNameText;
    public Button button;

    private DeckData data;
    private DeckSelectionManager manager;

    public void Setup(DeckData deck, DeckSelectionManager mgr)
    {
        data = deck;
        manager = mgr;

        if (deckNameText != null)
            deckNameText.text = deck.deckName;

        button.onClick.AddListener(() => manager.SelectDeck(data));
    }
}