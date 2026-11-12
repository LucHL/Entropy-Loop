using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Anime une page qui se tourne autour de la reliure d'un livre ouvert.
// La page est une feuille vierge : le contenu caché dessous est changé pendant qu'elle le recouvre.
public class BookPageTurn : MonoBehaviour
{
    [SerializeField] RectTransform turningPage;
    [SerializeField] Image turningPageImage;
    [SerializeField] float duration = 0.5f;
    [SerializeField] Color shadeColor = new(0.35f, 0.35f, 0.35f, 1f);

    public bool IsTurning { get; private set; }

    private Color pageColor;

    void Awake()
    {
        pageColor = turningPageImage.color;
        turningPage.gameObject.SetActive(false);
    }

    // forward : la page de droite passe à gauche (deck suivant)
    // onCovered : appelé quand la page recouvre la moitié de départ
    // onLanded : appelé quand la page recouvre la moitié d'arrivée, juste avant de disparaître
    public void Play(bool forward, Action onCovered, Action onLanded)
    {
        if (IsTurning)
            return;

        StartCoroutine(Turn(forward, onCovered, onLanded));
    }

    private IEnumerator Turn(bool forward, Action onCovered, Action onLanded)
    {
        IsTurning = true;
        turningPage.gameObject.SetActive(true);

        // 1re moitié : la page se soulève depuis le côté de départ
        PlaceOnSide(forward);
        onCovered?.Invoke();
        yield return Fold(1f, 0f, true);

        // 2e moitié : la page retombe de l'autre côté de la reliure
        PlaceOnSide(!forward);
        yield return Fold(0f, 1f, false);
        onLanded?.Invoke();

        turningPage.gameObject.SetActive(false);
        turningPageImage.color = pageColor;
        IsTurning = false;
    }

    // Place la page sur la moitié droite (pivot sur la reliure, à gauche) ou gauche
    private void PlaceOnSide(bool rightSide)
    {
        turningPage.anchorMin = new Vector2(rightSide ? 0.5f : 0f, 0f);
        turningPage.anchorMax = new Vector2(rightSide ? 1f : 0.5f, 1f);
        turningPage.pivot = new Vector2(rightSide ? 0f : 1f, 0.5f);
        turningPage.offsetMin = Vector2.zero;
        turningPage.offsetMax = Vector2.zero;
    }

    private IEnumerator Fold(float from, float to, bool easeIn)
    {
        float halfDuration = duration / 2f;
        float elapsed = 0f;

        while (elapsed < halfDuration) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            t = easeIn ? t * t : 1f - (1f - t) * (1f - t);
            float scale = Mathf.Lerp(from, to, t);

            turningPage.localScale = new Vector3(scale, 1f, 1f);
            // la page s'assombrit quand elle est vue de profil
            turningPageImage.color = Color.Lerp(shadeColor * pageColor, pageColor, scale);
            yield return null;
        }

        turningPage.localScale = new Vector3(to, 1f, 1f);
    }
}
