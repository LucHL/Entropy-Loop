using UnityEngine;
using UnityEngine.UI;

public class Units : MonoBehaviour
{
    public string entityTag = "Champion";
    public float speed = 10;
    public float health = 100;
    public float attackRate = 1f; // every seconde
    public float damagePerSecond = 10;
    public float attackRange = 15;
    public GameObject attackEffect;
    public Canvas canvas;
    public Slider slider;

    protected Animator animator;
    protected GameObject target;
    protected GameObject[] entities;

    protected virtual void Start()
    {
        animator = GetComponent<Animator>();
    }

    protected virtual void Update()
    {
        if (target != null) {
            if (Vector3.Distance(transform.position, target.transform.position) > attackRange) {
                animator.SetBool("IsMoving", true);
                animator.SetBool("IsAttacking", false);
                MoveTowardsTarget();
                CancelInvoke(nameof(Attack));
            } else {
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsAttacking", true);
                InvokeRepeating(nameof(Attack), 0f, attackRate);
            }
        } else {
            CancelInvoke(nameof(Attack));
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsAttacking", false);
            entities = GameObject.FindGameObjectsWithTag(entityTag);
            target = FindClosestEntity();
        }
    }

    protected GameObject FindClosestEntity()
    {
        if (entities == null)
            return null;

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
        return closest;
    }

    protected void MoveTowardsTarget()
    {
        Vector3 distance = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
        transform.position = distance;
        slider.transform.position = distance + new Vector3(0, 21, 0);
        transform.LookAt(target.transform.position);
    }

    public void receiveDamage(float damage)
    {
        health -= damage;
        slider.value = health / 100;
        if (health <= 0) {
            Die();
        }
    }

    protected virtual void Attack()
    {
        if (target == null)
            return;

        animator.SetTrigger("AttackTrigger");
        // target.GetComponent<>().receiveDamage(damagePerSecond * speed * Time.deltaTime);
        // targetScript = target.GetComponent<T>();

        // if (targetScript != null) {
        //     targetScript.GetType().GetMethod("receiveDamage").Invoke(targetScript, new object[] { damagePerSecond * speed * Time.deltaTime });
        // }

        if (attackEffect != null)
        {
            GameObject effect = Instantiate(attackEffect, target.transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    protected virtual void Die()
    {
            Destroy(gameObject);
            Destroy(canvas.GetComponent<CanvasScaler>());
            Destroy(canvas.GetComponent<GraphicRaycaster>());
            Destroy(canvas);
    }
}
