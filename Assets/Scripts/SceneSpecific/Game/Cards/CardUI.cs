using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image cardSelected;
    public CardData cardData;  
    public Image cardImage;

    private void Start()
    {
        if (cardData != null) {
            UpdateCardUI();
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && cardSelected != null && cardSelected.isActiveAndEnabled && !GameModeManager.isTutorial) {
            cardSelected.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) {
            if (cardSelected != null) {
                cardSelected.gameObject.SetActive(true);
                cardSelected.sprite = cardData.cardImage;
            } else
                BugTracker.Error("'CardUI' cardSelected is null.");
        }
    }

    public void UpdateCardUI()
    {
        cardImage.sprite = cardData != null ? cardData.cardImage : null;
    }

    public void SelectCard()
    {
        if (GameManager.Instance != null && cardData != null) {
            ManaManager mana = GameManager.Instance.manaManager;
            if (!mana.HasEnoughMana(cardData.manaCost)) {
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
