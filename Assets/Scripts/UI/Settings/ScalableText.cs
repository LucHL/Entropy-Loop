using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ScalableText : MonoBehaviour
{
    private TextMeshProUGUI text;
    private float baseFontSize;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        baseFontSize = text.fontSize;
    }

    void Start()
    {
        if (AccessibilityManager.Instance != null)
        {
            ApplyScale(AccessibilityManager.Instance.FontScale);
            AccessibilityManager.Instance.OnFontScaleChanged += ApplyScale;
        }
    }

    void OnDestroy()
    {
        if (AccessibilityManager.Instance != null)
            AccessibilityManager.Instance.OnFontScaleChanged -= ApplyScale;
    }

    void ApplyScale(float scale)
    {
        if (text != null)
            text.fontSize = baseFontSize * scale;
    }
}