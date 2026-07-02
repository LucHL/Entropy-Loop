using UnityEngine;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    public GameObject levelButtonPrefab;

    private LevelData[] levels;

    void Start()
    {
        levels = GameManager.instance.alllevelData;
        GenerateLevels();
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
        }
    }
}
