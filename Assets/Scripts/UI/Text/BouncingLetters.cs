using TMPro;
using UnityEngine;

public class BouncingLetters : MonoBehaviour
{
    public float bounceHeight = 7f;
    public float bounceSpeed = 5f;
    public float letterDelay = 5f;

    private TMP_Text[] text;
    private Vector3[] originalPositions;

    void Start()
    {
        text = GetComponentsInChildren<TMP_Text>();
        originalPositions = new Vector3[text.Length];

        for (int i = 0; i < text.Length; i++)
        {
            originalPositions[i] = text[i].rectTransform.localPosition;
        }
    }

    void Update()
    {
        for (int i = 0; i < text.Length; i++)
        {
            float time = Time.time * bounceSpeed + i * letterDelay;
            float bounce = Mathf.Sin(time) * bounceHeight;

            Vector3 newPos = originalPositions[i] + new Vector3(0, bounce, 0);
            text[i].rectTransform.localPosition = newPos;
        }
    }
}
