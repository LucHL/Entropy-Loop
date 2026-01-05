using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public List<CardData> deck = new List<CardData>();
    public HandSlot[] handSlots;

    void Awake()
    {
        LoadDeckFromFolder();
    }

    // TODO chercher dans chaque dossier, pour permettre le choix du deck. Utiliser le dossier comme nom du deck.
    private void LoadDeckFromFolder()
    {
        deck.Clear();

        CardData[] cards = Resources.LoadAll<CardData>("Card Data/Keronas");

        foreach (CardData card in cards)
        {
            deck.Add(card);
        }
    }

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
