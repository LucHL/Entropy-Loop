using UnityEngine;
using UnityEngine.AI;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab; // Le prefab du joueur
    public Transform spawnPoint; // Point de départ (facultatif)

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Clique gauche
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                SpawnPlayer(hit.point); // Crée un joueur à l'endroit cliqué
            }
        }
    }

    void SpawnPlayer(Vector3 position)
    {
        GameObject newPlayer = Instantiate(playerPrefab, Vector3.Scale(position, new Vector3(1f, 0f, 1f)), Quaternion.identity);
        Debug.Log("New Champion");
    }
}
