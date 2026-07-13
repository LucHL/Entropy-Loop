using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

        if (levelData.currentlevel == GameManager.instance.maxLevelReach)
            levelText.color = new Color32(159, 76, 221, 255);

        if (levelData.currentlevel > GameManager.instance.maxLevelFinish + 1)
            GetComponent<Button>().interactable = false;
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
