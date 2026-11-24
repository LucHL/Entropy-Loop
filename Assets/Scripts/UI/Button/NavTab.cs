using UnityEngine;
using UnityEngine.UI;

// Onglet de navigation : au clic, il s'affiche "sélectionné" et désélectionne les onglets frères.
// Les actions du bouton (afficher la bonne section) restent dans son onClick.
[RequireComponent(typeof(Button))]
public class NavTab : MonoBehaviour
{
    [SerializeField] GameObject selectedOverlay;
    [Tooltip("Onglet affiché comme sélectionné à l'ouverture.")]
    [SerializeField] bool selectedOnStart = false;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Select);
        SetSelected(selectedOnStart);
    }

    public void Select()
    {
        if (transform.parent == null) {
            SetSelected(true);
            return;
        }
        foreach (Transform sibling in transform.parent)
            if (sibling.TryGetComponent(out NavTab tab))
                tab.SetSelected(tab == this);
    }

    public void SetSelected(bool selected)
    {
        if (selectedOverlay != null)
            selectedOverlay.SetActive(selected);
    }
}
