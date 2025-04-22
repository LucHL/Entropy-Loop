using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public List<CardData> deck = new List<CardData>();  // Le deck de cartes
    public HandSlot[] handSlots;  // Les emplacements dans la main

    public void DrawCard()
    {
        if (deck.Count > 0)
        {
            CardData drawnCard = deck[Random.Range(0, deck.Count)];
            deck.Remove(drawnCard);

            // Trouve un slot libre dans la main
            foreach (HandSlot slot in handSlots)
            {
                if (slot.IsEmpty())
                {
                    slot.SetCard(drawnCard);
                    return;
                }
            }

            Debug.Log("La main est pleine !");
        }
        else
        {
            Debug.Log("Plus de cartes dans le deck !");
        }
    }
}
