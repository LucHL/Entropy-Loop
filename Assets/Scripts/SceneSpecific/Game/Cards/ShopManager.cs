using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject shopPopup;
    public TextMeshProUGUI moneyText;
    public Button closeButton;

    [Header("Shop Slots")]
    public GameObject shopSlotPrefab;
    public Transform cardsContainer;

    [Header("Deck")]
    public DeckData currentDeck;

    [Header("Économie")]
    public int slotCount = 4;

    private int playerGold = 200;
    private List<CardData> allCards = new();
    private List<GameObject> currentSlots = new();

    void Start()
    {
        shopPopup.SetActive(false);
        UpdateMoneyUI();
        closeButton.onClick.AddListener(ToggleShop);
        LoadCards();
    }

    void LoadCards()
    {
        DeckData deck = GameManager.Instance.selectedDeck;
        if (deck == null)
        {
            BugTracker.Error("[ShopManager] Aucun deck sélectionné dans le GameManager !");
            return;
        }
        allCards = new List<CardData>(deck.cards);
        BugTracker.Info($"[ShopManager] {allCards.Count} cartes chargées depuis '{deck.deckName}'.");
    }

    public void ToggleShop()
    {
        bool isActive = shopPopup.activeSelf;
        if (!isActive)
            RefreshShop();
        else
            ClearSlots();
        shopPopup.SetActive(!isActive);
        BugTracker.Info($"[ShopManager] Shop {(isActive ? "fermé" : "ouvert")}.");
    }

    void RefreshShop()
    {
        ClearSlots();

        if (allCards.Count == 0)
        {
            BugTracker.Warning("[ShopManager] Aucune carte trouvée !");
            return;
        }

        List<CardData> pool = new(allCards);
        int count = Mathf.Min(slotCount, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            CardData picked = pool[idx];
            pool.RemoveAt(idx);

            GameObject slot = Instantiate(shopSlotPrefab, cardsContainer);
            ShopCard shopCard = slot.GetComponent<ShopCard>();
            shopCard.Setup(picked, this);
            currentSlots.Add(slot);
        }

        BugTracker.Info($"[ShopManager] {count} cartes affichées dans le shop.");
    }

    void ClearSlots()
    {
        foreach (var slot in currentSlots)
            Destroy(slot);
        currentSlots.Clear();
    }

    public bool TrySpendGold(int amount)
    {
        if (playerGold < amount)
        {
            BugTracker.Warning($"[ShopManager] Pas assez d'or. Requis: {amount}, Disponible: {playerGold}.");
            return false;
        }
        playerGold -= amount;
        UpdateMoneyUI();
        BugTracker.Info($"[ShopManager] {amount} or dépensé. Or restant: {playerGold}.");
        return true;
    }

    void UpdateMoneyUI()
    {
        moneyText.text = playerGold.ToString();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
            ToggleShop();
    }
}