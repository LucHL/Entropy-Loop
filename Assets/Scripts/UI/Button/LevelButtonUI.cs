using UnityEngine;
using TMPro;

public class LevelButtonUI : MonoBehaviour
{
    public TMP_Text levelText;
    int levelIndex;

    private LevelData levelData;

    public void Init(LevelData data)
    {
        levelData = data;
        levelIndex = levelData.currentlevel;
        levelText.text = "Level " + levelData.currentlevel.ToString();
    }

    public void OnClick()
    {
        // if (levelData.hasStory) {
        //     // load story
        // }
        LoadingScene.Instance.LoadGame();
    }
}
