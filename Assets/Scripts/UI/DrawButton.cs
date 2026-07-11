using UnityEngine;

public class DrawButton : MonoBehaviour
{
    public void OnDrawButtonClick()
    {
        DeckManager.instance.DrawCard();
    }
}
