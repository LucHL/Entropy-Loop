using UnityEditor;
using UnityEngine;
using TMPro;

public class ScalableTextTool : EditorWindow
{
    [MenuItem("Tools/Accessibility/Add ScalableText to all texts")]
    public static void AddToAllTexts()
    {
        TextMeshProUGUI[] texts = GameObject.FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        int added = 0;
        foreach (var text in texts)
        {
            if (text.GetComponent<ScalableText>() == null)
            {
                Undo.AddComponent<ScalableText>(text.gameObject);
                added++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ScalableTextTool] {added} composants ScalableText ajoutés (sur {texts.Length} textes trouvés).");
    }
}