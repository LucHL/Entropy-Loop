using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class StoryManager : MonoBehaviour
{
    [SerializeField] Image backgroundImage;
    [SerializeField] Image charLeftImage;
    [SerializeField] Image charRightImage;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI dialogueText;

    private LevelData currentLevel;
    private DialogueLine[] dialogueLines;
    private string currentChapter;
    private int currentLineIndex = 0;

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
        currentLevel = GameManager.Instance.currentLevelData;
        currentChapter = "Story/" + currentLevel.chaptersBeforeGame;

        TextAsset jsonFile = Resources.Load<TextAsset>(currentChapter);

        if (jsonFile != null) {
            DialoguesWrapper wrapper = JsonUtility.FromJson<DialoguesWrapper>(jsonFile.text);
            dialogueLines = wrapper.dialogues;
            Debug.Log(dialogueLines[0].speakerName);
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
        // gameManager pour switch vers la game avec la ne currentlevel
    }
}
