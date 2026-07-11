using UnityEngine;

public class Enemy : Units
{
    public override UnitsClass unitsClass => UnitsClass.Dps;

    protected override void Start()
    {
        speed = 1f;
        attackRate = 1.5f;
        totalHealth = 3 * multiplierTotalHp;
        damagePerAttack = 4;
        defense = 4;

        manaCost = 3;
        entityType = EntityType.Basic;
        base.Start();
    }
}
