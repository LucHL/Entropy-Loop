using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float duration = 2f;
    public float moveSpeed = 50f;

    private TextMeshProUGUI text;
    private Transform transformChild;
    
    void Awake()
    {
        text = gameObject.GetComponentInChildren<TextMeshProUGUI>();
        transformChild = gameObject.GetComponentInChildren<Transform>();
    }

    public void Init(string message)
    {
        text.text = message;
        StartCoroutine(AnimateText());
    }

    private IEnumerator AnimateText()
    {
        float timer = 0f;

        while (timer < duration) {
            float t = timer / duration;

            text.gameObject.transform.Translate(moveSpeed * Time.deltaTime * Vector3.up);

            text.alpha = 1 - t;
            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
