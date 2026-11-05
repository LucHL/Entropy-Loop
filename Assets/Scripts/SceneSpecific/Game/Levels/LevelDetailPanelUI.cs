using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LevelDetailPanelUI : MonoBehaviour
{
    [Header("UI References du Panneau")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] TextMeshProUGUI levelNumberText;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI currentPhaseNbr; // pas encore ajouté
    [SerializeField] Image[] phaseBar; // pas encore ajouté
    [SerializeField] Button enterArenaButton;

    private LevelData currentLevelData;

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (enterArenaButton != null)
        {
            enterArenaButton.onClick.RemoveAllListeners();
            enterArenaButton.onClick.AddListener(OnEnterArenaClicked);
        }
    }

    public void ShowPanel(LevelData data)
    {
        currentLevelData = data;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (levelNumberText != null)
            levelNumberText.text = "NIVEAU " + data.currentlevel.ToString("00");

        if (titleText != null)
            titleText.text = data.levelTitle;

        if (descriptionText != null)
            descriptionText.text = data.levelDescription;
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void OnEnterArenaClicked()
    {
        if (currentLevelData == null) return;

        GameManager.instance.SaveLevelConfig(currentLevelData);

        bool tuto = (currentLevelData.currentlevel == 1);

        if (!string.IsNullOrEmpty(currentLevelData.chaptersBeforeGame)) {
            LoadingScene.Instance.LoadStory();
            return;
        }

        LoadingScene.Instance.LoadGame(tuto);
    }
}
