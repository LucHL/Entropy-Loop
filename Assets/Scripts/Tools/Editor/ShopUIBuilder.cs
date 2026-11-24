using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Construit (ou reconstruit) l'UI du shop dans la scène ouverte et câble le ShopManager.
// Remplace l'ancien popup "ShopBackground" par un panneau glissant à droite.
public static class ShopUIBuilder
{
    static readonly Color PanelColor = new(0.07f, 0.05f, 0.12f, 0.97f);
    static readonly Color ColumnColor = new(0.11f, 0.08f, 0.18f, 1f);
    static readonly Color BubbleColor = new(0.16f, 0.12f, 0.26f, 0.9f);
    static readonly Color ButtonColor = new(0.17f, 0.12f, 0.27f, 1f);
    static readonly Color ButtonHoverColor = new(0.27f, 0.2f, 0.4f, 1f);
    static readonly Color AccentColor = new(0.91f, 0.77f, 0.42f, 1f);
    static readonly Color TextColor = new(0.91f, 0.89f, 0.95f, 1f);
    static readonly Color SubTextColor = new(0.68f, 0.62f, 0.8f, 1f);

    const float PanelWidth = 1180f;
    const float ColumnWidth = 420f;
    const float PortraitHeight = 520f;
    // La colonne se construit depuis le bas : bouton, bulle, puis portrait posé sur la bulle
    const float LeaveButtonTop = 130f;
    const float BubbleHeight = 240f;
    const float PortraitBottom = LeaveButtonTop + BubbleHeight + 16f;
    const string DialogueFile = "Shop/merchant";
    const string FadeSpritePath = "Assets/Art/Shop/VerticalFade.png";

    [MenuItem("Tools/Shop/Build Shop UI")]
    public static void Build()
    {
        ShopManager manager = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (manager == null) {
            EditorUtility.DisplayDialog("Shop UI Builder", "Aucun ShopManager dans la scène ouverte.", "OK");
            return;
        }

        Transform[] sceneTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform shopRoot = sceneTransforms.FirstOrDefault(t => t.name == "Shop" && t.GetComponentInParent<Canvas>(true) != null);
        if (shopRoot == null) {
            EditorUtility.DisplayDialog("Shop UI Builder", "Objet 'Shop' introuvable sous un Canvas.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Build Shop UI");
        int undoGroup = Undo.GetCurrentGroup();

        // On garde l'icône d'or de l'ancienne UI avant de la supprimer
        Sprite moneySprite = FindSprite(shopRoot, "ShopBackground/MoneyIcon") ?? FindSprite(shopRoot, "ShopPanel/Offers/Footer/MoneyIcon");
        foreach (string oldName in new[] { "ShopBackground", "ShopPanel" }) {
            Transform old = shopRoot.Find(oldName);
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);
        }

        MerchantDialogueData dialogueData = LoadDialogueData();

        // ---- Panneau ----
        RectTransform panel = CreateRect("ShopPanel", shopRoot);
        panel.anchorMin = new Vector2(1, 0);
        panel.anchorMax = new Vector2(1, 1);
        panel.pivot = new Vector2(1, 0.5f);
        panel.sizeDelta = new Vector2(PanelWidth, -80);
        panel.anchoredPosition = Vector2.zero;
        AddImage(panel, PanelColor);
        SlidingPanel slidingPanel = panel.gameObject.AddComponent<SlidingPanel>();

        // ---- Colonne marchand ----
        RectTransform column = CreateRect("MerchantColumn", panel);
        column.anchorMin = Vector2.zero;
        column.anchorMax = new Vector2(0, 1);
        column.pivot = new Vector2(0, 0.5f);
        column.sizeDelta = new Vector2(ColumnWidth, 0);
        column.anchoredPosition = Vector2.zero;
        AddImage(column, ColumnColor);

        // Portrait : l'image entière tient dans le cadre sans se déformer
        RectTransform portraitFrame = CreateRect("PortraitFrame", column);
        AnchorBottom(portraitFrame, PortraitBottom, PortraitHeight);
        portraitFrame.gameObject.AddComponent<RectMask2D>();

        Image portrait = AddImage(CreateRect("Portrait", portraitFrame), Color.white);
        portrait.raycastTarget = false;
        portrait.sprite = LoadPortrait(dialogueData);
        AspectRatioFitter portraitFitter = portrait.gameObject.AddComponent<AspectRatioFitter>();
        portraitFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // image entière, réduite pour tenir dans le cadre
        portraitFitter.aspectRatio = portrait.sprite != null ? portrait.sprite.rect.width / portrait.sprite.rect.height : 0.8f;

        // Fondu en bas pour fondre le portrait dans la colonne
        Image bottomFade = AddImage(CreateRect("BottomFade", portraitFrame), ColumnColor);
        bottomFade.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FadeSpritePath);
        bottomFade.raycastTarget = false;
        AnchorBottom(bottomFade.rectTransform, 0, PortraitHeight * 0.4f);

