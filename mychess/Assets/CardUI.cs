using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    public CardData cardData;  
    public Image cardImage;
    public TextMeshProUGUI cardNameText;

    private void Start()
    {
        if (cardData != null)
        {
            UpdateCardUI();
        }
    }

    private void UpdateCardUI()
    {
        cardImage.sprite = cardData.cardImage;
        cardNameText.text = cardData.cardName;
    }

    public void SelectCard()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSelectedCard(this);
            GetComponent<Image>().color = Color.yellow;
        }
    }

    public void DeselectCard()
    {
        GetComponent<Image>().color = Color.white;
    }
}
