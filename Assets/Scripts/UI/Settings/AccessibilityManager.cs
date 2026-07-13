using UnityEngine;
using UnityEngine.SceneManagement;

public class AccessibilityManager : MonoBehaviour
{
    public event System.Action<float> OnFontScaleChanged;
    public static AccessibilityManager Instance { get; private set; }

    private const string COLORBLIND_KEY = "colorblind_mode";
    private const string FONTSCALE_KEY = "font_scale";

    public ColorBlindFilter.Mode ColorBlindMode { get; private set; }
    public float FontScale { get; private set; } = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
        Debug.Log("[Accessibility] AccessibilityManager initialisé !");
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyColorBlindMode();
    }

    void LoadSettings()
    {
        ColorBlindMode = (ColorBlindFilter.Mode)PlayerPrefs.GetInt(COLORBLIND_KEY, 0);
        FontScale = PlayerPrefs.GetFloat(FONTSCALE_KEY, 1f);
        BugTracker.Info($"[Accessibility] Réglages chargés : daltonisme={ColorBlindMode}, police={FontScale}.");
    }

    public void SetColorBlindMode(ColorBlindFilter.Mode mode)
    {
        ColorBlindMode = mode;
        PlayerPrefs.SetInt(COLORBLIND_KEY, (int)mode);
        PlayerPrefs.Save();
        ApplyColorBlindMode();
    }

    public void SetFontScale(float scale)
    {
        FontScale = scale;
        PlayerPrefs.SetFloat(FONTSCALE_KEY, scale);
        PlayerPrefs.Save();
        OnFontScaleChanged?.Invoke(scale);
        BugTracker.Info($"[Accessibility] Taille de police : {scale}.");
    }

    public void ApplyColorBlindMode()
    {
        ColorBlindFilter filter = Camera.main != null ? Camera.main.GetComponent<ColorBlindFilter>() : null;
        if (filter != null)
            filter.SetMode(ColorBlindMode);
        else
            BugTracker.Warning("[Accessibility] Aucun ColorBlindFilter trouvé sur la caméra principale.");
    }
}
