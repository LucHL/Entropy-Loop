using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DeckPreview : MonoBehaviour
{
    [Header("UI References")]
    public Image commanderImage;
    public Image randomCard1Image;
    public Image randomCard2Image;
    public TextMeshProUGUI deckNameText;
    public Button selectButton;

    private DeckData data;
    private DeckSelectionManager manager;

    public void Setup(DeckData deck, DeckSelectionManager mgr)
    {
        TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>();
        if (deck.unlockLevel > GameManager.instance.maxLevelFinish) {
            Debug.Log(deck.unlockLevel);
            Debug.Log(GameManager.instance.maxLevelFinish);
            text.gameObject.SetActive(true);
            text.text = "unlock level " + deck.unlockLevel.ToString();
            selectButton.interactable = false;
        } else
            text.gameObject.SetActive(false);

        data = deck;
        manager = mgr;

        if (deckNameText != null)
            deckNameText.text = deck.deckName;

        if (commanderImage != null && deck.commander != null)
            commanderImage.sprite = deck.commander.cardImage;

        SetRandomCards();

        selectButton.onClick.AddListener(() => manager.SelectDeck(data));
    }

    void SetRandomCards()
    {
        if (data.cards == null || data.cards.Count == 0)
            return;

        List<CardData> pool = new(data.cards);

        if (randomCard1Image != null && pool.Count > 0)
        {
            int idx = Random.Range(0, pool.Count);
            randomCard1Image.sprite = pool[idx].cardImage;
            pool.RemoveAt(idx);
        }

        if (randomCard2Image != null && pool.Count > 0)
        {
            int idx = Random.Range(0, pool.Count);
            randomCard2Image.sprite = pool[idx].cardImage;
            pool.RemoveAt(idx);
        }
    }
}