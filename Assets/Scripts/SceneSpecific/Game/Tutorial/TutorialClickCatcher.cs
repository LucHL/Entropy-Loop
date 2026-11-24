using UnityEngine;
using UnityEngine.EventSystems;

// Zone invisible posée sur un bouton du jeu : le clic fait avancer le tuto puis est transmis au bouton
public class TutorialClickCatcher : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject targetButton;
    [SerializeField] private bool isLeftClick = true;
    [SerializeField] private bool isRightClick = true;
    [Tooltip("Se recale à chaque image sur le bouton ciblé (il peut être déplacé par l'ATH ou une animation).")]
    [SerializeField] private bool followTarget = true;
    [Tooltip("Décoché : le clic est seulement transmis au bouton, l'étape avance autrement (ex. bouton Next).")]
    [SerializeField] private bool advanceOnClick = true;

    private int nbr = 0;

    public void Onclick(int nbrOfClick)
    {
        nbr = nbrOfClick;
    }

    void LateUpdate()
    {
        if (!followTarget || targetButton == null)
            return;
        if (transform is not RectTransform rect || targetButton.transform is not RectTransform control)
            return;

        Vector3[] corners = new Vector3[4];
        control.GetWorldCorners(corners);
        Vector3 min = rect.parent.InverseTransformPoint(corners[0]);
        Vector3 max = rect.parent.InverseTransformPoint(corners[2]);

        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.localPosition = (min + max) * 0.5f;
        rect.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if ((!isLeftClick && eventData.button == PointerEventData.InputButton.Left)
            || (!isRightClick && eventData.button == PointerEventData.InputButton.Right))
            return;

        if (eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Right) {
            if (advanceOnClick)
                TutorialManager.instance.ButtonNextAfterANumberOfClick(nbr);

            ExecuteEvents.Execute(targetButton, eventData, ExecuteEvents.pointerClickHandler);
        }
    }
}
