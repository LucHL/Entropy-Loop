using UnityEngine;

public class Paladin : Units
{
    protected override void Start()
    {
        speed = 1f;
        totalHealth = 100;
        damagePerAttack = 10;
        enemyTag = "Enemy";
        base.Start();
    }
}