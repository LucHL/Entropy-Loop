using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

public class Champion : MonoBehaviour
{
    public float speed = 10;
    public float health = 100;
    public float damagePerSecond = 10;
    private string entityTag = "Champion";

    void Start()
    {

    }

    void Update()
    {
        Transform target = FindClosestEntity();
        if (target != null) {
            MoveTowardsTarget(target);
        }
    }

    Transform FindClosestEntity()
    {
        GameObject[] entities = GameObject.FindGameObjectsWithTag(entityTag);
        GameObject closest = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (GameObject entity in entities) {
            if (entity == gameObject)
                continue;

            float distance = Vector3.Distance(currentPosition, entity.transform.position);
            if (distance < minDistance) {
                minDistance = distance;
                closest = entity;
            }
        }

        return closest?.transform;
    }

    void MoveTowardsTarget(Transform target)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
    }
}
