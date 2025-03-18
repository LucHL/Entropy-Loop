using UnityEngine;

public class Enemy : Units
{
    protected override void Start()
    {
        speed = 3f;
        health = 150;
        damagePerAttack = 15;
        entityTag = "Champion";
        base.Start();
    }
}
