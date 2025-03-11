using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Color unitColor = Color.blue;
    private GameObject spawnedUnit;

    void OnMouseDown()
    {
        if (GameManager.Instance != null && GameManager.Instance.SelectedCard != null)
        {
            SpawnUnit();
        } else {
            Debug.LogWarning("GameManager ou SelectedCard est nul !");
        }
    }

    void SpawnUnit()
    {
        if (spawnedUnit == null)
        {
            spawnedUnit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawnedUnit.transform.position = transform.position + Vector3.up * 0.5f;
            spawnedUnit.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            spawnedUnit.GetComponent<Renderer>().material.color = unitColor;
        }
    }
}
