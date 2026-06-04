using UnityEngine;

public class Tiger : Units
{
    public override UnitsClass unitsClass => UnitsClass.Archer;

    protected override void Start()
    {
        attackRate = 2f;
        totalHealth = 200;
        damagePerAttack = 25;
        manaCost = 5;
        entityType = EntityType.Basic;
        base.Start();
    }
}
