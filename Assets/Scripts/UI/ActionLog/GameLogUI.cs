using UnityEngine;

public class GameLogUI : MonoBehaviour
{
    public Transform log_parent;
    public GameObject log_template;

    void Start()
    {
        GameLogManager.Instance.Add_to_log += AddLog_to_UI;
    }

    void AddLog_to_UI(GameLogBase log_added)
    {
        GameObject obj = Instantiate(log_template, log_parent);

        LogTemplateUI item = obj.GetComponent<LogTemplateUI>();
        item.Setup(log_added);

        obj.transform.SetAsFirstSibling();
    }
}