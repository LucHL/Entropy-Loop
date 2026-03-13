using System.ComponentModel;
using UnityEngine;

public class testMSG : MonoBehaviour
{
    public MSGManaManager msg;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("Appui sur M - Affichage du message de mana insuffisant");
            msg.ShowNoMana();
        }
    }
}
