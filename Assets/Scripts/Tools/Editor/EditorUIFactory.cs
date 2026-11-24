using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Petites fonctions de création d'UI partagées par les outils "Build ... UI"
public static class EditorUIFactory
{
    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static Image AddImage(Component target, Color color)
    {
        Image image = target.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    // Image étirable (9-slice) ; un multiplicateur plus grand donne des coins plus petits
    public static Image AddSlicedImage(Component target, Sprite sprite, Color color, float cornerMultiplier = 1f)
    {
        Image image = AddImage(target, color);
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = cornerMultiplier;
        return image;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color,
        TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal, TMP_FontAsset font = null)
    {
        TextMeshProUGUI tmp = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    // Couleurs du bouton : l'image cible doit être blanche, la teinte vient du ColorBlock
    public static void SetButtonColors(Button button, Color normal, Color highlighted, Color pressed)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.selectedColor = normal;
        colors.pressedColor = pressed;
        colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.35f);
        button.colors = colors;
    }

    // Ancre en haut du parent, sur toute la largeur moins une marge
    public static void AnchorTop(RectTransform rect, float top, float height, float sideMargin = 0)
    {
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(-2 * sideMargin, height);
        rect.anchoredPosition = new Vector2(0, -top);
    }

    // Ancre en bas du parent, sur toute la largeur moins une marge
    public static void AnchorBottom(RectTransform rect, float bottom, float height, float sideMargin = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.sizeDelta = new Vector2(-2 * sideMargin, height);
        rect.anchoredPosition = new Vector2(0, bottom);
    }

    // Taille fixe, centrée sur le parent avec un décalage
    public static void AnchorCenter(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    public static void Stretch(RectTransform rect, float inset = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    public static void SetSize(Component target, float width, float height)
    {
        LayoutElement layout = target.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
    }
}
