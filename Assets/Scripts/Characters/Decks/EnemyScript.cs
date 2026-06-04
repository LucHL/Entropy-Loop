using UnityEngine;

public class Enemy : Units
{
    public override UnitsClass unitsClass => UnitsClass.Dps;

    protected override void Start()
    {
        speed = 1f;
        totalHealth = 200;
        damagePerAttack = 15;
        manaCost = 3;
        entityType = EntityType.Basic;
        base.Start();
    }
}
