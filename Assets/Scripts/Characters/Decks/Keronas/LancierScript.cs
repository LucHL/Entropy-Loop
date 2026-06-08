using UnityEngine;

public class Lancier : Units
{
    public override UnitsClass unitsClass => UnitsClass.Support;

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
        base.Start();
    }
}
