using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyCombat : MonoBehaviour
{
    public float attackRange = 15.0f;
    public float attackDelay = 1.5f;
    public GameObject attackEffect;

    private Transform target;
    private NavMeshAgent agent;
    private Animator animator;
    private bool isAttacking = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        FindNewTarget();
    }

    void Update()
    {
        if (target == null)
        {
            FindNewTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > attackRange)
        {
            if (!agent.hasPath || agent.remainingDistance > attackRange)
            {
                agent.SetDestination(target.position);
                agent.stoppingDistance = attackRange;
            }

            animator.SetBool("IsMoving", true);
            animator.SetBool("IsAttacking", false);

            if (isAttacking)
            {
                StopCoroutine("AttackLoop");
                isAttacking = false;
            }
        }
        else
        {
            agent.ResetPath();
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsAttacking", true);

            if (!isAttacking)
            {
                StartCoroutine("AttackLoop");
                isAttacking = true;
            }
        }
    }


    IEnumerator AttackLoop()
    {
        while (target != null && Vector3.Distance(transform.position, target.position) <= attackRange)
        {
            Attack();
            yield return new WaitForSeconds(attackDelay);
        }

        isAttacking = false;
    }

    void Attack()
    {
        if (target == null) return;

        animator.SetTrigger("AttackTrigger");

        if (attackEffect != null)
        {
            GameObject effect = Instantiate(attackEffect, target.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    void FindNewTarget()
    {
        GameObject[] champions = GameObject.FindGameObjectsWithTag("champion");

        if (champions.Length == 0)
        {
            target = null;
            return;
        }

        target = champions[0].transform;
        float closestDistance = Vector3.Distance(transform.position, target.position);

        foreach (GameObject champion in champions)
        {
            float distanceToChampion = Vector3.Distance(transform.position, champion.transform.position);
            if (distanceToChampion < closestDistance)
            {
                target = champion.transform;
                closestDistance = distanceToChampion;
            }
        }
    }

}

/*using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyCombat : MonoBehaviour
{
    public float attackRange = 1.5f;
    public float attackDelay = 1.5f;
    public float moveSpeed = 3.5f; // 📌 Ajustable depuis Unity

    public GameObject attackEffect;

    private Transform target;
    private NavMeshAgent agent;
    private Animator animator;
    private bool isAttacking = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        agent.speed = moveSpeed;

        FindNewTarget();
    }

    void Update()
    {
        if (target == null)
        {
            FindNewTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        float remaining = agent.remainingDistance;

        Debug.Log(gameObject.name + " - Distance avec cible: " + distance + " | Remaining Distance: " + remaining);

        if (remaining > attackRange) // 📌 Déplacement vers l'ennemi
        {
            if (!agent.hasPath || remaining > attackRange + 0.3f)
            {
                agent.SetDestination(target.position);
                agent.stoppingDistance = attackRange - 0.1f;
            }

            animator.SetBool("IsMoving", true);
            animator.SetBool("IsAttacking", false);

            if (isAttacking)
            {
                StopCoroutine("AttackLoop");
                isAttacking = false;
            }
        }
        else // 📌 Arrêt et attaque
        {
            agent.ResetPath();
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsAttacking", true);

            if (!isAttacking)
            {
                StartCoroutine("AttackLoop");
                isAttacking = true;
            }
        }
    }

    IEnumerator AttackLoop()
    {
        while (target != null && agent.remainingDistance <= attackRange)
        {
            Attack();
            yield return new WaitForSeconds(attackDelay);
        }

        isAttacking = false;
    }

    void Attack()
    {
        if (target == null) return;

        Debug.Log(gameObject.name + " attaque " + target.name + " ⚔️");

        animator.SetTrigger("AttackTrigger");

        if (attackEffect != null)
        {
            GameObject effect = Instantiate(attackEffect, target.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    void FindNewTarget()
    {
        GameObject[] champions = GameObject.FindGameObjectsWithTag("champion");

        if (champions.Length == 0)
        {
            target = null;
            Debug.LogWarning(gameObject.name + " 🚨 Aucun champion trouvé !");
            return;
        }

        target = champions[0].transform;
        float closestDistance = Vector3.Distance(transform.position, target.position);

        foreach (GameObject champion in champions)
        {
            float distanceToChampion = Vector3.Distance(transform.position, champion.transform.position);
            if (distanceToChampion < closestDistance)
            {
                target = champion.transform;
                closestDistance = distanceToChampion;
            }
        }

        Debug.Log(gameObject.name + " 🎯 Cible : " + target.name);
    }
}*/


