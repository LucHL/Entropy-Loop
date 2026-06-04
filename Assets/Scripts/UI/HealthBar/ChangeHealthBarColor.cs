using UnityEngine;
using UnityEngine.UI;

public class ChangeHealthBarColor : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] Image fill;

    public void ChangeColor(UnitsTeam team)
    {
        if (team == UnitsTeam.Player) {
            background.color = new Color32(47, 0, 80, 255);
            fill.color = new Color32(135, 0, 255, 255);
        } else {
            background.color = new Color32(80, 0, 0, 255);
            fill.color = new Color32(215, 0, 0, 255);
        }
    }
}
