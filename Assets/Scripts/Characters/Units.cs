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
    /* Must be handle in each entities script */
    protected float totalHealth = 100;
    public string entityTag = "Champion";
    public float hp = 100;
    public float attackRate = 3f; // every seconde
    public float attackRange = 15;
    public float damagePerAttack = 10;
    public float speed = 10f;
    public float animationSpeed = 3f;
    public float timeBeforeFirstAttack = 0f;
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
        animator.speed = 1 / animationSpeed;
        navMeshAgent.speed = speed;
        hpSlider = hpBarCanvas.GetComponentInChildren<Slider>();
        navMeshAgent.updateRotation = false;
        fixedRotationHPbar = hpBarCanvas.transform.rotation;
        hp = totalHealth;
        unitsClass.Add(UnitsClass.OnLand);

        animator.SetBool("IsMoving", false);
        animator.SetBool("IsAttacking", false);
    }

    private void LateUpdate() {
        hpBarCanvas.transform.rotation = fixedRotationHPbar;
    }

    protected virtual void Update() {
        if (!isCapaciteAlreadyUse)
            Capacite(); // Must be handle by the Champion

        if (target != null) {
            if (Vector3.Distance(transform.position, target.transform.position) > attackRange) {
                CancelInvoke(nameof(Attack));
                isAttacking = false;
                animator.SetBool("IsMoving", true);
                animator.SetBool("IsAttacking", false);
                MoveTowardsTarget();
            }
            else {
                animator.SetBool("IsAttacking", true);
                animator.SetBool("IsMoving", false);
                if (!isAttacking) {
                    InvokeRepeating(nameof(Attack), timeBeforeFirstAttack, attackRate);
                    isAttacking = true;
                }
            }
        } else {
            CancelInvoke(nameof(Attack));
            isAttacking = false;
            entities = GameObject.FindGameObjectsWithTag(entityTag);
            target = FindClosestEntity();
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
                closest = entity.transform.root.gameObject;
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
            // can be change with a bool and wait for 1 more loop before going to Die();
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
