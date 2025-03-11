using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public CardUI SelectedCard;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
