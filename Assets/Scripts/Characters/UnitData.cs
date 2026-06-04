using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "UnitData")]
public class UnitData : ScriptableObject
{
    public GameObject prefab;

    [Header("Stats")]
    public float damagePerAttack = 10f;
    public int manaCost = 3;
    public float totalHealth = 100f;
    public float totalShield = 0f;
    public float attackRate = 1f; // 1f = 1s
    public float attackRange = 1f; // 1f = 1 tile
    public float speed = 1f;
    public EntityType entityType = EntityType.Basic;
    public List<UnitsClass> unitsClass = new();

    [Header("Balance & Reward")]
    public int difficultyCost = 2;
    public int rewardValue = 50;
}
