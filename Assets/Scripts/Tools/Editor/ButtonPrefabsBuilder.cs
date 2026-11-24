using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static EditorUIFactory;

// Prefabs de boutons à partir des images de Art/Button (Boutons jeu, Boutons Settings, Boutons Menu du jeu).
// Les prefabs existants (Menu & Settings) sont rhabillés sur place ; les nouveaux sont créés dans Jeu/, Settings/ et Menu/.
// Relançable : règle l'import des images puis met à jour les prefabs.
public static class ButtonPrefabsBuilder
{
    const string GameArt = "Assets/Art/Button/Boutons jeu/";
    const string SettingsArt = "Assets/Art/Button/Boutons Settings/";
    const string MenuArt = "Assets/Art/Button/Boutons Menu du jeu/";
    const string GameOut = "Assets/Prefabs/UI/Button/Jeu/";
    const string SettingsOut = "Assets/Prefabs/UI/Button/Settings/";
    const string MenuOut = "Assets/Prefabs/UI/Button/Menu/";
    const string ExistingFolder = "Assets/Prefabs/UI/Button/Menu & Settings/";
    const string TitleFontPath = "Assets/Art/Fonts/Cinzel SDF.asset";
    const string BodyFontPath = "Assets/Art/Fonts/Comfortaa SDF.asset";

    // Les images sont dessinées en x2 : les prefabs font la moitié de leur taille en pixels
    const float Scale = 0.5f;

    static readonly Color Ivory = new Color32(241, 232, 213, 255);

    // Marges d'étirement (9-slice) en pixels de l'image : gauche, bas, droite, haut.
    // Elles englobent les icônes dessinées (flèches, case du numéro, coche...) pour qu'elles ne se déforment pas.
    static readonly (string prefix, Vector4 border)[] Borders = {
        ("back_button", new Vector4(100, 34, 34, 34)),
        ("previous_button", new Vector4(100, 34, 34, 34)),
        ("skip_button", new Vector4(100, 34, 34, 34)),
        ("next_button", new Vector4(34, 34, 100, 34)),
        ("level_button", new Vector4(165, 40, 80, 40)),
        ("nameplate", new Vector4(40, 40, 40, 40)),
        ("dialogue_window", new Vector4(60, 60, 60, 60)),
        ("button_", new Vector4(24, 24, 24, 24)),
        ("nav_", new Vector4(24, 24, 24, 24)),
        ("dropdown_field", new Vector4(24, 24, 24, 24)),
        ("dropdown_option", new Vector4(10, 4, 10, 4)),
        ("dropdown_panel", new Vector4(24, 24, 24, 24)),
        ("slider_track", new Vector4(14, 0, 14, 0)),
        ("slider_fill", new Vector4(14, 0, 14, 0)),
        ("panel_section", new Vector4(32, 32, 32, 32)),
        ("panel_header", new Vector4(32, 32, 32, 32)),
        ("settings_", new Vector4(110, 30, 30, 30)),
        ("play_", new Vector4(110, 30, 30, 30)),
    };

    static TMP_FontAsset titleFont, bodyFont;

