using System;
using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager instance;

    public GameObject canvasDamagePopup;

    void Awake()
    {
        instance = this;
    }

    public void Init(Transform position, float dmg)
    {
        GameObject dmgPopup = Instantiate(canvasDamagePopup, position.position + Vector3.up * 1.5f, Quaternion.identity);
        dmgPopup.GetComponent<DamagePopup>().Setup(dmg);
    }
}
