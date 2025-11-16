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
        if (GameManager.Instance != null && GameManager.Instance.HasSelectedCard())
        {
            SpawnUnit();
            GameManager.Instance.DeselectCard();
        }
        else
        {
            Debug.LogWarning("No card select");
        }
    }

    private void SpawnUnit()
    {
        if (spawnedUnit == null)
        {
            GameObject unitPrefab = GameManager.Instance.GetSelectedUnitPrefab();
            if (unitPrefab != null)
            {
                spawnedUnit = Instantiate(unitPrefab, Vector3.Scale(transform.position, new Vector3(1f, 0f, 1f)), Quaternion.identity);
                Debug.Log("unit spawned");
                spawnedUnit.GetComponentInChildren<Units>().enabled = false;
            }
            else
            {
                Debug.LogWarning("Missing model");
            }
        }
    }
}
