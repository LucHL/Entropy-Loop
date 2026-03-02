using UnityEngine;
using UnityEngine.AI;

public class GridCell : MonoBehaviour
{
    private GameObject spawnedUnit;

    void Start()
    {
        GameObject[] entities = GameObject.FindGameObjectsWithTag("Entities");
        foreach (GameObject e in entities) {
            e.GetComponentInChildren<Units>().enabled = false;
        }
    }

    private void OnMouseDown()
    {
        Debug.Log("GridCell OnMouseDown");

        if (GameManager.Instance != null && GameManager.Instance.HasSelectedCard())
        {
            Debug.Log("GridCell clicked");


            CardUI card = GameManager.Instance.selectedCard;
            ManaManager mana = GameManager.Instance.manaManager;

            int cost = card.cardData.manaCost;

            if (!mana.HasEnoughMana(cost))
            {
                return;
            }

            Debug.Log("Mana OK, spawn autorisé");

            SpawnUnit();
            mana.SpendMana(cost);

            HandSlot slot = card.GetComponent<HandSlot>();
            Debug.Log(slot == null ? "HandSlot NULL" : "HandSlot OK");

            card.GetComponent<HandSlot>().ClearSlot();
            GameManager.Instance.DeselectCard();
        } else {
            Debug.LogWarning("No card select");
        }
    }

    private void SpawnUnit()
    {
        if (spawnedUnit == null)
        {
            GameObject unitPrefab = GameManager.Instance.GetSelectedUnitPrefab();
            if (unitPrefab != null) {
                //spawnedUnit = Instantiate(unitPrefab, position, Quaternion.identity);
                unitPrefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                // Movements are NOT managed by the navmesh
                unitPrefab.GetComponent<NavMeshAgent>().enabled = false;

                spawnedUnit = Instantiate(unitPrefab, Vector3.Scale(transform.position, new Vector3(1f, 2.3f, 1f)), Quaternion.identity);
                spawnedUnit.GetComponentInChildren<Units>().enabled = false;

                BugTracker.Info("Unit '"+unitPrefab.name+"' spawned.");
            } else {
                BugTracker.Error("Missing model (Gidcell.cs/SpawnUnit).");
            }
        }
    }
}