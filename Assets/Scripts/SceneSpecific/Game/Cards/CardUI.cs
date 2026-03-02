using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    public CardData cardData;  
    public Image cardImage;

    private void Start()
    {
        if (cardData != null)
        {
            UpdateCardUI();
        }
    }

    public void UpdateCardUI()
    {
        cardImage.sprite = cardData != null ? cardData.cardImage : null;
    }

    public void SelectCard()
    {
        Debug.Log("SelectCard appelé");

        if (GameManager.Instance != null && cardData != null)
        {
            ManaManager mana = GameManager.Instance.manaManager;
            if (!mana.HasEnoughMana(cardData.manaCost))
            {
                Debug.Log("Pas assez de mana pour jouer : " + cardData.cardName);
                return;
            }
            GameManager.Instance.SetSelectedCard(this);
            GetComponent<Image>().color = Color.yellow;
        }
    }

    public void DeselectCard()
    {
        GetComponent<Image>().color = Color.white;
    }
}