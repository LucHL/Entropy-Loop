using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialClickCatcher : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject targetButton;
    [SerializeField] private bool isLeftClick = true;
    [SerializeField] private bool isRightClick = true;

    private int nbr = 0;

    public void Onclick(int nbrOfClick)
    {
        nbr = nbrOfClick;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if ((!isLeftClick && eventData.button == PointerEventData.InputButton.Left)
            || (!isRightClick && eventData.button == PointerEventData.InputButton.Right))
            return;

        if (eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Right) {
            TutorialManager.instance.ButtonNextAfterANumberOfClick(nbr);

            ExecuteEvents.Execute(targetButton, eventData, ExecuteEvents.pointerClickHandler);
        }
    }
}
