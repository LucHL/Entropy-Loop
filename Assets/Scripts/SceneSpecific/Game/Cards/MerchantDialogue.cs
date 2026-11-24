using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Affiche les répliques du marchand (chargées depuis Resources/Shop/*.json)
public class MerchantDialogue : MonoBehaviour, IPointerClickHandler
{
    public enum Mood { Greeting, Purchase, CantAfford }

    [Header("UI")]
    [SerializeField] Image portraitImage;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI dialogueText;

    [Header("Data")]
    [SerializeField] string dialogueFile = "Shop/merchant";
    [SerializeField] float typingSpeed = 0.025f;

    private MerchantDialogueData data;
    private Coroutine typingCoroutine;
    private string lastLine = "";

    void Awake()
    {
        LoadDialogues();
    }

    void LoadDialogues()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(dialogueFile);
        if (jsonFile == null) {
            BugTracker.Error($"[MerchantDialogue] Fichier de dialogue '{dialogueFile}' introuvable.");
            return;
        }

        data = JsonUtility.FromJson<MerchantDialogueData>(jsonFile.text);

        if (nameText != null)
            nameText.text = data.merchantName;

        if (titleText != null)
            titleText.text = data.merchantTitle;

        if (portraitImage != null && !string.IsNullOrEmpty(data.portrait)) {
            Sprite sprite = LoadPortrait("Sprites/Story/" + data.portrait);
            if (sprite != null) {
                portraitImage.sprite = sprite;
                // garde les proportions de l'image si le portrait remplit son cadre
                if (portraitImage.TryGetComponent(out AspectRatioFitter fitter))
                    fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            } else
                BugTracker.Warning($"[MerchantDialogue] Portrait '{data.portrait}' introuvable.");
        }
    }

    // Fonctionne aussi si l'image n'est pas importée en "Sprite (2D and UI)"
    Sprite LoadPortrait(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
            return sprite;

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
            return null;

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    public void Say(Mood mood)
    {
        string[] lines = GetLines(mood);
        if (lines == null || lines.Length == 0)
            return;

        string line = lines[Random.Range(0, lines.Length)];
        if (lines.Length > 1 && line == lastLine)
            line = lines[(System.Array.IndexOf(lines, line) + 1) % lines.Length];
        lastLine = line;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (isActiveAndEnabled)
            typingCoroutine = StartCoroutine(TypingText("« " + line + " »"));
        else
            dialogueText.text = "« " + line + " »";
    }

    string[] GetLines(Mood mood)
    {
        if (data == null)
            return null;

        return mood switch {
            Mood.Purchase => data.purchase,
            Mood.CantAfford => data.cantAfford,
            _ => data.greetings,
        };
    }

    // Clic sur le texte : affiche la réplique en entier
    public void OnPointerClick(PointerEventData eventData)
    {
        if (typingCoroutine == null)
            return;

        StopCoroutine(typingCoroutine);
        typingCoroutine = null;
        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
    }

    private IEnumerator TypingText(string fullText)
    {
        dialogueText.text = fullText;
        dialogueText.ForceMeshUpdate();

        int totalCharacters = dialogueText.textInfo.characterCount;
        dialogueText.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalCharacters; i++) {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        typingCoroutine = null;
    }
}
