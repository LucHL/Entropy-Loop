using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Entropy Loop HUD. Reuses the scene's real cards and gameplay managers.</summary>
public sealed class VoidbornHud : MonoBehaviour
{
    static readonly Color Ivory = new Color32(241, 232, 213, 255);
    static readonly Color Gold = new Color32(194, 166, 111, 255);
    static readonly Color Violet = new Color32(184, 105, 255, 255);
    static readonly Color Ink = new Color32(18, 13, 29, 250);
    readonly List<Sprite> ownedSprites = new();
    readonly List<TMP_FontAsset> ownedFonts = new();
    Transform canvas;
    RectTransform root;
    RectTransform manaDock, handRoot;
    Sprite headerSprite, buttonSprite, manaSprite, combatSprite, phaseSprite, gemSprite;
    Sprite stoneSprite, cardFrameSprite, drawBaseSprite;
    TMP_FontAsset titleFont, bodyFont;
    TMP_Text phase, manaValue, remaining;
    Button battle, draw, shop, journal;
    Image drawArt;
    Image[] manaGems;
    Image[] cardFrames;
    HandSlot[] slots;
    Transform journalPanel;
    ActionLogWindow logWindow;
    GameLogUI journalView;
    TMP_Text journalCount, journalEmpty;
    readonly HashSet<int> styledLogRows = new();
    Transform resultPanel;
    TMP_Text resultTitle, resultMessage, resultAction, resultKicker;
    Image resultDivider;
    bool built;
    int lastMana = -1;
    int lastMaxMana = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Game") return;
        foreach (var go in scene.GetRootGameObjects())
        {
            var loop = go.GetComponentInChildren<GameLoopManager>(true);
            if (loop && !loop.GetComponent<VoidbornHud>()) loop.gameObject.AddComponent<VoidbornHud>();
        }
    }

    IEnumerator Start()
    {
        yield return null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.gameObject.scene == gameObject.scene && c.transform.Find("StartBattle")) canvas = c.transform;
        if (!canvas || !DeckManager.instance) yield break;
        Build();
    }

    void Build()
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        titleFont = LoadFont("Cinzel");
        bodyFont = LoadFont("Comfortaa");
        var atlas = Resources.Load<Texture2D>("UI/EntropyLoop/CombatHudAtlas");
        if (atlas)
        {
            headerSprite = Slice(atlas, 0, 90, 768, 180);
            buttonSprite = Slice(atlas, 807, 125, 350, 114);
            manaSprite = Slice(atlas, 65, 367, 640, 283);
            combatSprite = Slice(atlas, 882, 365, 525, 282);
            gemSprite = Slice(atlas, 304, 690, 160, 311);
            phaseSprite = Slice(atlas, 799, 779, 711, 130);
        }
        root = NewRect("Entropy Loop HUD", canvas);
        Stretch(root);
        root.SetAsFirstSibling(); // Existing shop, tutorial and victory overlays stay in front.
        var borders = Resources.Load<Texture2D>("UI/EntropyLoop/CombatHudBorders");
        if (borders)
        {
            stoneSprite = Slice(borders, 0, 20, 1536, 260);
            cardFrameSprite = Slice(borders, 158, 398, 368, 550);
            drawBaseSprite = Slice(borders, 807, 636, 432, 232);
            var stone = Art("Obsidian lower surround", root, stoneSprite, new Vector2(.5f, 0), new Vector2(0, 100), new Vector2(1920, 200));
            stone.rectTransform.anchorMin = new Vector2(0, 0);
            stone.rectTransform.anchorMax = new Vector2(1, 0);
            stone.rectTransform.sizeDelta = new Vector2(0, 200);
            var upperStone = Art("Obsidian upper surround", root, stoneSprite, new Vector2(.5f, 1), new Vector2(0, -100), new Vector2(1920, 200));
            upperStone.rectTransform.anchorMin = new Vector2(0, 1);
            upperStone.rectTransform.anchorMax = Vector2.one;
            upperStone.rectTransform.sizeDelta = new Vector2(0, 200);
            // Turn the same stone strip upside down: flat edge at the screen top,
            // fractured edge towards the board. Decorative images never receive clicks.
            upperStone.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
        }
        var oldLevelBanner = canvas.Find("LevelInformations");
        if (oldLevelBanner) oldLevelBanner.gameObject.SetActive(false);
        var logo = Art("Entropy Loop", root, headerSprite, new Vector2(.5f, 1), new Vector2(0, -62), new Vector2(1160, 230));
        logo.preserveAspect = true;
        if (!headerSprite) Label(logo.transform, "ENTROPY LOOP", Vector2.zero, new Vector2(700, 60), 36, true);
        var plaque = Art("Phase plaque", root, phaseSprite, new Vector2(.5f, 1), new Vector2(0, -140), new Vector2(480, 52));
        phase = Label(plaque.transform, "PRÉPARATION", Vector2.zero, new Vector2(380, 34), 24, true);

        // Keep the original scene objects and their persistent click events.
        shop = AdoptButton(canvas.Find("Shop/ShopButton"), "Boutique", "BOUTIQUE", new Vector2(1, 1), new Vector2(-426, -48), new Vector2(202, 58));
        var cart = NewRect("Boutique icon", shop.transform);
        Place(cart, new Vector2(.5f, .5f), new Vector2(-73, 0), new Vector2(30, 30));
        cart.gameObject.AddComponent<CanvasRenderer>();
        var cartGraphic = cart.gameObject.AddComponent<CombatHudCartGlyph>();
        cartGraphic.color = Gold;
        cartGraphic.raycastTarget = false;
        shop.GetComponentInChildren<TMP_Text>().rectTransform.offsetMin = new Vector2(45, 7);
        var action = canvas.Find("ActionLogButton");
        if (action) logWindow = action.GetComponent<ActionLogWindow>();
        if (logWindow && logWindow.actionLogPanel) journalPanel = logWindow.actionLogPanel.transform;
        var originalJournal = action ? action.GetComponentInChildren<Button>(true) : null;
        var bookIcon = originalJournal && originalJournal.GetComponent<Image>() ? originalJournal.GetComponent<Image>().sprite : null;
        journal = AdoptButton(originalJournal ? originalJournal.transform : null, "Journal", "JOURNAL", new Vector2(1, 1), new Vector2(-210, -48), new Vector2(202, 58));
        journal.onClick.AddListener(() => StartCoroutine(ShowJournalHistory()));
        if (bookIcon)
        {
            var icon = Art("Livre rouge", journal.transform, bookIcon, new Vector2(.5f, .5f), new Vector2(-69, 0), new Vector2(28, 32));
            icon.preserveAspect = true;
            journal.GetComponentInChildren<TMP_Text>().rectTransform.offsetMin = new Vector2(46, 7);
        }
        var oldMenu = canvas.Find("BackToMenuButton");
        var menu = AdoptButton(oldMenu, "Menu", "", new Vector2(1, 1), new Vector2(-62, -48), new Vector2(68, 58));
        for (int i = -1; i <= 1; i++)
            Art("Menu line", menu.transform, null, new Vector2(.5f, .5f), new Vector2(0, i * 7), new Vector2(23, 2)).color = Gold;

        battle = AdoptButton(canvas.Find("StartBattle"), "Combat", "LANCER\nLE COMBAT", new Vector2(1, 0), new Vector2(-184, 128), new Vector2(344, 220));
        battle.gameObject.AddComponent<CombatHudBattleEmphasis>().Initialize(battle);
        SetupMana();
        SetupHand();
        SetupDraw();
        SetupJournal();
        SetupResult();
        AlignTutorialTargets();
        built = true;
        Refresh();
    }

    void SetupMana()
    {
        var mana = canvas.Find("Manabar");
        if (mana)
        {
            // ManaManager is a non-UI child: never disable it with the old UI.
            foreach (Transform child in mana)
                if (child is RectTransform) child.gameObject.SetActive(false);
            var graphic = mana.GetComponent<Graphic>();
            if (graphic) graphic.enabled = false;
        }
        var dock = Art("Mana", root, manaSprite, Vector2.zero, new Vector2(253, 128), new Vector2(506, 236));
        manaDock = dock.rectTransform;
        manaValue = Label(dock.transform, "MANA", new Vector2(0, 49), new Vector2(420, 48), 32, true);
        int max = ManaManager.instance ? ManaManager.instance.maxMana : 10;
        manaGems = new Image[Mathf.Max(1, max)];
        float step = Mathf.Min(39, 390f / manaGems.Length);
        for (int i = 0; i < manaGems.Length; i++)
        {
            var image = Art("Mana " + (i + 1), dock.transform, gemSprite, new Vector2(.5f, .5f), new Vector2((i - (manaGems.Length - 1) * .5f) * step, -25), new Vector2(34, 60));
            image.preserveAspect = true;
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(220, 178, 255, 200);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            manaGems[i] = image;
        }
    }

    void SetupHand()
    {
        slots = DeckManager.instance.handSlots;
        cardFrames = new Image[slots.Length];
        // The old full-screen transparent Hand image would otherwise remain in front
        // of the relocated controls and swallow every pointer event.
        var oldHand = canvas.Find("Deck/Hand");
        if (oldHand)
            foreach (var graphic in oldHand.GetComponents<Graphic>())
                graphic.raycastTarget = false;
        var hand = NewRect("Hand", root);
        handRoot = hand;
        Place(hand, new Vector2(.5f, 0), new Vector2(-24, 192), new Vector2(880, 324));
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i]) continue;
            var slot = slots[i];
            if (cardFrameSprite)
                cardFrames[i] = Art("Obsidian card border " + (i + 1), hand, cardFrameSprite, new Vector2(.5f, .5f), new Vector2((i - (slots.Length - 1) * .5f) * 222, 0), new Vector2(222, 334));
            slot.transform.SetParent(hand, false);
            // Stable slot order uses DeckManager's array, including inactive empty slots.
            Place(slot.transform, new Vector2(.5f, .5f), new Vector2((i - (slots.Length - 1) * .5f) * 222, 0), new Vector2(208, 312));
            var card = slot.GetComponent<CardUI>();
            if (card && card.cardImage)
            {
                card.cardImage.type = Image.Type.Simple;
                card.cardImage.preserveAspect = true;
                card.cardImage.color = Color.white;
            }
            var b = slot.GetComponent<Button>();
            if (b)
            {
                b.transition = Selectable.Transition.None;
            }
            slot.gameObject.tag = "BlockClick";
        }
        // Keep the existing full-size right-click preview with its original sprite references.
        var preview = canvas.Find("Deck/CardSelected");
        if (preview) Place(preview, new Vector2(.5f, .5f), Vector2.zero, new Vector2(430, 602));
        var shade = canvas.Find("Deck/BackgroundCardSelected") as RectTransform;
        if (shade) Stretch(shade);
    }

    void SetupDraw()
    {
        var deck = canvas.Find("Deck");
        var target = deck ? deck.Find("DrawButton") : null;
        Sprite back = target && target.GetComponent<Image>() ? target.GetComponent<Image>().sprite : null;
        if (drawBaseSprite)
            Art("Draw pile pedestal", root, drawBaseSprite, new Vector2(1, 0), new Vector2(-442, 65), new Vector2(180, 120));
        draw = AdoptButton(target, "Pioche", "", new Vector2(1, 0), new Vector2(-442, 235), new Vector2(150, 230));
        drawArt = draw.GetComponent<Image>();
        if (back) { drawArt.sprite = back; drawArt.preserveAspect = true; drawArt.type = Image.Type.Simple; }
        Label(draw.transform, "PIOCHER", new Vector2(0, -145), new Vector2(164, 34), 25, true);
        var counter = deck ? deck.GetComponentInChildren<NumberOfCardRemaining>(true) : null;
        if (counter)
        {
            counter.transform.SetParent(root, false);
            Place(counter.transform, new Vector2(1, 0), new Vector2(-442, 45), new Vector2(100, 44));
            remaining = counter.GetComponent<TMP_Text>();
            Format(remaining, 32, true);
        }
        else
        {
            var label = NewRect("Remaining", root);
            Place(label, new Vector2(1, 0), new Vector2(-442, 45), new Vector2(100, 44));
            remaining = label.gameObject.AddComponent<TextMeshProUGUI>();
            Format(remaining, 32, true);
        }
    }

    void LateUpdate() { if (built) { Refresh(); RefreshJournal(); } }

    void SetupJournal()
    {
        if (!journalPanel) return;
        journalView = journalPanel.GetComponentInChildren<GameLogUI>(true);
        var scroll = journalPanel.GetComponentInChildren<ScrollRect>(true);
        var close = journalPanel.GetComponentInChildren<Button>(true);
        if (!journalView || !scroll || !close) return;
        Stretch((RectTransform)journalPanel);
        var dim = journalPanel.gameObject.GetComponent<Image>() ?? journalPanel.gameObject.AddComponent<Image>();
        dim.color = new Color32(4, 3, 9, 210);
        dim.raycastTarget = true;
        journalPanel.gameObject.tag = "BlockClick";
        var window = Art("Journal obsidian panel", journalPanel, null, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1120, 800));
        window.color = new Color32(15, 11, 24, 255);
        window.raycastTarget = true;
        var top = Art("Obsidian crown", window.transform, stoneSprite, new Vector2(.5f, 1), new Vector2(0, -46), new Vector2(1120, 100));
        top.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
        Art("Obsidian base", window.transform, stoneSprite, new Vector2(.5f, 0), new Vector2(0, 36), new Vector2(1120, 80));
        for (int side = -1; side <= 1; side += 2)
            Art("Gold edge", window.transform, null, new Vector2(.5f, .5f), new Vector2(side * 554, 0), new Vector2(2, 700)).color = Gold;
        Label(window.transform, "CHRONOLOGIE DES ÉVÉNEMENTS", new Vector2(0, 310), new Vector2(850, 54), 30, true);
        Art("Header divider", window.transform, null, new Vector2(.5f, .5f), new Vector2(0, 260), new Vector2(1000, 1)).color = Gold;
        close.transform.SetParent(window.transform, false);
        Place(close.transform, new Vector2(1, 1), new Vector2(-95, -56), new Vector2(150, 52));
        SkinButton(close, "FERMER", 18);
        scroll.transform.SetParent(window.transform, false);
        Place(scroll.transform, new Vector2(.5f, .5f), new Vector2(0, -28), new Vector2(1000, 546));
        var scrollBackground = scroll.GetComponent<Image>();
        if (scrollBackground) { scrollBackground.sprite = null; scrollBackground.color = new Color32(9, 7, 16, 255); }
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35;
        if (scroll.horizontalScrollbar) scroll.horizontalScrollbar.gameObject.SetActive(false);
        scroll.horizontalScrollbar = null;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        if (scroll.verticalScrollbar)
        {
            var bar = scroll.verticalScrollbar;
            var rect = (RectTransform)bar.transform;
            rect.anchorMin = new Vector2(1, 0); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(1, .5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(10, 0);
            bar.GetComponent<Image>().color = new Color32(35, 25, 49, 255);
            bar.targetGraphic.color = Violet;
        }
        Stretch(scroll.viewport);
        scroll.viewport.offsetMax = new Vector2(-22, 0);
        var layout = journalView.log_parent.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        journalCount = Label(window.transform, "", new Vector2(0, -330), new Vector2(960, 28), 17, false);
        journalCount.color = Gold;
        journalEmpty = Label(window.transform, "Aucun événement pour le moment.\nLes actions du combat apparaîtront ici.", new Vector2(0, 0), new Vector2(880, 120), 23, false);
        close.transform.SetAsLastSibling();
    }

    void SetupResult()
    {
        resultPanel = canvas.Find("PopupEndGame");
        if (!resultPanel) return;
        var buttons = resultPanel.GetComponentsInChildren<Button>(true);
        foreach (var text in resultPanel.GetComponentsInChildren<TMP_Text>(true))
            if (!text.GetComponentInParent<Button>(true)) { resultTitle = text; break; }
        if (!resultTitle || buttons.Length != 2) return;
        // Replace only the presentation; preserve the original buttons and callbacks.
        foreach (var graphic in resultPanel.GetComponentsInChildren<Image>(true))
            if (!graphic.GetComponentInParent<Button>(true)) graphic.enabled = false;
        Stretch((RectTransform)resultPanel);
        var shade = resultPanel.GetComponent<Image>() ?? resultPanel.gameObject.AddComponent<Image>();
        shade.enabled = true; shade.sprite = null; shade.color = new Color32(4, 2, 12, 225); shade.raycastTarget = true;
        resultPanel.gameObject.tag = "BlockClick";
        var panel = Art("Result obsidian panel", resultPanel, null, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1000, 600));
        panel.color = new Color32(17, 10, 29, 255); panel.raycastTarget = true;
        resultTitle.transform.SetParent(panel.transform, false);
        Place(resultTitle.transform, new Vector2(.5f, .5f), new Vector2(0, 70), new Vector2(850, 100));
        Format(resultTitle, 68, true);
        resultTitle.fontStyle = FontStyles.Bold;
        // The original end-game code finds this text first when setting the result.
        resultTitle.transform.SetAsFirstSibling();
        var top = Art("Result crown", panel.transform, stoneSprite, new Vector2(.5f, 1), new Vector2(0, -42), new Vector2(1000, 90));
        top.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
        Art("Result base", panel.transform, stoneSprite, new Vector2(.5f, 0), new Vector2(0, 32), new Vector2(1000, 70));
        for (int side = -1; side <= 1; side += 2)
            Art("Result gold edge", panel.transform, null, new Vector2(.5f, .5f), new Vector2(side * 493, 0), new Vector2(2, 500)).color = Gold;
        var crest = Art("Void crest", panel.transform, phaseSprite, new Vector2(.5f, .5f), new Vector2(0, 196), new Vector2(460, 58));
        resultKicker = Label(crest.transform, "COMBAT TERMINÉ", Vector2.zero, new Vector2(380, 34), 20, true);
        resultKicker.color = Gold;
        resultDivider = Art("Result divider", panel.transform, null, new Vector2(.5f, .5f), new Vector2(0, -10), new Vector2(660, 2));
        resultMessage = Label(panel.transform, "", new Vector2(0, -63), new Vector2(850, 66), 23, false);
        foreach (var button in buttons)
        {
            var text = button.GetComponentInChildren<TMP_Text>(true);
            bool home = text && text.text == "Home";
            button.transform.SetParent(panel.transform, false);
            Place(button.transform, new Vector2(.5f, .5f), new Vector2(home ? 205 : -205, -183), new Vector2(360, 80));
            SkinButton(button, home ? "RETOUR AU MENU" : "RÉESSAYER", 23);
            button.GetComponent<Image>().enabled = true;
            if (!home) resultAction = button.GetComponentInChildren<TMP_Text>();
        }
        ShowResult(false);
    }

    public void ShowResult(bool victory)
    {
        if (!resultTitle || !resultAction) return;
        resultTitle.text = victory ? "VICTOIRE" : "DÉFAITE";
        resultTitle.color = victory ? new Color32(255, 225, 155, 255) : new Color32(231, 157, 202, 255);
        resultDivider.color = victory ? Gold : Violet;
        resultMessage.text = victory ? "Le néant recule. Votre voyage continue." : "La boucle n’est pas brisée. Tentez une nouvelle stratégie.";
        resultAction.text = victory ? "CONTINUER" : "RÉESSAYER";
        if (resultPanel.gameObject.activeInHierarchy) resultPanel.SetAsLastSibling();
    }

    void RefreshJournal()
    {
        if (!journalView || !journalPanel.gameObject.activeInHierarchy) return;
        int count = journalView.log_parent.childCount;
        journalCount.text = count + (count == 1 ? " ÉVÉNEMENT" : " ÉVÉNEMENTS") + "  •  LES PLUS RÉCENTS EN PREMIER";
        journalEmpty.gameObject.SetActive(count == 0);
        foreach (Transform child in journalView.log_parent)
        {
            if (!styledLogRows.Add(child.GetInstanceID())) continue;
            var row = child.GetComponent<LogTemplateUI>();
            if (!row || !row.text) continue;
            var group = child.GetComponent<LayoutGroup>();
            if (group) group.enabled = false;
            var fitter = child.GetComponent<ContentSizeFitter>();
            if (fitter) fitter.enabled = false;
            var background = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
            background.sprite = null; background.color = new Color32(29, 21, 42, 255); background.raycastTarget = false;
            Format(row.text, 22, false);
            row.text.alignment = TextAlignmentOptions.MidlineLeft;
            row.text.enableWordWrapping = true;
            Stretch(row.text.rectTransform);
            row.text.rectTransform.offsetMin = new Vector2(70, 14);
            row.text.rectTransform.offsetMax = new Vector2(-22, -14);
            if (row.icon)
            {
                Place(row.icon.transform, new Vector2(0, .5f), new Vector2(33, 0), new Vector2(36, 36));
                row.icon.preserveAspect = true; row.icon.raycastTarget = false;
            }
            var element = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = Mathf.Max(68, row.text.GetPreferredValues(row.text.text, 850, 0).y + 28);
            Art("Violet accent", child, null, new Vector2(0, .5f), new Vector2(2, 0), new Vector2(3, 32)).color = Violet;
        }
    }

    IEnumerator ShowJournalHistory()
    {
        // Let the original panel and GameLogUI start first, then show the existing history.
        // New live entries continue through the original GameLogUI subscription.
        yield return null;
        if (!journalPanel || !GameLogManager.Instance) yield break;
        GameLogUI view = null;
        foreach (var candidate in FindObjectsByType<GameLogUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (candidate.log_parent && candidate.log_parent.IsChildOf(journalPanel)) { view = candidate; break; }
        if (!view || !view.log_template) yield break;
        styledLogRows.Clear();
        foreach (Transform child in view.log_parent) Destroy(child.gameObject);
        foreach (var entry in GameLogManager.Instance.logs)
        {
            var row = Instantiate(view.log_template, view.log_parent);
            row.GetComponent<LogTemplateUI>().Setup(entry);
        }
        Canvas.ForceUpdateCanvases();
        var scroll = view.GetComponent<ScrollRect>();
        if (scroll) scroll.verticalNormalizedPosition = 1;
    }

    void AlignTutorialTargets()
    {
        // Move only the existing HUD highlight/click regions to follow their controls.
        // Tutorial progression, wording, buttons and callbacks are not changed.
        Canvas.ForceUpdateCanvases();
        AlignTutorialTarget(2, manaDock);
        AlignTutorialTarget(3, draw.transform as RectTransform);
        AlignTutorialRect(canvas.Find("Tutorial/Step3/Next") as RectTransform, draw.transform as RectTransform);
        AlignTutorialTarget(4, handRoot);
        if (slots.Length > 0)
        {
            AlignTutorialTarget(5, slots[0].transform as RectTransform);
            AlignTutorialRect(canvas.Find("Tutorial/Step5/Background/BlockRayCast") as RectTransform, slots[0].transform as RectTransform);
        }
        var preview = canvas.Find("Deck/CardSelected") as RectTransform;
        AlignTutorialTarget(6, preview);
        AlignTutorialTarget(7, preview, new Rect(0, .86f, .18f, .14f));
        AlignTutorialTarget(8, preview, new Rect(0, 0, 1, .38f));
        AlignTutorialTarget(9, shop.transform as RectTransform);
        AlignTutorialRect(canvas.Find("Tutorial/Step9/Background/BlockRayCast") as RectTransform, shop.transform as RectTransform);
        AlignTutorialTarget(14, battle.transform as RectTransform);
    }

    void AlignTutorialTarget(int step, RectTransform control, Rect? region = null)
    {
        var proxy = canvas.Find("Tutorial/Step" + step + "/Background/Target") as RectTransform;
        AlignTutorialRect(proxy, control, region);
    }

    static void AlignTutorialRect(RectTransform proxy, RectTransform control, Rect? region = null)
    {
        if (!proxy || !control) return;
        var corners = new Vector3[4];
        control.GetWorldCorners(corners);
        var r = region ?? new Rect(0, 0, 1, 1);
        var size = corners[2] - corners[0];
        var min = proxy.parent.InverseTransformPoint(corners[0] + Vector3.Scale(size, new Vector3(r.xMin, r.yMin, 0)));
        var max = proxy.parent.InverseTransformPoint(corners[0] + Vector3.Scale(size, new Vector3(r.xMax, r.yMax, 0)));
        proxy.localScale = Vector3.one;
        proxy.localRotation = Quaternion.identity;
        proxy.pivot = new Vector2(.5f, .5f);
        proxy.localPosition = (min + max) * .5f;
        proxy.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
    }

    void Refresh()
    {
        var loop = GameLoopManager.instance;
        var deck = DeckManager.instance;
        if (!loop || !deck) return;
        bool running = loop.isGameRunning;
        bool paused = GameManager.instance && GameManager.instance.isPaused;
        bool ready = GameModeManager.selectedDeck != null;
        bool modal = (journalPanel && journalPanel.gameObject.activeInHierarchy)
            || (resultPanel && resultPanel.gameObject.activeInHierarchy)
            || (ShopManager.instance && ShopManager.instance.shopPopup.activeInHierarchy);
        phase.text = running ? "COMBAT EN COURS" : "PRÉPARATION";
        battle.interactable = !running && !paused && !modal && ready && loop.playerUnits.Exists(u => u && u.activeInHierarchy);
        bool full = true;
        for (int i = 0; i < slots.Length; i++)
            if (cardFrames[i]) cardFrames[i].gameObject.SetActive(slots[i] && slots[i].gameObject.activeSelf && !slots[i].IsEmpty());
        foreach (var slot in slots)
        {
            if (!slot) continue;
            if (slot.IsEmpty()) full = false;
            var b = slot.GetComponent<Button>();
            if (b) b.interactable = !running && !paused && !modal && ready;
        }
        draw.interactable = !running && !paused && !modal && ready && !full && deck.deck != null && deck.deck.Count > 0;

        shop.interactable = ready && !paused;
        if (remaining && deck.deck != null) remaining.text = deck.deck.Count.ToString();
        var mana = ManaManager.instance;
        if (mana && (lastMana != mana.currentMana || lastMaxMana != mana.maxMana))
        {
            lastMana = mana.currentMana;
            lastMaxMana = mana.maxMana;
            manaValue.text = "MANA  " + lastMana + " / " + lastMaxMana;
            for (int i = 0; i < manaGems.Length; i++)
                manaGems[i].color = i < lastMana ? Color.white : new Color32(62, 44, 76, 230);
        }
    }

    TMP_FontAsset LoadFont(string name)
    {
        var font = Resources.Load<Font>("UI/EntropyLoop/Fonts/" + name);
        if (!font) return TMP_Settings.defaultFontAsset;
        var asset = TMP_FontAsset.CreateFontAsset(font);
        ownedFonts.Add(asset);
        return asset;
    }

    Sprite Slice(Texture2D texture, int x, int top, int width, int height)
    {
        // Platform import settings may resize this non-power-of-two source atlas.
        float sx = texture.width / 1536f, sy = texture.height / 1024f;
        var sprite = Sprite.Create(texture, new Rect(x * sx, texture.height - (top + height) * sy, width * sx, height * sy), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        ownedSprites.Add(sprite);
        return sprite;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform r)
    {
        r.localScale = Vector3.one;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static void Place(Transform target, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var r = target as RectTransform;
        if (!r) return;
        var layout = r.GetComponent<LayoutGroup>();
        if (layout) layout.enabled = false;
        var fitter = r.GetComponent<ContentSizeFitter>();
        if (fitter) fitter.enabled = false;
        r.localScale = Vector3.one;
        r.localRotation = Quaternion.identity;
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = position;
        r.sizeDelta = size;
    }

    Image Art(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var r = NewRect(name, parent);
        Place(r, anchor, position, size);
        var image = r.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite ? Color.white : Ink;
        image.raycastTarget = false;
        return image;
    }

    Button AdoptButton(Transform target, string name, string caption, Vector2 anchor, Vector2 position, Vector2 size)
    {
        if (!target) throw new System.InvalidOperationException("Missing original combat HUD object: " + name);
        target.SetParent(root, false);
        target.name = name;
        Place(target, anchor, position, size);
        var button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
        var image = target.GetComponent<Image>() ?? target.gameObject.AddComponent<Image>();
        image.raycastTarget = true;
        button.targetGraphic = image;
        SkinButton(button, caption, name == "Combat" ? 37 : 21);
        target.gameObject.tag = "BlockClick";
        target.gameObject.SetActive(true);
        return button;
    }

    void SkinButton(Button button, string caption, float fontSize)
    {
        if (!button) return;
        var image = button.GetComponent<Image>();
        if (image) { image.sprite = button.name == "Combat" ? combatSprite : buttonSprite; image.type = Image.Type.Simple; image.color = buttonSprite ? Color.white : Ink; }
        foreach (var outline in button.GetComponents<Outline>()) outline.enabled = false;
        var colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.05f, 1.3f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.7f, .58f, .85f);
        colors.disabledColor = new Color(.72f, .70f, .78f, 1f);
        colors.fadeDuration = .12f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (!label) label = Label(button.transform, caption, Vector2.zero, new Vector2(180, 40), fontSize, true);
        else Format(label, fontSize, true);
        label.text = caption;
        var r = label.rectTransform;
        Stretch(r); r.offsetMin = new Vector2(15, 7); r.offsetMax = new Vector2(-15, -7);
        button.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    TMP_Text Label(Transform parent, string text, Vector2 position, Vector2 size, float fontSize, bool title)
    {
        var r = NewRect("Label", parent);
        Place(r, new Vector2(.5f, .5f), position, size);
        var label = r.gameObject.AddComponent<TextMeshProUGUI>();
        Format(label, fontSize, title);
        label.text = text;
        return label;
    }

    void Format(TMP_Text label, float fontSize, bool title)
    {
        label.font = title ? titleFont : bodyFont;
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.fontStyle = FontStyles.Normal;
        label.color = Ivory;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    void OnDestroy()
    {
        foreach (var sprite in ownedSprites) if (sprite) Destroy(sprite);
        foreach (var font in ownedFonts)
        {
            if (!font) continue;
            foreach (var texture in font.atlasTextures) if (texture) Destroy(texture);
            if (font.material) Destroy(font.material);
            Destroy(font);
        }
    }
}

// Small vector icon: stays crisp at any game resolution and never receives clicks.
public sealed class CombatHudBattleEmphasis : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    Button button;
    TMP_Text caption;
    CombatHudReadyHalo halo;
    bool hovered;

    public void Initialize(Button target)
    {
        button = target;
        caption = button.GetComponentInChildren<TMP_Text>();
        caption.fontStyle |= FontStyles.Bold;
        var go = new GameObject("Ready violet halo", typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        halo = go.AddComponent<CombatHudReadyHalo>();
        halo.raycastTarget = false;
        halo.enabled = false;
    }

    void LateUpdate()
    {
        if (!button || !halo) return;
        bool ready = button.IsInteractable();
        halo.enabled = ready;
        caption.color = ready ? new Color32(255, 244, 219, 255) : new Color32(191, 180, 199, 255);
        if (!ready) return;
        float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * .8f);
        halo.color = new Color( .72f, .3f, 1f, hovered ? .95f : Mathf.Lerp(.38f, .72f, pulse));
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData data) { hovered = true; }
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData data) { hovered = false; }
    void OnDisable() { hovered = false; if (halo) halo.enabled = false; }
}

public sealed class CombatHudReadyHalo : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        // Light spills outside the frame without resizing the button or its hit area.
        Vector2[] shape = { new(-.34f, .39f), new(.34f, .39f), new(.48f, 0), new(.34f, -.39f), new(-.34f, -.39f), new(-.48f, 0) };
        float[] spread = { 0, 3, 10, 20 };
        float[] opacity = { .15f, 1f, .35f, 0 };
        for (int ring = 0; ring < spread.Length; ring++)
        {
            foreach (var p in shape)
            {
                var position = new Vector2(p.x * r.width, p.y * r.height);
                position += position.normalized * spread[ring];
                var tint = color;
                tint.a *= opacity[ring];
                vh.AddVert((Vector3)(r.center + position), tint, Vector2.zero);
            }
            if (ring == 0) continue;
            for (int i = 0; i < shape.Length; i++)
            {
                int next = (i + 1) % shape.Length;
                int a = (ring - 1) * shape.Length, b = ring * shape.Length;
                vh.AddTriangle(a + i, b + i, a + next);
                vh.AddTriangle(a + next, b + i, b + next);
            }
        }
    }
}

