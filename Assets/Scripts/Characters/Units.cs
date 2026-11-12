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
    Support,

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

    [Header("Grid Movement Rules")]
    public bool canMoveDiagonally = false;

    [Tooltip("Nombre de ticks requis pour effectuer un déplacement d'une case")]
    public int ticksPerMove = 1;

    private Vector2Int gridPosition;
    private int currentTickCount = 0;
    private LineRenderer lineRenderer;
    private Coroutine moveCoroutine;
    [SerializeField] private float pathHeightOffset = 2f;
    private float chessTileSize = 1f;

    [Header("Must be handle in each entities script")] // voir pour un UnitData
    public float damagePerAttack = 10;
    public int manaCost = 3;
    public float totalHealth;
    public float hp = 0;
    public float defense = 10;
    public float attackRate = 1f; // every seconde
    public float attackRange = 1.5f; // 1f is equal to 1 chess tile
    public float speed = 1f;
    public float attackAnimDuration = 1f;
    protected EntityType entityType = EntityType.Basic;
    protected int rewardValue = 10; // Reward System
    protected bool EntityHasCapacity = false;

    protected int protectedHits = 0;
    protected float passifTimer = 0f;

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
    protected int multiplierTotalHp = 5;

    private AnimationState currentAnimationState;
    private BackupUnits backupUnits = new();
    public bool isGameRunning = false;

    protected float capacityTriggerMax = 100f;
    protected float capacityPoints = 0f;

    // hp * 5
    // 4 def = 40%
    // 100% = 10 def
    // capacité: faire un jauge (ex: 100 pts, atk recu 5pts, defense 10pts) // lucas
    // passif: c'est en permanant (sauf si temps)


    void Awake()
    {
        BugTracker.Info("New entity '" + gameObject.name + "' created.");

        animator = GetComponentInChildren<Animator>();
        unitsRigidbody = GetComponent<Rigidbody>();
        currentAnimationState = AnimationState.Idle;
        team = UnitsTeam.Enemy;
        gameObject.layer = 0;
        unitsRigidbody.isKinematic = true;

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
    }

    protected virtual void Start() {
        // LoadInformationsFromUnitData();

        isGameRunning = false;

        SetAnimationState(AnimationState.Idle);

        ResetHpBarQuaternion();

        hp = totalHealth;

        chessTileSize = VoidMapGeneratorGPU.instance.chessTile;

        SaveNewPosition();

        team = GetComponentInChildren<EntityTeam>().GetTeam();
        GetComponentInChildren<ChangeHealthBarColor>().ChangeColor(team);
        CombatUnitFeedback.Register(this, hpBarCanvas);
        if (team == UnitsTeam.Enemy)
            enemyTag = "Champion";
        else
            enemyTag = "Enemy";
        
        unitsRigidbody.isKinematic = false;

        attackRange *= VoidMapGeneratorGPU.instance.chessTile;

        BugTracker.Info("Entity '" + gameObject.name + "' backup created.");

        capacityPoints = 0f;
    }

    public void ResetUnit()
    {
        gameObject.SetActive(true);

        gameObject.layer = 0;
        isAlive = true;
        hpSlider.value = totalHealth;
        gameObject.transform.SetPositionAndRotation(backupUnits.position, backupUnits.rotation);
        unitsRigidbody.isKinematic = true;

        target = null;
        BugTracker.Info("Entity '" + gameObject.name + "' has been reset.");
        Start();
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

        Passif();

        attackTimer -= Time.deltaTime;

        if (target != null && targetUnitsComponent != null) {
            if (!targetUnitsComponent.isAlive) {
                target = null;

                if (lineRenderer != null)
                    lineRenderer.positionCount = 0;

                return;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance > (attackRange * chessTileSize)) {
                SetAnimationState(AnimationState.Moving);
                MoveTowardsTarget();
            } else {
                SetAnimationState(AnimationState.Attacking);
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

        // Debug.Log($"{name} | HasCapacity={EntityHasCapacity} | Capacity={capacityPoints}/{capacityTriggerMax}");
        UpdatePathVisual();

        if (EntityHasCapacity && capacityPoints >= capacityTriggerMax) {
            capacityPoints = 0;

            BugTracker.Info("'"+name+"' capacity points is full, Capacity() activation.");
            //GameLogManager.Instance.AddLog("'"+name.Split("(")[0] +"' activated Capacity");

            Capacite();
        }
    }

    private void UpdatePathVisual()
    {
        if (target == null || lineRenderer == null || !isAlive) {
            if (lineRenderer != null) lineRenderer.positionCount = 0;
            return;
        }

        Vector2Int currentGridPos = WorldToGridPosition(transform.position);
        Vector2Int targetGridPos = WorldToGridPosition(target.transform.position);

        Vector2Int delta = targetGridPos - currentGridPos;

        if (delta == Vector2Int.zero) {
            lineRenderer.positionCount = 0;
            return;
        }

        int stepX = System.Math.Sign(delta.x);
        int stepY = System.Math.Sign(delta.y);

        Vector2Int nextGridPos = currentGridPos;
        if (canMoveDiagonally && stepX != 0 && stepY != 0) {
            nextGridPos = new Vector2Int(currentGridPos.x + stepX, currentGridPos.y + stepY);
        } else {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) {
                nextGridPos.x += stepX;
            } else {
                nextGridPos.y += stepY;
            }
        }

        Vector3 startWorldPos = transform.position + Vector3.up * pathHeightOffset;
        Vector3 nextWorldPos = GridToWorldPosition(nextGridPos) + Vector3.up * pathHeightOffset;
        Vector3 targetWorldPos = target.transform.position + Vector3.up * pathHeightOffset;

        if (canMoveDiagonally && stepX != 0 && stepY != 0)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startWorldPos);
            lineRenderer.SetPosition(1, targetWorldPos);
        }
        else
        {
            Vector3 cornerWorldPos = new Vector3(targetWorldPos.x, startWorldPos.y, startWorldPos.z);

            lineRenderer.positionCount = 3;
            lineRenderer.SetPosition(0, startWorldPos);
            lineRenderer.SetPosition(1, cornerWorldPos);
            lineRenderer.SetPosition(2, targetWorldPos);
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

    protected virtual void Passif()
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
        if (target == null) return;

        Vector2Int currentGridPos = WorldToGridPosition(transform.position);
        Vector2Int targetGridPos = WorldToGridPosition(target.transform.position);

        Vector2Int delta = targetGridPos - currentGridPos;

        if (delta == Vector2Int.zero)
        {
            SetAnimationState(AnimationState.Idle);
            return;
        }

        int stepX = System.Math.Sign(delta.x);
        int stepY = System.Math.Sign(delta.y);

        Vector2Int nextGridPos = currentGridPos;

        // 1. Détermination du prochain pas (Diagonale ou Orthogonal)
        if (canMoveDiagonally && stepX != 0 && stepY != 0)
        {
            nextGridPos = new Vector2Int(currentGridPos.x + stepX, currentGridPos.y + stepY);
        }
        else
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) {
                nextGridPos.x += stepX;
            } else {
                nextGridPos.y += stepY;
            }
        }

        // 2. Gestion des collisions (si la case est occupée par une autre entité)
        if (IsTileOccupied(nextGridPos))
        {
            // Essai d'un axe secondaire pour contourner
            Vector2Int alternativePos = currentGridPos;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) {
                alternativePos.y += stepY;
            } else {
                alternativePos.x += stepX;
            }

            if (!IsTileOccupied(alternativePos))
            {
                nextGridPos = alternativePos;
            }
            else
            {
                // Chemin complètement bloqué : l'unité s'arrête et attend en Idle
                SetAnimationState(AnimationState.Idle);
                return;
            }
        }

        // 3. Déplacement vers le centre exact de la case choisie
        Vector3 targetWorldPos = GridToWorldPosition(nextGridPos);
        SetAnimationState(AnimationState.Moving);

        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, speed * Time.deltaTime);

        // Rotation fluide vers la direction
        Vector3 direction = targetWorldPos - transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero) {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        // 4. PAUSE / ARRÊT SUR LA CASE : Dès qu'elle atteint le centre exact, elle s'arrête (prêt pour les ticks)
        if (Vector3.Distance(transform.position, targetWorldPos) < 0.001f)
        {
            transform.position = targetWorldPos;
            // Ici, l'entité est parfaitement ancrée au centre de la case et fait une pause.
            // Plus tard, tu pourras conditionner ce déplacement pour qu'il n'avance d'une case qu'à l'écoute d'un "OnTick".
        }
    }

    private bool IsTileOccupied(Vector2Int gridPos)
    {
        GameObject[] allChampions = GameObject.FindGameObjectsWithTag("Champion");
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        List<GameObject> allEntities = new(allChampions);
        allEntities.AddRange(allEnemies);

        foreach (GameObject entity in allEntities) {
            if (entity == null || entity == gameObject)
                continue;

            Units unit = entity.GetComponentInParent<Units>();

            if (unit != null && unit.isAlive) {
                Vector2Int otherPos = unit.WorldToGridPosition(entity.transform.position);

                if (otherPos == gridPos)
                    return true;
            }
        }
        return false;
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / chessTileSize),
            Mathf.RoundToInt(worldPos.z / chessTileSize)
        );
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * chessTileSize, transform.position.y, gridPos.y * chessTileSize);
    }

    /// <summary>
    /// Inflicts damage on the unit.
    /// INFO: If the damage is negative, the unit will be healed
    /// </summary>
    /// <param name="damage">If the damage is negative, the unit will be healed</param>
    public void TakeDamage(float damage, bool tDMG = false) {
        if (hpSlider == null)
            BugTracker.Error("'" + gameObject.name + "' has a hpSlider null !");
        // if true DMG ?
        if (protectedHits > 0 && damage > 0)
        {
            protectedHits--;
            DamagePopupManager.instance.Init(transform, 0);
            BugTracker.Info("'" + name + "' blocked a hit. Remaining: " + protectedHits);
            return;
        }

        float reduction = defense * 0.05f;
        reduction = Mathf.Clamp(reduction, 0f, 1f);
        float multiplier = 1f - reduction;
        damage *= multiplier;

        DamagePopupManager.instance.Init(transform, damage);

        GainCapacityPoints(reduction * 10 / 2);
        hp -= damage;
        hpSlider.value = hp / totalHealth;

        if (hp <= 0)
            Die();
    }

    // TrueDmg is a term used when dmg have effect to go through DEF, reduction dmg buff or redirection dmg effect.
    // TrueDmg apply dmg directly on target attacked unit HP.
    public void TakeTrueDMG(float damage)
    {
        if (hpSlider == null)
            BugTracker.Error("'" + gameObject.name + "' has a hpSlider null !");
        
        DamagePopupManager.instance.Init(transform, damage);

        hp -= damage;
        hpSlider.value = hp / totalHealth;

        if (hp <= 0)
            Die();

    }

    public virtual void Attack()
    {
        if (target == null)
            return;

        GainCapacityPoints(damagePerAttack * 3); //rework forumla for the amount of point earned

        target.GetComponent<Units>().TakeDamage(damagePerAttack);
        PlaySound(attackSound);

        BugTracker.Info("Entity '" + gameObject.name + "' attack '"+ target.name + "' and deal " + damagePerAttack + " damage. (hp: "+target.GetComponent<Units>().hp+"/"+target.GetComponent<Units>().totalHealth+").");
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
        CombatUnitFeedback.Death(this);
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
        // Some unit prefabs intentionally have no audio assigned.
        // Unity's missing-object references must be checked before dereferencing.
        if (audioClip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(audioClip);
    }

    private void GainCapacityPoints(float pts)
    {
        if (capacityPoints < 100)
        {
            capacityPoints += pts;
        }
    }

    public void SaveNewPosition()
    {
        backupUnits.position = gameObject.transform.position;
        backupUnits.rotation = gameObject.transform.rotation;
    }
}
