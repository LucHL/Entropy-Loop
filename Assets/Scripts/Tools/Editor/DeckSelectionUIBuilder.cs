using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using static EditorUIFactory;

// Reconstruit le prefab DeckSelectionList sous forme de livre ouvert (utilisé par les scènes Menu et Game).
// On garde BlockUI et le bouton de fermeture d'origine : les scènes ont des overrides dessus.
public static class DeckSelectionUIBuilder
{
    const string PrefabPath = "Assets/Prefabs/UI/Canvas/DeckSelectionList.prefab";
    // bouton commun du jeu (menu principal, paramètres)
    const string GameButtonPrefabPath = "Assets/Prefabs/UI/Button/Menu & Settings/ButtonMenu.prefab";
    const string SpritesFolder = "Assets/Art/DeckSelection/";
    // polices de l'ATH (EntropyLoopHud) : Cinzel pour les titres et libellés, Comfortaa pour les paragraphes
    const string TitleFontPath = "Assets/Resources/UI/EntropyLoop/Fonts/Cinzel.ttf";
    const string TitleFontAssetPath = "Assets/Art/Fonts/Cinzel SDF.asset";
    const string BodyFontPath = "Assets/Resources/UI/EntropyLoop/Fonts/Comfortaa.ttf";
    const string BodyFontAssetPath = "Assets/Art/Fonts/Comfortaa SDF.asset";
    const string FontCharacters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "ÀÂÄÆÇÉÈÊËÎÏÔÖŒÙÛÜŸàâäæçéèêëîïôöœùûüÿ«»‹›’‘“”…—–·×";

    // fond indigo presque noir + halo violet diffus autour du livre (comme la maquette)
    // l'opacité du fond est appliquée par le DeckSelectionManager selon la scène
    static readonly Color BackdropColor = new Color32(6, 3, 18, 255);
    const float MenuBackdropAlpha = 0.94f;
    const float GameBackdropAlpha = 0.97f;
    static readonly Color HaloColor = new Color32(90, 45, 160, 12);
    const float HaloMargin = 130; // marge transparente autour du livre dans BookGlow.png
    static readonly Color TitleColor = new(0.98f, 0.93f, 0.84f, 1f);
    static readonly Color Ivory = new Color32(241, 232, 213, 255);
    static readonly Color Gold = new(0.82f, 0.64f, 0.36f, 1f);
    static readonly Color GoldSoft = new(0.82f, 0.64f, 0.36f, 0.55f);
    static readonly Color GoldLight = new(1f, 0.85f, 0.55f, 1f);
    static readonly Color CoverColor = new(0.21f, 0.13f, 0.07f, 1f);
    static readonly Color TurningPageColor = new(0.27f, 0.17f, 0.09f, 1f);
    static readonly Color SpineColor = new(0.3f, 0.19f, 0.1f, 1f);
    static readonly Color GrimoireTitleColor = new(0.72f, 0.54f, 0.3f, 1f);
    static readonly Color SectionColor = new(0.66f, 0.46f, 0.98f, 1f);
    static readonly Color BodyTextColor = new(0.8f, 0.72f, 0.62f, 1f);
    static readonly Color MutedTextColor = new(0.58f, 0.5f, 0.42f, 1f);
    static readonly Color TagColor = new(0.4f, 0.2f, 0.66f, 1f);

    // bouton grisé : assombri mais opaque, pour rester bien détaché du fond
    static readonly Color DisabledButtonColor = new(0.55f, 0.53f, 0.6f, 1f);

    static readonly Vector2 BookSize = new(1320, 680);
    static readonly Vector2 BookPosition = new(0, 10);
    static readonly Vector2 SelectButtonSize = new(340, 136);

    static Sprite rounded, spine, bookGlow;
    static Sprite obsidian;
    static TMP_FontAsset titleFont, bodyFont;

