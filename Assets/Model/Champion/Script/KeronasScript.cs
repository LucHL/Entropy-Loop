using UnityEngine;

public class Keronas : Units
{
    protected override void Start()
    {
        speed = 3f;
        totalHealth = 150;
        damagePerAttack = 25;
        entityTag = "Enemy";
        base.Start();
    }

    protected override void Capacite()
    {
        if (isCapaciteAlreadyUse)
            return;
        if (pv > totalHealth / 2)
            return;

        isCapaciteAlreadyUse = true;
        GameObject[] champions = GameObject.FindGameObjectsWithTag("Champion");
        foreach (GameObject e in champions) {
            e.GetComponent<Units>().pv += 50;
            e.GetComponent<Units>().damagePerAttack += 10;
            e.GetComponent<Units>().slider.value = pv / 100;
        }
        Invoke(nameof(ResetDamagePerAttack), 5f);
    }

    private void ResetDamagePerAttack()
    {
        GameObject[] champions = GameObject.FindGameObjectsWithTag("Champion");
        foreach (GameObject e in champions) {
            e.GetComponent<Units>().damagePerAttack -= 10;
        }
    }
}
