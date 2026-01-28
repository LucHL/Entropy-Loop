using UnityEngine;
using TMPro;

public class LevelButtonUI : MonoBehaviour
{
    public TMP_Text levelText;
    int levelIndex;

    public void Init(int index)
    {
        levelIndex = index;
        levelText.text = "Level " + index.ToString();
    }

    public void OnClick()
    {
        LoadingScene.Instance.LoadScene("Game");
    }
}
