using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager instance;
    public new GameObject gameObject;
    
    void Awake()
    {
        instance = this;
    }

    public void Show(string message)
    {
        GameObject popup = Instantiate(gameObject);
        popup.GetComponentInChildren<FloatingText>().Init(message);
    }
}
