using UnityEngine;

public class HandSlot : MonoBehaviour
{
    private CardUI cardUI;

    private void Awake()
    {
        // Récupère automatiquement le CardUI attaché au slot
        cardUI = GetComponent<CardUI>();

        if (cardUI == null)
        {
            Debug.LogError("CardUI manquant sur le slot : " + gameObject.name);
        }

        ClearSlot();  // On démarre avec un slot vide
    }

    public bool IsEmpty()
    {
        return cardUI.cardData == null;  // Vérifie si la carte est vide
    }

    public void SetCard(CardData newCard)
    {
        if (cardUI != null && newCard != null)
        {
            cardUI.cardData = newCard;
            cardUI.UpdateCardUI();
            gameObject.SetActive(true);  // Rend le slot visible quand une carte arrive
        }
    }

    public void ClearSlot()
    {
        if (cardUI != null)
        {
            cardUI.cardData = null;
            cardUI.UpdateCardUI();
            gameObject.SetActive(false);  // Cache le slot s'il est vide
        }
    }
}
