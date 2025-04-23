using UnityEngine;
using UnityEngine.UI;

public class ShopCard : MonoBehaviour
{
    public CardData cardData;
    public Button buyButton;
    public int cardPrice = 25;
    public ShopManager shopManager;

    private DeckManager deckManager;

    void Start()
    {
        deckManager = FindFirstObjectByType<DeckManager>();
        shopManager = FindFirstObjectByType<ShopManager>();

        buyButton.onClick.AddListener(BuyCard);
    }

    void BuyCard()
    {
        if (deckManager != null && cardData != null && shopManager != null)
        {
            if (shopManager.playerGold >= cardPrice)
            {
                deckManager.deck.Add(cardData);
                shopManager.SpendGold(cardPrice);
                Debug.Log("Carte achetée : " + cardData.cardName);
            }
            else
            {
                Debug.Log("Pas assez d'or !");
            }
        }
    }
}
