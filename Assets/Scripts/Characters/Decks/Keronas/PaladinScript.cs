using UnityEngine;

public class Paladin : Units
{
    public override UnitsClass unitsClass => UnitsClass.Tank;

    protected override void Start()
    {
        speed = 1f;
        attackRate = 0.7f;
        totalHealth = 4 * multiplierTotalHp;
        damagePerAttack = 2;
        defense = 4;

        manaCost = 3;
        team = UnitsTeam.Player;
        entityType = EntityType.Basic;
        base.Start();
    }
}