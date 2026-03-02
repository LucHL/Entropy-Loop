using UnityEngine;
using TMPro;

public class ManaManager : MonoBehaviour
{
    public int currentMana = 2;
    public int maxMana = 10;

    [Header("UI")]
    //public TextMeshProUGUI manaText;
    public ManaBarUI manaBarUI;

    private void Start()
    {
        UpdateUI();
    }

    public bool HasEnoughMana(int amount)
    {
        return currentMana >= amount;
    }

    public void AddMana()
    {
        currentMana = Mathf.Clamp(currentMana + 1, 0, maxMana);
        UpdateUI();
    }

    public void RemoveMana()
    {
        currentMana = Mathf.Clamp(currentMana - 1, 0, maxMana);
        UpdateUI();
    }

    public void SpendMana(int amount)
    {
        currentMana -= amount;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        UpdateUI();
    }

    private void UpdateUI()
    {
        //if (manaText != null)
        //    manaText.text = currentMana.ToString();
        if (manaBarUI != null)
        manaBarUI.UpdateMana(currentMana);
    }
}
