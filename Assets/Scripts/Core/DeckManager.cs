using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    public List<CardData> deck;
    public HandSlot[] handSlots;

    private int tutorialDeck = 0;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        SetDeck();
    }

    public void SetDeck()
    {
        if (GameModeManager.selectedDeck == null)
            return;

        deck = new(GameModeManager.selectedDeck.cards);
        UpdateNbrCardInDeck();
    }

    public void DrawCard()
    {
        if (GameModeManager.isTutorial) {
            if (CheckIfHandIsFull())
                return;

            CardData drawnCard = deck[tutorialDeck];
            tutorialDeck++;

            foreach (HandSlot slot in handSlots) {
                if (slot.IsEmpty()) {
                    slot.SetCard(drawnCard);
                    UpdateNbrCardInDeck();
                    return;
                }
            }

            UpdateNbrCardInDeck();
            return;
        }

        if (deck.Count > 0) {
            if (CheckIfHandIsFull())
                return;
            
            CardData drawnCard = deck[Random.Range(0, deck.Count)];
            deck.Remove(drawnCard);

            foreach (HandSlot slot in handSlots) {
                if (slot.IsEmpty()) {
                    slot.SetCard(drawnCard);
                    UpdateNbrCardInDeck();
                    return;
                }
            }

            UpdateNbrCardInDeck();
        } else {
            FloatingTextManager.instance.Show("Deck is empty");
            BugTracker.Info("Deck is empty.");
        }
    }

    void UpdateNbrCardInDeck()
    {
        GameModeManager.nbrCardInDeck = deck.Count + handSlots.Count();
        NumberOfCardRemaining.instance.UpdateNumber(deck.Count);
    }

    bool CheckIfHandIsFull() {
        if (handSlots.All(slot => !slot.IsEmpty())) {
            FloatingTextManager.instance.Show("Hand is full");
            return true;
        }
        return false;
    }
}
