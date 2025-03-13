using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class CharacterCombat : MonoBehaviour
{
    public Transform target;
    public float attackRange = 15.0f;
    public float attackDelay = 1f;

    private NavMeshAgent agent;
    private Animator animator;
    private bool isAttacking = false;

    public GameObject attackEffect;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (target == null)
        {
            GameObject enemy = GameObject.FindGameObjectWithTag("enemy");
            if (enemy != null)
                target = enemy.transform;
            else
                Debug.LogError("Aucun objet avec le tag 'Enemy' n'a été trouvé !");
        }
    }

    void Update()
    {
        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            if (distance > attackRange)
            {
                agent.SetDestination(target.position);
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
    }


    IEnumerator AttackLoop()
    {
        while (Vector3.Distance(transform.position, target.position) <= attackRange)
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

}


/*using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class CharacterCombat : MonoBehaviour
{
    public Transform target;
    public float attackRange = 1.5f;
    public float attackDelay = 1f;
    public float moveSpeed = 3.5f;
    public GameObject attackEffect;

    private NavMeshAgent agent;
    private Animator animator;
    private bool isAttacking = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = moveSpeed;

        if (target == null)
        {
            GameObject enemy = GameObject.FindGameObjectWithTag("enemy");
            if (enemy != null)
                target = enemy.transform;
            else
                Debug.LogError("Aucun objet avec le tag 'Enemy' n'a été trouvé !");
        }
    }

    void Update()
    {
        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            float remaining = agent.remainingDistance;

            Debug.Log(gameObject.name + " - Distance avec cible: " + distance + " | Remaining Distance: " + remaining);

            if (remaining > attackRange)
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
}*/

