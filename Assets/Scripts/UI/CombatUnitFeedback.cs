using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Presentation only: health and damage remain owned by Units.
public sealed class CombatUnitFeedback : MonoBehaviour
{
    static CombatUnitFeedback instance;
    static readonly Color Ally = new Color32(181, 83, 255, 255);
    static readonly Color Enemy = new Color32(255, 57, 81, 255);
    readonly Dictionary<Units, Bar> bars = new();
    readonly List<Units> removed = new();
    readonly List<Burst> bursts = new();
    RectTransform root;

    sealed class Tint
    {
        public Renderer renderer;
        public int index, property;
        public Color color;
        public MaterialPropertyBlock original, flash;
    }
    sealed class Bar
    {
        public Units unit;
        public Canvas oldCanvas;
        public RectTransform rect, fill, trail;
        public Image color;
        public TMP_Text label;
        public float fraction = 1, delay, flashUntil;
        public bool flashing;
        public int hitSequence;
        public float lastHit = -10;
        public readonly List<Tint> tints = new();
    }
    sealed class Burst
    {
        public RectTransform rect;
        public TMP_Text text;
        public CombatImpactGraphic graphic;
        public Vector3 world;
        public float age, duration;
        public bool death;
        public int lane;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    public static void Register(Units unit, Canvas oldCanvas)
    {
        if (unit.gameObject.scene.name != "Game") return;
        if (!instance)
        {
            var go = new GameObject("Combat unit feedback", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(go, unit.gameObject.scene);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -10; // Under the combat HUD and its modal windows.
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            instance = go.AddComponent<CombatUnitFeedback>();
            instance.root = (RectTransform)go.transform;
        }
        if (instance.bars.TryGetValue(unit, out var existing))
        {
            instance.Restore(existing);
            existing.fraction = 1; existing.delay = 0;
            if (oldCanvas) oldCanvas.enabled = false;
            return;
        }
        var b = new Bar { unit = unit, oldCanvas = oldCanvas };
        b.rect = Rect("Health - " + unit.name, instance.root, new Vector2(136, 30));
        Panel("Border", b.rect, new Color32(200, 174, 126, 255), Vector2.zero, new Vector2(136, 30));
        Panel("Dark backing", b.rect, new Color32(10, 7, 20, 255), Vector2.zero, new Vector2(132, 26));
        b.trail = Panel("Damage trail", b.rect, new Color32(255, 195, 159, 255), new Vector2(-63, 0), new Vector2(126, 20)).rectTransform;
        b.trail.pivot = new Vector2(0, .5f);
        b.fill = Panel("Health", b.rect, Ally, new Vector2(-63, 0), new Vector2(126, 20)).rectTransform;
        b.fill.pivot = new Vector2(0, .5f);
        b.color = b.fill.GetComponent<Image>();
        b.label = Text("Health value", b.rect, 16, new Vector2(128, 26));
        b.label.fontStyle = FontStyles.Bold;
        foreach (var renderer in unit.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (!material) continue;
                int property = material.HasProperty("_BaseColor") ? Shader.PropertyToID("_BaseColor") : Shader.PropertyToID("_Color");
                if (!material.HasProperty(property)) continue;
                b.tints.Add(new Tint { renderer = renderer, index = i, property = property, color = material.GetColor(property), original = new MaterialPropertyBlock(), flash = new MaterialPropertyBlock() });
            }
        }
        instance.bars.Add(unit, b);
        if (oldCanvas) oldCanvas.enabled = false;
    }

