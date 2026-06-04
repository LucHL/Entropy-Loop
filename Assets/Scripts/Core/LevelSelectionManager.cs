using UnityEngine;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    public GameObject levelButtonPrefab;
    public int levelCount = 50;

    private LevelData[] levels;

    void Start()
    {
        LoadLevelsConfig();
        GenerateLevels();
    }

    void LoadLevelsConfig()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Levels/levels_config");

        if (jsonFile != null) {
            LevelsWrapper wrapper = JsonUtility.FromJson<LevelsWrapper>(jsonFile.text);
            levels = wrapper.levels;
            GameManager.instance.alllevelData = levels;
        } else
            BugTracker.Critical("Failed to load levels configs from 'levels_config.json'.");
    }

    void GenerateLevels()
    {
        for (int i = 1; i <= levels.Length; i++) {
            GameObject btn = Instantiate(levelButtonPrefab, transform);

            RectTransform rt = btn.GetComponent<RectTransform>();

            rt.anchoredPosition = new Vector2((i - 1) * 200f - 600, 0f); // TODO delete -600

            LevelButtonUI ui = btn.GetComponent<LevelButtonUI>();

            ui.Init(levels[i - 1]);

            Button button = btn.GetComponent<Button>();
            button.onClick.AddListener(ui.OnClick);

            // if (i != 1) {
            //     button.interactable = false; // Only one level for now
            // }
        }
    }
}
