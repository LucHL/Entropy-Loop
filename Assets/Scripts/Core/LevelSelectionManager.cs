using UnityEngine;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    public GameObject levelButtonPrefab;
    public int levelCount = 50;

    private LevelData levelConfig;

    void Start()
    {
        GenerateLevels();
    }

    void GenerateLevels()
    {
        for (int i = 1; i <= levelCount; i++) {
            GameObject btn = Instantiate(levelButtonPrefab, transform);

            RectTransform rt = btn.GetComponent<RectTransform>();

            rt.anchoredPosition = new Vector2((i - 1) * 200f - 600, 0f); // TODO delete -600

            LevelButtonUI ui = btn.GetComponent<LevelButtonUI>();

            levelConfig.currentLevel = i;
            ui.Init(levelConfig);

            Button button = btn.GetComponent<Button>();
            button.onClick.AddListener(ui.OnClick);

            if (i != 1) {
                button.interactable = false; // Only one level for now
            }
        }
    }
}
