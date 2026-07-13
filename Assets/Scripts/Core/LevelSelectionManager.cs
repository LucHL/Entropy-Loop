using UnityEngine;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    [SerializeField] GameObject levelButtonPrefab;
    [SerializeField] Transform parentsPosition;

    private LevelData[] levels;

    void Start()
    {
        GenerateLevels();
    }

    void GenerateLevels()
    {
        levels = GameManager.instance.alllevelData;

        for (int i = 1; i <= levels.Length; i++) {
            GameObject btn = Instantiate(levelButtonPrefab, parentsPosition);

            RectTransform rt = btn.GetComponent<RectTransform>();

            rt.anchoredPosition = new Vector2((i - 1) * 200f, 0f);

            LevelButtonUI ui = btn.GetComponent<LevelButtonUI>();

            ui.Init(levels[i - 1]);

            Button button = btn.GetComponent<Button>();
            button.onClick.AddListener(ui.OnClick);
        }
    }

    public void UpdateLevels()
    {
        levels = GameManager.instance.alllevelData;
        GenerateLevels();
    }
}
