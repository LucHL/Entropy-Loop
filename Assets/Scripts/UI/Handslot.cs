using UnityEngine;

public class HandSlot : MonoBehaviour
{
    private CardUI cardUI;

    private void Awake()
    {
        cardUI = GetComponent<CardUI>();

        if (cardUI == null) {
            BugTracker.Error("CardUI missing '" + gameObject.name + "'.");
        }

        ClearSlot();
    }

    public bool IsEmpty()
    {
        return cardUI.cardData == null;
    }

    public void SetCard(CardData newCard)
    {
        if (cardUI != null && newCard != null) {
            cardUI.cardData = newCard;
            cardUI.UpdateCardUI();
            gameObject.SetActive(true);
        }
    }

    public void ClearSlot()
    {
        if (cardUI != null) {
            cardUI.cardData = null;
            cardUI.UpdateCardUI();
            gameObject.SetActive(false);
        }
    }
}