public sealed class CombatHudTopShadow : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        // Opaque black behind the entire header; only the lower edge fades.
        float[] heights = { 0f, .8f, .9f, 1f };
        byte[] alpha = { 255, 255, 150, 0 };
        for (int i = 0; i < heights.Length; i++)
        {
            float y = Mathf.Lerp(r.yMax, r.yMin, heights[i]);
            var tint = new Color32(0, 0, 0, alpha[i]);
            vh.AddVert(new Vector3(r.xMin, y), tint, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, y), tint, Vector2.zero);
            if (i == 0) continue;
            int n = i * 2;
            vh.AddTriangle(n - 2, n, n - 1);
            vh.AddTriangle(n - 1, n, n + 1);
        }
    }
}

public sealed class CombatHudCartGlyph : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Line(vh, new Vector2(-15, 10), new Vector2(-11, 10));
        Line(vh, new Vector2(-11, 10), new Vector2(-7, -5));
        Line(vh, new Vector2(-10, 5), new Vector2(14, 5));
        Line(vh, new Vector2(14, 5), new Vector2(10, -5));
        Line(vh, new Vector2(10, -5), new Vector2(-7, -5));
        Line(vh, new Vector2(-8, 0), new Vector2(12, 0));
        Line(vh, new Vector2(-6, -10), new Vector2(-3, -10));
        Line(vh, new Vector2(7, -10), new Vector2(10, -10));
    }

    void Line(VertexHelper vh, Vector2 a, Vector2 b)
    {
        Vector2 n = Vector2.Perpendicular((b - a).normalized) * 1.2f;
        int index = vh.currentVertCount;
        vh.AddVert(a - n, color, Vector2.zero);
        vh.AddVert(a + n, color, Vector2.zero);
        vh.AddVert(b + n, color, Vector2.zero);
        vh.AddVert(b - n, color, Vector2.zero);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }
}


