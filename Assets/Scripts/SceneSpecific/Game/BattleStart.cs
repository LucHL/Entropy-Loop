using UnityEngine;

public class BattleStart : MonoBehaviour
{
    public void ActivateAllCombatUnits()
    {
        GameObject[] entities = GameObject.FindGameObjectsWithTag("Entities");
        if (entities == null)
            return;

        foreach (GameObject e in entities) {
            e.GetComponentInChildren<Units>().enabled = true;
        }
    }
}
