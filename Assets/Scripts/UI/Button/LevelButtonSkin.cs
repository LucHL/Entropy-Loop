using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Apparence du bouton de niveau : disponible, terminé ou verrouillé, et mis en avant (niveau en cours)
[RequireComponent(typeof(Button))]
public class LevelButtonSkin : MonoBehaviour
{
    public enum LevelState { Available, Completed, Locked }

    [SerializeField] LevelState state = LevelState.Available;
    [Tooltip("Niveau en cours : affiché avec le cadre mis en avant en permanence.")]
    [SerializeField] bool isCurrent = false;

    [Header("Textes")]
    public TextMeshProUGUI numberText;
    public TextMeshProUGUI titleText;

    [Header("Sprites")]
    [SerializeField] Sprite normal;
    [SerializeField] Sprite normalSelected;
    [SerializeField] Sprite completed;
    [SerializeField] Sprite completedSelected;
    [SerializeField] Sprite locked;

    [Header("Couleurs du texte")]
    [SerializeField] Color textColor = new Color32(241, 232, 213, 255);
    [SerializeField] Color lockedTextColor = new Color32(140, 128, 150, 255);

    public LevelState State => state;

    void Awake()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    public void SetState(LevelState newState, bool current = false)
    {
        state = newState;
        isCurrent = current;
        Apply();
    }

    public void SetTexts(string number, string title)
    {
        if (numberText != null)
            numberText.text = number;
        if (titleText != null)
            titleText.text = title;
    }

    void Apply()
    {
        Button button = GetComponent<Button>();
        Image image = button.targetGraphic as Image;
        if (image == null)
            return;

        Sprite baseSprite = state switch {
            LevelState.Completed => isCurrent ? completedSelected : completed,
            LevelState.Locked => locked,
            _ => isCurrent ? normalSelected : normal,
        };
        Sprite highlight = state == LevelState.Completed ? completedSelected : normalSelected;

        image.sprite = baseSprite;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState {
            highlightedSprite = highlight,
            pressedSprite = highlight,
            selectedSprite = baseSprite,
            disabledSprite = locked,
        };
        button.interactable = state != LevelState.Locked;

        Color color = state == LevelState.Locked ? lockedTextColor : textColor;
        if (numberText != null)
            numberText.color = color;
        if (titleText != null)
            titleText.color = color;
    }
}
