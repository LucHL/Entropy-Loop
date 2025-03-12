using UnityEngine;

public class GridCell : MonoBehaviour
{
    private GameObject spawnedUnit;

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
                spawnedUnit = Instantiate(unitPrefab, transform.position + Vector3.up * 25.0f, Quaternion.identity);
                Debug.Log("unit spawned");
            }
            else
            {
                Debug.LogWarning("Missing model");
            }
        }
    }
}
