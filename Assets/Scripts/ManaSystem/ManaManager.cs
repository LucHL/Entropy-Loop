using UnityEngine;

public class ManaManager : MonoBehaviour
{
    public int currentMana = 3;
    public int maxMana = 10;

    [Header("UI")]
    //public TextMeshProUGUI manaText;
    public ManaBarUI manaBarUI;
    public static ManaManager instance;

    private void Start()
    {
        instance = this;
        UpdateUI();
    }

    public bool HasEnoughMana(int amount)
    {
        return currentMana >= amount;
    }

    public void AddMana(int nbrToAdd = 1)
    {
        currentMana = Mathf.Clamp(currentMana + nbrToAdd, 0, maxMana);
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
