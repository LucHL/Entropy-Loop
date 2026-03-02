using UnityEngine;

public class MSGManaManager : MonoBehaviour
{
    [SerializeField] private GameObject popupNoMana;
    [SerializeField] private float duration = 1.2f;

    private float timer;
    private bool isShowing;

    void Update()
    {
        if (!isShowing) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            popupNoMana.SetActive(false);
            isShowing = false;
        }
    }

    public void ShowNoMana()
    {
        Debug.Log("ShowNoMana appelé");

        popupNoMana.SetActive(true);
        timer = duration;
        isShowing = true;
    }
}