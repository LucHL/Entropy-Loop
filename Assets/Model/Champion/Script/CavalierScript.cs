using UnityEngine;

public class Cavalier : Units
{
    protected override void Start()
    {
        speed = 3f;
        health = 100;
        damagePerAttack = 10;
        attackRange = 50;
        entityTag = "Enemy";
        base.Start();
    }
}