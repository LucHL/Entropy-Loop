using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopCard : MonoBehaviour
{
    [Header("UI Elements")]
    public Image cardImage;         
    public Button buyButton;        
    public TextMeshProUGUI priceText; 
    public GameObject coinIcon;     // NOUVEAU : La petite icône de pièce !

    private CardData myData;
    private int myPrice;
    private ShopManager manager;

    public void Setup(CardData data, int price, ShopManager shopManager)
    {
        myData = data;
        myPrice = price;
        manager = shopManager;

        // 1. Visuel
        if (cardImage != null) cardImage.sprite = data.cardImage;
        if (priceText != null) priceText.text = "Buy for " + price; 

        // On s'assure que la pièce est visible (utile si on rafraîchit le shop)
        if (coinIcon != null) coinIcon.SetActive(true);

        // 2. Reset du bouton
        buyButton.interactable = true;
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(TryBuy);
    }

    void TryBuy()
    {
        if (manager != null)
        {
            if (manager.TryBuyCard(myData, myPrice))
            {
                // Si acheté : on désactive le bouton
                buyButton.interactable = false;
                if (priceText != null) priceText.text = "SOLD";
                
                // NOUVEAU : On cache la pièce d'or !
                if (coinIcon != null) coinIcon.SetActive(false);
            }
        }
    }
}