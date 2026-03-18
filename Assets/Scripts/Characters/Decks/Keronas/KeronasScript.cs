using UnityEngine;

public class Keronas : Units
{
    [SerializeField] GameObject particulCapacite;
    [SerializeField] AudioClip particulSound;

    private readonly float healCapacite = -50;

    protected override void Start()
    {
        speed = 0.5f;
        attackRate = 2f;
        totalHealth = 150;
        damagePerAttack = 25;
        manaCost = 5;
        enemyTag = "Enemy";
        entityType = EntityType.Champion;
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
            e.GetComponentInParent<Units>().TakeDamage(healCapacite);
            e.GetComponentInParent<Units>().damagePerAttack += 10;
        }

        PlaySound(particulSound);

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