    public static bool Hit(Transform target, float amount)
    {
        if (!instance) return false;
        var unit = target.GetComponent<Units>();
        if (!unit || !instance.bars.TryGetValue(unit, out var bar)) return false;
        bool blocked = Mathf.Approximately(amount, 0), heal = amount < 0;
        Color color = blocked ? new Color32(130, 208, 255, 255) : heal ? new Color32(103, 255, 174, 255) : new Color32(255, 224, 172, 255);
        bool heavy = amount >= unit.totalHealth * .2f;
        string caption = blocked ? "BLOQUÉ" : (heal ? "+" : "−") + Mathf.Abs(amount).ToString("0.#");
        bar.hitSequence = Time.time - bar.lastHit < .3f ? (bar.hitSequence + 1) % 4 : 0;
        bar.lastHit = Time.time;
        instance.Emit(unit.transform.position + Vector3.up * 1.5f, caption, color, heavy, false, bar.hitSequence);
        if (amount > 0)
        {
            bar.delay = .35f;
            if (!bar.flashing)
                foreach (var tint in bar.tints) if (tint.renderer) tint.renderer.GetPropertyBlock(tint.original, tint.index);
            bar.flashing = true;
            bar.flashUntil = Time.time + .14f;
            foreach (var tint in bar.tints)
            {
                if (!tint.renderer) continue;
                tint.renderer.GetPropertyBlock(tint.flash, tint.index);
                tint.flash.SetColor(tint.property, Color.Lerp(tint.color, new Color(1.8f, 1.25f, 2f), .85f));
                tint.renderer.SetPropertyBlock(tint.flash, tint.index);
            }
        }
        return true;
    }

    public static void Death(Units unit)
    {
        if (!instance || !instance.bars.TryGetValue(unit, out var bar)) return;
        instance.Restore(bar);
        // Death only restores the hit flash; no shockwave is emitted.
    }

    void Emit(Vector3 world, string caption, Color color, bool heavy, bool death, int lane = 0)
    {
        // Bound transient visuals during large simultaneous fights.
        if (bursts.Count >= 80) { Destroy(bursts[0].rect.gameObject); bursts.RemoveAt(0); }
        var burst = new Burst { world = world, duration = death ? .7f : .9f, death = death, lane = lane };
        burst.rect = Rect(death ? "Death shockwave" : "Damage impact", root, new Vector2(180, 120));
        var art = Rect("Impact particles", burst.rect, new Vector2(100, 100));
        art.gameObject.AddComponent<CanvasRenderer>();
        burst.graphic = art.gameObject.AddComponent<CombatImpactGraphic>();
        burst.graphic.raycastTarget = false;
        burst.graphic.color = color;
        burst.graphic.death = death;
        burst.graphic.intensity = heavy ? 1.35f : 1;
        burst.text = Text("Damage number", burst.rect, heavy ? 40 : 32, new Vector2(200, 70));
        burst.text.text = caption;
        burst.text.fontStyle = FontStyles.Bold;
        // Thicken only floating numbers, using their own TMP material instance.
        burst.text.fontMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, .18f);
        burst.text.outlineWidth = .32f;
        burst.text.outlineColor = new Color32(5, 2, 10, 255);
        burst.text.UpdateMeshPadding();
        burst.text.color = color;
        bursts.Add(burst);
    }

    void LateUpdate()
    {
        var camera = Camera.main;
        if (!camera) return;
        removed.Clear();
        foreach (var pair in bars)
        {
            var b = pair.Value;
            if (!b.unit) { Destroy(b.rect.gameObject); removed.Add(pair.Key); continue; }
            if (b.flashing && (Time.time >= b.flashUntil || !b.unit.gameObject.activeInHierarchy)) Restore(b);
            bool visible = b.unit.gameObject.activeInHierarchy && b.unit.isAlive;
            Vector3 world = b.oldCanvas ? b.oldCanvas.transform.position : b.unit.transform.position + Vector3.up * 2;
            var screen = camera.WorldToScreenPoint(world);
            b.rect.gameObject.SetActive(visible && screen.z > 0);
            if (!visible) continue;
            Position(b.rect, screen);
            float value = Mathf.Clamp01(b.unit.hp / Mathf.Max(1, b.unit.totalHealth));
            if (value > b.fraction) b.fraction = value;
            b.delay -= Time.deltaTime;
            if (b.delay <= 0) b.fraction = Mathf.MoveTowards(b.fraction, value, Time.deltaTime * 1.4f);
            b.fill.sizeDelta = new Vector2(126 * value, 20);
            b.trail.sizeDelta = new Vector2(126 * b.fraction, 20);
            var baseColor = b.unit.team == UnitsTeam.Player ? Ally : Enemy;
            b.color.color = value <= .25f ? Color.Lerp(baseColor, Color.white, .22f + .22f * Mathf.Sin(Time.time * 7)) : baseColor;
            b.label.text = Mathf.CeilToInt(Mathf.Max(0, b.unit.hp)) + " / " + Mathf.CeilToInt(b.unit.totalHealth);
        }
        foreach (var unit in removed) bars.Remove(unit);
        for (int i = bursts.Count - 1; i >= 0; i--)
        {
            var b = bursts[i];
            b.age += Time.deltaTime;
            float t = Mathf.Clamp01(b.age / b.duration);
            if (t >= 1) { Destroy(b.rect.gameObject); bursts.RemoveAt(i); continue; }
            var screen = camera.WorldToScreenPoint(b.world);
            b.rect.gameObject.SetActive(screen.z > 0);
            Position(b.rect, screen);
            b.graphic.progress = t;
            b.graphic.SetVerticesDirty();
            b.text.rectTransform.anchoredPosition = new Vector2(b.lane % 2 == 0 ? -18 : 18, 65 + b.lane * 35 + 65 * t);
            b.text.rectTransform.localScale = Vector3.one * (1 + .25f * Mathf.Sin(Mathf.Min(1, t * 4) * Mathf.PI));
            b.text.alpha = 1 - Mathf.InverseLerp(.55f, 1, t);
        }
    }

