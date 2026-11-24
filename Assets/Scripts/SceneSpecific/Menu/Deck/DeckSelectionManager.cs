using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

// Sélection du deck présentée comme un livre : un deck (grimoire) par double page
public class DeckSelectionManager : MonoBehaviour
{
    public static DeckSelectionManager instance;

    [Header("Page de gauche")]
    public TextMeshProUGUI deckNameText;
    public Transform cardsContainer;
    public GameObject cardTemplate;

    [Header("Page de droite")]
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI grimoireTitleText;
    public TextMeshProUGUI loreText;
    public TextMeshProUGUI deckTypeText;
    public TextMeshProUGUI mechanicsText;

    [Header("Navigation")]
    public BookPageTurn pageTurn;
    public Button previousButton;
    public Button nextButton;
    public Button selectButton;
    public TextMeshProUGUI selectButtonText;
    public TextMeshProUGUI pageIndicatorText;

    [Header("Style")]
    public Image backdrop;
    [Range(0f, 1f)] public float menuBackdropAlpha = 0.94f;
    // en jeu, le plateau et l'ATH derrière sont chargés : on assombrit davantage pour garder du contraste
    [Range(0f, 1f)] public float gameBackdropAlpha = 0.97f;
    public Color lockedCardColor = new(0.35f, 0.35f, 0.4f, 1f);
    [Range(0f, 1f)] public float disabledTextAlpha = 0.5f;
    public Vector2 cardSpacing = new(22f, 24f);
    public float maxCardHeight = 300f;

    private List<DeckData> availableDecks = new();
    private int currentIndex = 0;

    void Awake()
    {
        instance = this;

        previousButton.onClick.AddListener(PreviousDeck);
        nextButton.onClick.AddListener(NextDeck);
        selectButton.onClick.AddListener(SelectCurrentDeck);
        cardTemplate.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            PreviousDeck();
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            NextDeck();
    }

    void LoadDecks()
    {
        // decks débloqués en premier
        availableDecks = Resources.LoadAll<DeckData>("Decks")
            .Where(deck => deck.showInSelection)
            .OrderBy(deck => deck.unlockLevel)
            .ThenBy(deck => deck.deckName)
            .ToList();
        BugTracker.Info($"[DeckSelection] {availableDecks.Count} decks load.");
    }

    public void OpenSelection()
    {
        LoadDecks();
        if (availableDecks.Count == 0) {
            BugTracker.Error("[DeckSelection] Aucun deck dans Resources/Decks.");
            return;
        }

        int selected = availableDecks.IndexOf(GameModeManager.selectedDeck);
        currentIndex = selected >= 0 ? selected : 0;

        if (backdrop != null) {
            Color color = backdrop.color;
            color.a = DeckManager.instance != null ? gameBackdropAlpha : menuBackdropAlpha;
            backdrop.color = color;
        }

        ShowLeftPage();
        ShowRightPage();
        UpdateNavigation();
    }

    public void CloseSelection()
    {
        gameObject.SetActive(false);
    }

    public void NextDeck()
    {
        TurnPage(1);
    }

    public void PreviousDeck()
    {
        TurnPage(-1);
    }

    void TurnPage(int direction)
    {
        int target = currentIndex + direction;
        if (target < 0 || target >= availableDecks.Count || pageTurn.IsTurning)
            return;

        currentIndex = target;
        UpdateNavigation();

        bool forward = direction > 0;
        pageTurn.Play(forward,
            forward ? ShowRightPage : ShowLeftPage,
            forward ? ShowLeftPage : ShowRightPage);
    }

    bool IsUnlocked(DeckData deck)
    {
        // débloqué dès que le niveau est atteint, comme la sélection de niveau (LevelButtonUI)
        return GameManager.instance == null || deck.unlockLevel <= GameManager.instance.maxLevelFinish + 1;
    }

    void ShowLeftPage()
    {
        DeckData deck = availableDecks[currentIndex];
        deckNameText.text = string.IsNullOrEmpty(deck.displayName) ? deck.deckName : deck.displayName;

        foreach (Transform child in cardsContainer)
            if (child.gameObject != cardTemplate)
                Destroy(child.gameObject);

        // une seule fois chaque carte, commandant en premier, avec le nombre d'exemplaires
        List<CardData> cards = new();
        if (deck.commander != null)
            cards.Add(deck.commander);
        cards.AddRange(deck.cards.Where(card => card != null));

        Color cardColor = IsUnlocked(deck) ? Color.white : lockedCardColor;
        List<RectTransform> slots = new();
        foreach (var group in cards.GroupBy(card => card)) {
            GameObject slot = Instantiate(cardTemplate, cardsContainer);
            slot.SetActive(true);

            Image image = slot.GetComponent<Image>();
            image.sprite = group.Key.cardImage;
            image.color = cardColor;

            int copies = deck.cards.Count(card => card == group.Key);
            TextMeshProUGUI badge = slot.GetComponentInChildren<TextMeshProUGUI>(true);
            badge.transform.parent.gameObject.SetActive(copies > 1);
            badge.text = "x" + copies;

            slots.Add((RectTransform)slot.transform);
        }

        LayoutCards(slots);
    }

