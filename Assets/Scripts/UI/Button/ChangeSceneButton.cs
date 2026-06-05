using System;
using UnityEngine;
using UnityEngine.UI;

public class ChangeSceneButton : MonoBehaviour
{
    [SerializeField] string sceneName = "Menu";
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ChangeScene);
    }

    private void ChangeScene()
    {
        if (sceneName == "Game") {
            BugTracker.Info("[Change Scene] loading next level.");
            GameManager.instance.SetNextLevel();
        } else {
            BugTracker.Info("[Change Scene] Moving to 'Menu'.");
            LoadingScene.Instance.ChangeScene(sceneName);
        }
    }
}
