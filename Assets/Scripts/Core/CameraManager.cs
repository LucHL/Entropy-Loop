using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public GameObject gameplayCamera;
    public GameObject freeCamera;

    bool freeCamEnabled = false;

    void Start()
    {
        gameplayCamera.SetActive(true);
        freeCamera.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCamera();
        }
    }

    void ToggleCamera()
    {
        freeCamEnabled = !freeCamEnabled;

        gameplayCamera.SetActive(!freeCamEnabled);
        freeCamera.SetActive(freeCamEnabled);

        Cursor.lockState = freeCamEnabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !freeCamEnabled;
    }
}
