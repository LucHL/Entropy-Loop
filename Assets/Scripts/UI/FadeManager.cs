using UnityEngine;
using System.Collections;
using System;

public class FadeManager : MonoBehaviour
{
    public static FadeManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }

        instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    public void FadeOut(CanvasGroup canvasGroup, float duration, Action onComplete = null)
    {
        if (canvasGroup == null)
            return;

        StartCoroutine(FadeOutRoutine(canvasGroup, duration, onComplete));
    }

    public void FadeIn(CanvasGroup canvasGroup, float duration, Action onComplete = null)
    {
        if (canvasGroup == null)
            return;

        StartCoroutine(FadeInRoutine(canvasGroup, duration, onComplete));
    }

    private IEnumerator FadeOutRoutine(CanvasGroup canvasGroup, float duration, Action onComplete)
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        onComplete?.Invoke();
    }

    private IEnumerator FadeInRoutine(CanvasGroup canvasGroup, float duration, Action onComplete)
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        canvasGroup.gameObject.SetActive(true);

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        onComplete?.Invoke();
    }
}
