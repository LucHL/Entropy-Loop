using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AnimeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Button button;
    private Image image;

    [Tooltip("sprite[0] is the default sprite.")]
    public Sprite[] sprite;

    void Start()
    {
        image = GetComponent<Image>();
        image.sprite = sprite[0];
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        image.sprite = sprite[2];
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.sprite = sprite[1];
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.sprite = sprite[0];
    }
}
