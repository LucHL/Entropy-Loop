using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;

public class SettingsScript : MonoBehaviour
{
    [SerializeField] Toggle fullscreenTog;
    [SerializeField] Toggle vsincTog;

    [Header("GRAPHIC")]
    [SerializeField] TMP_Dropdown graphicsDropdown;
    [SerializeField] TMP_Dropdown resolutionDropdown;

    [Header("AUDIO")]
    [SerializeField] Slider MasterVolume;
    [SerializeField] Slider MusicVolume;
    [SerializeField] Slider SfxVolume;
    [SerializeField] AudioMixer MainAudioMixer;

    [Header("ACCESSIBILITY")]
    [SerializeField] TMP_Dropdown colorBlindDropdown;
    [SerializeField] Slider fontScaleSlider;

    [Header("SYSTEM")]
    [SerializeField] TextMeshProUGUI logFilePath;

    private Resolution[] resolutions;
    private const string RESOLUTION_KEY = "resolution_index";

    void Awake()
    {
        logFilePath.text = BugTracker.logPath;

        float volume;
        MainAudioMixer.GetFloat("MasterVolume", out volume);
        MasterVolume.value = Mathf.Pow(10f, volume / 20f);
        MainAudioMixer.GetFloat("MusicVolume", out volume);
        MusicVolume.value = Mathf.Pow(10f, volume / 20f);
        MainAudioMixer.GetFloat("SfxVolume", out volume);
        SfxVolume.value = Mathf.Pow(10f, volume / 20f);
    }

    void Start()
    {
        // Initialise les contrôles d'accessibilité avec les valeurs sauvegardées
        if (AccessibilityManager.Instance != null)
        {
            if (colorBlindDropdown != null)
                colorBlindDropdown.value = (int)AccessibilityManager.Instance.ColorBlindMode;
            if (fontScaleSlider != null)
                fontScaleSlider.value = AccessibilityManager.Instance.FontScale;
        }

        SetupResolutionDropdown();
    }

    /* AUDIO */
    public void ChangeMasterVolume()
    {
        MainAudioMixer.SetFloat("MasterVolume", Mathf.Log10(MasterVolume.value) * 20f);
    }
    public void ChangeMusicVolume()
    {
        MainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(MusicVolume.value) * 20f);
    }
    public void ChangeSfxVolume()
    {
        MainAudioMixer.SetFloat("SfxVolume", Mathf.Log10(SfxVolume.value) * 20f);
    }

    /* GRAPHICS */
    public void ChangeGraphicsQuality()
    {
        QualitySettings.SetQualityLevel(graphicsDropdown.value);
    }
    public void SetFullScreen(bool fullScreen)
    {
        Screen.fullScreen = fullScreen;
    }
    public void ApplyGraphics()
    {
    }

    void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            int hz = Mathf.RoundToInt((float)resolutions[i].refreshRateRatio.value);
            string option = $"{resolutions[i].width} x {resolutions[i].height} @ {hz}Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentIndex = i;
        }

        resolutionDropdown.AddOptions(options);

        int savedIndex = PlayerPrefs.GetInt(RESOLUTION_KEY, currentIndex);
        savedIndex = Mathf.Clamp(savedIndex, 0, resolutions.Length - 1);
        resolutionDropdown.value = savedIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void ChangeResolution()
    {
        if (resolutions == null || resolutionDropdown == null) return;

        Resolution res = resolutions[resolutionDropdown.value];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);

        PlayerPrefs.SetInt(RESOLUTION_KEY, resolutionDropdown.value);
        PlayerPrefs.Save();

        BugTracker.Info($"[Settings] Résolution changée : {res.width}x{res.height}.");
    }

    /* ACCESSIBILITY */
    public void ChangeColorBlindMode()
    {
        if (AccessibilityManager.Instance != null)
            AccessibilityManager.Instance.SetColorBlindMode((ColorBlindFilter.Mode)colorBlindDropdown.value);
    }
    public void ChangeFontScale()
    {
        BugTracker.Info($"[Settings] ChangeFontScale appelé : {fontScaleSlider.value}");
        if (AccessibilityManager.Instance != null)
            AccessibilityManager.Instance.SetFontScale(fontScaleSlider.value);
        else
            BugTracker.Warning("[Settings] AccessibilityManager.Instance est null !");
    }

    public void CloseSettingsScreen()
    {
        GameManager.instance.ResumeGame();
    }

    /* SYSTEM */
    public void ResetLogFile()
    {
        BugTracker.ResetBugTrackerFile();
    }
    public void ResetSaveFile()
    {
        SaveSystem.ResetFile();
    }
}