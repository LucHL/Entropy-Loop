using System;
using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using Unity.VisualScripting;
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

public enum AnimationState {
    Idle,
    Moving,
    Attacking
}

public class Units : MonoBehaviour
{
    /* Must be handle in each entities script */
    protected float totalHealth = 100;
    public string entityTag = "Champion";
    public float hp = 100;
    public float attackRate = 1f; // every seconde
    public float attackRange = 1.5f;
    public float damagePerAttack = 10;
    public float speed = 10f;
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
    protected AnimationState state = AnimationState.Idle;
    protected float attackTimer = 0f;

    private Quaternion fixedRotationHPbar;
    private AnimationState currentAnimationState;

    protected virtual void Start() {
        // navMeshAgent.speed = speed;
        // navMeshAgent.updateRotation = false;
        // navMeshAgent.Warp(transform.position);

        animator = GetComponentInChildren<Animator>();
        animator.speed = 1 / attackRate;
        animator.SetFloat("MoveSpeed", speed);
        currentAnimationState = AnimationState.Idle;

        hpSlider = hpBarCanvas.GetComponentInChildren<Slider>();
        fixedRotationHPbar = hpBarCanvas.transform.rotation;
        hp = totalHealth;
        unitsClass.Add(UnitsClass.OnLand);

        // animator.SetBool("IsMoving", false);
        // animator.SetBool("IsAttacking", false);

        BugTracker.Info("New entity '" + gameObject.name + "' created.");
    }

    private void LateUpdate() {
        hpBarCanvas.transform.rotation = fixedRotationHPbar;
    }

    protected virtual void Update() {
        // if (!isCapaciteAlreadyUse) {
        //     // BugTracker.Info("Entity '" + gameObject.name + "' active function Capacite().");
        //     Capacite(); // Must be handle by the Champion
        // }

        // if (target != null) {
        //     if (Vector3.Distance(transform.position, target.transform.position) > attackRange) {
        //         CancelInvoke(nameof(Attack));
        //         isAttacking = false;
        //         // animator.SetBool("IsMoving", true);
        //         // animator.SetBool("IsAttacking", false);
        //         MoveTowardsTarget();
        //     }
        //     else {
        //         // animator.SetBool("IsAttacking", true);
        //         // animator.SetBool("IsMoving", false);
        //         if (!isAttacking) {
        //             InvokeRepeating(nameof(Attack), timeBeforeFirstAttack, attackRate);
        //             isAttacking = true;
        //         }
        //     }
        // } else {
        //     CancelInvoke(nameof(Attack));
        //     isAttacking = false;
        //     target = FindClosestEntity();
        //     // animator.SetBool("IsMoving", false);
        //     // animator.SetBool("IsAttacking", false);
        // }

        if (!isCapaciteAlreadyUse)
        Capacite();

        attackTimer -= Time.deltaTime;

        if (target != null) {
            float distance = Vector3.Distance(transform.position, target.transform.position);

            Debug.Log(distance + " "+attackRange);

            if (distance > attackRange) {
                SetAnimationState(AnimationState.Moving);
                MoveTowardsTarget();
            } else {
                if (attackTimer <= 0f) {
                    SetAnimationState(AnimationState.Attacking);
                    Attack();
                    attackTimer = attackRate;
                }
            }
        } else {
            SetAnimationState(AnimationState.Idle);
            target = FindClosestEntity();
        }
    }

    protected void SetAnimationState(AnimationState newState)
    {
        if (currentAnimationState == newState)
            return;

        currentAnimationState = newState;

        animator.SetBool("IsMoving", newState == AnimationState.Moving);
        animator.SetBool("IsAttacking", newState == AnimationState.Attacking);
    }

    protected virtual void Capacite()
    {
        return;
    }

    protected GameObject FindClosestEntity()
    {
        entities = GameObject.FindGameObjectsWithTag(entityTag);

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

    // ------------------------------------------------------------------ //
    // This function use NavMesh
    // ------------------------------------------------------------------ //
        // protected void MoveTowardsTarget()
        // {
        //     navMeshAgent.SetDestination(target.transform.position);
        //     navMeshAgent.steeringTarget.Set(target.transform.position.x, target.transform.position.y, target.transform.position.z);
        // }
    
    protected void MoveTowardsTarget()
    {
        Vector3 direction = target.transform.position - transform.position;

        direction.Normalize();

        // Cannot work for entity who can fly
        direction.y = 0f;

        transform.position += speed * Time.deltaTime * direction;

        // transform.rotation = Quaternion.LookRotation(direction);
        if (direction != Vector3.zero) {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
        }

        Debug.Log(gameObject.name+" move: "+gameObject.transform.position);
    }


    /// <summary>
    /// Inflicts damage on the unit.
    /// INFO: If the damage is negative, the unit will be healed
    /// </summary>
    /// <param name="damage">If the damage is negative, the unit will be healed</param>
    public void TakeDamage(float damage) {
        if (hpSlider == null)
            BugTracker.Critical("'" + gameObject.name + "' has a hpSlider null !");

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

        BugTracker.Info("Entity '" + gameObject.name + "' attack '"+ target.name + "' and deal '" + damagePerAttack + "' damage");
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
        BugTracker.Info("Entity '" + gameObject.name + "' destroyed.");
        Destroy(gameObject);
    }
}
