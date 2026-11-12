using UnityEngine;

// Affiche un objet du tuto (ex. le bouton Next de l'étape de pioche) seulement quand la main est pleine
public class TutorialShowWhenHandFull : MonoBehaviour
{
    [SerializeField] GameObject target;

    void Update()
    {
        if (target == null || DeckManager.instance == null)
            return;

        bool full = DeckManager.instance.IsHandFull;
        if (target.activeSelf != full)
            target.SetActive(full);
    }
}
