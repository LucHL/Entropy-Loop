using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Interrupteur on/off : la piste change de sprite et le bouton rond glisse d'un côté à l'autre
[RequireComponent(typeof(Toggle))]
public class UISwitch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image track;
    [SerializeField] Image knob;
    [SerializeField] float knobTravel = 18f;
    [SerializeField] float slideSpeed = 14f;

    [Header("Piste")]
    [SerializeField] Sprite trackOff;
    [SerializeField] Sprite trackOffHover;
    [SerializeField] Sprite trackOffDisabled;
    [SerializeField] Sprite trackOn;
    [SerializeField] Sprite trackOnHover;
    [SerializeField] Sprite trackOnDisabled;

    [Header("Bouton rond")]
    [SerializeField] Sprite knobOff;
    [SerializeField] Sprite knobOn;
    [SerializeField] Sprite knobDisabled;

    Toggle toggle;
    bool hovered;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(_ => Refresh());
        Refresh();
        SnapKnob();
    }

    void OnEnable()
    {
        hovered = false;
        if (toggle != null) {
            Refresh();
            SnapKnob();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        Refresh();
    }

    void Update()
    {
        // l'état activé/désactivé peut changer de l'extérieur
        Refresh();
        if (knob != null) {
            Vector2 target = KnobPosition();
            RectTransform rect = knob.rectTransform;
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, target, Time.unscaledDeltaTime * slideSpeed);
        }
    }

    Vector2 KnobPosition()
    {
        return new Vector2(toggle.isOn ? knobTravel : -knobTravel, 0);
    }

    void SnapKnob()
    {
        if (knob != null)
            knob.rectTransform.anchoredPosition = KnobPosition();
    }

    void Refresh()
    {
        bool on = toggle.isOn;
        bool active = toggle.IsInteractable();

        if (track != null)
            track.sprite = !active ? (on ? trackOnDisabled : trackOffDisabled)
                : hovered ? (on ? trackOnHover : trackOffHover)
                : (on ? trackOn : trackOff);

        if (knob != null)
            knob.sprite = !active ? knobDisabled : on ? knobOn : knobOff;
    }
}
