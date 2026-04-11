using UnityEngine;

public class Paladin : Units
{
    protected override void Start()
    {
        speed = 1f;
        totalHealth = 100;
        damagePerAttack = 10;
        manaCost = 3;
        team = UnitsTeam.Player;
        entityType = EntityType.Basic;
        base.Start();
    }
}