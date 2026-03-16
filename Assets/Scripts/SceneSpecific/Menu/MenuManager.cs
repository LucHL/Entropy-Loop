using TMPro;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public TextMeshProUGUI version;

    void Awake()
    {
        version.text = Application.version;
    }
}
