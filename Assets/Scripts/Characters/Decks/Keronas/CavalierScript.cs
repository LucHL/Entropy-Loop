using UnityEngine;

public class Cavalier : Units
{
    public override UnitsClass unitsClass => UnitsClass.Dps;

    [SerializeField] private float ignoreDefenseChance = 0.15f;

    protected override void Start()
    {
        speed = 1f;
        attackRate = 1f;
        totalHealth = 6 * multiplierTotalHp;
        damagePerAttack = 4;
        defense = 3;

        manaCost = 4;
        team = UnitsTeam.Player;
        entityType = EntityType.Basic;
        base.Start();
    }

    public override void Attack()
    {
        if (target == null)
            return;

        Units enemy = target.GetComponent<Units>();

        if (Random.value <= ignoreDefenseChance)
        {
            GameLogManager.Instance.AddLog("Cavalier utilise sa charge sur l'ennemi");
            enemy.TakeTrueDMG(damagePerAttack);
        }
        else
        {
            enemy.TakeDamage(damagePerAttack);
        }

        PlaySound(attackSound);
    }
}