    [MenuItem("Tools/UI/Build Button Prefabs")]
    public static void Build()
    {
        ConfigureSprites(GameArt);
        ConfigureSprites(SettingsArt);
        ConfigureSprites(MenuArt);

        titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);
        bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);
        if (titleFont == null || bodyFont == null)
            Debug.LogWarning("[ButtonPrefabsBuilder] Polices Cinzel/Comfortaa introuvables dans Assets/Art/Fonts : lance d'abord Tools > Deck > Build Deck Selection UI.");

        // ---- Boutons du jeu ----
        BuildIconButton("BackButton", "back_button", "RETOUR", iconOnLeft: true);
        BuildIconButton("PreviousButton", "previous_button", "PRÉCÉDENT", iconOnLeft: true);
        BuildIconButton("NextButton", "next_button", "SUIVANT", iconOnLeft: false);
        BuildIconButton("SkipButton", "skip_button", "PASSER", iconOnLeft: true);
        BuildLevelButton();

        // ---- Paramètres : on réutilise les prefabs existants (Menu & Settings), seul ResetButton est nouveau ----
        BuildStateButton("ResetButton", "button_reset", "EFFACER");
        ReskinApplyButton();
        ReskinTabs();
        ReskinToggle();
        ReskinSlider();
        ReskinDropdown();

        // ---- Menu principal ----
        // Deck et Quitter : le texte / l'icône sont dans l'image, taille fixe. Jouer et Paramètres : étirables, libellé ajouté.
        BuildMenuButton("PlayButton", "play", "JOUER", sliced: true);
        BuildMenuButton("DeckButton", "deck", null, sliced: false);
        BuildMenuButton("QuitButton", "quit_square", null, sliced: false);
        BuildMenuButton("SettingsButton", "settings", "PARAMÈTRES", sliced: true);

        AssetDatabase.SaveAssets();
        Debug.Log($"[ButtonPrefabsBuilder] Prefabs générés dans {GameOut}, {SettingsOut} et {MenuOut}.");
    }

    // ---------------------------------------------------------------- Import des images

    static void ConfigureSprites(string folder)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder.TrimEnd('/') })) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                continue;

            string name = Path.GetFileNameWithoutExtension(path);
            Vector4 border = Vector4.zero;
            foreach (var (prefix, value) in Borders)
                if (name.StartsWith(prefix)) {
                    border = value;
                    break;
                }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    static Sprite LoadSprite(string folder, string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(folder + name + ".png");
        if (sprite == null)
            Debug.LogWarning($"[ButtonPrefabsBuilder] Sprite introuvable : {folder}{name}.png");
        return sprite;
    }

    static Vector2 SizeOf(Sprite sprite, Vector2 fallback)
    {
        return sprite != null ? sprite.rect.size * Scale : fallback;
    }

    // ---------------------------------------------------------------- Boutons du jeu

    // Bouton avec une icône dessinée d'un côté (flèche, avance rapide) et un libellé de l'autre
    static void BuildIconButton(string prefabName, string spriteName, string label, bool iconOnLeft)
    {
        Sprite normal = LoadSprite(GameArt, spriteName + "_normal");
        Sprite hover = LoadSprite(GameArt, spriteName + "_hover");

        RectTransform root = CreateRect(prefabName, null);
        root.sizeDelta = SizeOf(normal, new Vector2(160, 62));
        Image image = AddSlicedImage(root, normal, Color.white, 1f / Scale);
        Button button = root.gameObject.AddComponent<Button>();
        SetSpriteSwap(button, image, hover, hover, normal, normal);

        // le libellé occupe la partie sans icône (marge d'étirement côté icône)
        float iconWidth = 100 * Scale + 4;
        TextMeshProUGUI text = CreateText("Label", root, label, 20, Ivory, TextAlignmentOptions.Center, font: titleFont);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(iconOnLeft ? iconWidth : 18, 8);
        text.rectTransform.offsetMax = new Vector2(iconOnLeft ? -18 : -iconWidth, -8);
        AutoSize(text, 12, 20);

        Save(root, GameOut + prefabName + ".prefab");
    }

    static void BuildLevelButton()
    {
        Sprite normal = LoadSprite(GameArt, "level_button_normal");

        RectTransform root = CreateRect("LevelButton", null);
        root.sizeDelta = SizeOf(normal, new Vector2(340, 86));
        Image image = AddSlicedImage(root, normal, Color.white, 1f / Scale);
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        // numéro dans la case de gauche, titre au centre ; la coche / le cadenas sont dans l'image à droite
        TextMeshProUGUI number = CreateText("Number", root, "1", 30, Ivory, TextAlignmentOptions.Center, font: titleFont);
        RectTransform numberRect = number.rectTransform;
        numberRect.anchorMin = Vector2.zero;
        numberRect.anchorMax = new Vector2(0, 1);
        numberRect.pivot = new Vector2(0, 0.5f);
        numberRect.offsetMin = new Vector2(17 * Scale, 17 * Scale);
        numberRect.offsetMax = new Vector2(145 * Scale, -17 * Scale);

        TextMeshProUGUI title = CreateText("Title", root, "Niveau 1", 22, Ivory, TextAlignmentOptions.MidlineLeft, font: titleFont);
        Stretch(title.rectTransform);
        title.rectTransform.offsetMin = new Vector2(170 * Scale, 8);
        title.rectTransform.offsetMax = new Vector2(-90 * Scale, -8);
        AutoSize(title, 14, 22);

        LevelButtonSkin skin = root.gameObject.AddComponent<LevelButtonSkin>();
        skin.numberText = number;
        skin.titleText = title;
        SetFields(skin,
            ("normal", normal),
            ("normalSelected", LoadSprite(GameArt, "level_button_selected")),
            ("completed", LoadSprite(GameArt, "level_button_completed")),
            ("completedSelected", LoadSprite(GameArt, "level_button_completed_selected")),
            ("locked", LoadSprite(GameArt, "level_button_locked")));
        skin.SetState(LevelButtonSkin.LevelState.Available);

        Save(root, GameOut + "LevelButton.prefab");
    }

    // ---------------------------------------------------------------- Menu principal

    // Images "_normal", "_hovered", "_pressed" et éventuellement "_disabled"
    static void BuildMenuButton(string prefabName, string spriteName, string label, bool sliced)
    {
        Sprite normal = LoadSprite(MenuArt, spriteName + "_normal");
        Sprite disabled = AssetDatabase.LoadAssetAtPath<Sprite>(MenuArt + spriteName + "_disabled.png");

        RectTransform root = CreateRect(prefabName, null);
        root.sizeDelta = SizeOf(normal, new Vector2(160, 160));
        Image image = sliced ? AddSlicedImage(root, normal, Color.white, 1f / Scale) : AddImage(root, Color.white);
        image.sprite = normal;
        if (!sliced)
            image.preserveAspect = true;

        Button button = root.gameObject.AddComponent<Button>();
        SetSpriteSwap(button, image,
            LoadSprite(MenuArt, spriteName + "_hovered"),
            LoadSprite(MenuArt, spriteName + "_pressed"),
            normal,
            disabled != null ? disabled : normal);

        if (label != null) {
            // le libellé occupe la partie à droite de l'icône (marge d'étirement gauche)
            TextMeshProUGUI text = CreateText("Label", root, label, 24, Ivory, TextAlignmentOptions.Center, font: titleFont);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(110 * Scale + 4, 8);
            text.rectTransform.offsetMax = new Vector2(-18, -8);
            AutoSize(text, 14, 24);
        }

        Save(root, MenuOut + prefabName + ".prefab");
    }

    // ---------------------------------------------------------------- Paramètres

    // Bouton à 4 états dessinés (normal, survol, appuyé, désactivé)
    static void BuildStateButton(string prefabName, string spriteName, string label)
    {
        string folder = SettingsArt + "Buttons/";
        Sprite normal = LoadSprite(folder, spriteName + "_normal");

        RectTransform root = CreateRect(prefabName, null);
        root.sizeDelta = SizeOf(normal, new Vector2(200, 44));
        Image image = AddSlicedImage(root, normal, Color.white, 1f / Scale);
        Button button = root.gameObject.AddComponent<Button>();
        SetSpriteSwap(button, image,
            LoadSprite(folder, spriteName + "_hover"),
            LoadSprite(folder, spriteName + "_pressed"),
            normal,
            LoadSprite(folder, spriteName + "_disabled"));

        TextMeshProUGUI text = CreateText("Label", root, label, 18, Ivory, TextAlignmentOptions.Center, font: titleFont);
        Stretch(text.rectTransform, 8);
        AutoSize(text, 12, 18);

        Save(root, SettingsOut + prefabName + ".prefab");
    }

    // ---------------------------------------------------------------- Prefabs existants (rhabillés sur place)
    // On ne change que l'apparence : les objets et composants restent les mêmes, donc toutes leurs
    // utilisations (panneau Paramètres, scènes) gardent leurs réglages, actions et références.

    static void EditPrefab(string path, System.Action<GameObject> edit)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) {
            Debug.LogWarning($"[ButtonPrefabsBuilder] Prefab introuvable : {path}");
            return;
        }
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // une échelle non uniforme déformerait les bords dessinés : on la reporte sur la taille
    static void UniformScale(RectTransform rect)
    {
        Vector3 scale = rect.localScale;
        if (Mathf.Approximately(scale.x, 0))
            return;
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y * scale.y / scale.x);
        rect.localScale = new Vector3(scale.x, scale.x, scale.x);
    }

    static void SetSliced(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f / Scale;
        image.color = Color.white;
    }

    static void SetSimple(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
    }

    // l'ancien script d'animation remplaçait le sprite au survol : il entrerait en conflit avec le SpriteSwap
    static void RemoveOldAnimations(GameObject go)
    {
        foreach (AnimeButton anime in go.GetComponentsInChildren<AnimeButton>(true))
            Object.DestroyImmediate(anime);
        // un contour ajouté par-dessus le cadre dessiné le doublerait
        foreach (Outline outline in go.GetComponents<Outline>())
            outline.enabled = false;
    }

    static void StyleLabel(TMP_Text label, TMP_FontAsset font, float maxSize)
    {
        StyleText(label, font, maxSize, label.alignment);
        label.enableAutoSizing = true;
        label.fontSizeMin = 8;
        label.fontSizeMax = maxSize;
    }

    // Bouton Appliquer du panneau Paramètres
    static void ReskinApplyButton()
    {
        string folder = SettingsArt + "Buttons/";
        EditPrefab(ExistingFolder + "ApplyButton.prefab", root => {
            RectTransform rect = (RectTransform)root.transform;
            UniformScale(rect);
            Image image = root.GetComponent<Image>();
            Sprite normal = LoadSprite(folder, "button_apply_normal");
            SetSliced(image, normal);
            SetSpriteSwap(root.GetComponent<Button>(), image,
                LoadSprite(folder, "button_apply_hover"),
                LoadSprite(folder, "button_apply_pressed"),
                normal,
                LoadSprite(folder, "button_apply_disabled"));
            RemoveOldAnimations(root);
            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true)) {
                StyleLabel(label, titleFont, Mathf.Max(10, rect.sizeDelta.y * 0.5f));
                label.alignment = TextAlignmentOptions.Center;
            }
        });
    }

    // Onglets du panneau Paramètres (ButtonSettings) : cadre "sélectionné" sur l'onglet actif
    static void ReskinTabs()
    {
        string folder = SettingsArt + "Nav/";
        EditPrefab(ExistingFolder + "ButtonSettings.prefab", root => {
            RectTransform rect = (RectTransform)root.transform;
            UniformScale(rect);
            Image image = root.GetComponent<Image>();
            Sprite normal = LoadSprite(folder, "nav_normal");
            SetSliced(image, normal);
            SetSpriteSwap(root.GetComponent<Button>(), image,
                LoadSprite(folder, "nav_hover"),
                LoadSprite(folder, "nav_pressed"),
                normal,
                LoadSprite(folder, "nav_disabled"));
            RemoveOldAnimations(root);

            Transform existing = root.transform.Find("Selected");
            Image selected = existing != null ? existing.GetComponent<Image>() : AddImage(CreateRect("Selected", root.transform), Color.white);
            SetSliced(selected, LoadSprite(folder, "nav_selected"));
            selected.raycastTarget = false;
            Stretch(selected.rectTransform);
            selected.transform.SetAsFirstSibling();
            selected.gameObject.SetActive(false);

            NavTab tab = root.GetComponent<NavTab>();
            if (tab == null)
                tab = root.AddComponent<NavTab>();
            SetFields(tab, ("selectedOverlay", selected.gameObject));

            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true)) {
                StyleLabel(label, titleFont, Mathf.Max(10, rect.sizeDelta.y * 0.4f));
                label.alignment = TextAlignmentOptions.Center;
            }
        });
    }

    // Case à cocher (Toggle) -> interrupteur : la case devient la piste, la coche devient le bouton rond
    static void ReskinToggle()
    {
        string folder = SettingsArt + "Toggle/";
        EditPrefab(ExistingFolder + "Toggle.prefab", root => {
            Toggle toggle = root.GetComponent<Toggle>();
            Transform background = root.transform.Find("Background");
            Image track = background != null ? background.GetComponent<Image>() : null;
            Transform checkmark = background != null ? background.Find("Checkmark") : null;
            Image knob = checkmark != null ? checkmark.GetComponent<Image>() : null;
            if (toggle == null || track == null || knob == null) {
                Debug.LogWarning("[ButtonPrefabsBuilder] Toggle.prefab : Background/Checkmark introuvables.");
                return;
            }

            Sprite trackOff = LoadSprite(folder, "toggle_track_off_normal");
            Sprite knobOff = LoadSprite(folder, "toggle_knob_off");
            Vector2 trackSize = SizeOf(trackOff, new Vector2(72, 36));

            SetSimple(track, trackOff);
            track.preserveAspect = false;
            track.rectTransform.localScale = Vector3.one;
            track.rectTransform.sizeDelta = trackSize;

            SetSimple(knob, knobOff);
            knob.raycastTarget = false;
            knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = knob.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            knob.rectTransform.localScale = Vector3.one;
            knob.rectTransform.sizeDelta = SizeOf(knobOff, new Vector2(28, 28));

            // la coche ne doit plus disparaître quand on décoche : c'est UISwitch qui la déplace
            toggle.graphic = null;
            toggle.targetGraphic = track;
            toggle.transition = Selectable.Transition.None;

            UISwitch skin = root.GetComponent<UISwitch>();
            if (skin == null)
                skin = root.AddComponent<UISwitch>();
            SetFields(skin,
                ("track", track), ("knob", knob),
                ("knobTravel", (object)(trackSize.x / 2 - trackSize.y / 2)),
                ("trackOff", trackOff),
                ("trackOffHover", LoadSprite(folder, "toggle_track_off_hover")),
                ("trackOffDisabled", LoadSprite(folder, "toggle_track_off_disabled")),
                ("trackOn", LoadSprite(folder, "toggle_track_on_normal")),
                ("trackOnHover", LoadSprite(folder, "toggle_track_on_hover")),
                ("trackOnDisabled", LoadSprite(folder, "toggle_track_on_disabled")),
                ("knobOff", knobOff),
                ("knobOn", LoadSprite(folder, "toggle_knob_on")),
                ("knobDisabled", LoadSprite(folder, "toggle_knob_disabled")));
        });
    }

    static void ReskinSlider()
    {
        string folder = SettingsArt + "Sliders/";
        EditPrefab(ExistingFolder + "Slider.prefab", root => {
            Slider slider = root.GetComponent<Slider>();
            Transform background = root.transform.Find("Background");
            Image track = background != null ? background.GetComponent<Image>() : null;
            Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
            Image handle = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
            if (track == null || fill == null || handle == null) {
                Debug.LogWarning("[ButtonPrefabsBuilder] Slider.prefab : Background/Fill/Handle introuvables.");
                return;
            }

            Sprite trackNormal = LoadSprite(folder, "slider_track_normal");
            Sprite fillNormal = LoadSprite(folder, "slider_fill_normal");
            Sprite knobNormal = LoadSprite(folder, "slider_knob_normal");
            SetSliced(track, trackNormal);
            SetSliced(fill, fillNormal);
            fill.raycastTarget = false;
            SetSimple(handle, knobNormal);

            SetSpriteSwap(slider, handle,
                LoadSprite(folder, "slider_knob_hover"),
                LoadSprite(folder, "slider_knob_pressed"),
                knobNormal,
                LoadSprite(folder, "slider_knob_disabled"));

            UISliderSkin skin = root.GetComponent<UISliderSkin>();
            if (skin == null)
                skin = root.AddComponent<UISliderSkin>();
            SetFields(skin,
                ("track", track), ("fill", fill),
                ("trackNormal", trackNormal),
                ("trackHover", LoadSprite(folder, "slider_track_hover")),
                ("trackDisabled", LoadSprite(folder, "slider_track_disabled")),
                ("fillNormal", fillNormal),
                ("fillHover", LoadSprite(folder, "slider_fill_hover")),
                ("fillDisabled", LoadSprite(folder, "slider_fill_disabled")));
        });
    }

    static void ReskinDropdown()
    {
        string folder = SettingsArt + "Dropdown/";
        EditPrefab(ExistingFolder + "Dropdown.prefab", root => {
            TMP_Dropdown dropdown = root.GetComponent<TMP_Dropdown>();
            RectTransform rect = (RectTransform)root.transform;
            UniformScale(rect);

            Image field = root.GetComponent<Image>();
            Sprite fieldNormal = LoadSprite(folder, "dropdown_field_normal");
            SetSliced(field, fieldNormal);
            SetSpriteSwap(dropdown, field,
                LoadSprite(folder, "dropdown_field_hover"),
                LoadSprite(folder, "dropdown_field_pressed"),
                fieldNormal,
                LoadSprite(folder, "dropdown_field_disabled"));

            if (dropdown.captionText != null)
                StyleLabel(dropdown.captionText, bodyFont, Mathf.Max(10, rect.sizeDelta.y * 0.5f));

            Transform arrowTransform = root.transform.Find("Arrow");
            if (arrowTransform != null && arrowTransform.TryGetComponent(out Image arrow)) {
                SetSimple(arrow, LoadSprite(folder, "dropdown_arrow_normal"));
                arrow.rectTransform.localScale = Vector3.one;
                arrow.rectTransform.sizeDelta = new Vector2(14, 9);
            }

            RectTransform template = dropdown.template;
            if (template != null) {
                template.localScale = Vector3.one;
                if (template.TryGetComponent(out Image panel))
                    SetSliced(panel, LoadSprite(folder, "dropdown_panel_expanded"));
            }

            Toggle item = template != null ? template.GetComponentInChildren<Toggle>(true) : null;
            if (item != null) {
                Sprite option = LoadSprite(folder, "dropdown_option_normal");
                Sprite optionHover = LoadSprite(folder, "dropdown_option_hover");
                if (item.targetGraphic is Image background) {
                    SetSliced(background, option);
                    background.rectTransform.localScale = Vector3.one;
                    Stretch(background.rectTransform);
                    SetSpriteSwap(item, background, optionHover, optionHover, option, option);
                }
                if (item.graphic is Image checkmark) {
                    SetSliced(checkmark, LoadSprite(folder, "dropdown_option_selected"));
                    checkmark.raycastTarget = false;
                    checkmark.rectTransform.localScale = Vector3.one;
                    Stretch(checkmark.rectTransform);
                }
            }
            if (dropdown.itemText != null) {
                StyleLabel(dropdown.itemText, bodyFont, 16);
                dropdown.itemText.rectTransform.localScale = Vector3.one;
                dropdown.itemText.transform.SetAsLastSibling();
            }
        });
    }

    // ---------------------------------------------------------------- Utilitaires

    static void SetSpriteSwap(Selectable selectable, Image image, Sprite highlighted, Sprite pressed, Sprite selected, Sprite disabled)
    {
        selectable.targetGraphic = image;
        selectable.transition = Selectable.Transition.SpriteSwap;
        selectable.spriteState = new SpriteState {
            highlightedSprite = highlighted,
            pressedSprite = pressed,
            selectedSprite = selected,
            disabledSprite = disabled,
        };
        // pas de navigation clavier : évite qu'un bouton reste affiché "sélectionné" après un clic
        selectable.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    static void StyleText(TMP_Text text, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
    {
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.color = Ivory;
        text.alignment = alignment;
        text.fontStyle = FontStyles.Normal;
    }

    static void AutoSize(TMP_Text text, float min, float max)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = min;
        text.fontSizeMax = max;
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    static void SetFields(Object target, params (string name, object value)[] fields)
    {
        SerializedObject so = new(target);
        foreach (var (name, value) in fields) {
            SerializedProperty property = so.FindProperty(name);
            if (property == null) {
                Debug.LogWarning($"[ButtonPrefabsBuilder] Champ '{name}' introuvable sur {target.GetType().Name}.");
                continue;
            }
            if (value is float number)
                property.floatValue = number;
            else
                property.objectReferenceValue = value as Object;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    static void Save(RectTransform root, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
        Object.DestroyImmediate(root.gameObject);
    }
}
