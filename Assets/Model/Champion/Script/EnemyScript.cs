using UnityEngine;

public class Enemy : Units
{
    protected override void Start()
    {
        speed = 3f;
        health = 150;
        damagePerSecond = 15;
        entityTag = "Champion";
        base.Start();
    }

    protected override void Attack()
    {
        if (target == null)
            return;

        animator.SetTrigger("AttackTrigger");
        target.GetComponent<Champion>().receiveDamage(damagePerSecond * speed * Time.deltaTime);

        if (attackEffect != null)
        {
            GameObject effect = Instantiate(attackEffect, target.transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
}
