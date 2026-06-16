using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class CardDataCreator : EditorWindow
{
    private string cardName = "";
    private string deckFolderName = "NewDeck";
    private Sprite cardImage;
    private GameObject cardUnitPrefab;
    private float cardRarity = 0f;
    private int cardGoldCost = 100;
    private int cardManaCost = 0;
    private bool isCommander = false;

    private string[] availableDecks = new string[0];
    private int selectedDeckIndex = -1;
    private bool useExistingDeck = true;

    private GUIStyle titleStyle, headerStyle, bigButtonStyle;

    [MenuItem("Tools/Card Data Creator")]
    public static void ShowWindow()
    {
        var win = GetWindow<CardDataCreator>("Card Creator");
        win.minSize = new Vector2(400, 470);
        win.RefreshDeckList();
    }

    private void OnFocus() => RefreshDeckList();

    private void RefreshDeckList()
    {
        string decksRoot = "Assets/Resources/Decks";
        if (!Directory.Exists(decksRoot))
        {
            availableDecks = new string[0];
            return;
        }

        string[] files = Directory.GetFiles(decksRoot, "*Deck.asset");
        List<string> names = new();
        foreach (var file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.EndsWith("Deck"))
                fileName = fileName.Substring(0, fileName.Length - 4);
            names.Add(fileName);
        }
        availableDecks = names.ToArray();

        if (selectedDeckIndex >= availableDecks.Length)
            selectedDeckIndex = -1;
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Création de Card Data", titleStyle);
        GUILayout.Space(5);

        EditorGUILayout.BeginVertical(headerStyle);

        // Sélection du deck
        EditorGUILayout.LabelField("Deck", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        useExistingDeck = GUILayout.Toggle(useExistingDeck, "Existant", EditorStyles.radioButton, GUILayout.Width(80));
        useExistingDeck = !GUILayout.Toggle(!useExistingDeck, "Nouveau", EditorStyles.radioButton, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        if (useExistingDeck)
        {
            if (availableDecks.Length > 0)
            {
                selectedDeckIndex = EditorGUILayout.Popup("Choisir un deck", selectedDeckIndex, availableDecks);
                if (selectedDeckIndex >= 0)
                    deckFolderName = availableDecks[selectedDeckIndex];
            }
            else
            {
                EditorGUILayout.HelpBox("Aucun deck trouvé dans Assets/Resources/Decks/", MessageType.Warning);
            }
        }
        else
        {
            deckFolderName = EditorGUILayout.TextField(new GUIContent("Nom du Deck", "Créera un nouveau deck dans Assets/Resources/Decks/"), deckFolderName);
        }

        GUILayout.Space(5);

        // Champs de la carte
        cardName = EditorGUILayout.TextField("Nom de la Carte", cardName);
        cardImage = (Sprite)EditorGUILayout.ObjectField("Image (Sprite)", cardImage, typeof(Sprite), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        cardUnitPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab de l'Unité", cardUnitPrefab, typeof(GameObject), false);
        cardRarity = EditorGUILayout.Slider("Rareté (0-10)", cardRarity, 0f, 10f);
        cardGoldCost = EditorGUILayout.IntField("Coût en Or", cardGoldCost);
        cardManaCost = EditorGUILayout.IntField("Coût en Mana", cardManaCost);

        GUILayout.Space(5);
        isCommander = EditorGUILayout.Toggle(new GUIContent("Commandant du deck", "Définit cette carte comme commandant du deck (remplace l'ancien si déjà défini)"), isCommander);

        EditorGUILayout.EndVertical();

        GUILayout.Space(15);

        bool ready = !string.IsNullOrEmpty(cardName)
                  && !string.IsNullOrEmpty(deckFolderName)
                  && cardUnitPrefab != null
                  && (useExistingDeck ? selectedDeckIndex >= 0 : true);

        using (new EditorGUI.DisabledScope(!ready))
        {
            if (GUILayout.Button("Créer la Card Data", bigButtonStyle))
            {
                CreateCardData();
                ResetFields();
            }
        }

        if (!ready)
            EditorGUILayout.HelpBox("Veuillez remplir le nom, choisir un deck et assigner un Prefab.", MessageType.Info);
    }

    private void ResetFields()
    {
        cardName = "";
        cardImage = null;
        cardUnitPrefab = null;
        cardRarity = 0f;
        cardGoldCost = 100;
        cardManaCost = 0;
        isCommander = false;
        GUI.FocusControl(null);
    }

    private void CreateCardData()
    {
        CardData newData = CreateInstance<CardData>();
        SerializedObject so = new SerializedObject(newData);

        SetPropertySafe(so, "cardName", cardName);
        SetPropertySafe(so, "cardImage", cardImage);
        SetPropertySafe(so, "unitPrefab", cardUnitPrefab);
        SetPropertySafe(so, "rarity", cardRarity);
        SetPropertySafe(so, "goldCost", cardGoldCost);
        SetPropertySafe(so, "manaCost", cardManaCost);

        so.ApplyModifiedProperties();
        newData.name = cardName;

        string cardPath = $"Assets/Prefabs/Decks/{deckFolderName}/Cards Data";
        EnsureFolder(cardPath);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{cardPath}/{cardName}_Card.asset");
        AssetDatabase.CreateAsset(newData, assetPath);
        AssetDatabase.SaveAssets();

        BugTracker.Info($"[CardTool] CardData '{cardName}' créée dans : {assetPath}");

        UpdateDeckData(newData);
        RefreshDeckList();

        Selection.activeObject = newData;
        EditorGUIUtility.PingObject(newData);
    }

    private void UpdateDeckData(CardData newCard)
    {
        string deckFolder = "Assets/Resources/Decks";
        EnsureFolder(deckFolder);
        string deckPath = $"{deckFolder}/{deckFolderName}Deck.asset";

        DeckData deckData = AssetDatabase.LoadAssetAtPath<DeckData>(deckPath);

        if (deckData == null)
        {
            deckData = CreateInstance<DeckData>();
            deckData.deckName = deckFolderName;
            deckData.cards = new List<CardData>();
            AssetDatabase.CreateAsset(deckData, deckPath);
            BugTracker.Info($"[CardTool] Nouveau DeckData '{deckFolderName}' créé dans Resources.");
        }

        // Définir le commandant si la case est cochée
        if (isCommander)
        {
            deckData.commander = newCard;
            BugTracker.Info($"[CardTool] '{newCard.cardName}' défini comme commandant de '{deckFolderName}'.");
        }

        if (!deckData.cards.Contains(newCard))
        {
            deckData.cards.Add(newCard);
            BugTracker.Info($"[CardTool] '{newCard.cardName}' ajoutée au deck '{deckFolderName}'.");
        }
        else
        {
            BugTracker.Warning($"[CardTool] '{newCard.cardName}' déjà dans le deck '{deckFolderName}'.");
        }

        EditorUtility.SetDirty(deckData);
        AssetDatabase.SaveAssets();
    }

    private void SetPropertySafe(SerializedObject so, string propName, object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop == null) prop = so.FindProperty(char.ToUpper(propName[0]) + propName.Substring(1));

        if (prop != null)
        {
            if (value is string s) prop.stringValue = s;
            else if (value is float f) prop.floatValue = f;
            else if (value is int i) prop.intValue = i;
            else if (value is Object o) prop.objectReferenceValue = o;
        }
        else
        {
            BugTracker.Warning($"[CardTool] Propriété '{propName}' introuvable dans CardData.");
        }
    }

    private void EnsureStyles()
    {
        if (titleStyle == null)
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, margin = new RectOffset(0, 0, 10, 5), alignment = TextAnchor.MiddleCenter };
        if (headerStyle == null)
            headerStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 10, 10) };
        if (bigButtonStyle == null)
            bigButtonStyle = new GUIStyle(EditorStyles.miniButton) { fixedHeight = 40, fontStyle = FontStyle.Bold, fontSize = 12 };
    }

    public static void EnsureFolder(string path)
    {
        if (Directory.Exists(path)) return;
        string[] folders = path.Split('/');
        string current = folders[0];
        for (int i = 1; i < folders.Length; i++)
        {
            if (!AssetDatabase.IsValidFolder(current + "/" + folders[i]))
                AssetDatabase.CreateFolder(current, folders[i]);
            current += "/" + folders[i];
        }
    }
}