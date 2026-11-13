using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Piste et remplissage du slider : sprites de survol et désactivé (le bouton rond utilise le SpriteSwap du Slider)
[RequireComponent(typeof(Slider))]
public class UISliderSkin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image track;
    [SerializeField] Image fill;

    [SerializeField] Sprite trackNormal;
    [SerializeField] Sprite trackHover;
    [SerializeField] Sprite trackDisabled;
    [SerializeField] Sprite fillNormal;
    [SerializeField] Sprite fillHover;
    [SerializeField] Sprite fillDisabled;

    Slider slider;
    bool hovered;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void OnEnable()
    {
        hovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }

    void Update()
    {
        bool active = slider.IsInteractable();
        if (track != null)
            track.sprite = !active ? trackDisabled : hovered ? trackHover : trackNormal;
        if (fill != null)
            fill.sprite = !active ? fillDisabled : hovered ? fillHover : fillNormal;
    }
}
