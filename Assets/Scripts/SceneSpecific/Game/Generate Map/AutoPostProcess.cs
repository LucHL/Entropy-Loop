// AutoPostProcess.cs — Built-in Render Pipeline, no external dependencies
// Sets ambient light, fog, and skybox exposure only.
// For full bloom/vignette, install "Post Processing" via Package Manager later.

using UnityEngine;
using UnityEngine.Rendering;

[AddComponentMenu("Void/Auto Post Process")]
[ExecuteAlways]
public class AutoPostProcess : MonoBehaviour
{
    [Header("Ambient Light")]
<<<<<<< Updated upstream
    public Color ambientSky     = new Color(0.30f, 0.40f, 0.55f);
    public Color ambientEquator = new Color(0.20f, 0.24f, 0.30f);
    public Color ambientGround  = new Color(0.07f, 0.09f, 0.06f);

    [Header("Fog")]
    public bool  enableFog  = true;
    public Color fogColor   = new Color(0.65f, 0.72f, 0.80f);
    [Range(0f, 0.08f)]
    public float fogDensity = 0.007f;

    [Header("Skybox")]
    [Range(0f, 8f)]
    public float skyboxExposure = 1.6f;
=======
    public Color ambientSky     = new Color(0.18f, 0.22f, 0.30f);
    public Color ambientEquator = new Color(0.12f, 0.14f, 0.18f);
    public Color ambientGround  = new Color(0.06f, 0.07f, 0.08f);

    [Header("Fog")]
    public bool  enableFog  = true;
    public Color fogColor   = new Color(0.55f, 0.60f, 0.65f);
    [Range(0f, 0.08f)]
    public float fogDensity = 0.012f;

    [Header("Skybox")]
    [Range(0f, 8f)]
    public float skyboxExposure = 1.3f;
>>>>>>> Stashed changes

    void OnEnable()   => Apply();
    void OnValidate() => Apply();

    public void Apply()
    {
        // Ambient trilight
        RenderSettings.ambientMode        = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor    = ambientSky;
        RenderSettings.ambientEquatorColor= ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;

        // Fog
        RenderSettings.fog        = enableFog;
        RenderSettings.fogMode    = FogMode.ExponentialSquared;
        RenderSettings.fogColor   = fogColor;
        RenderSettings.fogDensity = fogDensity;

        // Skybox exposure
        if (RenderSettings.skybox != null)
            RenderSettings.skybox.SetFloat("_Exposure", skyboxExposure);

        DynamicGI.UpdateEnvironment();
    }
}
