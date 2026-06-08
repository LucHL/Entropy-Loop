using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsScript : MonoBehaviour
{
    [SerializeField] Toggle fullscreenTog;
    [SerializeField] Toggle vsincTog;

    [Header("GRAPHIC")]
    [SerializeField] TMP_Dropdown graphicsDropdown;

    [Header("AUDIO")]
    [SerializeField] Slider MasterVolume;
    [SerializeField] Slider MusicVolume;
    [SerializeField] Slider SfxVolume;
    [SerializeField] AudioMixer MainAudioMixer;

    [Header("AUDIO")]
    [SerializeField] TextMeshProUGUI logFilePath;

    void Awake()
    {
        logFilePath.text = BugTracker.logPath;
        // fullscreenTog.isOn = Screen.fullScreen;

        // if (QualitySettings.vSyncCount == 0)
        //     vsincTog.isOn = false;
        // else
        //     vsincTog.isOn = true;

        float volume;
        MainAudioMixer.GetFloat("MasterVolume", out volume);
        MasterVolume.value = Mathf.Pow(10f, volume / 20f);

        MainAudioMixer.GetFloat("MusicVolume", out volume);
        MusicVolume.value = Mathf.Pow(10f, volume / 20f);

        MainAudioMixer.GetFloat("SfxVolume", out volume);
        SfxVolume.value = Mathf.Pow(10f, volume / 20f);
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
        // Screen.fullScreen = fullscreenTog;

        // if (vsincTog.isOn)
        //     QualitySettings.vSyncCount = 1;
        // else
        //     QualitySettings.vSyncCount = 0;
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
}
