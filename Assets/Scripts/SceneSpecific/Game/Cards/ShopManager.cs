using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    // instace pour relier la récompense à l'or du player" //
    public static ShopManager instance;

    [Header("UI")]
    public SlidingPanel shopPanel;
    public TextMeshProUGUI moneyText;
    public Button leaveButton;
    public Button openButton;
    public MerchantDialogue merchant;
    // cachés quand le shop est ouvert (manabar, bouton du shop...)
    public GameObject[] hiddenWhileOpen;

    [Header("Shop Slots")]
    public GameObject shopSlotPrefab;
    public Transform cardsContainer;

    [Header("Économie")]
    public int slotCount = 4;

    private int playerGold = 200;
    private List<CardData> allCards = new();
    private List<ShopCard> currentSlots = new();

    public bool IsOpen => shopPanel.IsOpen;

    // Le shop n'est disponible que pendant la phase de pose des unités
    bool IsPlacementPhase => GameLoopManager.instance == null || !GameLoopManager.instance.isGameRunning;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        shopPanel.SnapClosed();
        UpdateMoneyUI();
        leaveButton.onClick.AddListener(CloseShop);
        LoadCards();
    }

    void Update()
    {
        bool canShop = IsPlacementPhase;

        if (openButton != null)
            openButton.interactable = canShop;

        if (!canShop && IsOpen)
            CloseShop();
    }

    public void LoadCards()
    {
        DeckData deck = GameModeManager.selectedDeck;
        if (deck == null)
        {
            BugTracker.Error("[ShopManager] Aucun deck sélectionné !");
            return;
        }
        allCards = new List<CardData>(deck.cards);
        BugTracker.Info($"[ShopManager] {allCards.Count} cartes chargées depuis '{deck.deckName}'.");

        GenerateOffers();
    }

    public void ToggleShop()
    {
        if (IsOpen)
            CloseShop();
        else
            OpenShop();
    }

    public void OpenShop()
    {
        if (!IsPlacementPhase)
        {
            FloatingTextManager.instance.Show("Le marchand ne vend rien pendant le combat");
            return;
        }

        if (currentSlots.Count == 0)
            GenerateOffers();

        SetHiddenObjectsActive(false);
        shopPanel.Open();
        MerchantSay(MerchantDialogue.Mood.Greeting);
        BugTracker.Info("[ShopManager] Shop ouvert.");
    }

    public void CloseShop()
    {
        shopPanel.Close();
        SetHiddenObjectsActive(true);
        BugTracker.Info("[ShopManager] Shop fermé.");
    }

    void SetHiddenObjectsActive(bool active)
    {
        foreach (GameObject obj in hiddenWhileOpen)
            if (obj != null)
                obj.SetActive(active);
    }

    // Tirage des offres : une seule fois par phase de pose (pas de reroll en fermant/rouvrant)
    void GenerateOffers()
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

            ShopCard shopCard = Instantiate(shopSlotPrefab, cardsContainer).GetComponent<ShopCard>();
            shopCard.Setup(picked, this);
            shopCard.RefreshAffordable(playerGold);
            currentSlots.Add(shopCard);
        }

        BugTracker.Info($"[ShopManager] {count} cartes proposées dans le shop.");
    }

    // Une carte reste en vente après l'achat : on peut la racheter tant qu'on a l'or
    public void BuyCard(ShopCard slot)
    {
        if (!TrySpendGold(slot.Data.goldCost))
        {
            MerchantSay(MerchantDialogue.Mood.CantAfford);
            return;
        }

        DeckManager.instance.AddCardInDeck(slot.Data);
        MerchantSay(MerchantDialogue.Mood.Purchase);
        BugTracker.Info($"[ShopManager] '{slot.Data.cardName}' achetée. Deck: {DeckManager.instance.deck.Count} cartes.");
    }

    void MerchantSay(MerchantDialogue.Mood mood)
    {
        if (merchant != null)
            merchant.Say(mood);
    }

    public void AddGold(int amount)
    {
        playerGold += amount;
        UpdateMoneyUI();
    }

    void ClearSlots()
    {
        foreach (var slot in currentSlots)
            Destroy(slot.gameObject);
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

        foreach (ShopCard slot in currentSlots)
            slot.RefreshAffordable(playerGold);
    }
}
