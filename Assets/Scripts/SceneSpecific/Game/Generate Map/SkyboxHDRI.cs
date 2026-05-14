using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Configure automatiquement le skybox HDRI téléchargé depuis Poly Haven.
///
/// COMMENT L'UTILISER :
/// 1. Importe ton fichier .hdr dans Unity (ex: Assets/HDRI/bismarckturm_hillside_4k.hdr)
/// 2. Sélectionne le fichier dans Project → Inspector :
///      - Texture Shape = 2D
///      - sRGB = décoché
///      - Generate Mip Maps = coché
///      - Clique "Apply"
/// 3. Glisse ce script sur n'importe quel GameObject de la scène
/// 4. Assigne ta texture dans le champ "Hdri Texture" dans l'Inspector
/// 5. Clique "Apply HDRI Skybox" dans le menu contextuel (clic droit sur le composant)
///
/// Le script configure aussi l'Ambient Lighting depuis le HDRI.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("Void/Skybox HDRI Setup")]
public class SkyboxHDRI : MonoBehaviour
{
    [Header("HDRI Texture")]
    [Tooltip("Glisse ici ton fichier .hdr importé depuis Poly Haven")]
    public Texture hdriTexture;

    [Header("Skybox Settings")]
    [Range(0f, 8f)]  public float exposure    = 1.1f;
    [Range(0f, 360f)]public float rotation    = 0f;

    [Header("Ambient Lighting")]
    [Range(0f, 2f)]  public float ambientIntensity = 1.0f;
    public bool  updateReflections = true;

    [Header("Sun (Directional Light)")]
    public Light sunLight;
    [Range(0f, 5f)]  public float sunIntensity  = 1.4f;
    public Color                  sunColor       = new Color(1.00f, 0.96f, 0.85f);
    [Range(-180f, 180f)] public float sunYaw    =  50f;
    [Range(10f, 85f)]    public float sunPitch  =  45f;

    // ─────────────────────────────────────────────────
    void Start()  => ApplySkybox();
    void OnEnable() => ApplySkybox();

    [ContextMenu("Apply HDRI Skybox")]
    public void ApplySkybox()
    {
        if (hdriTexture == null)
        {
            Debug.LogWarning("[SkyboxHDRI] Aucune texture HDRI assignée !");
            return;
        }

        // ── Crée le matériau skybox panoramique
        Shader panoramicShader = Shader.Find("Skybox/Panoramic");
        if (panoramicShader == null)
        {
            Debug.LogError("[SkyboxHDRI] Shader 'Skybox/Panoramic' introuvable.");
            return;
        }

        Material skyMat = new Material(panoramicShader);
        skyMat.name     = "HDRI_Skybox";
        skyMat.SetTexture("_MainTex",   hdriTexture);
        skyMat.SetFloat("_Exposure",    exposure);
        skyMat.SetFloat("_Rotation",    rotation);
        skyMat.SetFloat("_ImageType",   1f); // Latitude-Longitude layout

        RenderSettings.skybox           = skyMat;
        RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = ambientIntensity;

        // ── Configure la lumière directionnelle (soleil)
        if (sunLight != null)
        {
            sunLight.type      = LightType.Directional;
            sunLight.color     = sunColor;
            sunLight.intensity = sunIntensity;
            sunLight.shadows   = LightShadows.Soft;
            sunLight.shadowStrength     = 0.85f;
            sunLight.shadowNormalBias   = 0.4f;
            sunLight.shadowBias         = 0.05f;
            sunLight.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f);
        }

        // ── Refresh
        DynamicGI.UpdateEnvironment();

        if (updateReflections)
            DynamicGI.UpdateEnvironment();

        Debug.Log("[SkyboxHDRI] Skybox HDRI appliqué : " + hdriTexture.name);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        ApplySkybox();
    }
#endif
}
