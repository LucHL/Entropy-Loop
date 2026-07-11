using UnityEngine;

public class Paladin : Units
{
    public override UnitsClass unitsClass => UnitsClass.Tank;

    [SerializeField] private float protectionCooldown = 12f;
    [SerializeField] private int protectionHits = 4;

    protected override void Start()
    {
        speed = 1f;
        attackRate = 0.7f;
        totalHealth = 4 * multiplierTotalHp;
        damagePerAttack = 2;
        defense = 4;

        manaCost = 3;
        team = UnitsTeam.Player;
        entityType = EntityType.Basic;
        base.Start();
    }

    protected override void Passif()
    {
        passifTimer += Time.deltaTime;

        if (passifTimer < protectionCooldown)
            return;

        passifTimer = 0f;
        protectedHits = protectionHits;
        protectedHits = Mathf.Min(protectedHits, 4);

        BugTracker.Info("'" + name + "' activated Protection");
        GameLogManager.Instance.AddLog("Paladin obtient Protection");
    }
}