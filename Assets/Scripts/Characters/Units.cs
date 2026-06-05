using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum UnitsTeam {
    Player,
    Enemy
}

public enum UnitsClass {
    // corps à corps
    Tank,
    Dps,

    // milieu
    Assassin,
    Archer,
    Mage,

    // distance
    Buffer,
    Healer,
}

public enum EntityType {
    Champion,
    Basic
}

public enum AnimationState {
    Idle,
    Moving,
    Attacking,
    Dead
}

public class BackupUnits
{
    public Vector3 position;
    public Quaternion rotation;
}

public class Units : MonoBehaviour
{
    public Units instance;

    [Header("NavMesh")]
    [SerializeField] NavMeshAgent navMeshAgent;

    [Header("HP Bar")]
    [SerializeField] Canvas hpBarCanvas;
    [SerializeField] Slider hpSlider;
    public Transform modelPosition;

    [Header("Must be handle in each entities script")] // voir pour un UnitData
    public float damagePerAttack = 10;
    public int manaCost = 3;
    public float totalHealth = 100;
    public float hp = 0;
    public float shield = 10;
    public float attackRate = 1f; // every seconde
    public float attackRange = 1.5f; // 1f is equal to 1 chess tile
    public float speed = 1f;
    public float attackAnimDuration = 1f;
    public EntityType entityType = EntityType.Basic;
    public int rewardValue = 10; // Reward System
    public virtual UnitsClass unitsClass => UnitsClass.Dps;


    /* end */
    public bool isAlive = true;
    public UnitsTeam team;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip attackSound = null;
    public AudioClip deathSound = null;
    
    [Header("Effects")]
    public GameObject attackEffect;

    [Header("Do not touch")]
    protected string enemyTag = "Champion";
    protected GameObject target = null;
    protected Units targetUnitsComponent = null;
    protected GameObject[] entities = null;
    protected Animator animator;
    protected Rigidbody unitsRigidbody;
    protected AnimationState state = AnimationState.Idle;
    protected float attackTimer = 0f;
    protected bool isCapaciteAlreadyUse = false;

    private AnimationState currentAnimationState;
    private BackupUnits backupUnits = new();
    public bool isGameRunning = false;
    private float chessTileSize = 1f;

    void Awake()
    {
        BugTracker.Info("New entity '" + gameObject.name + "' created.");

        animator = GetComponentInChildren<Animator>();
        unitsRigidbody = GetComponent<Rigidbody>();
        currentAnimationState = AnimationState.Idle;
        team = UnitsTeam.Enemy;
        gameObject.layer = 0;
        unitsRigidbody.isKinematic = true;
    }

    protected virtual void Start() {
        // LoadInformationsFromUnitData();

        isGameRunning = false;

        SetAnimationState(AnimationState.Idle);

        ResetHpBarQuaternion();

        hp = totalHealth;

        chessTileSize = VoidbornMapGeneratorHybrid.instance.chessTile;

        backupUnits.position = gameObject.transform.position;
        backupUnits.rotation = gameObject.transform.rotation;

        GetComponentInChildren<ChangeHealthBarColor>().ChangeColor(team);
        if (team == UnitsTeam.Enemy)
            enemyTag = "Champion";
        else
            enemyTag = "Enemy";
        
        unitsRigidbody.isKinematic = false;

        BugTracker.Info("Entity '" + gameObject.name + "' backup created.");
    }

    public void ResetUnit()
    {
        gameObject.SetActive(true);

        gameObject.layer = 0;
        isAlive = true;
        hpSlider.value = totalHealth;
        isCapaciteAlreadyUse = false;
        gameObject.transform.SetPositionAndRotation(backupUnits.position, backupUnits.rotation);
        unitsRigidbody.isKinematic = true;

        target = null;
        Start();
        BugTracker.Info("Entity '" + gameObject.name + "' has been reset.");
    }

    protected void LoadInformationsFromUnitData()
    {
        speed = 0.5f;
        attackRate = 2f;
        totalHealth = 150;
        damagePerAttack = 25;
        manaCost = 5;
        team = UnitsTeam.Enemy;
        entityType = EntityType.Champion;
    }

