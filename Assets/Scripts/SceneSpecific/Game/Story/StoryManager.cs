using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryManager : MonoBehaviour
{
    [SerializeField] Image backgroundImage;
    [SerializeField] Image charLeftImage;
    [SerializeField] Image charRightImage;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI dialogueText;

    private LevelData currentStory;
    private int currentLineIndex = 0;

    public void LoadStory(LevelData story)
    {
        currentStory = story;
        currentLineIndex = 0;
        DisplayLine();
    }

    public void OnScreenClicked()
    {
        // currentLineIndex++;
        // if (currentLineIndex < currentStory.lines.Length) {
        //     DisplayLine();
        // }
        // else {
        //     EndStory();
        // }
    }

    public void OnSkipClicked()
    {
        EndStory();
    }

    private void DisplayLine()
    {
        // DialogueLine line = currentStory.lines[currentLineIndex];

        // dialogueText.text = line.text;
        // nameText.text = line.speakerName;
        // backgroundImage.sprite = line.background;

        // if (line.characterLeft != null) {
        //     charLeftImage.gameObject.SetActive(true);
        //     charLeftImage.sprite = line.characterLeft;
        // } else
        //     charLeftImage.gameObject.SetActive(false);

        // if (line.characterRight != null) {
        //     charRightImage.gameObject.SetActive(true);
        //     charRightImage.sprite = line.characterRight;
        // } else
        //     charRightImage.gameObject.SetActive(false);
    }

    private void EndStory()
    {
        // Scene GAME avec le bon niveau
    }
}
