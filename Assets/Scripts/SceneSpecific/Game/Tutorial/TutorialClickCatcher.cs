using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialClickCatcher : MonoBehaviour, IPointerClickHandler
{
    public GameObject targetButton;

    private int nbr = 0;

    public void Onclick(int nbrOfClick)
    {
        nbr = nbrOfClick;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Right) {
            TutorialManager.instance.ButtonNextAfterANumberOfClick(nbr);

            ExecuteEvents.Execute(targetButton, eventData, ExecuteEvents.pointerClickHandler);
        }
    }
}
