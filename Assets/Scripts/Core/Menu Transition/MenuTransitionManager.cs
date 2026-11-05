using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class MenuTransitionManager : MonoBehaviour
{
    [Header("Video Players")]
    [SerializeField] VideoPlayer loopVideoPlayer;
    [SerializeField] VideoClip mainLoopClip;
    [SerializeField] VideoClip transitionClip;
    [SerializeField] VideoClip reverseTransitionClip;

    [Header("UI Elements")]
    [SerializeField] GameObject levelSelection;
    [SerializeField] RectTransform[] buttonsToMove; 
    [SerializeField] float targetYOffset = 1200f;

    private bool isTransitioning = false;
    private Vector2[] initialPositions;

    void Start()
    {
        if (buttonsToMove != null && buttonsToMove.Length > 0) {
            initialPositions = new Vector2[buttonsToMove.Length];
            for (int i = 0; i < buttonsToMove.Length; i++) {
                if (buttonsToMove[i] != null) {
                    initialPositions[i] = buttonsToMove[i].anchoredPosition;
                }
            }
        }
    }

    public void OnPlayButtonClicked()
    {
        if (isTransitioning)
            return;

        StartCoroutine(PlayTransitionRoutine());
    }

    public void OnPlayBackButtonClicked()
    {
        if (!isTransitioning)
            return;

        StartCoroutine(PlayReverseTransitionRoutine());
    }

    private IEnumerator PlayTransitionRoutine()
    {
        isTransitioning = true;

        loopVideoPlayer.Stop();
        loopVideoPlayer.clip = transitionClip;
        loopVideoPlayer.Prepare();

        while (!loopVideoPlayer.isPrepared) {
            yield return null;
        }

        loopVideoPlayer.playbackSpeed = 1;
        loopVideoPlayer.isLooping = false;

        loopVideoPlayer.Play();

        float duration = (float)loopVideoPlayer.length;
        float elapsed = 0f;

        Vector2[] targetPositions = new Vector2[buttonsToMove.Length];
        for (int i = 0; i < buttonsToMove.Length; i++) {
            if (buttonsToMove[i] != null) {
                targetPositions[i] = initialPositions[i] + new Vector2(0, targetYOffset);
            }
        }

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (buttonsToMove != null) {
                for (int i = 0; i < buttonsToMove.Length; i++) {
                    if (buttonsToMove[i] != null) {
                        buttonsToMove[i].anchoredPosition = Vector2.Lerp(initialPositions[i], targetPositions[i], smoothT);
                    }
                }
            }
            
            yield return null;
        }

        levelSelection.gameObject.SetActive(true);
    }

    private IEnumerator PlayReverseTransitionRoutine()
    {
        isTransitioning = true;

        if (levelSelection != null)
            levelSelection.SetActive(false);

        loopVideoPlayer.Stop();
        loopVideoPlayer.clip = reverseTransitionClip;
        loopVideoPlayer.Prepare();

        while (!loopVideoPlayer.isPrepared) {
            yield return null;
        }

        loopVideoPlayer.playbackSpeed = 1f;
        loopVideoPlayer.isLooping = false;
        loopVideoPlayer.Play();

        float duration = (float)loopVideoPlayer.length;
        float elapsed = 0f;

        Vector2[] targetPositions = new Vector2[buttonsToMove.Length];
        for (int i = 0; i < buttonsToMove.Length; i++) {
            if (buttonsToMove[i] != null) {
                targetPositions[i] = initialPositions[i] + new Vector2(0, targetYOffset);
            }
        }

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / duration);
            
            float t = 1f - rawT;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (buttonsToMove != null) {
                for (int i = 0; i < buttonsToMove.Length; i++) {
                    if (buttonsToMove[i] != null) {
                        buttonsToMove[i].anchoredPosition = Vector2.Lerp(initialPositions[i], targetPositions[i], smoothT);
                    }
                }
            }
            
            yield return null;
        }

        for (int i = 0; i < buttonsToMove.Length; i++) {
            if (buttonsToMove[i] != null) {
                buttonsToMove[i].anchoredPosition = initialPositions[i];
            }
        }

        loopVideoPlayer.clip = mainLoopClip;
        loopVideoPlayer.isLooping = true;
        loopVideoPlayer.playbackSpeed = 0.1f;
        loopVideoPlayer.Play();

        isTransitioning = false;
    }
}
