using UnityEngine;

public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        other.gameObject.GetComponent<Units>().Die();
        BugTracker.Info("Entity '" + other.gameObject.name + "' has been killed by DeathZone.");
    }
}
