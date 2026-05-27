using System;
using UnityEngine;

public class GridDragAndDrop : MonoBehaviour
{
    [SerializeField] LayerMask unitLayer;
    [SerializeField] LayerMask gridLayer;
    [SerializeField] float yOffset = 3f;

    private Transform selectedUnit;
    private Rigidbody selectedRigidbody;
    private BoxCollider selectedBoxCollider;
    private Vector3 originalPosition;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && selectedUnit == null)
            SelectUnit();

        if (Input.GetMouseButton(0) && selectedUnit != null) {
            DragUnit();
        }

        if (Input.GetMouseButtonUp(0) && selectedUnit != null) {
            DropUnit();
        }
    }

    private void SelectUnit()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 200f)) {
            if (hit.collider.gameObject.tag == "Entities" && hit.collider.gameObject.GetComponentInChildren<EntityTeam>().tag != "Enemy") {
                selectedUnit = hit.transform;
                originalPosition = selectedUnit.position;

                selectedRigidbody = selectedUnit.GetComponent<Rigidbody>();
                if (selectedRigidbody != null)
                    selectedRigidbody.isKinematic = true;

                selectedBoxCollider = selectedUnit.GetComponent<BoxCollider>();
                if (selectedBoxCollider != null)
                    selectedBoxCollider.enabled = false;

                selectedUnit.position = new Vector3(hit.point.x, hit.point.y + yOffset, hit.point.z);
            }
        }
    }

    private void DragUnit()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 200f)) {
            selectedUnit.position = new Vector3(hit.point.x, originalPosition.y + yOffset, hit.point.z);
        }
    }

    private void DropUnit()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200f)) {
            if (hit.collider.gameObject.tag.StartsWith("Tile")) {
                selectedUnit.position = new Vector3(hit.collider.gameObject.transform.position.x, 2f, hit.collider.gameObject.transform.position.z);
            }
        }
        selectedRigidbody.isKinematic = false;
        selectedBoxCollider.enabled = true;
        selectedUnit = null;
    }
}
