using UnityEditor;
using UnityEngine;
using System.IO;

public class CardDataCreator : EditorWindow
{
    private string cardName = "";
    private string deckFolderName = "NewDeck";
    private Sprite cardImage;
    private GameObject cardUnitPrefab;
    private float cardRarity = 0f;

    private GUIStyle titleStyle, headerStyle, bigButtonStyle;

    [MenuItem("Tools/Card Data Creator")]
    public static void ShowWindow()
    {
        var win = GetWindow<CardDataCreator>("Card Creator");
        win.minSize = new Vector2(400, 350);
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Création de Card Data", titleStyle);
        GUILayout.Space(5);

        EditorGUILayout.BeginVertical(headerStyle);

        cardName = EditorGUILayout.TextField("Nom de la Carte", cardName);
        deckFolderName = EditorGUILayout.TextField(new GUIContent("Nom du Deck", "Le nom du sous-dossier dans Resources/Card Data/"), deckFolderName);

        cardImage = (Sprite)EditorGUILayout.ObjectField("Image (Sprite)", cardImage, typeof(Sprite), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        
        cardUnitPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab de l'Unité", cardUnitPrefab, typeof(GameObject), false);
        
        cardRarity = EditorGUILayout.Slider("Rareté (0-10)", cardRarity, 0f, 10f);

        EditorGUILayout.EndVertical();

        GUILayout.Space(15);

        bool ready = !string.IsNullOrEmpty(cardName) 
                  && !string.IsNullOrEmpty(deckFolderName) 
                  && cardUnitPrefab != null;

        using (new EditorGUI.DisabledScope(!ready))
        {
            if (GUILayout.Button("Créer la Card Data", bigButtonStyle))
            {
                CreateCardData();
            }
        }

        if (!ready)
        {
            EditorGUILayout.HelpBox("Veuillez remplir le nom, le deck et assigner un Prefab.", MessageType.Info);
        }
    }

    private void CreateCardData()
    {
        CardData newData = CreateInstance<CardData>();

        SerializedObject so = new SerializedObject(newData);

        SetPropertySafe(so, "cardName", cardName);
        SetPropertySafe(so, "cardImage", cardImage);
        SetPropertySafe(so, "unitPrefab", cardUnitPrefab);
        SetPropertySafe(so, "rarity", cardRarity); 

        so.ApplyModifiedProperties();

        string path = $"Assets/Resources/Card Data/{deckFolderName}";
        EnsureFolder(path);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{cardName}_Card.asset");

        AssetDatabase.CreateAsset(newData, assetPath);
        AssetDatabase.SaveAssets();

        Selection.activeObject = newData;
        EditorGUIUtility.PingObject(newData);

        BugTracker.Info($"[CardTool] Nouvelle Card Data '{cardName}' créée avec succès dans : {assetPath}");
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
            BugTracker.Warning($"[CardTool] Propriété '{propName}' introuvable dans le script CardData. Vérifiez l'orthographe.");
        }
    }

    private void EnsureStyles()
    {
        if (titleStyle == null) 
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, margin = new RectOffset(0,0,10,5), alignment = TextAnchor.MiddleCenter };
        
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
