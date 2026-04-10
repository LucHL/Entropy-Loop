using System.Collections;
using UnityEngine;

public class SpawnParticule : MonoBehaviour
{
    public static SpawnParticule instance;

    void Awake()
    {
        instance = this;
    }

    public void Init(GameObject particule, Transform t, float duration)
    {
        StartCoroutine(HandleParticule(particule, t, duration));
    }

    private IEnumerator HandleParticule(GameObject particule, Transform t, float duration)
    {
        GameObject p = Instantiate(particule, t);
        yield return new WaitForSeconds(duration);
        Destroy(p);
    }
}
