using UnityEngine;

public class Lancier : Units
{
    public override UnitsClass unitsClass => UnitsClass.Support;

    [SerializeField] private float spearRange = 2f;

    protected override void Start()
    {
        speed = 1f;
        attackRate = 1.2f;
        totalHealth = 6 * multiplierTotalHp;
        damagePerAttack = 3;
        defense = 3;

        manaCost = 3;
        team = UnitsTeam.Player;
        entityType = EntityType.Basic;
        EntityHasCapacity = true;
        base.Start();
    }

    protected override void Capacite()
    {
        capacityPoints = 0f;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        GameLogManager.Instance.AddLog("Lancier active sa capacité");

        foreach (GameObject enemy in enemies)
        {
            Units unit = enemy.GetComponentInParent<Units>();

            if (unit == null || !unit.isAlive)
                continue;

            Vector3 direction = enemy.transform.position - transform.position;

            if (direction.magnitude > spearRange)
                continue;

            float angle = Vector3.Angle(transform.forward, direction);

            if (angle <= 20f)
            {
                unit.TakeDamage(damagePerAttack);
            }
        }
    }
}
