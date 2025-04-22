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
        GameObject newPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
        Debug.Log("🎮 Nouveau joueur ajouté !");
    }
}
