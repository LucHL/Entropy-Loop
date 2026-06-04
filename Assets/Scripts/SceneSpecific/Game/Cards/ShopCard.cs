using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopCard : MonoBehaviour
{
    [Header("UI References")]
    public Image cardImage;
    public TextMeshProUGUI priceText;
    public Button buyButton;
    public GameObject goldIcon;
    public TextMeshProUGUI soldText;

    private CardData data;
    private ShopManager shopManager;
    private DeckManager deckManager;

    public void Setup(CardData cardData, ShopManager manager)
    {
        data = cardData;
        shopManager = manager;
        deckManager = FindFirstObjectByType<DeckManager>();

        if (cardImage != null && data.cardImage != null)
            cardImage.sprite = data.cardImage;

        if (priceText != null)
            priceText.text = data.goldCost.ToString();

        if (soldText != null)
            soldText.gameObject.SetActive(false);

        buyButton.onClick.AddListener(BuyCard);

        BugTracker.Info($"[ShopCard] Carte '{data.cardName}' initialisée. Prix: {data.goldCost}.");
    }

    void BuyCard()
    {
        if (shopManager.TrySpendGold(data.goldCost))
        {
            deckManager?.deck.Add(data);
            EventSystem.current.SetSelectedGameObject(null);
            BugTracker.Info($"[ShopCard] '{data.cardName}' achetée. Deck: {deckManager?.deck.Count} cartes.");
        }
        else
        {
            BugTracker.Warning($"[ShopCard] Achat échoué pour '{data.cardName}'. Prix: {data.goldCost}.");
        }
    }
}