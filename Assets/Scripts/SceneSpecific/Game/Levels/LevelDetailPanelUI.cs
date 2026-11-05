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
    [SerializeField] TextMeshProUGUI currentPhaseNbr;
    [SerializeField] TextMeshProUGUI difficulty;
    [SerializeField] Image[] phaseBar;
    [SerializeField] Button enterArenaButton;

    private LevelData currentLevelData;

    private Color colorBarFill;
    private Color colorBarEmpty;

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (enterArenaButton != null) {
            enterArenaButton.onClick.RemoveAllListeners();
            enterArenaButton.onClick.AddListener(OnEnterArenaClicked);
        }

        ColorUtility.TryParseHtmlString("#B7A0FF", out colorBarFill);
        ColorUtility.TryParseHtmlString("#343047", out colorBarEmpty);
    }

    public void ShowPanel(LevelData data)
    {
        currentLevelData = data;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (levelNumberText != null)
            levelNumberText.text = "NIVEAU " + data.currentlevel.ToString("00");

        if (titleText != null)
            titleText.text = data.panelDetail.levelTitle;

        if (descriptionText != null)
            descriptionText.text = data.panelDetail.levelDescription;

        if (currentPhaseNbr != null) {
            string romanChar = data.panelDetail.totalPhases switch {
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                5 => "V",
                _ => "?",
            };
            currentPhaseNbr.text = "Phase " + romanChar;
        }

        if (difficulty != null)
            difficulty.text = "Difficulté " + FirstLetterToUpper(data.spawnAlgo.difficulty);

        for (int i = 1; i <= 6; i++) {
            if (i <= data.panelDetail.totalPhases)
                phaseBar[i - 1].color = colorBarFill;
            else
                phaseBar[i - 1].color = colorBarEmpty;
        }
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void OnEnterArenaClicked()
    {
        if (currentLevelData == null)
            return;

        GameManager.instance.SaveLevelConfig(currentLevelData);

        if (currentLevelData.chaptersBeforeGame != "") {
            LoadingScene.Instance.LoadStory();
            return;
        }

        bool tuto = currentLevelData.currentlevel == 1;

        LoadingScene.Instance.LoadGame(tuto);
    }

    protected static string FirstLetterToUpper(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0]) + input.Substring(1);
    }
}
