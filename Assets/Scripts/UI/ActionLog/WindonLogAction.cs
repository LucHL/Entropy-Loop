using UnityEngine;

public class ActionLogWindow : MonoBehaviour
{
    public GameObject actionLogPanel;

    public void OpenWindow()
    {
        actionLogPanel.SetActive(true);
    }

    public void CloseWindow()
    {
        actionLogPanel.SetActive(false);
    }
}