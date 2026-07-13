using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ColorBlindFilter : MonoBehaviour
{
    public enum Mode { None = 0, Protanopia = 1, Deuteranopia = 2, Tritanopia = 3 }

    private Material material;
    private Mode currentMode = Mode.None;

    void Awake()
    {
        Shader shader = Shader.Find("Hidden/ColorBlindFilter");
        if (shader != null)
            material = new Material(shader);
        else
            BugTracker.Error("[ColorBlind] Shader 'Hidden/ColorBlindFilter' not found !");
    }

    public void SetMode(Mode mode)
    {
        currentMode = mode;
        BugTracker.Info($"[ColorBlind] Change mode : {mode}.");
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (material == null || currentMode == Mode.None)
        {
            Graphics.Blit(src, dest);
            return;
        }

        material.SetInt("_Mode", (int)currentMode);
        Graphics.Blit(src, dest, material);
    }
}