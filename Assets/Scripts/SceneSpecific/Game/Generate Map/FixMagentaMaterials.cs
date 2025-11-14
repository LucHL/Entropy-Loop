using UnityEngine;
using UnityEditor;
using System.IO;

public class FixMagentaMaterials
{
    [MenuItem("VoidGalaxy/Fix All Materials")]
    public static void FixMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                Debug.Log("✅ Shader fixed for: " + mat.name);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("🎉 Tous les matériaux sont corrigés !");
    }
}
