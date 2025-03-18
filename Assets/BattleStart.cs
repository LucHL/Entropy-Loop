using UnityEngine;

public class BattleStart : MonoBehaviour
{
    public void ActivateAllCombatUnits()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject[] champions = GameObject.FindGameObjectsWithTag("Champion");
        Debug.Log("clické");
        foreach (GameObject enemy in enemies)
        {
            enemy.GetComponent<Enemy>().enabled = true;
        }

        foreach (GameObject champion in champions)
        {
            champion.GetComponent<Champion>().enabled = true;
        }
    }
}

