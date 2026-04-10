using UnityEngine;

public class Enemy : Units
{
    protected override void Start()
    {
        speed = 1f;
        totalHealth = 200;
        damagePerAttack = 15;
        enemyTag = "Champion";
        entityType = EntityType.Basic;
        base.Start();
    }
}
