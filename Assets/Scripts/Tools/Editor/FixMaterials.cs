using UnityEngine;
using UnityEditor;

public class FixMaterials : MonoBehaviour
{
    [MenuItem("VoidGalaxy/Fix URP Materials")]
    public static void FixAllMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                mat.SetColor("_BaseColor", mat.color); // Si déjà définie
                Debug.Log("✅ Matériau corrigé : " + mat.name);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("🎉 Tous les matériaux URP sont maintenant corrigés !");
    }
}
