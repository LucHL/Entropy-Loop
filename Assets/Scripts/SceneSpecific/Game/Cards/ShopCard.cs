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

    [Header("Style")]
    public Color affordableColor = Color.white;
    public Color tooExpensiveColor = new(0.9f, 0.3f, 0.3f);

    public CardData Data { get; private set; }

    private ShopManager shopManager;

    public void Setup(CardData cardData, ShopManager manager)
    {
        Data = cardData;
        shopManager = manager;

        if (cardImage != null && Data.cardImage != null) {
            cardImage.sprite = Data.cardImage;
            cardImage.preserveAspect = true; // évite d'étirer la carte dans sa case
        }

        if (priceText != null)
            priceText.text = Data.goldCost.ToString();

        buyButton.onClick.AddListener(OnBuyClicked);

        BugTracker.Info($"[ShopCard] Carte '{Data.cardName}' initialisée. Prix: {Data.goldCost}.");
    }

    void OnBuyClicked()
    {
        EventSystem.current.SetSelectedGameObject(null);
        shopManager.BuyCard(this);
    }

    public void RefreshAffordable(int gold)
    {
        if (priceText != null)
            priceText.color = gold >= Data.goldCost ? affordableColor : tooExpensiveColor;
    }
}
