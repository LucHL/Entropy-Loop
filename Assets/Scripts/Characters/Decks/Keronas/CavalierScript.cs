using UnityEngine;

public class Cavalier : Units
{
    public override UnitsClass unitsClass => UnitsClass.Dps;

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
}