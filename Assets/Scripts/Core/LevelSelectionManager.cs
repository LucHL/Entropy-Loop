using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectManager : MonoBehaviour, IDataPersistence
{
    [Header("References")]
    [SerializeField] GameObject levelButtonPrefab;
    [SerializeField] GameObject linePrefab;
    [SerializeField] Transform parentsPosition;
    [SerializeField] LevelDetailPanelUI detailPanel;
    [SerializeField] TextMeshProUGUI CurrentMaxLevelReachText;

    [Header("Configs Zigzag")]
    [SerializeField] float horizontalSpacing = 180f;
    [SerializeField] float verticalAmplitude = 120f;
    [SerializeField] float frequency = 1.0f;

    [Header("Zones")]
    [SerializeField] int levelsPerZone = 12;
    [SerializeField] GameObject[] zoneBackgrounds;

    private LevelData[] levels;

    public void LoadData(GameData data)
    {
        GenerateLevels();
    }

    public void SaveData(GameData data)
    {
    }

    void GenerateLevels()
    {
        if (GameManager.instance == null || GameManager.instance.alllevelData == null)
            return;

        foreach (Transform child in parentsPosition) {
            Destroy(child.gameObject);
        }

        if (CurrentMaxLevelReachText != null)
            CurrentMaxLevelReachText.text = "Niveau " + GameManager.instance.maxLevelFinish + " / 20";

        levels = GameManager.instance.alllevelData;

        RectTransform contentRect = parentsPosition as RectTransform;
        if (contentRect != null) {
            float totalWidth = levels.Length * horizontalSpacing + 400f;
            contentRect.sizeDelta = new Vector2(totalWidth, contentRect.sizeDelta.y);
        }

        Vector2 previousPosition = Vector2.zero;

        for (int i = 0; i < levels.Length; i++)  {
            int currentZone = i / levelsPerZone;

            GameObject btn = Instantiate(levelButtonPrefab, parentsPosition);
            RectTransform rt = btn.GetComponent<RectTransform>();

            float posX = i * horizontalSpacing + 80f;
            float posY = Mathf.Sin(i * frequency) * verticalAmplitude;

            Vector2 currentPosition = new Vector2(posX, posY);
            rt.anchoredPosition = currentPosition;

            LevelButtonUI ui = btn.GetComponent<LevelButtonUI>();
            if (ui != null) {
                ui.Init(levels[i], (selectedLevelData) => {
                    if (detailPanel != null) {
                        detailPanel.ShowPanel(selectedLevelData);
                    }
                });
            }

            if (i > 0 && linePrefab != null) {
                CreateConnectingLine(previousPosition, currentPosition);
            }

            previousPosition = currentPosition;
        }
    }

    void CreateConnectingLine(Vector2 posA, Vector2 posB)
    {
        GameObject lineObj = Instantiate(linePrefab, parentsPosition);
        lineObj.transform.SetAsFirstSibling();

        RectTransform lineRt = lineObj.GetComponent<RectTransform>();

        Vector2 direction = posB - posA;
        float distance = direction.magnitude;
        Vector2 middlePoint = (posA + posB) / 2f;

        lineRt.anchoredPosition = middlePoint;
        lineRt.sizeDelta = new Vector2(distance, lineRt.sizeDelta.y);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineRt.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void UpdateLevels()
    {
        GenerateLevels();
    }
}
