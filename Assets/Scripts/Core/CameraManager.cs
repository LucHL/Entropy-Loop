using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    public GameObject gameplayCamera;
    public GameObject freeCamera;

    bool freeCamEnabled = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        SetGameplayCamera();
    }

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.C))
        //     ToggleCamera();
    }

    // ── API publique ───────────────────────────────────────────────────────

    public void SetGameplayCamera()
    {
        freeCamEnabled = false;
        gameplayCamera.SetActive(true);
        freeCamera.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetFreeCamera()
    {
        freeCamEnabled = true;
        gameplayCamera.SetActive(false);
        freeCamera.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ToggleCamera()
    {
        if (freeCamEnabled) SetGameplayCamera();
        else SetFreeCamera();
    }

    public bool IsFreeCameraActive() => freeCamEnabled;
}
