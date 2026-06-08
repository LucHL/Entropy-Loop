using UnityEngine;

public class Tiger : Units
{
    public override UnitsClass unitsClass => UnitsClass.Dps;

    protected override void Start()
    {
        speed = 1f;
        attackRate = 1.5f;
        totalHealth = 7 * multiplierTotalHp;
        damagePerAttack = 6;
        defense = 5;

        manaCost = 6;
        entityType = EntityType.Basic;
        base.Start();
    }
}
