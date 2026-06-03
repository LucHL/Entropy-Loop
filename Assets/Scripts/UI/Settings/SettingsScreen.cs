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
    [SerializeField] Slider masterVolume;
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
    }

    /* AUDIO */

    public void ChangeMasterVolume()
    {
        MainAudioMixer.SetFloat("MasterVolume", masterVolume.value);
    }

    public void ChangeMusicVolume()
    {
        MainAudioMixer.SetFloat("MusicVolume", MusicVolume.value);
    }

    public void ChangeSfxVolume()
    {
        MainAudioMixer.SetFloat("SfxVolume", SfxVolume.value);
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
}
