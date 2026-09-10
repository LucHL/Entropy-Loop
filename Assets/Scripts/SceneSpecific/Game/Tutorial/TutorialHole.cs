using UnityEngine;
using UnityEngine.UI;

public class TutorialHole : MonoBehaviour
{
    [SerializeField] RectTransform target;

    [SerializeField] RectTransform top;
    [SerializeField] RectTransform bottom;
    [SerializeField] RectTransform left;
    [SerializeField] RectTransform right;

    public float padding = 20f;

    private Canvas parentCanvas;
    private RectTransform canvasRect;

    void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null) {
            canvasRect = parentCanvas.GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        if (target == null)
            return;

        UpdateHole();
    }

    void UpdateHole()
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(cam, corners[0]), cam, out Vector2 localBottomLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(cam, corners[2]), cam, out Vector2 localTopRight);

        float leftX = localBottomLeft.x - padding;
        float rightX = localTopRight.x + padding;
        float bottomY = localBottomLeft.y - padding;
        float topY = localTopRight.y + padding;

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        float canvasLeft = canvasRect.rect.xMin;
        float canvasRight = canvasRect.rect.xMax;
        float canvasBottom = canvasRect.rect.yMin;
        float canvasTop = canvasRect.rect.yMax;

        SetupPanel(top, new Vector2((canvasLeft + canvasRight) / 2f, (topY + canvasTop) / 2f), new Vector2(canvasWidth, canvasTop - topY));
        SetupPanel(bottom, new Vector2((canvasLeft + canvasRight) / 2f, (bottomY + canvasBottom) / 2f), new Vector2(canvasWidth, bottomY - canvasBottom));
        SetupPanel(left, new Vector2((canvasLeft + leftX) / 2f, (topY + bottomY) / 2f), new Vector2(leftX - canvasLeft, topY - bottomY));
        SetupPanel(right, new Vector2((rightX + canvasRight) / 2f, (topY + bottomY) / 2f), new Vector2(canvasRight - rightX, topY - bottomY));

        target.gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
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
