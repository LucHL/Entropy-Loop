using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LogTemplateUI : MonoBehaviour
{
    public TMP_Text text;
    public Image icon;

    public void Setup(GameLogBase log_added)
    {
        text.text = log_added.msg;

        if (log_added.icon != null)
        {
            icon.sprite = log_added.icon;
            icon.gameObject.SetActive(true);
        }
        else
        {
            icon.gameObject.SetActive(false);
        }
    }
}