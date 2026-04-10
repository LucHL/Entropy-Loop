using UnityEngine;

public class Tiger : Units
{
    protected override void Start()
    {
        attackRate = 2f;
        totalHealth = 200;
        damagePerAttack = 25;
        manaCost = 5;
        enemyTag = "Champion";
        entityType = EntityType.Basic;
        base.Start();
    }
}
