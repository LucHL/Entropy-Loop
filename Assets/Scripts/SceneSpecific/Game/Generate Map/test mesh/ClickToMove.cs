using UnityEngine;
using UnityEngine.AI;

public class ClickToMove : MonoBehaviour
{
    public Camera cam;
    public float maxRayDistance = 500f;
    public float navmeshSampleRadius = 5f;

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("[ClickToMove] Pas de NavMeshAgent sur " + name);
            return;
        }

        if (cam == null)
            cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("[ClickToMove] Pas de caméra assignée et aucune MainCamera trouvée.");
        }

        // Essaie de snap le joueur sur le NavMesh au démarrage
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            Debug.Log("[ClickToMove] Warp initial sur le NavMesh : " + hit.position);
        }
        else
        {
            Debug.LogWarning("[ClickToMove] Aucun NavMesh trouvé autour de " + transform.position);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (cam == null)
                return;

            Debug.Log("[ClickToMove] Clic détecté.");

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, Color.red, 2f);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
            {
                Debug.Log("[ClickToMove] Raycast hit: " + hit.collider.name + " @ " + hit.point);

                // On projette le point cliqué sur le NavMesh le plus proche
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(hit.point, out navHit, navmeshSampleRadius, NavMesh.AllAreas))
                {
                    bool ok = agent.SetDestination(navHit.position);
                    Debug.Log("[ClickToMove] SetDestination vers " + navHit.position + " -> " + ok);
                }
                else
                {
                    Debug.LogWarning("[ClickToMove] Pas de NavMesh proche de " + hit.point);
                }
            }
            else
            {
                Debug.LogWarning("[ClickToMove] Raycast ne touche rien.");
            }
        }
    }
}
