using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDeck", menuName = "Deck")]
public class DeckData : ScriptableObject
{
    public string deckName;
    public CardData commander;
    public List<CardData> cards;
}