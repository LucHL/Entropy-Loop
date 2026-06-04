using UnityEngine;

public class DrawButton : MonoBehaviour
{
    public DeckManager deckManager;

    public void OnDrawButtonClick()
    {
        if (deckManager != null)
        {
            deckManager.DrawCard();
            //GameLogManager.Instance.AddLog("Test log");
        }
    }
}
