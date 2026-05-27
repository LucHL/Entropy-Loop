using UnityEngine;
using TMPro;

public class LevelButtonUI : MonoBehaviour
{
    public TMP_Text levelText;

    private LevelData levelData;

    public void Init(LevelData data)
    {
        levelData = data;
        levelText.text = "Level " + levelData.currentlevel.ToString();
    }

    public void OnClick()
    {
        if (levelData.chaptersBeforeGame != "") {
            Debug.Log(levelData.chaptersBeforeGame);
            GameManager.Instance.SaveLevelConfig(levelData);

            LoadingScene.Instance.LoadStory();
            return;
        }

        LoadingScene.Instance.LoadGame();
    }
}
