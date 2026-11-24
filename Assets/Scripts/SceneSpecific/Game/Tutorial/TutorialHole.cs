using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Assombrit tout l'écran sauf la zone "target" (4 panneaux noirs autour).
// Si "follow" est renseigné, la zone se cale à chaque image sur ces contrôles réels :
// elle reste juste quand l'ATH déplace ses boutons ou quand la boutique glisse.
public class TutorialHole : MonoBehaviour
{
    [SerializeField] RectTransform target;

    [SerializeField] RectTransform top;
    [SerializeField] RectTransform bottom;
    [SerializeField] RectTransform left;
    [SerializeField] RectTransform right;

    [Header("Suivi d'un contrôle")]
    [Tooltip("Contrôles à laisser visibles. La zone couvre l'union de leurs rectangles (le texte réel pour un TextMeshPro).")]
    [SerializeField] RectTransform[] follow = new RectTransform[0];
    [Tooltip("Utilise les enfants actifs des contrôles suivis (ex. les cartes d'une grille) au lieu du conteneur.")]
    [SerializeField] bool followChildren = false;
    [Tooltip("Partie du contrôle à montrer (0 à 1), quand un seul contrôle est suivi.")]
    [SerializeField] Rect followRegion = new(0, 0, 1, 1);
    [Tooltip("Zone transparente qui bloque les clics dans le trou. Par défaut : l'enfant \"BlockRayCast\".")]
    [SerializeField] RectTransform blocker;

    public float padding = 12f;

    private Canvas parentCanvas;
    private RectTransform canvasRect;
    private readonly Vector3[] corners = new Vector3[4];

    void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null) {
            canvasRect = parentCanvas.GetComponent<RectTransform>();
        }

        if (blocker == null)
            blocker = transform.Find("BlockRayCast") as RectTransform;
    }

    // Appelé par l'ATH pour caler une étape sur un de ses contrôles
    public void Follow(RectTransform control, Rect? region = null)
    {
        follow = control != null ? new[] { control } : new RectTransform[0];
        followChildren = false;
        followRegion = region ?? new Rect(0, 0, 1, 1);
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        if (FollowedBounds(out Vector3 min, out Vector3 max)) {
            PlaceOver(target, min, max);
            if (blocker != null && blocker.gameObject.activeSelf)
                PlaceOver(blocker, min, max);
        }

        UpdateHole();
    }

    // Rectangle couvrant les contrôles suivis, dans l'espace du parent de la cible
    bool FollowedBounds(out Vector3 min, out Vector3 max)
    {
        min = new Vector3(float.MaxValue, float.MaxValue);
        max = new Vector3(float.MinValue, float.MinValue);
        bool found = false;
        Transform space = target.parent;

        foreach (RectTransform control in follow) {
            if (control == null)
                continue;

            if (followChildren) {
                foreach (Transform child in control)
                    if (child.gameObject.activeInHierarchy && child is RectTransform rect)
                        found |= Encapsulate(rect, space, ref min, ref max);
            } else {
                found |= Encapsulate(control, space, ref min, ref max);
            }
        }

        if (found && follow.Length == 1 && !followChildren && followRegion != new Rect(0, 0, 1, 1)) {
            Vector3 size = max - min;
            Vector3 origin = min;
            min = origin + Vector3.Scale(size, new Vector3(followRegion.xMin, followRegion.yMin));
            max = origin + Vector3.Scale(size, new Vector3(followRegion.xMax, followRegion.yMax));
        }
        return found;
    }

    bool Encapsulate(RectTransform control, Transform space, ref Vector3 min, ref Vector3 max)
    {
        // pour un texte, on ne garde que les caractères affichés (pas toute la boîte)
        Rect local = control.rect;
        if (control.TryGetComponent(out TMP_Text text) && !string.IsNullOrEmpty(text.text) && text.textBounds.size.x > 0)
            local = new Rect(text.textBounds.min, text.textBounds.size);

        Vector3[] points = {
            new(local.xMin, local.yMin), new(local.xMin, local.yMax),
            new(local.xMax, local.yMax), new(local.xMax, local.yMin),
        };
        foreach (Vector3 point in points) {
            Vector3 p = space.InverseTransformPoint(control.TransformPoint(point));
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        return true;
    }

    static void PlaceOver(RectTransform rect, Vector3 min, Vector3 max)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.localPosition = (min + max) * 0.5f;
        rect.sizeDelta = new Vector2(max.x - min.x, max.y - min.y);
    }

    void UpdateHole()
    {
        target.GetWorldCorners(corners);

        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(cam, corners[0]), cam, out Vector2 localBottomLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(cam, corners[2]), cam, out Vector2 localTopRight);

        float leftX = localBottomLeft.x - padding;
        float rightX = localTopRight.x + padding;
        float bottomY = localBottomLeft.y - padding;
        float topY = localTopRight.y + padding;

        float canvasWidth = canvasRect.rect.width;

        float canvasLeft = canvasRect.rect.xMin;
        float canvasRight = canvasRect.rect.xMax;
        float canvasBottom = canvasRect.rect.yMin;
        float canvasTop = canvasRect.rect.yMax;

        SetupPanel(top, new Vector2((canvasLeft + canvasRight) / 2f, (topY + canvasTop) / 2f), new Vector2(canvasWidth, canvasTop - topY));
        SetupPanel(bottom, new Vector2((canvasLeft + canvasRight) / 2f, (bottomY + canvasBottom) / 2f), new Vector2(canvasWidth, bottomY - canvasBottom));
        SetupPanel(left, new Vector2((canvasLeft + leftX) / 2f, (topY + bottomY) / 2f), new Vector2(leftX - canvasLeft, topY - bottomY));
        SetupPanel(right, new Vector2((rightX + canvasRight) / 2f, (topY + bottomY) / 2f), new Vector2(canvasRight - rightX, topY - bottomY));

        if (target.TryGetComponent(out Image image))
            image.color = new Color(1f, 1f, 1f, 0f);
    }

    void SetupPanel(RectTransform panel, Vector2 anchoredPosition, Vector2 size)
    {
        if (panel == null) return;

        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);

        panel.anchoredPosition = anchoredPosition;
        panel.sizeDelta = size;
    }
}
