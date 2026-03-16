using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public GameObject shopPopup;
    public GameObject backgroundPopup;

    public int playerGold = 100;
    public TextMeshProUGUI moneyText;
    public Button closeButton;

    void Start()
    {
        shopPopup.SetActive(false);
        backgroundPopup.SetActive(false);

        UpdateMoneyUI();
        closeButton.onClick.AddListener(ToggleShop);
    }

    public void ToggleShop()
    {
        bool isActive = shopPopup.activeSelf;
        shopPopup.SetActive(!isActive);
        backgroundPopup.SetActive(!isActive);
    }

    public void SpendGold(int amount)
    {
        playerGold -= amount;
        UpdateMoneyUI();
    }

    void UpdateMoneyUI()
    {
        moneyText.text = playerGold.ToString();
    }

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.S))
        // {
        //     ToggleShop();
        // }
    }
}
