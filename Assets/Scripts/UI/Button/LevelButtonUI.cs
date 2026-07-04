using UnityEngine;
using TMPro;

public class LevelButtonUI : MonoBehaviour
{
    public TextMeshProUGUI levelText;

    private LevelData levelData;

    public void Init(LevelData data)
    {
        levelData = data;
        levelText.text = "Level " + levelData.currentlevel.ToString();

        if (levelData.currentlevel <= GameManager.instance.maxLevelFinish)
            levelText.color = new Color32(81, 255, 0, 255);
    }

    public void OnClick()
    {
        GameManager.instance.SaveLevelConfig(levelData);

        if (levelData.chaptersBeforeGame != "") {
            LoadingScene.Instance.LoadStory();
            return;
        }

        bool tuto = false;
        if (levelData.currentlevel == 1)
            tuto = true;

        LoadingScene.Instance.LoadGame(tuto);
    }
}
