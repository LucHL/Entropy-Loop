using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public List<CardData> deck = new();
    public HandSlot[] handSlots;

    public void DrawCard()
    {
        if (deck.Count > 0) {
            CardData drawnCard = deck[Random.Range(0, deck.Count)];
            deck.Remove(drawnCard);

            foreach (HandSlot slot in handSlots) {
                if (slot.IsEmpty()) {
                    slot.SetCard(drawnCard);
                    return;
                }
            }

            FloatingTextManager.instance.Show("Hand is full");
        } else {
            Debug.Log("Plus de cartes dans le deck !");
        }
    }
}
