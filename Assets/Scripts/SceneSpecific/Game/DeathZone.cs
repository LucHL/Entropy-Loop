using UnityEngine;

public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Units units = other.gameObject.GetComponent<Units>();
        if (units == null)
            return;

        units.Die();
        BugTracker.Info("Entity '" + other.gameObject.name + "' has been killed by DeathZone.");
    }
}