    void Position(RectTransform rect, Vector3 screen)
    {
        var canvas = root.GetComponent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
        rect.anchoredPosition = local;
    }
    void Restore(Bar bar)
    {
        if (!bar.flashing) return;
        foreach (var tint in bar.tints) if (tint.renderer) tint.renderer.SetPropertyBlock(tint.original, tint.index);
        bar.flashing = false;
    }
    void OnDestroy()
    {
        foreach (var b in bars.Values) { Restore(b); if (b.oldCanvas) b.oldCanvas.enabled = true; }
        if (instance == this) instance = null;
    }
    static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false); rect.sizeDelta = size;
        return rect;
    }
    static Image Panel(string name, Transform parent, Color color, Vector2 position, Vector2 size)
    {
        var rect = Rect(name, parent, size); rect.anchoredPosition = position;
        var image = rect.gameObject.AddComponent<CombatRoundedHealthGraphic>(); image.color = color; image.raycastTarget = false; return image;
    }
    static TMP_Text Text(string name, Transform parent, float size, Vector2 dimensions)
    {
        var rect = Rect(name, parent, dimensions);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white; text.raycastTarget = false;
        text.outlineWidth = .22f; text.outlineColor = new Color32(12, 5, 20, 255);
        return text;
    }
}

public sealed class CombatImpactGraphic : MaskableGraphic
{
    public float progress, intensity = 1;
    public bool death;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float t = death ? progress : Mathf.Clamp01(progress * 2.5f);
        var tint = color; tint.a *= 1 - t;
        float radius = Mathf.Lerp(5, death ? 90 : 42, t) * intensity;
        int count = death ? 32 : 9;
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2 / count;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 tangent = new Vector2(-direction.y, direction.x);
            Vector2 center = direction * radius;
            float width = death ? radius * .105f : 2.5f;
            float length = death ? 2.5f : Mathf.Lerp(10, 2, t) * intensity;
            int n = vh.currentVertCount;
            vh.AddVert(center - tangent * width - direction * length, tint, Vector2.zero);
            vh.AddVert(center + tangent * width - direction * length, tint, Vector2.zero);
            vh.AddVert(center + tangent * width + direction * length, tint, Vector2.zero);
            vh.AddVert(center - tangent * width + direction * length, tint, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
    }
}

// Rounded geometry follows the actual health width, including almost-empty bars.
public sealed class CombatRoundedHealthGraphic : Image
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        if (r.width <= 0 || r.height <= 0) return;
        float radius = Mathf.Min(r.height * .3f, r.width * .5f);
        const int segments = 8;
        vh.AddVert(r.center, color, Vector2.zero);
        for (int corner = 0; corner < 4; corner++)
        {
            var center = new Vector2(
                corner == 0 || corner == 3 ? r.xMax - radius : r.xMin + radius,
                corner < 2 ? r.yMax - radius : r.yMin + radius);
            for (int step = 0; step <= segments; step++)
            {
                float angle = (corner * 90f + step * 90f / segments) * Mathf.Deg2Rad;
                var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(point, color, Vector2.zero);
            }
        }
        int perimeter = 4 * (segments + 1);
        for (int i = 0; i < perimeter; i++)
            vh.AddTriangle(0, i + 1, (i + 1) % perimeter + 1);
    }
}
