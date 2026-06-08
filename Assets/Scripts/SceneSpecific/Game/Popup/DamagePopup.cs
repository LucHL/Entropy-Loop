using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    private float timer;
    private float duration = 0.8f;
    private Vector3 startPos;
    private Vector3 randomOffset;

    public void Setup(float damage)
    {
        textMesh.text = damage.ToString();
        startPos = transform.position;
        randomOffset = new Vector3(Random.Range(-1f, 1f), 1.5f, 0);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;

        transform.position = startPos + (randomOffset * progress) + (Vector3.up * Mathf.Sin(progress * Mathf.PI));

        if (progress > 0.5f) {
            textMesh.alpha = 1f - ((progress - 0.5f) * 2f);
        }

        if (timer >= duration)
            Destroy(gameObject);
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.forward);
    }
}
