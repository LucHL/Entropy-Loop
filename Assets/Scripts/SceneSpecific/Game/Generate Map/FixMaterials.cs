#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class FixMaterials
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
                mat.SetColor("_BaseColor", mat.color);
                Debug.Log("Materiau corrige : " + mat.name);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Tous les materiaux URP sont maintenant corriges !");
    }
}
#endif
