using UnityEngine;

public class Keronas : Units
{
    protected override void Start()
    {
        speed = 1.5f;
        totalHealth = 150;
        damagePerAttack = 25;
        entityTag = "Enemy";
        base.Start();
    }

    protected override void Capacite()
    {
        if (isCapaciteAlreadyUse)
            return;
        if (hp > totalHealth / 2)
            return;

        isCapaciteAlreadyUse = true;
        GameObject[] champions = GameObject.FindGameObjectsWithTag("Champion");
        foreach (GameObject e in champions) {
            e.GetComponentInParent<Units>().TakeDamage(-50);
            e.GetComponentInParent<Units>().damagePerAttack += 10;
        }
        Invoke(nameof(ResetDamagePerAttack), 5f);
    }

    private void ResetDamagePerAttack()
    {
        GameObject[] champions = GameObject.FindGameObjectsWithTag("Champion");
        foreach (GameObject e in champions) {
            e.GetComponentInParent<Units>().damagePerAttack -= 10;
        }
    }
}
