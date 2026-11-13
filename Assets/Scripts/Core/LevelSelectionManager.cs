using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour, IDataPersistence
{
    [Header("References")]
    [SerializeField] GameObject levelBtnPrefab;
    [SerializeField] List<SelectLevelButtonData> allBtnType;
    [SerializeField] GameObject linePrefab;
    [SerializeField] Transform parentsPosition;
    [SerializeField] LevelDetailPanelUI detailPanel;
    [SerializeField] TextMeshProUGUI CurrentMaxLevelReachText;

    [Header("Configs Zigzag")]
    [SerializeField] float horizontalSpacing = 180f;
    [SerializeField] float verticalAmplitude = 120f;
    [SerializeField] float frequency = 1.0f;

    [Header("Zones")]
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
            CurrentMaxLevelReachText.text = "Niveau " + GameManager.instance.maxLevelFinish + " / " + GameManager.instance.alllevelData.Count().ToString();

        levels = GameManager.instance.alllevelData;

        RectTransform contentRect = parentsPosition as RectTransform;
        if (contentRect != null) {
            float totalWidth = levels.Length * horizontalSpacing + 400f;
            contentRect.sizeDelta = new Vector2(totalWidth, contentRect.sizeDelta.y);
        }

        Vector2 previousPosition = Vector2.zero;

        for (int i = 0; i < levels.Length; i++)  {
            bool isBoss = (i + 1) % 10 == 0;

            GameObject btn = InstantiateLevelButtonPrefab(levels[i].panelDetail.levelZone, isBoss, out Sprite levelSprite);
            if (btn == null)
                continue;

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
                        detailPanel.SetIslandSprite(levelSprite);
                    }
                });
            }

            if (i > 0 && linePrefab != null) {
                CreateConnectingLine(previousPosition, currentPosition);
            }

            previousPosition = currentPosition;
        }

        BugTracker.Info("[LevelSelectionManager] All levels are successfuly generated.");
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

    private GameObject InstantiateLevelButtonPrefab(string name, bool isBoss, out Sprite assignedSprite)
    {
        assignedSprite = null;

        SelectLevelButtonData currentBtn = null;
        foreach (SelectLevelButtonData btnType in allBtnType) {
            if (btnType.spriteName == name && btnType.isBossLevel == isBoss) {
                currentBtn = btnType;
                break;
            }
        }

        if (currentBtn == null) {
            BugTracker.Error("[Level Selection] Failed to load sprite for zone: " + name);
            return null;
        }

        GameObject btn = Instantiate(levelBtnPrefab, parentsPosition);

        Image btnImage = btn.GetComponent<Image>();
        if (btnImage != null) {
            btnImage.sprite = currentBtn.levelSprite;
            assignedSprite = currentBtn.levelSprite;
        }

        return btn;
    }
}
