using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    public CardData cardData;  
    public Image cardImage;    
    public Text cardNameText;  

    void Start()
    {
        if (cardData != null)
        {
            UpdateCardUI();
        }
    }

    void UpdateCardUI()
    {
        cardImage.sprite = cardData.cardImage;
        cardNameText.text = cardData.cardName;
    }

    public void SelectCard()
    {

        foreach (CardUI card in Object.FindObjectsByType<CardUI>(FindObjectsSortMode.None))
        {
            card.DeselectCard();
        }

        GetComponent<Image>().color = Color.yellow;  
        GameManager.Instance.SelectedCard = this;

        Debug.Log("Carte sélectionnée : " + cardData.cardName);
    }


    public void DeselectCard()
    {
        GetComponent<Image>().color = Color.white;
    }

}
