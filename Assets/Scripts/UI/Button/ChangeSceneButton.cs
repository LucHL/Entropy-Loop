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
        LoadingScene.Instance.ChangeScene(sceneName);
    }
}
