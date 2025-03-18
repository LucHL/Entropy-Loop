using UnityEngine;

public class Keronas : Units
{
    protected override void Start()
    {
        speed = 5f;
        health = 200;
        damagePerAttack = 25;
        entityTag = "Enemy";
        base.Start();
    }
}
