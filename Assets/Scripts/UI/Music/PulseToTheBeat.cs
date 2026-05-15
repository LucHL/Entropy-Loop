using UnityEngine;
using System.Collections;

public class PulseToTheBeat : MonoBehaviour
{
    [SerializeField] bool testBeat;
    [SerializeField] float pulseSize = 1.15f;
    [SerializeField] float returnSpeed = 5f;
    private Vector3 startSize;
    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startSize = rectTransform.localScale;
        if (testBeat)
        {
            StartCoroutine(TestBeat());
        }
    }

    private void Update()
    {
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, startSize, Time.deltaTime * returnSpeed);
    }

    public void Pulse()
    {
        rectTransform.localScale = startSize * pulseSize;
    }

    IEnumerator TestBeat()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            Pulse();
        }
    }
}
