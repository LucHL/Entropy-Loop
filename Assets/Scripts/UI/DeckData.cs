using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDeck", menuName = "Deck")]
public class DeckData : ScriptableObject
{
    public string deckName;
    public CardData commander;
    public List<CardData> cards;
    public int unlockLevel = 0;

    // Textes affichés sur la page de droite du livre de sélection
    [Header("Grimoire")]
    [Tooltip("Décocher pour cacher le deck du livre de sélection (deck incomplet, en cours de création...)")]
    public bool showInSelection = true;
    [Tooltip("Nom affiché dans le livre. Vide : deckName est utilisé.")]
    public string displayName;
    public string category;
    public string grimoireTitle;
    [TextArea(3, 8)] public string lore;
    public string deckType;
    [TextArea(3, 8)] public string mechanics;
}
