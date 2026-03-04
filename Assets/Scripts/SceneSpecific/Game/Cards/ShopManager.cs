using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("--- UI WINDOW ---")]
    public GameObject shopPopup;
    public GameObject backgroundPopup;
    public Button closeButton;
    public TextMeshProUGUI goldText;

    [Header("--- GENERATION ---")]
    public Transform CardsContainer;
    public GameObject shopSlotPrefab;
    public string resourcePath = "Card Data";
    public int fixedPrice = 100;

    [Header("--- DATA ---")]
    public int playerGold = 500;
    
    private List<CardData> allCardsInGame = new List<CardData>();
    private DeckManager deckManager;

    void Start()
    {
        deckManager = FindFirstObjectByType<DeckManager>();

        if (shopPopup != null) shopPopup.SetActive(false);
        if (backgroundPopup != null) backgroundPopup.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ToggleShop);
        }

        LoadCards();
        RefreshShop(); 
        UpdateGoldUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            ToggleShop();
        }
    }

    public void ToggleShop()
    {
        if (shopPopup != null)
        {
            bool isActive = !shopPopup.activeSelf;
            shopPopup.SetActive(isActive);

            if (backgroundPopup != null) 
                backgroundPopup.SetActive(isActive);
        }
    }

    void LoadCards()
    {
        CardData[] cards = Resources.LoadAll<CardData>(resourcePath);
        allCardsInGame = new List<CardData>(cards);
        
        if (allCardsInGame.Count == 0) Debug.LogError("SHOP: Pas de cartes trouvées !");
    }

    public void RefreshShop()
    {
        foreach (Transform child in CardsContainer) Destroy(child.gameObject);

        List<CardData> availableCards = new List<CardData>(allCardsInGame);

        for (int i = 0; i < 4; i++)
        {
            if (availableCards.Count == 0) break;

            int randomIndex = Random.Range(0, availableCards.Count);
            CardData randomCard = availableCards[randomIndex];

            availableCards.RemoveAt(randomIndex);

            GameObject newSlot = Instantiate(shopSlotPrefab, CardsContainer);
            newSlot.transform.localScale = Vector3.one;

            ShopCard slotScript = newSlot.GetComponent<ShopCard>();
            if (slotScript != null)
            {
                slotScript.Setup(randomCard, fixedPrice, this);
            }
        }
    }

    public bool TryBuyCard(CardData card, int cost)
    {
        if (playerGold >= cost)
        {
            playerGold -= cost;
            
            if (deckManager.deck == null) deckManager.deck = new List<CardData>();
            deckManager.deck.Add(card);

            UpdateGoldUI();
            return true;
        }
        return false;
    }

    void UpdateGoldUI()
    {
        if (goldText != null) goldText.text = playerGold.ToString();
    }
}