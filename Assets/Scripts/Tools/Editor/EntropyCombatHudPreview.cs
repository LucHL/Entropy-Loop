using UnityEditor;
using UnityEditor.SceneManagement;

// Play always follows the original entry point, even while editing a combat scene.
[InitializeOnLoad]
public static class EntropyCombatHudPreview
{
    static EntropyCombatHudPreview()
    {
        if (!UnityEngine.Application.isBatchMode) EditorApplication.delayCall += UseOriginalMenu;
    }

    static void UseOriginalMenu()
    {
        var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Menu.unity");
        if (menu) EditorSceneManager.playModeStartScene = menu;
    }
}
