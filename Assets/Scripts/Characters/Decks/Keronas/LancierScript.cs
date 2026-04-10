using UnityEngine;

public class Lancier : Units
{
    protected override void Start()
    {
        speed = 1f;
        totalHealth = 100;
        damagePerAttack = 7;
        attackRange = 5f;
        manaCost = 3;
        enemyTag = "Enemy";
        entityType = EntityType.Basic;
        base.Start();
    }
}