    // Choisit le nombre de colonnes qui donne les plus grandes cartes, puis centre chaque ligne
    void LayoutCards(List<RectTransform> slots)
    {
        int count = slots.Count;
        if (count == 0)
            return;

        Rect area = ((RectTransform)cardsContainer).rect;
        Sprite sprite = slots[0].GetComponent<Image>().sprite;
        float aspect = sprite != null ? sprite.rect.width / sprite.rect.height : 150f / 210f;

        int bestColumns = 1;
        float bestWidth = 0f;
        int bestEmpty = int.MaxValue;
        for (int columns = 1; columns <= count; columns++) {
            int rows = Mathf.CeilToInt(count / (float)columns);
            float width = Mathf.Min(
                (area.width - (columns - 1) * cardSpacing.x) / columns,
                (area.height - (rows - 1) * cardSpacing.y) / rows * aspect);
            width = Mathf.Min(width, maxCardHeight * aspect);
            int empty = columns * rows - count;

            // à taille égale, on préfère la grille la plus remplie (4 cartes : 2x2 plutôt que 3+1)
            if (width > bestWidth + 1f || (Mathf.Abs(width - bestWidth) <= 1f && empty < bestEmpty)) {
                bestColumns = columns;
                bestWidth = width;
                bestEmpty = empty;
            }
        }

        Vector2 cell = new(bestWidth, bestWidth / aspect);
        int rowCount = Mathf.CeilToInt(count / (float)bestColumns);
        float blockHeight = rowCount * cell.y + (rowCount - 1) * cardSpacing.y;

        for (int i = 0; i < count; i++) {
            int row = i / bestColumns;
            int inRow = Mathf.Min(bestColumns, count - row * bestColumns);
            float rowWidth = inRow * cell.x + (inRow - 1) * cardSpacing.x;
            int column = i % bestColumns;

            RectTransform slot = slots[i];
            slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(0.5f, 0.5f);
            slot.sizeDelta = cell;
            slot.anchoredPosition = new Vector2(
                -rowWidth / 2 + cell.x / 2 + column * (cell.x + cardSpacing.x),
                blockHeight / 2 - cell.y / 2 - row * (cell.y + cardSpacing.y));
        }
    }

    void ShowRightPage()
    {
        DeckData deck = availableDecks[currentIndex];
        categoryText.text = deck.category;
        grimoireTitleText.text = deck.grimoireTitle;
        loreText.text = deck.lore;
        deckTypeText.text = deck.deckType;
        mechanicsText.text = deck.mechanics;
    }

    void UpdateNavigation()
    {
        DeckData deck = availableDecks[currentIndex];
        bool unlocked = IsUnlocked(deck);
        bool alreadySelected = deck == GameModeManager.selectedDeck;

        SetButtonInteractable(previousButton, currentIndex > 0);
        SetButtonInteractable(nextButton, currentIndex < availableDecks.Count - 1);
        SetButtonInteractable(selectButton, unlocked && !alreadySelected);
        selectButtonText.text = !unlocked ? $"DÉBLOQUÉ AU NIVEAU {deck.unlockLevel}"
            : alreadySelected ? "DECK SÉLECTIONNÉ"
            : "SÉLECTIONNER CE DECK";
        pageIndicatorText.text = $"GRIMOIRE {currentIndex + 1} / {availableDecks.Count}";
    }

    // Le Button ne grise que son image : on estompe aussi le texte posé dessus
    void SetButtonInteractable(Button button, bool interactable)
    {
        button.interactable = interactable;
        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            label.alpha = interactable ? 1f : disabledTextAlpha;
    }

    void SelectCurrentDeck()
    {
        if (availableDecks.Count == 0)
            return;

        DeckData deck = availableDecks[currentIndex];
        if (!IsUnlocked(deck) || deck == GameModeManager.selectedDeck || pageTurn.IsTurning)
            return;

        SelectDeck(deck);
    }

    public void SelectDeck(DeckData deck)
    {
        GameModeManager.selectedDeck = deck;

        if (DeckManager.instance != null) {
            DeckManager.instance.SetDeck();
            ShopManager.instance.LoadCards();
        }

        BugTracker.Info($"[DeckSelection] Deck '{deck.deckName}' selected.");
        CloseSelection();
    }
}
