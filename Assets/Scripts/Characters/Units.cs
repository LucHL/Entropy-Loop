using System;
using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

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
    // public string entityTag = "Champion";
    // public float speed = 10;
    // public float totalHealth = 100;
    // public float attackRate = 3f; // every seconde
    // public float damagePerAttack = 10;
    // public float attackRange = 15;
    // public Canvas canvas;
    // public Slider slider;
    // public float pv;
    // public NavMeshAgent agent;

    // protected Animator animator;
    // protected GameObject target = null;
    // protected GameObject[] entities = null;

    // private Transform transformParents;


    /* Must be handle in each entities script */
    protected float totalHealth = 100;
    public string entityTag = "Champion";
    public float hp = 100;
    public float attackRate = 3f; // every seconde
    public float attackRange = 15;
    public float damagePerAttack = 10;
    public float speed = 10f;
    public List<UnitsClass> unitsClass = new();
    public bool isCapaciteAlreadyUse = false;

    public GameObject attackEffect;
    public NavMeshAgent navMeshAgent;
    public Canvas hpBarCanvas;
    protected Slider hpSlider;
    public Transform modelPosition;

    protected GameObject target = null;
    protected GameObject[] entities = null;
    protected Animator animator;

    private Quaternion fixedRotationHPbar;
    private bool isAttacking = false;

    protected virtual void Start() {
        animator = GetComponentInChildren<Animator>();
        animator.speed = 1 / attackRate;
        navMeshAgent.speed = speed;
        hpSlider = hpBarCanvas.GetComponentInChildren<Slider>();
        navMeshAgent.updateRotation = false;
        fixedRotationHPbar = hpBarCanvas.transform.rotation;
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsAttacking", false);

        hp = totalHealth;
        unitsClass.Add(UnitsClass.OnLand);
    }

    private void LateUpdate() {
        hpBarCanvas.transform.rotation = fixedRotationHPbar;
    }

    protected virtual void Update() {
        // if (unitsClass.Contains(UnitsClass.OnLand) && transformParents.position.y != 0) {
        //     // transformParents.position = new Vector3(transformParents.position.x, 0f, transformParents.position.z); // worst code ever
        //     MoveTowardsTarget();
        // }

        // if (target != null) {
        //     if (Vector3.Distance(transformParents.position, target.transform.position) > attackRange) {
        //         animator.SetBool("IsMoving", true);
        //         animator.SetBool("IsAttacking", false);
        //         MoveTowardsTarget();

        //         if (isAttacking)
        //         {
        //             CancelInvoke(nameof(Attack));
        //             isAttacking = false;
        //         }
        //     }
        //     else
        //     {
        //         animator.SetBool("IsAttacking", true);
        //         animator.SetBool("IsMoving", false);
        //         if (!isAttacking)
        //         {
        //             InvokeRepeating(nameof(Attack), 0f, attackRate);
        //             isAttacking = true;
        //         }
        //     }
        // }
        // else
        // {
        //     if (isAttacking)
        //     {
        //         CancelInvoke(nameof(Attack));
        //         isAttacking = false;
        //     }
        //     animator.SetBool("IsMoving", false);
        //     animator.SetBool("IsAttacking", false);
        // entities = GameObject.FindGameObjectsWithTag(entityTag);
        //     target = FindClosestEntity();
        // }

        if (!isCapaciteAlreadyUse)
            Capacite(); // Must be handle by the Champion

        entities = GameObject.FindGameObjectsWithTag(entityTag);
        target = FindClosestEntity();

        if (target != null) {
            if (Vector3.Distance(transform.position, target.transform.position) > attackRange) {
                CancelInvoke(nameof(Attack));
                animator.SetBool("IsMoving", true);
                animator.SetBool("IsAttacking", false);
                MoveTowardsTarget();
            }
            else {
                animator.SetBool("IsAttacking", true);
                animator.SetBool("IsMoving", false);
                InvokeRepeating(nameof(Attack), 0f, attackRate);
            }
        }
        else {
            CancelInvoke(nameof(Attack));
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsAttacking", false);
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

    protected void MoveTowardsTarget() {
        navMeshAgent.SetDestination(target.transform.position);
        navMeshAgent.steeringTarget.Set(target.transform.position.x, target.transform.position.y, target.transform.position.z);
    }

    /// <summary>
    /// Inflicts damage on the unit.
    /// If the damage is negative, the unit will be healed
    /// </summary>
    /// <param name="damage">If the damage is negative, the unit will be healed</param>
    public void TakeDamage(float damage) {
        hp -= damage;
        hpSlider.value = hp / 100;
        if (hp <= 0) {
            // Capacite(); // exemple: in case of a commander who can revive
            Die();
        }
    }

    protected virtual void Attack()
    {
        if (target == null)
            return;

        animator.SetTrigger("AttackTrigger");
        target.GetComponent<Units>().TakeDamage(damagePerAttack);

        // if (attackEffect != null)
        // {
        //     GameObject effect = Instantiate(attackEffect, target.transform.position + new Vector3(0, 10, 0), Quaternion.identity);
        //     Destroy(effect, 2f);
        // }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
