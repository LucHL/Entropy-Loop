using TMPro;
using UnityEngine;

public class NumberOfCardRemaining : MonoBehaviour
{
    public static NumberOfCardRemaining instance;

    private TextMeshProUGUI text;
    public int _remaining = 0;

    void Awake()
    {
        instance = this;
        text = GetComponent<TextMeshProUGUI>();
    }

    public void UpdateNumber(int remaining)
    {
        text.text = remaining.ToString();
        _remaining = remaining;
    }
}
