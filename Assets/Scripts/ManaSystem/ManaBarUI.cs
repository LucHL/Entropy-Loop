using UnityEngine;
using UnityEngine.UI;

public class ManaBarUI : MonoBehaviour
{
    [SerializeField] private Image[] segments;
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private Color inactiveColor = Color.gray;

    public void UpdateMana(int currentMana)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].color = i < currentMana ? activeColor : inactiveColor;
        }
    }
}
