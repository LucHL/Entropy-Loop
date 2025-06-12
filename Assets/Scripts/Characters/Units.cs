using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using NUnit.Framework.Constraints;

public enum TypeUnits {
    Champion,
    Enemy
}

public enum UnitsClass {
    OnLand,
    Flying
}

public class Units : MonoBehaviour
{

    public string entityTag = "Champion";
    public float speed = 10;
    public float totalHealth = 100;
    public float attackRate = 3f; // every seconde
    public float damagePerAttack = 10;
    public float attackRange = 15;
    public GameObject attackEffect;
    public Canvas canvas;
    public Slider slider;
    public float pv;

    protected Animator animator;
    protected GameObject target = null;
    protected GameObject[] entities = null;
    protected bool isCapaciteAlreadyUse = false;
    protected UnitsClass[] unitsClass = null;

    private bool isAttacking = true;

    protected virtual void Start()
    {
        animator = GetComponent<Animator>();
        animator.speed = 1 / attackRate;
        pv = totalHealth;
    }

    protected virtual void Update()
    {
        if (target != null)
        {
            if (Vector3.Distance(transform.position, target.transform.position) > attackRange)
            {
                animator.SetBool("IsMoving", true);
                animator.SetBool("IsAttacking", false);
                MoveTowardsTarget();

                if (isAttacking)
                {
                    CancelInvoke(nameof(Attack));
                    isAttacking = false;
                }
            }
            else
            {
                animator.SetBool("IsAttacking", true);
                animator.SetBool("IsMoving", false);
                if (!isAttacking)
                {
                    InvokeRepeating(nameof(Attack), 0f, attackRate);
                    isAttacking = true;
                }
            }
        }
        else
        {
            if (isAttacking)
            {
                CancelInvoke(nameof(Attack));
                isAttacking = false;
            }
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsAttacking", false);
            entities = GameObject.FindGameObjectsWithTag(entityTag);
            target = FindClosestEntity();
        }
    }

    protected virtual void Capacite()
    {
        return;
    }

    protected GameObject FindClosestEntity()
    {
        if (entities == null)
            return null;

        GameObject closest = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (GameObject entity in entities)
        {
            if (entity == gameObject)
                continue;

            float distance = Vector3.Distance(currentPosition, entity.transform.position);
            if (distance < minDistance)
            {
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
        pv -= damage;
        slider.value = pv / 100;
        if (pv <= 0)
        {
            Capacite();
            Die();
        }
    }

    protected virtual void Attack()
    {
        Capacite();
        if (target == null)
            return;

        animator.SetTrigger("AttackTrigger");
        target.GetComponent<Units>().receiveDamage(damagePerAttack);

        if (attackEffect != null)
        {
            GameObject effect = Instantiate(attackEffect, target.transform.position + new Vector3(0, 10, 0), Quaternion.identity);
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
