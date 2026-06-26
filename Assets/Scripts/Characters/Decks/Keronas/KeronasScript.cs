using UnityEngine;

public class Keronas : Units
{
    public override UnitsClass unitsClass => UnitsClass.Dps;

    [Header("Specific to Keronas")]
    [SerializeField] GameObject particulCapacite;
    // [SerializeField] AudioClip CapaciteSound = null;

    //private readonly float healCapacite = -50;

    protected override void Start()
    {
        speed = 1f;
        attackRate = 1.5f;
        totalHealth = 8 * multiplierTotalHp;
        damagePerAttack = 4;
        defense = 4;

        manaCost = 5;
        team = UnitsTeam.Player;
        entityType = EntityType.Champion;
        EntityHasCapacity = true;
        base.Start();
    }

    protected override void Capacite()
    {
        //if (hp > totalHealth / 2)
            //return;
        capacityPoints = 0f;

        GameLogManager.Instance.AddLog("Keronas utilise sa capacité");

        GameObject[] champions = GameObject.FindGameObjectsWithTag("Champion");
        foreach (GameObject e in champions) {
            if (!e.GetComponentInParent<Units>().isAlive)
                continue;

            //e.GetComponentInParent<Units>().TakeDamage(healCapacite);
            e.GetComponentInParent<Units>().damagePerAttack += 10;
            e.GetComponentInParent<Units>().attackRate += 1f;
            e.GetComponentInParent<Units>().totalHealth += 2;

            SpawnParticuleManager.instance.Init(particulCapacite, transform, 2f); //spawn animation particule
        }

        // PlaySound(CapaciteSound);

        Invoke(nameof(ResetDamagePerAttack), 30f);
    }

    private void ResetDamagePerAttack()
    {
        GameObject[] champions = GameObject.FindGameObjectsWithTag("Champion");
        foreach (GameObject e in champions) {
            e.GetComponentInParent<Units>().damagePerAttack -= 10;
            e.GetComponentInParent<Units>().attackRate -= 1f;
            e.GetComponentInParent<Units>().totalHealth -= 2;
        }
    }
}
