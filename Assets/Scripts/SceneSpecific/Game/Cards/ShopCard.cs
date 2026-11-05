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

    [Header("Style")]
    public Color affordableColor = Color.white;
    public Color tooExpensiveColor = new(0.9f, 0.3f, 0.3f);

    public CardData Data { get; private set; }
    public bool IsSold { get; private set; }

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

        if (soldText != null)
            soldText.gameObject.SetActive(false);

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
        if (priceText != null && !IsSold)
            priceText.color = gold >= Data.goldCost ? affordableColor : tooExpensiveColor;
    }

    public void MarkSold()
    {
        IsSold = true;
        buyButton.interactable = false;

        if (cardImage != null)
            cardImage.color = new Color(0.4f, 0.4f, 0.4f);

        if (priceText != null)
            priceText.gameObject.SetActive(false);

        if (goldIcon != null)
            goldIcon.SetActive(false);

        if (soldText != null)
            soldText.gameObject.SetActive(true);
    }
}
