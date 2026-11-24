using System.Collections;
using UnityEngine;

// Panneau UI qui glisse depuis un bord de l'écran
[RequireComponent(typeof(RectTransform))]
public class SlidingPanel : MonoBehaviour
{
    public enum Side { Left, Right }

    [SerializeField] Side side = Side.Right;
    [SerializeField] float duration = 0.35f;

    public bool IsOpen { get; private set; }

    private RectTransform rect;
    private Vector2 openPosition;
    private Coroutine slideCoroutine;
    private bool initialized = false;

    private void Init()
    {
        if (initialized)
            return;

        rect = GetComponent<RectTransform>();
        openPosition = rect.anchoredPosition;
        initialized = true;
    }

    private Vector2 ClosedPosition()
    {
        float offset = rect.rect.width + 50f;
        return openPosition + new Vector2(side == Side.Right ? offset : -offset, 0f);
    }

    public void SnapClosed()
    {
        Init();
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        IsOpen = false;
        rect.anchoredPosition = ClosedPosition();
        gameObject.SetActive(false);
    }

    public void Open()
    {
        Init();
        IsOpen = true;
        gameObject.SetActive(true);
        Slide(openPosition, false);
    }

    public void Close()
    {
        Init();
        IsOpen = false;
        if (!gameObject.activeInHierarchy) {
            SnapClosed();
            return;
        }
        Slide(ClosedPosition(), true);
    }

    private void Slide(Vector2 target, bool disableAtEnd)
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlideTo(target, disableAtEnd));
    }

    private IEnumerator SlideTo(Vector2 target, bool disableAtEnd)
    {
        Vector2 start = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f); // ease out cubic
            rect.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
            yield return null;
        }

        rect.anchoredPosition = target;
        slideCoroutine = null;

        if (disableAtEnd)
            gameObject.SetActive(false);
    }
}