    private void LateUpdate() {
        ResetHpBarQuaternion();

        if (target != null) {
            Vector3 direction = target.transform.position - transform.position;
            if (direction != Vector3.zero) {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    10f * Time.deltaTime
                );
            }
        }
    }

    protected virtual void Update()
    {
        if (!isGameRunning)
            return;

        if (!isCapaciteAlreadyUse)
            Capacite();

        attackTimer -= Time.deltaTime;

        if (target != null && targetUnitsComponent != null) {
            if (!targetUnitsComponent.isAlive) {
                target = null;
                return;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance > (attackRange * chessTileSize)) {
                SetAnimationState(AnimationState.Moving);
                MoveTowardsTarget();
            } else {
                if (attackTimer <= 0f) {
                    SetAnimationState(AnimationState.Attacking);
                    attackTimer = attackRate;
                }
            }
        } else {
            SetAnimationState(AnimationState.Idle);
            target = FindClosestEntity();
            if (target != null)
                targetUnitsComponent = target.GetComponent<Units>();
        }
    }

    protected void SetAnimationState(AnimationState newState)
    {
        if (currentAnimationState == newState)
            return;

        currentAnimationState = newState;

        animator.SetBool("IsIdle", newState == AnimationState.Idle);
        animator.SetBool("IsMoving", newState == AnimationState.Moving);
        animator.SetBool("IsAttacking", newState == AnimationState.Attacking);
        if (newState == AnimationState.Dead)
            animator.SetTrigger("Die");
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
    //     navMeshAgent.Warp(transform.position); 
    //     navMeshAgent.SetDestination(target.transform.position - new Vector3(chessTileSize, chessTileSize, chessTileSize));
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
            BugTracker.Error("'" + gameObject.name + "' has a hpSlider null !");

        if (damage - shield < 0) {
            damage = 0;
            // popup shield
            // DamagePopupManager.instance.InitShield(transform);
        } else
            damage -= shield;

        DamagePopupManager.instance.Init(transform, damage);

        hp -= damage;
        hpSlider.value = hp / totalHealth;
        if (hp <= 0) {
            // Capacite(); // exemple: in case of a commander who can revive
            // can be change with a bool and wait for 1 more loop before going to Die();
            Die();
        }
    }

    public virtual void Attack()
    {
        if (target == null)
            return;

        target.GetComponent<Units>().TakeDamage(damagePerAttack);
        PlaySound(attackSound);

        BugTracker.Info("Entity '" + gameObject.name + "' attack '"+ target.name + "' and deal '" + damagePerAttack + "' damage. (hp: "+target.GetComponent<Units>().hp+"/"+target.GetComponent<Units>().totalHealth+").");
        // if (attackEffect != null)
        // {
        //     GameObject effect = Instantiate(attackEffect, target.transform.position + new Vector3(0, 10, 0), Quaternion.identity);
        //     Destroy(effect, 2f);
        // }
    }

    public void Die()
    {
        if (!isAlive)
            return;

        isAlive = false;
        SetAnimationState(AnimationState.Dead);
        gameObject.layer = 6;

        if (deathSound != null) {
            audioSource.PlayOneShot(deathSound);
            StartCoroutine(DisablePrefabAfterDeathSound());
            return;
        }

        BugTracker.Info("Entity '" + gameObject.name + "' is dead.");

        GameLoopManager.instance.CheckVictory();
    }

    IEnumerator DisablePrefabAfterDeathSound()
    {
        yield return new WaitForSeconds(deathSound.length);

        isAlive = false;

        BugTracker.Info("Entity '" + gameObject.name + "' is dead.");

        // DONNER L'OR
        if (ShopManager.instance != null)
        {
            ShopManager.instance.AddGold(rewardValue);
        }

        GameLoopManager.instance.CheckVictory();
    }

    private void ResetHpBarQuaternion()
    {
        if (hpBarCanvas != null)
            hpBarCanvas.transform.rotation = Quaternion.LookRotation(
                hpBarCanvas.transform.position - Camera.main.transform.position
            );
    }

    protected void PlaySound(AudioClip audioClip)
    {
        if (audioClip != null)
            audioSource.PlayOneShot(audioClip);
        else
            BugTracker.Warning("Function 'PlaySound': "+ audioClip.name + " is null.");
    }
}
