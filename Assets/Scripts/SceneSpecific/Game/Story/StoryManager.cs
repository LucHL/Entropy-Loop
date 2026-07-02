using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Purchasing;

public class StoryManager : MonoBehaviour
{
    [SerializeField] Image backgroundImage;
    [SerializeField] Image charLeftImage;
    [SerializeField] Image charRightImage;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI dialogueText;
    [SerializeField] GameObject popupEndStory;
    [SerializeField] TextMeshProUGUI chapterName;

    private LevelData currentLevel;
    private DialogueLine[] dialogueLines;
    private string currentChapter;
    private int currentLineIndex = 0;

    void Awake()
    {
        popupEndStory.SetActive(false);
    }

    void Start()
    {
        LoadLevelInformation();
    }

    void Update()
    {
        // si clic, afficher le text en entier ou passer au suivant
    }

    public void LoadLevelInformation()
    {
        currentLevel = GameManager.instance.currentLevelData;
        currentChapter = "Story/" + GameManager.instance.nextStory;
        Debug.Log(currentChapter);

        TextAsset jsonFile = Resources.Load<TextAsset>(currentChapter);

        if (jsonFile != null) {
            DialoguesWrapper wrapper = JsonUtility.FromJson<DialoguesWrapper>(jsonFile.text);
            dialogueLines = wrapper.dialogues;
        } else {
            BugTracker.Critical("[StoryManager] Failed to parse Json for the Story.");
            return;
        }

        currentLineIndex = 0;
        DisplayLine();
    }

    public void OnNextClicked()
    {
        currentLineIndex++;
        if (currentLineIndex < dialogueLines.Length)
            DisplayLine();
        else
            EndStory();
    }

    public void OnSkipClicked()
    {
        EndStory();
    }

    private void DisplayLine()
    {
        if (dialogueLines == null || currentLineIndex >= dialogueLines.Length) {
            EndStory();
            return;
        }

        DialogueLine currentDialogue = dialogueLines[currentLineIndex];

        dialogueText.text = currentDialogue.text;
        nameText.text = currentDialogue.speakerName;

        UpdateCharacterSprite(currentDialogue.backgroundName, backgroundImage);
        UpdateCharacterSprite(currentDialogue.charLeftImg, charLeftImage);
        UpdateCharacterSprite(currentDialogue.charRightImg, charRightImage);
    }

    void UpdateCharacterSprite(string spriteName, Image targetImage)
    {
        if (!string.IsNullOrEmpty(spriteName)) {
            Sprite loadedSprite = Resources.Load<Sprite>("Sprites/" + spriteName);
            if (loadedSprite != null) {
                targetImage.gameObject.SetActive(true);
                targetImage.sprite = loadedSprite;
            }
        } else {
            targetImage.gameObject.SetActive(false);
        }
    }

    private void EndStory()
    {
        BugTracker.Info("End of story for level '" + currentLevel.currentlevel + "'.");

        if (GameManager.instance.nextStory == currentLevel.chaptersAfterGame) {
            string[] name = currentLevel.chaptersAfterGame.Split("t"); // "Chapter1/chpt1-1" => "1-1"
            chapterName.text = "Chapter " + name[2];
            popupEndStory.SetActive(true);
            return;
        }

        backgroundImage.GetComponentInParent<Canvas>().gameObject.SetActive(false);
        LoadingScene.Instance.LoadGame();
    }
}