    [MenuItem("Tools/Deck/Build Deck Selection UI")]
    public static void Build()
    {
        rounded = LoadSprite("RoundedRect.png");
        spine = LoadSprite("SpineGradient.png");
        bookGlow = LoadSprite("BookGlow.png");
        obsidian = LoadSprite("ObsidianButton.png");
        titleFont = GetOrCreateFont(TitleFontPath, TitleFontAssetPath, "Cinzel SDF");
        bodyFont = GetOrCreateFont(BodyFontPath, BodyFontAssetPath, "Comfortaa SDF");

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try {
            BuildBook(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[DeckSelectionUIBuilder] Prefab DeckSelectionList reconstruit.");
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void BuildBook(GameObject root)
    {
        Transform rootTransform = root.transform;
        DeckSelectionManager manager = root.GetComponent<DeckSelectionManager>();

        // Bouton de fermeture d'origine ("Button") : on le garde pour ne pas casser les overrides des scènes
        Button closeButton = root.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(b => b.name == "Button" || b.name == "CloseButton");
        if (closeButton != null)
            closeButton.transform.SetParent(rootTransform, false);

        foreach (Transform child in rootTransform.Cast<Transform>().ToList())
            if (child.name != "BlockUI" && (closeButton == null || child != closeButton.transform))
                Object.DestroyImmediate(child.gameObject);

        // ---- Fond : indigo presque opaque, halo violet derrière le livre ----
        Transform blockUI = rootTransform.Find("BlockUI");
        if (blockUI != null) {
            Stretch(blockUI.GetComponent<RectTransform>());
            Image backdrop = blockUI.GetComponent<Image>();
            backdrop.color = BackdropColor;
            manager.backdrop = backdrop;
            manager.menuBackdropAlpha = MenuBackdropAlpha;
            manager.gameBackdropAlpha = GameBackdropAlpha;
        }

        Image halo = AddSlicedImage(CreateRect("BookHalo", rootTransform), bookGlow, HaloColor, 1f);
        halo.raycastTarget = false;
        AnchorCenter(halo.rectTransform, BookSize + Vector2.one * (2 * HaloMargin), BookPosition);

        TextMeshProUGUI title = CreateText("Title", rootTransform, "Choisissez votre Deck", 66, TitleColor, TextAlignmentOptions.Center, font: titleFont);
        AnchorTop(title.rectTransform, 38, 90);

        Image underline = AddImage(CreateRect("TitleUnderline", rootTransform), GoldSoft);
        underline.rectTransform.anchorMin = underline.rectTransform.anchorMax = new Vector2(0.5f, 1);
        underline.rectTransform.sizeDelta = new Vector2(240, 2);
        underline.rectTransform.anchoredPosition = new Vector2(0, -138);

        // ---- Livre ----
        RectTransform book = CreateRect("Book", rootTransform);
        AnchorCenter(book, BookSize, BookPosition);

        Stretch(AddSlicedImage(CreateRect("BookBorder", book), rounded, Gold, 1.4f).rectTransform);
        Stretch(AddSlicedImage(CreateRect("BookCover", book), rounded, CoverColor, 1.4f).rectTransform, 3);

        RectTransform leftPage = CreateRect("LeftPage", book);
        leftPage.anchorMin = Vector2.zero;
        leftPage.anchorMax = new Vector2(0.5f, 1);
        leftPage.offsetMin = new Vector2(40, 30);
        leftPage.offsetMax = new Vector2(-35, -30);

        RectTransform rightPage = CreateRect("RightPage", book);
        rightPage.anchorMin = new Vector2(0.5f, 0);
        rightPage.anchorMax = Vector2.one;
        rightPage.offsetMin = new Vector2(40, 30);
        rightPage.offsetMax = new Vector2(-45, -30);

        BuildLeftPage(leftPage, manager);
        BuildRightPage(rightPage, manager);

        // Page qui se tourne : au-dessus des pages, sous la reliure
        RectTransform pageArea = CreateRect("PageArea", book);
        Stretch(pageArea, 3);
        Image turningPage = AddSlicedImage(CreateRect("TurningPage", pageArea), rounded, TurningPageColor, 2.8f);
        BookPageTurn pageTurn = book.gameObject.AddComponent<BookPageTurn>();
        SerializedObject pageTurnSO = new(pageTurn);
        pageTurnSO.FindProperty("turningPage").objectReferenceValue = turningPage.rectTransform;
        pageTurnSO.FindProperty("turningPageImage").objectReferenceValue = turningPage;
        pageTurnSO.ApplyModifiedPropertiesWithoutUndo();
        turningPage.gameObject.SetActive(false);

        BuildSpine(book);

        if (closeButton != null)
            PlaceCloseButton(closeButton, book);

        // ---- Navigation ----
        float arrowX = BookSize.x / 2 + 75;
        manager.previousButton = CreateArrow("PreviousButton", rootTransform, "‹", new Vector2(-arrowX, BookPosition.y));
        manager.nextButton = CreateArrow("NextButton", rootTransform, "›", new Vector2(arrowX, BookPosition.y));

        float belowBook = BookPosition.y - BookSize.y / 2;
        Button selectButton = CreateGameButton("SelectButton", rootTransform,
            new Vector2(0, belowBook - 12 - SelectButtonSize.y / 2), out TextMeshProUGUI selectText);

        TextMeshProUGUI indicator = CreateText("PageIndicator", rootTransform, "GRIMOIRE 1 / 1", 18, Gold, TextAlignmentOptions.Center, font: titleFont);
        indicator.characterSpacing = 4;
        AnchorCenter(indicator.rectTransform, new Vector2(300, 30), new Vector2(0, belowBook - SelectButtonSize.y - 25));
        foreach (float side in new[] { -1f, 1f }) {
            Image dash = AddImage(CreateRect("Dash", indicator.rectTransform), GoldSoft);
            AnchorCenter(dash.rectTransform, new Vector2(36, 2), new Vector2(side * 175, 0));
        }

        manager.disabledTextAlpha = 0.5f;
        manager.pageTurn = pageTurn;
        manager.selectButton = selectButton;
        manager.selectButtonText = selectText;
        manager.pageIndicatorText = indicator;
    }

    static void BuildLeftPage(RectTransform page, DeckSelectionManager manager)
    {
        TextMeshProUGUI deckName = CreateText("DeckName", page, "Deck", 46, Gold, TextAlignmentOptions.Center, font: titleFont);
        AnchorTop(deckName.rectTransform, 12, 60);

        // ornement : trait - losange - trait
        RectTransform ornament = CreateRect("Ornament", page);
        AnchorTop(ornament, 78, 16);
        foreach (float side in new[] { -1f, 1f }) {
            Image line = AddImage(CreateRect("Line", ornament), GoldSoft);
            AnchorCenter(line.rectTransform, new Vector2(80, 2), new Vector2(side * 58, 0));
        }
        Image diamond = AddImage(CreateRect("Diamond", ornament), Gold);
        AnchorCenter(diamond.rectTransform, new Vector2(9, 9), Vector2.zero);
        diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

        // zone des cartes : le DeckSelectionManager les dispose selon leur nombre
        RectTransform grid = CreateRect("CardsGrid", page);
        Stretch(grid);
        grid.offsetMin = new Vector2(0, 2);
        grid.offsetMax = new Vector2(0, -104);
        manager.cardSpacing = new Vector2(18, 16);
        manager.maxCardHeight = 320;

        // modèle de carte, dupliqué par le DeckSelectionManager
        Image card = AddImage(CreateRect("CardTemplate", grid), Color.white);
        card.preserveAspect = true;
        card.raycastTarget = false;
        Shadow shadow = card.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(4, -4);

        Image badge = AddSlicedImage(CreateRect("Badge", card.rectTransform), rounded, new Color(0, 0, 0, 0.8f), 3f);
        badge.raycastTarget = false;
        RectTransform badgeRect = badge.rectTransform;
        badgeRect.anchorMin = badgeRect.anchorMax = badgeRect.pivot = new Vector2(1, 0);
        badgeRect.sizeDelta = new Vector2(44, 26);
        badgeRect.anchoredPosition = new Vector2(-4, 4);
        TextMeshProUGUI badgeText = CreateText("Count", badgeRect, "x2", 17, GoldLight, TextAlignmentOptions.Center, font: titleFont);
        Stretch(badgeText.rectTransform);

        manager.deckNameText = deckName;
        manager.cardsContainer = grid;
        manager.cardTemplate = card.gameObject;
    }

    static void BuildRightPage(RectTransform page, DeckSelectionManager manager)
    {
        TextMeshProUGUI category = CreateText("Category", page, "CATÉGORIE", 17, Gold, TextAlignmentOptions.TopLeft, font: titleFont);
        category.characterSpacing = 3;
        AnchorTop(category.rectTransform, 22, 26);

        TextMeshProUGUI grimoireTitle = CreateText("GrimoireTitle", page, "Titre du grimoire", 44, GrimoireTitleColor, TextAlignmentOptions.TopLeft, font: titleFont);
        grimoireTitle.textWrappingMode = TextWrappingModes.NoWrap;
        grimoireTitle.enableAutoSizing = true;
        grimoireTitle.fontSizeMin = 28;
        grimoireTitle.fontSizeMax = 44;
        AnchorTop(grimoireTitle.rectTransform, 50, 60);

        Image separator = AddImage(CreateRect("Separator", page), GoldSoft);
        AnchorTop(separator.rectTransform, 118, 2);

        AnchorTop(CreateText("HistoryHeader", page, "HISTOIRE & ORIGINE", 19, SectionColor, TextAlignmentOptions.TopLeft, font: titleFont).rectTransform, 140, 26);
        TextMeshProUGUI lore = CreateBodyText("Lore", page);
        AnchorTop(lore.rectTransform, 172, 175);

        AnchorTop(CreateText("MechanicsHeader", page, "MÉCANIQUE DE JEU", 19, SectionColor, TextAlignmentOptions.TopLeft, font: titleFont).rectTransform, 362, 26);

        // Ligne "Type :" + étiquette dont la largeur suit le texte
        RectTransform typeRow = CreateRect("TypeRow", page);
        AnchorTop(typeRow, 396, 34);
        HorizontalLayoutGroup rowLayout = typeRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 10;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;

        CreateText("TypeLabel", typeRow, "Type :", 18, MutedTextColor, TextAlignmentOptions.MidlineLeft, font: bodyFont);
        Image tag = AddSlicedImage(CreateRect("Tag", typeRow), rounded, TagColor, 3f);
        HorizontalLayoutGroup tagLayout = tag.gameObject.AddComponent<HorizontalLayoutGroup>();
        tagLayout.padding = new RectOffset(14, 14, 5, 5);
        tagLayout.childControlWidth = tagLayout.childControlHeight = true;
        tagLayout.childForceExpandWidth = tagLayout.childForceExpandHeight = false;
        TextMeshProUGUI deckType = CreateText("TagText", tag.rectTransform, "TYPE", 16, Color.white, TextAlignmentOptions.Center, font: titleFont);

        TextMeshProUGUI mechanics = CreateBodyText("Mechanics", page);
        AnchorTop(mechanics.rectTransform, 442, 140);

        manager.categoryText = category;
        manager.grimoireTitleText = grimoireTitle;
        manager.loreText = lore;
        manager.deckTypeText = deckType;
        manager.mechanicsText = mechanics;
    }

    static TextMeshProUGUI CreateBodyText(string name, RectTransform page)
    {
        TextMeshProUGUI text = CreateText(name, page, "", 19, BodyTextColor, TextAlignmentOptions.TopLeft, font: bodyFont);
        text.lineSpacing = 8;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14;
        text.fontSizeMax = 19;
        return text;
    }

    static void BuildSpine(RectTransform book)
    {
        Image spineImage = AddImage(CreateRect("Spine", book), SpineColor);
        spineImage.sprite = spine;
        spineImage.raycastTarget = false;
        AnchorCenter(spineImage.rectTransform, new Vector2(40, BookSize.y - 24), Vector2.zero);

        foreach (float y in new[] { -255f, -85f, 85f, 255f }) {
            Image ring = AddSlicedImage(CreateRect("Ring", spineImage.rectTransform), rounded, Gold, 8f);
            ring.raycastTarget = false;
            AnchorCenter(ring.rectTransform, new Vector2(8, 26), new Vector2(0, y));
        }
    }

    // Croix dans le coin haut-droit du livre, à cheval sur la bordure, au-dessus de tout le reste
    static void PlaceCloseButton(Button closeButton, RectTransform book)
    {
        closeButton.name = "CloseButton";
        closeButton.transform.SetParent(book, false);
        closeButton.transform.SetAsLastSibling();
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = Vector2.one;
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(64, 64);
        closeRect.anchoredPosition = new Vector2(-6, -6);
        closeRect.localScale = Vector3.one;
        SkinObsidianButton(closeButton, 2.2f);

        TextMeshProUGUI closeText = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (closeText != null) {
            closeText.text = "×";
            closeText.font = titleFont != null ? titleFont : closeText.font;
            closeText.fontStyle = FontStyles.Normal;
            closeText.fontSize = 38;
            closeText.color = Ivory;
            closeText.alignment = TextAlignmentOptions.Center;
            Stretch(closeText.rectTransform);
            closeText.rectTransform.anchoredPosition = new Vector2(0, 3);
        }
    }

    // Instance du prefab ButtonMenu : même sprite, police et animation de survol que les boutons du menu
    static Button CreateGameButton(string name, Transform parent, Vector2 position, out TextMeshProUGUI label)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameButtonPrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        AnchorCenter((RectTransform)instance.transform, SelectButtonSize, position);
        instance.transform.localScale = Vector3.one;

        // le prefab appelle MainMenu.PlayGame : le DeckSelectionManager branche son propre listener
        Button button = instance.GetComponent<Button>();
        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, 0);
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.disabledColor = DisabledButtonColor;
        button.colors = colors;

        // texte comme le bouton BOUTIQUE de l'ATH : Cinzel ivoire, sans gras
        label = instance.GetComponentInChildren<TextMeshProUGUI>(true);
        if (titleFont != null)
            label.font = titleFont;
        label.fontStyle = FontStyles.Normal;
        label.color = Ivory;
        label.text = "SÉLECTIONNER CE DECK";
        label.enableAutoSizing = true;
        label.fontSizeMin = 18;
        label.fontSizeMax = 32;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.rectTransform.offsetMin = new Vector2(34, 16);
        label.rectTransform.offsetMax = new Vector2(-34, -16);
        return button;
    }

    static Button CreateArrow(string name, Transform parent, string glyph, Vector2 position)
    {
        RectTransform rect = CreateRect(name, parent);
        AnchorCenter(rect, new Vector2(72, 112), position);
        Button button = SkinObsidianButton(rect.gameObject.AddComponent<Button>(), 1.6f);

        TextMeshProUGUI label = CreateText("Glyph", rect, glyph, 56, Ivory, TextAlignmentOptions.Center, font: titleFont);
        Stretch(label.rectTransform);
        label.rectTransform.anchoredPosition = new Vector2(0, 4);
        return button;
    }

    // Même rendu que les boutons de l'ATH : pierre d'obsidienne, survol plus lumineux, appui violet
    static Button SkinObsidianButton(Button button, float cornerMultiplier)
    {
        Image image = button.GetComponent<Image>();
        if (image == null)
            image = button.gameObject.AddComponent<Image>();
        image.sprite = obsidian;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = cornerMultiplier;
        image.color = Color.white;
        image.raycastTarget = true;
        button.targetGraphic = image;

        foreach (Outline outline in button.GetComponents<Outline>())
            outline.enabled = false;

        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.05f, 1.3f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.7f, 0.58f, 0.85f);
        colors.disabledColor = DisabledButtonColor;
        colors.fadeDuration = 0.12f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }

    // ---- Assets ----

    static Sprite LoadSprite(string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesFolder + fileName);
        if (sprite == null)
            Debug.LogWarning($"[DeckSelectionUIBuilder] Sprite introuvable : {SpritesFolder}{fileName} (vérifie qu'il est importé en Sprite).");
        return sprite;
    }

    // TMP font asset généré une fois depuis le .ttf
    static TMP_FontAsset GetOrCreateFont(string fontPath, string assetPath, string name)
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (fontAsset != null)
            return fontAsset;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null) {
            Debug.LogWarning($"[DeckSelectionUIBuilder] Police introuvable : {fontPath}.");
            return null;
        }

        fontAsset = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        fontAsset.name = name;
        AssetDatabase.CreateAsset(fontAsset, assetPath);

        fontAsset.material.name = name + " Material";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        fontAsset.atlasTexture.name = name + " Atlas";
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);

        // Atlas rempli une fois puis figé : sinon TMP le modifie à chaque Play et l'asset change dans git
        fontAsset.TryAddCharacters(FontCharacters);
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        EditorUtility.SetDirty(fontAsset.atlasTexture);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }
}
