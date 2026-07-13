using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Purchasing;
using System.Collections;

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
    private float typingSpeed = 0.03f;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

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
        if (Input.GetMouseButtonDown(0))
            OnNextClicked();
    }

    public void LoadLevelInformation()
    {
        currentLevel = GameManager.instance.currentLevelData;
        currentChapter = "Story/" + GameManager.instance.nextStory;

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
        if (isTyping) {
            CompleteCurrentLine();
            return;
        }

        currentLineIndex++;
        if (currentLineIndex < dialogueLines.Length)
            DisplayLine();
        else
            EndStory();
    }

    public void OnSkipClicked()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        EndStory();
    }

    private void DisplayLine()
    {
        if (dialogueLines == null || currentLineIndex >= dialogueLines.Length) {
            EndStory();
            return;
        }

        DialogueLine currentDialogue = dialogueLines[currentLineIndex];

        nameText.text = currentDialogue.speakerName;

        UpdateCharacterSprite(currentDialogue.backgroundName, backgroundImage);
        UpdateCharacterSprite(currentDialogue.charLeftImg, charLeftImage);
        UpdateCharacterSprite(currentDialogue.charRightImg, charRightImage);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypingText(currentDialogue.text));
    }

    void UpdateCharacterSprite(string spriteName, Image targetImage)
    {
        if (!string.IsNullOrEmpty(spriteName)) {
            Sprite loadedSprite = Resources.Load<Sprite>("Sprites/Story/" + spriteName);
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
        BugTracker.Info("[StoryManager] End of story for level " + currentLevel.currentlevel + ".");

        if (GameManager.instance.nextStory == currentLevel.chaptersAfterGame) {
            string[] name = currentLevel.chaptersAfterGame.Split("t"); // "Chapter1/chpt1-1" => "1-1"
            name = name[2].Split("a"); // chpt1-1aft => 1-1aft => 1-1
            chapterName.text = "Chapter " + name[0];
            popupEndStory.SetActive(true);
            return;
        }

        backgroundImage.GetComponentInParent<Canvas>().gameObject.SetActive(false);
        LoadingScene.Instance.LoadGame();
    }

    private IEnumerator TypingText(string fullText)
    {
        isTyping = true;
        dialogueText.text = fullText;
        
        dialogueText.ForceMeshUpdate();

        int totalCharacters = dialogueText.textInfo.characterCount;
        dialogueText.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalCharacters; i++) {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void CompleteCurrentLine()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
        isTyping = false;
    }
}
