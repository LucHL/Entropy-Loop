using UnityEngine;
using UnityEngine.UI;

public class SettingsScript : MonoBehaviour
{
    public Toggle fullscreenTog;
    public Toggle vsincTog;

    void Start()
    {
        // fullscreenTog.isOn = Screen.fullScreen;

        // if (QualitySettings.vSyncCount == 0)
        //     vsincTog.isOn = false;
        // else
        //     vsincTog.isOn = true;
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
