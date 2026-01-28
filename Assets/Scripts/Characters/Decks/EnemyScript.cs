using UnityEngine;

public class Enemy : Units
{
    protected override void Start()
    {
        speed = 1f;
        totalHealth = 100;
        damagePerAttack = 10;
        enemyTag = "Champion";
        base.Start();
    }
}
