using UnityEngine;

public class Enemy : Units
{
    protected override void Start()
    {
        // speed = 3f;
        // totalHealth = 150;
        // damagePerAttack = 15;
        entityTag = "Champion";
        base.Start();
    }
}
