using System.Collections.Generic;
using UnityEngine;

// Attache ce script à la gameplay camera.
// Les arbres qui passent devant l'arène deviennent transparents automatiquement.
public class CameraOcclusionTransparency : MonoBehaviour
{
    public static CameraOcclusionTransparency instance;

    [Header("Arène")]
    public Transform arenaCenter;      // Centre de l'arène — assigner dans l'Inspector
    public float     arenaRadius = 2f; // Rayon de l'arène (couvre toute la grille)

    [Header("Transparence")]
    [Range(0f, 1f)] public float fadeAlpha = 0.2f;
    public float fadeSpeed = 8f;

    [Header("Détection")]
    public float detectionRadius = 4.8f; // Rayon autour de chaque arbre

    // ── État interne ──────────────────────────────────────────────────
    private class OccluderState
    {
        public Renderer    renderer;
        public Material[]  originalMaterials;
        public Material[]  fadedMaterials;
        public float       currentAlpha = 1f;
        public bool        shouldFade;
    }

    private readonly List<OccluderState> states    = new();
    private readonly List<TreeOccluder>  lastTrees = new();

    void Awake() => instance = this;

    // ── API publique ───────────────────────────────────────────────────────

    public void SetArenaCenter(Transform center)  => arenaCenter = center;
    public void SetArenaRadius(float radius)      => arenaRadius = radius;
    public void SetFadeAlpha(float alpha)         => fadeAlpha   = alpha;

    // 5 points : centre + 4 coins de l'arène
    Vector3[] GetArenaPoints()
    {
        Vector3 c = (arenaCenter != null ? arenaCenter.position : Vector3.zero) + Vector3.up * 0.5f;
        float   r = arenaRadius;
        return new[]
        {
            c,
            c + new Vector3( r, 0,  r),
            c + new Vector3(-r, 0,  r),
            c + new Vector3( r, 0, -r),
            c + new Vector3(-r, 0, -r),
        };
    }

    void LateUpdate()
    {
        RefreshTreeList();

        if (arenaCenter == null) return;

        Vector3   camPos      = transform.position;
        Vector3[] arenaPoints = GetArenaPoints();

        // ── Marquer quels arbres occludent l'arène ──────────────────
        foreach (var state in states)
        {
            state.shouldFade = false;
            foreach (var point in arenaPoints)
            {
                if (IsOccluding(state.renderer.bounds.center, camPos, point))
                {
                    state.shouldFade = true;
                    break;
                }
            }
        }

        // ── Appliquer le fondu ───────────────────────────────────────
        float dt = Time.deltaTime * fadeSpeed;
        foreach (var state in states)
        {
            float t = state.shouldFade ? fadeAlpha : 1f;
            state.currentAlpha = Mathf.MoveTowards(state.currentAlpha, t, dt);
            ApplyAlpha(state);
        }
    }

    // ── Détection géométrique (pas besoin de collider) ───────────────
    bool IsOccluding(Vector3 objPos, Vector3 camPos, Vector3 targetPos)
    {
        Vector3 line  = targetPos - camPos;
        Vector3 toObj = objPos    - camPos;

        float t = Vector3.Dot(toObj, line) / line.sqrMagnitude;
        if (t <= 0.05f || t >= 0.95f) return false;

        Vector3 closest = camPos + t * line;
        return Vector3.Distance(closest, objPos) < detectionRadius;
    }

    // ── Rafraîchir la liste d'arbres après chaque génération ─────────
    void RefreshTreeList()
    {
        TreeOccluder[] current = FindObjectsByType<TreeOccluder>(FindObjectsSortMode.None);

        bool changed = current.Length != lastTrees.Count;
        if (!changed)
        {
            for (int i = 0; i < current.Length; i++)
                if (current[i] != lastTrees[i]) { changed = true; break; }
        }
        if (!changed) return;

        RestoreAll();
        states.Clear();
        lastTrees.Clear();
        lastTrees.AddRange(current);

        foreach (var occ in current)
        {
            foreach (var r in occ.GetComponentsInChildren<Renderer>())
            {
                var state = new OccluderState
                {
                    renderer          = r,
                    originalMaterials = r.sharedMaterials,
                    fadedMaterials    = CreateFadedMaterials(r.sharedMaterials),
                    currentAlpha      = 1f
                };
                states.Add(state);
            }
        }
    }

    // ── Créer une copie transparente des matériaux ────────────────────
    Material[] CreateFadedMaterials(Material[] originals)
    {
        Material[] faded = new Material[originals.Length];
        for (int i = 0; i < originals.Length; i++)
        {
            faded[i] = new Material(originals[i]);
            SetTransparentMode(faded[i]);
        }
        return faded;
    }

    void SetTransparentMode(Material m)
    {
        // URP
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_AlphaClip", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        // Built-in Standard
        if (m.HasProperty("_Mode"))
            m.SetFloat("_Mode", 3f);

        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
    }

    void ApplyAlpha(OccluderState state)
    {
        float a = state.currentAlpha;

        if (a >= 0.999f)
        {
            state.renderer.sharedMaterials = state.originalMaterials;
            return;
        }

        state.renderer.sharedMaterials = state.fadedMaterials;

        foreach (var m in state.fadedMaterials)
        {
            if (m.HasProperty("_BaseColor"))
            {
                Color c = m.GetColor("_BaseColor"); c.a = a;
                m.SetColor("_BaseColor", c);
            }
            if (m.HasProperty("_Color"))
            {
                Color c = m.GetColor("_Color"); c.a = a;
                m.SetColor("_Color", c);
            }
        }
    }

    void RestoreAll()
    {
        foreach (var state in states)
            if (state.renderer != null)
                state.renderer.sharedMaterials = state.originalMaterials;
    }

    void OnDisable() => RestoreAll();
    void OnDestroy() => RestoreAll();
}