        TextMeshProUGUI shopLabel = CreateText("ShopLabel", column, "BOUTIQUE", 20, AccentColor, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        shopLabel.characterSpacing = 12;
        AnchorTop(shopLabel.rectTransform, 24, 30, 30);

        // Nom posé sur le bas du portrait
        TextMeshProUGUI merchantName = CreateText("MerchantName", column, dialogueData?.merchantName ?? "Marchand", 46, AccentColor, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        AnchorBottom(merchantName.rectTransform, PortraitBottom + 54, 56, 30);
        TextMeshProUGUI merchantTitle = CreateText("MerchantTitle", column, dialogueData?.merchantTitle ?? "", 24, SubTextColor, TextAlignmentOptions.TopLeft, FontStyles.Italic);
        AnchorBottom(merchantTitle.rectTransform, PortraitBottom + 20, 32, 30);

        // Bulle de dialogue, juste au-dessus du bouton Quitter
        RectTransform bubble = CreateRect("DialogueBubble", column);
        AnchorBottom(bubble, LeaveButtonTop, BubbleHeight, 24);
        AddImage(bubble, BubbleColor).raycastTarget = false;
        Outline bubbleOutline = bubble.gameObject.AddComponent<Outline>();
        bubbleOutline.effectColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.45f);
        bubbleOutline.effectDistance = new Vector2(1.5f, -1.5f);

        TextMeshProUGUI dialogueText = CreateText("Dialogue", bubble, "", 24, TextColor, TextAlignmentOptions.TopLeft, FontStyles.Italic);
        dialogueText.raycastTarget = true; // clic = afficher la réplique en entier
        Stretch(dialogueText.rectTransform);
        dialogueText.rectTransform.offsetMin = new Vector2(20, 16);
        dialogueText.rectTransform.offsetMax = new Vector2(-20, -16);

        Button leaveButton = CreateButton("LeaveButton", column, "Quitter");
        AnchorBottom(leaveButton.GetComponent<RectTransform>(), 30, 70, 24);

        MerchantDialogue merchant = dialogueText.gameObject.AddComponent<MerchantDialogue>();
        SerializedObject merchantSO = new(merchant);
        merchantSO.FindProperty("portraitImage").objectReferenceValue = portrait;
        merchantSO.FindProperty("nameText").objectReferenceValue = merchantName;
        merchantSO.FindProperty("titleText").objectReferenceValue = merchantTitle;
        merchantSO.FindProperty("dialogueText").objectReferenceValue = dialogueText;
        merchantSO.ApplyModifiedPropertiesWithoutUndo();

        // ---- Offres ----
        RectTransform offers = CreateRect("Offers", panel);
        Stretch(offers);
        offers.offsetMin = new Vector2(ColumnWidth, 0);

        // Cases au format des cartes (ratio ~0.71) + 76px pour le prix sous la carte
        RectTransform cardsGrid = CreateRect("CardsGrid", offers);
        Stretch(cardsGrid);
        cardsGrid.offsetMin = new Vector2(40, 95);
        cardsGrid.offsetMax = new Vector2(-40, -25);
        GridLayoutGroup grid = cardsGrid.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(270, 425);
        grid.spacing = new Vector2(40, 30);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        RectTransform footer = CreateRect("Footer", offers);
        AnchorBottom(footer, 15, 70, 40);
        HorizontalLayoutGroup footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerLayout.childAlignment = TextAnchor.MiddleLeft;
        footerLayout.spacing = 14;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = false;
        footerLayout.childForceExpandHeight = false;

        Image moneyIcon = AddImage(CreateRect("MoneyIcon", footer), moneySprite != null ? Color.white : AccentColor);
        moneyIcon.sprite = moneySprite;
        moneyIcon.preserveAspect = true;
        moneyIcon.raycastTarget = false;
        SetSize(moneyIcon, 50, 50);

        TextMeshProUGUI moneyText = CreateText("MoneyText", footer, "200", 40, AccentColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetSize(moneyText, 300, 50);

        Undo.RegisterCreatedObjectUndo(panel.gameObject, "Build Shop UI");

        // ---- Câblage du ShopManager ----
        Transform shopButton = shopRoot.Find("ShopButton");
        Transform manabar = sceneTransforms.FirstOrDefault(t => t != null && t.name == "Manabar");

        Undo.RecordObject(manager, "Build Shop UI");
        manager.shopPanel = slidingPanel;
        manager.moneyText = moneyText;
        manager.leaveButton = leaveButton;
        manager.openButton = shopButton != null ? shopButton.GetComponent<Button>() : null;
        manager.merchant = merchant;
        manager.cardsContainer = cardsGrid;
        manager.hiddenWhileOpen = new[] { manabar, shopButton }
            .Where(t => t != null)
            .Select(t => t.gameObject)
            .ToArray();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Selection.activeGameObject = panel.gameObject;

        Debug.Log("[ShopUIBuilder] UI du shop construite. Pense à sauvegarder la scène.");
    }

    // ---- Data ----

    static MerchantDialogueData LoadDialogueData()
    {
        TextAsset json = Resources.Load<TextAsset>(DialogueFile);
        return json != null ? JsonUtility.FromJson<MerchantDialogueData>(json.text) : null;
    }

    // Le portrait est chargé en Sprite au runtime : on s'assure que la texture est importée comme telle
    static Sprite LoadPortrait(MerchantDialogueData data)
    {
        if (data == null || string.IsNullOrEmpty(data.portrait))
            return null;

        string resourcePath = "Sprites/Story/" + data.portrait;
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
            return null;

        string assetPath = AssetDatabase.GetAssetPath(texture);
        if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer && importer.textureType != TextureImporterType.Sprite) {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static Sprite FindSprite(Transform root, string path)
    {
        Transform t = root.Find(path);
        return t != null && t.TryGetComponent(out Image image) ? image.sprite : null;
    }

    // ---- UI helpers ----

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Image AddImage(Component target, Color color)
    {
        Image image = target.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color,
        TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI tmp = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    static Button CreateButton(string name, Transform parent, string label)
    {
        RectTransform rect = CreateRect(name, parent);
        Image background = AddImage(rect, Color.white);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHoverColor;
        colors.selectedColor = ButtonColor;
        colors.pressedColor = new Color(0.55f, 0.46f, 0.25f, 1f);
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", rect, label, 30, TextColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(24, 0);

        return button;
    }

    // Ancre en haut de la colonne, sur toute la largeur moins une marge
    static void AnchorTop(RectTransform rect, float top, float height, float sideMargin = 0)
    {
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(-2 * sideMargin, height);
        rect.anchoredPosition = new Vector2(0, -top);
    }

    // Ancre en bas du parent, sur toute la largeur moins une marge
    static void AnchorBottom(RectTransform rect, float bottom, float height, float sideMargin = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.sizeDelta = new Vector2(-2 * sideMargin, height);
        rect.anchoredPosition = new Vector2(0, bottom);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void SetHeight(Component target, float height, float flexibleHeight = 0)
    {
        LayoutElement layout = target.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.flexibleHeight = flexibleHeight;
    }

    static void SetSize(Component target, float width, float height)
    {
        LayoutElement layout = target.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
    }
}
