using UnityEngine;

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
        if (GameManager.Instance != null && GameManager.Instance.HasSelectedCard()) {
            SpawnUnit();
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
                unitPrefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                spawnedUnit = Instantiate(unitPrefab, Vector3.Scale(transform.position, new Vector3(1f, 2.3f, 1f)), Quaternion.identity);
                spawnedUnit.GetComponentInChildren<Units>().enabled = false;
                BugTracker.Info("Unit '"+unitPrefab.name+"' spawned.");
            } else {
                BugTracker.Error("Missing model (Gidcell.cs/SpawnUnit).");
            }
        }
    }
}
