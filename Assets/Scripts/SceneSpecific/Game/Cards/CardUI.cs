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
        if (GameManager.Instance != null && cardData != null)
        {
            ManaManager mana = GameManager.Instance.manaManager;
            if (!mana.HasEnoughMana(cardData.manaCost))
            {
                FloatingTextManager.instance.Show("Not enough mana");
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