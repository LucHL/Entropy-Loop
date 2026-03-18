using System.Collections;
using UnityEngine;

public class SpawnParticule : MonoBehaviour
{
    public float duration = 2f;

    private GameObject particule;

    public void Init(string message)
    {
        StartCoroutine(AnimateParticule());
    }

    private IEnumerator AnimateParticule()
    {
        float timer = 0f;

        while (timer < duration) {
            float t = timer / duration;

            // particule

            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
