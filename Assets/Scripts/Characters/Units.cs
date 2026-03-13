using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum UnitsClass {
    OnLand,
    Flying
}

public enum AnimationState {
    Idle,
    Moving,
    Attacking
}

public class BackupUnits
{
    public Vector3 position;
    public Quaternion rotation;
}

public class Units : MonoBehaviour
{
    public Units instance;
    public GameObject attackEffect;
    public NavMeshAgent navMeshAgent;
    public Canvas hpBarCanvas;
    protected Slider hpSlider;
    public Transform modelPosition;
    public bool isAlive = true;

    /* Must be handle in each entities script */
    public float damagePerAttack = 10;
    protected string enemyTag = "Champion";
    protected float totalHealth = 100;
    protected float hp = 0;
    protected float attackRate = 1f; // every seconde
    protected float attackRange = 1.5f;
    protected float speed = 1f;
    protected float attackAnimDuration = 1f;
    protected float timeBeforeFirstAttack = 0f;
    /* end */

    protected List<UnitsClass> unitsClass = new();
    protected GameObject target = null;
    protected GameObject[] entities = null;
    protected Animator animator;
    protected AnimationState state = AnimationState.Idle;
    protected float attackTimer = 0f;
    protected bool isCapaciteAlreadyUse = false;

    private Quaternion fixedRotationHPbar;
    private AnimationState currentAnimationState;
    private BackupUnits backupUnits = new();

    protected virtual void Start() {
        // navMeshAgent.speed = speed;
        // navMeshAgent.updateRotation = false;
        // navMeshAgent.Warp(transform.position);

        animator = GetComponentInChildren<Animator>();
        // animator.speed = 1 / attackRate;
        // animator.SetFloat("MoveSpeed", speed);
        currentAnimationState = AnimationState.Idle;

        hpSlider = hpBarCanvas.GetComponentInChildren<Slider>();
        fixedRotationHPbar = hpBarCanvas.transform.rotation;
        hp = totalHealth;
        unitsClass.Add(UnitsClass.OnLand);

        // animator.SetBool("IsMoving", false);
        // animator.SetBool("IsAttacking", false);

        BugTracker.Info("New entity '" + gameObject.name + "' created.");

        backupUnits.position = gameObject.transform.position;
        backupUnits.rotation = gameObject.transform.rotation;
        BugTracker.Info("Entity '" + gameObject.name + "' backup created.");
    }

    public void ResetUnit()
    {
        gameObject.SetActive(true);

        isAlive = true;
        hp = totalHealth;
        hpSlider.value = totalHealth;
        isCapaciteAlreadyUse = false;
        gameObject.transform.position = backupUnits.position;
        gameObject.transform.rotation = backupUnits.rotation;

        BugTracker.Info("Entity '" + gameObject.name + "' has been reset.");
    }

    private void LateUpdate() {
        hpBarCanvas.transform.rotation = fixedRotationHPbar;
    }

    protected virtual void Update()
    {
        if (!isCapaciteAlreadyUse)
            Capacite();

        attackTimer -= Time.deltaTime;

        if (target != null) {
            float distance = Vector3.Distance(transform.position, target.transform.position);

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
        entities = GameObject.FindGameObjectsWithTag(enemyTag);

        GameObject closest = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (GameObject entity in entities)
        {
            if (entity == null || entity == gameObject)
                continue;
            
            Units unit = entity.GetComponentInParent<Units>();

            if (unit == null || !unit.isAlive)
                continue;

            float sqrDistance = (entity.transform.position - currentPosition).sqrMagnitude;

            if (sqrDistance < minDistance)
            {
                minDistance = sqrDistance;
                closest = entity.transform.root.gameObject;
            }
            // float distance = Vector3.Distance(currentPosition, entity.transform.position);
            // if (distance < minDistance)
            // {
            //     minDistance = distance;
            //     closest = entity.transform.root.gameObject;
            // }
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
        // animator.SetTrigger("AttackTrigger");
        target.GetComponent<Units>().TakeDamage(damagePerAttack);

        // if (attackEffect != null)
        // {
        //     GameObject effect = Instantiate(attackEffect, target.transform.position + new Vector3(0, 10, 0), Quaternion.identity);
        //     Destroy(effect, 2f);
        // }
    }

    protected virtual void Die()
    {
        isAlive = false;
        gameObject.SetActive(false);

        GameLoopManager.instance.CheckVictory();
    }
}
