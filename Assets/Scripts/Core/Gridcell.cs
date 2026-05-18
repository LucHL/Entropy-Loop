using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class GridCell : MonoBehaviour
{
    private GameObject spawnedUnit;

    private Renderer cellRenderer;
    private Color originalColor;
    private bool isHighlighted = false;

    void Start()
    {
        cellRenderer = GetComponent<Renderer>();
        if (cellRenderer != null) {
            originalColor = cellRenderer.material.color;
        }
    }

    public void SetHighlight(bool shouldHighlight, Color color)
    {
        if (cellRenderer == null)
            return;

        if (color == Color.white)
            color = originalColor;

        if (shouldHighlight && !isHighlighted) {
            cellRenderer.material.color = color;
            isHighlighted = true;
        }
        else if (!shouldHighlight && isHighlighted) {
            cellRenderer.material.color = originalColor;
            isHighlighted = false;
        }
    }

    private void OnMouseDown()
    {
        if (IsPointerOverBlockingUI())
            return;

        Debug.Log("GridCell OnMouseDown");

        if (GameManager.Instance != null && GameManager.Instance.HasSelectedCard()) {
            Debug.Log("GridCell clicked");


            CardUI card = GameManager.Instance.selectedCard;
            ManaManager mana = GameManager.Instance.manaManager;

            int cost = card.cardData.manaCost;

            if (!mana.HasEnoughMana(cost)) {
                return;
            }

            SpawnUnit();
            mana.SpendMana(cost);

            HandSlot slot = card.GetComponent<HandSlot>();
            Debug.Log(slot == null ? "HandSlot NULL" : "HandSlot OK");

            card.GetComponent<HandSlot>().ClearSlot();
            GameManager.Instance.DeselectCard();
        } else {
            FloatingTextManager.instance.Show("No card selected");
        }
    }

    private bool IsPointerOverBlockingUI()
    {
        if (GameLoopManager.instance.isGameRunning)
            return true;

        PointerEventData eventData = new(EventSystem.current) {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.CompareTag("BlockClick"))
                return true;
        }

        return false;
    }

    private void SpawnUnit()
    {
        if (spawnedUnit == null)
        {
            GameObject unitPrefab = GameManager.Instance.GetSelectedUnitPrefab();
            if (unitPrefab != null) {
                //spawnedUnit = Instantiate(unitPrefab, position, Quaternion.identity);
                unitPrefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                // unitPrefab.GetComponent<NavMeshAgent>().enabled = false;

                spawnedUnit = Instantiate(unitPrefab, Vector3.Scale(transform.position, new(1f, 1f, 1f)), Quaternion.identity);
                // spawnedUnit.GetComponentInChildren<Units>().enabled = false;

                GameLoopManager.instance.RegisterUnit(spawnedUnit, true);

                BugTracker.Info("Unit '"+unitPrefab.name+"' spawned.");
            } else {
                BugTracker.Error("Missing model (Gidcell.cs/SpawnUnit).");
            }
        }
    }
}