using System;
using TMPro;
using UnityEngine;

public class GridHighlightManager : MonoBehaviour
{
    private Camera mainCamera;
    private GridCell lastHoveredCell;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200f)) {
            if (hit.collider.gameObject.tag.StartsWith("Tile")) {
                GridCell currentCell = hit.collider.GetComponent<GridCell>();

                if (currentCell != null) {
                    if (currentCell != lastHoveredCell) {
                        if (lastHoveredCell != null)
                            lastHoveredCell.SetHighlight(false, Color.white);

                        String []list = hit.collider.gameObject.name.Split("_");

                        if (int.Parse(list[2]) < 4)
                            currentCell.SetHighlight(true, Color.green);
                        else
                            currentCell.SetHighlight(true, Color.red);

                        lastHoveredCell = currentCell;
                    }
                }
            }
            else
                ClearLastCell();
        }
        else
            ClearLastCell();
    }

    private void ClearLastCell()
    {
        if (lastHoveredCell != null) {
            lastHoveredCell.SetHighlight(false, Color.white);
            lastHoveredCell = null;
        }
    }
}
