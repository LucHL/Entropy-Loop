using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Additional presentation layer, generated with the same seed as the island.
public sealed class EntropyWorldDirector : MonoBehaviour
{
    [Serializable] public class Decoration { public string kind = "obelisk", resource = ""; public int count = 8; public float minRadius = 17, maxRadius = 29, scale = 1; }
    [Serializable] public class WorldConfig
    {
        public float daySeconds = 240, weatherSeconds = 60, collapseSeconds = 12;
        public bool dayNight = true, weather = true, chaos = true;
        public float chaosStartSeconds = 180, chaosPeakSeconds = 300;
        public Decoration[] structures = { new Decoration(), new Decoration { kind = "ruin", count = 5 }, new Decoration { kind = "crystals", count = 12 } };
    }
    public WorldConfig configuration = new WorldConfig();
    public TextAsset configurationJson;
    Transform generated;
    Light sun;
    float elapsed, initialIntensity, initialFog;
    Quaternion initialRotation;
    Color initialAmbient, initialFogColor;
    bool captured, collapsing;
    Material stone, crystal, darkness, ash, gold;
    Mesh crystalMesh;
    readonly List<Transform> ornaments = new List<Transform>();
    ParticleSystem rain;
    public void Generate(VoidMapGeneratorGPU map, Transform parent)
    {
        StopAllCoroutines(); RestoreAtmosphere(); elapsed = 0; collapsing = false; ornaments.Clear(); generated = parent;
        var source = configurationJson ? configurationJson : Resources.Load<TextAsset>("World/entropy-world");
        if (source) { try { configuration = JsonUtility.FromJson<WorldConfig>(source.text) ?? new WorldConfig(); } catch (Exception e) { Debug.LogWarning("World JSON: " + e.Message); } }
        if (configuration.structures == null) configuration.structures = new Decoration[0];
        if (!stone) stone = MaterialFor(new Color(.12f,.10f,.17f));
        if (!crystal) crystal = MaterialFor(new Color(.48f,.10f,.8f));
        if (!darkness) darkness = MaterialFor(Color.black);
        if (!ash) ash = MaterialFor(new Color(.29f,.25f,.34f));
        if (!gold) gold = MaterialFor(new Color(.48f,.36f,.17f));
        crystal.EnableKeyword("_EMISSION");
        crystal.SetColor("_EmissionColor",new Color(.25f,.06f,.45f));
        var group = new GameObject("Entropy structures").transform; group.SetParent(parent, false);
        var rng = new System.Random(map.seed ^ 7381);
        var occupied = new List<Vector3>(map.lastHousePositions);
        foreach (var item in configuration.structures)
        {
            if (item == null) continue;
            float inner = Mathf.Max(map.chessboardSize * map.chessTile + 5, item.minRadius);
            float outer = Mathf.Min(map.islandRadius - 5, item.maxRadius);
            if (outer <= inner) continue;
            for (int i = 0; i < Mathf.Clamp(item.count,0,100); i++)
            for (int attempt = 0; attempt < 40; attempt++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2;
                float radius = Mathf.Lerp(inner,outer,(float)rng.NextDouble());
                var position = new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                if (map.IsInsideArena(position, 3f * Mathf.Clamp(item.scale,.2f,4)) || map.IsInsideLake(position) || map.IsInVoidBite(position)) continue;
                if (occupied.Exists(p => new Vector2(p.x-position.x,p.z-position.z).sqrMagnitude < 36)) continue;
                position.y = map.TerrainHeight(position.x,position.z);
                var prefab = string.IsNullOrEmpty(item.resource) ? null : Resources.Load<GameObject>(item.resource);
                var obj = prefab ? Instantiate(prefab,group).transform : Build(item.kind,group);
                obj.localPosition = position; obj.localRotation = Quaternion.Euler(0,(float)rng.NextDouble()*360,0);
                obj.localScale *= Mathf.Clamp(item.scale,.2f,4);
                occupied.Add(position); ornaments.Add(obj); break;
            }
        }
        if (Application.isPlaying) StartCoroutine(CaptureAtmosphere());
    }
    IEnumerator CaptureAtmosphere()
    {
        yield return null;
        sun = RenderSettings.sun;
        if (!sun) foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (light.type == LightType.Directional) { sun = light; break; }
        initialAmbient = RenderSettings.ambientLight; initialFogColor = RenderSettings.fogColor; initialFog = RenderSettings.fogDensity;
        if (sun) { initialIntensity = sun.intensity; initialRotation = sun.transform.rotation; }
        captured = true;
        var go = new GameObject("Void rain"); go.transform.SetParent(generated,false); go.transform.localPosition = Vector3.up*22;
        rain = go.AddComponent<ParticleSystem>(); rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = rain.main; main.startLifetime = 2; main.startSpeed = 16; main.startSize = .055f; main.maxParticles = 1200; main.startColor = new Color(.6f,.45f,.8f,.45f);
        go.transform.localRotation = Quaternion.Euler(90,0,0);
        var shape = rain.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(60,60,1);
        var emission = rain.emission; emission.rateOverTime = 0;
        rain.GetComponent<ParticleSystemRenderer>().sharedMaterial = crystal;
        rain.Play();
    }
    void Update()
    {
        if (!captured || collapsing) return;
        elapsed += Time.deltaTime;
        float daylight = configuration.dayNight ? .5f + .5f*Mathf.Cos(elapsed / Mathf.Max(30,configuration.daySeconds)*Mathf.PI*2) : 1;
        float chaos = configuration.chaos ? Mathf.InverseLerp(configuration.chaosStartSeconds,Mathf.Max(configuration.chaosStartSeconds+1,configuration.chaosPeakSeconds),elapsed) : 0;
        float wet = configuration.weather ? .5f-.5f*Mathf.Cos(elapsed/Mathf.Max(10,configuration.weatherSeconds)*Mathf.PI*2) : 0;
        if (sun) { sun.intensity = initialIntensity*Mathf.Lerp(.25f,1,daylight)*(1-chaos*.25f); sun.transform.rotation = initialRotation*Quaternion.Euler(configuration.dayNight ? elapsed/Mathf.Max(30,configuration.daySeconds)*360 : 0,0,0); }
        RenderSettings.ambientLight = Color.Lerp(initialAmbient,new Color(.16f,.06f,.25f),chaos*.65f);
        RenderSettings.fogColor = Color.Lerp(initialFogColor,new Color(.12f,.035f,.19f),chaos);
        RenderSettings.fogDensity = initialFog + wet*.006f + chaos*.008f;
        if (rain) { var emission = rain.emission; emission.rateOverTime = wet*400; }
        foreach (var ornament in ornaments) if (ornament) ornament.localRotation *= Quaternion.Euler(0,chaos*Time.deltaTime*8,0);
    }
    [ContextMenu("Play final black hole (Play mode)")]
    public void BeginCollapse() { if (Application.isPlaying && generated && !collapsing) StartCoroutine(Collapse()); }
    IEnumerator Collapse()
    {
        collapsing = true;
        // Animate a visual copy so units, colliders and navigation remain untouched.
        var copy = new GameObject("Collapsing island visual").transform;
        var originals = generated.GetComponentsInChildren<MeshRenderer>();
        var pieces = new List<Transform>(); var origins = new List<Vector3>(); var scales = new List<Vector3>();
        foreach (var renderer in originals)
        {
            var filter = renderer.GetComponent<MeshFilter>(); if (!filter || !renderer.enabled) continue;
            var piece = new GameObject("Fragment",typeof(MeshFilter),typeof(MeshRenderer)).transform; piece.SetParent(copy);
            piece.position = renderer.transform.position; piece.rotation = renderer.transform.rotation; piece.localScale = renderer.transform.lossyScale;
            piece.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh; piece.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            pieces.Add(piece); origins.Add(piece.position); scales.Add(piece.localScale); renderer.enabled = false;
        }
        var hole = Part(copy,PrimitiveType.Sphere,new Vector3(0,6,0),Vector3.one*.1f,darkness);
        float duration = Mathf.Clamp(configuration.collapseSeconds,3,60), time = 0;
        while (time < duration)
        {
            time += Time.deltaTime; float t = Mathf.Clamp01(time/duration), pull = t*t;
            hole.localScale = Vector3.one*Mathf.Lerp(.1f,7,Mathf.Sin(t*Mathf.PI));
            for (int i=0;i<pieces.Count;i++) { var delta=origins[i]-hole.position; pieces[i].position=hole.position+Quaternion.Euler(0,t*t*540,0)*delta*(1-pull); pieces[i].localScale=scales[i]*(1-pull); }
            yield return null;
        }
        Destroy(copy.gameObject);
        foreach (var renderer in originals) if (renderer) renderer.enabled = true;
        collapsing = false;
    }
    Transform Build(string kind, Transform parent)
    {
        var root = new GameObject(kind).transform; root.SetParent(parent,false);
        if (kind == "ruin")
        {
            // Uneven masonry, interrupted arch and buttresses: a ruined sanctuary.
            for (int side=-1;side<=1;side+=2)
            {
                for(int level=0;level<4;level++)
                    Part(root,PrimitiveType.Cube,new Vector3(side*1.35f,.38f+level*.65f,0),new Vector3(.82f,.59f,.85f),level%2==0?stone:ash);
                Part(root,PrimitiveType.Cube,new Vector3(side*1.8f,.65f,.1f),new Vector3(.4f,1.3f,1.1f),stone).localRotation=Quaternion.Euler(0,0,side*12);
                for(int step=0;step<2;step++)
                    Part(root,PrimitiveType.Cube,new Vector3(side*(1.15f-step*.38f),2.85f+step*.34f,0),new Vector3(.62f,.55f,.88f),ash).localRotation=Quaternion.Euler(0,0,-side*(25+step*15));
            }
            Part(root,PrimitiveType.Cube,new Vector3(0,.12f,0),new Vector3(3.8f,.24f,2),stone);
            Crystal(root,new Vector3(-.6f,.25f,.45f),new Vector3(.35f,.9f,.35f),18);
        }
        else if(kind == "crystals")
        {
            for(int i=0;i<5;i++)
            {
                float angle=i*2.4f;
                Crystal(root,new Vector3(Mathf.Cos(angle)*.7f,.12f,Mathf.Sin(angle)*.7f),new Vector3(.35f,1.1f+(i%3)*.55f,.35f),i*9-15);
            }
        }
        else if(kind == "watchtower")
        {
            Part(root,PrimitiveType.Cube,new Vector3(0,.2f,0),new Vector3(2.8f,.4f,2.8f),stone);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                Part(root,PrimitiveType.Cube,new Vector3(x,2,z),new Vector3(.3f,4,.3f),ash);
            Part(root,PrimitiveType.Cube,new Vector3(0,3.6f,0),new Vector3(2.7f,.25f,2.7f),stone);
            for(int side=-1;side<=1;side+=2)
            {
                Part(root,PrimitiveType.Cube,new Vector3(side*1.2f,4.15f,0),new Vector3(.18f,.8f,2.6f),ash);
                Part(root,PrimitiveType.Cube,new Vector3(0,4.15f,side*1.2f),new Vector3(2.6f,.8f,.18f),ash);
                Part(root,PrimitiveType.Cube,new Vector3(side*.68f,5.1f,0),new Vector3(1.8f,.16f,3.1f),stone).localRotation=Quaternion.Euler(0,0,-side*35);
            }
            for(int i=0;i<9;i++) Part(root,PrimitiveType.Cube,new Vector3(0,.35f+i*.37f,-1.35f),new Vector3(.65f,.10f,.18f),gold);
            Crystal(root,new Vector3(0,4.1f,0),new Vector3(.25f,.8f,.25f),0);
        }
        else if(kind == "merchant")
        {
            Part(root,PrimitiveType.Cube,new Vector3(0,.15f,0),new Vector3(3.8f,.3f,2.8f),stone);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                Part(root,PrimitiveType.Cube,new Vector3(x*1.55f,1.55f,z),new Vector3(.16f,2.8f,.16f),gold);
            for(int side=-1;side<=1;side+=2)
                Part(root,PrimitiveType.Cube,new Vector3(side*.85f,3,0),new Vector3(2.1f,.12f,3),ash).localRotation=Quaternion.Euler(0,0,-side*18);
            Part(root,PrimitiveType.Cube,new Vector3(0,.85f,-.8f),new Vector3(2.8f,1.2f,.6f),stone);
            for(int i=0;i<4;i++) Crystal(root,new Vector3(-.9f+i*.6f,1.5f,-.8f),new Vector3(.16f,.35f+(i%2)*.15f,.16f),0);
            for(int i=0;i<3;i++) Part(root,PrimitiveType.Cube,new Vector3(-.9f+i*.85f,.6f,.5f),new Vector3(.65f,.9f,.7f),ash);
        }
        else if(kind == "observatory")
        {
            for(int tier=0;tier<3;tier++) Part(root,PrimitiveType.Cylinder,new Vector3(0,.15f+tier*.25f,0),new Vector3(4-tier*.4f,.12f,4-tier*.4f),stone);
            Part(root,PrimitiveType.Cube,new Vector3(0,1.25f,0),new Vector3(.7f,1.2f,.7f),ash);
            // Two polygonal armillary rings surround the suspended void crystal.
            for(int ring=0;ring<2;ring++) for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;
                Vector3 p=ring==0?new Vector3(Mathf.Cos(a),Mathf.Sin(a),0):new Vector3(0,Mathf.Sin(a),Mathf.Cos(a));
                Vector3 q=ring==0?new Vector3(Mathf.Cos(b),Mathf.Sin(b),0):new Vector3(0,Mathf.Sin(b),Mathf.Cos(b));
                var bar=Part(root,PrimitiveType.Cube,Vector3.up*2.6f+(p+q)*.65f,new Vector3(.09f,Vector3.Distance(p,q)*1.3f,.09f),gold);
                bar.localRotation=Quaternion.FromToRotation(Vector3.up,q-p);
            }
            Crystal(root,new Vector3(0,2.1f,0),new Vector3(.5f,1,.5f),0);
        }

        else if(kind == "shrine")
        {
            for(int tier=0;tier<3;tier++) Part(root,PrimitiveType.Cylinder,new Vector3(0,.12f+tier*.2f,0),new Vector3(3-tier*.45f,.1f,3-tier*.45f),stone);
            Part(root,PrimitiveType.Cube,new Vector3(0,.9f,0),new Vector3(1.25f,.6f,.85f),ash);
            Crystal(root,new Vector3(0,1.3f,0),new Vector3(.55f,1.4f,.55f),0);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;
                var pos=new Vector3(Mathf.Cos(a)*1.8f,.15f,Mathf.Sin(a)*1.8f);
                Part(root,PrimitiveType.Cube,pos,new Vector3(.35f,.3f,.35f),gold);
                Crystal(root,pos+Vector3.up*.2f,new Vector3(.13f,.45f,.13f),0);
            }
        }
        else
        {
            for(int tier=0;tier<3;tier++) Part(root,PrimitiveType.Cube,new Vector3(0,.15f+tier*.24f,0),new Vector3(1.8f-tier*.3f,.24f,1.8f-tier*.3f),stone);
            Part(root,PrimitiveType.Cube,new Vector3(0,1.8f,0),new Vector3(.65f,2.4f,.65f),stone);
            for(int i=0;i<4;i++) Part(root,PrimitiveType.Cube,new Vector3(0,1+i*.43f,-.331f),new Vector3(.22f,.07f,.025f),crystal).localRotation=Quaternion.Euler(0,0,i%2==0?35:-35);
            Part(root,PrimitiveType.Cube,new Vector3(0,2.95f,0),new Vector3(.85f,.15f,.85f),gold);
            Crystal(root,new Vector3(0,3.05f,0),new Vector3(.5f,.9f,.5f),0);
        }
        // Local debris composition, rather than uniformly scattered decoration.
        for(int i=0;i<(kind == "watchtower" || kind == "merchant" || kind == "observatory" ? 0 : 7);i++)
        {
            float a=i*2.39996f, distance=1.65f+(i%3)*.3f;
            var rock=Part(root,PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*distance,.13f,Mathf.Sin(a)*distance),new Vector3(.24f+(i%2)*.2f,.22f,.36f),i%3==0?ash:stone);
            rock.localRotation=Quaternion.Euler(i*7,i*63,i*11);
        }
        return root;
    }
    void Crystal(Transform parent, Vector3 position, Vector3 scale, float tilt)
    {
        if (!crystalMesh)
        {
            // Six flat-shaded sides and pointed tips, shared by all crystal instances.
            var vertices=new List<Vector3>(); var indices=new List<int>();
            for(int side=0;side<6;side++)
            {
                float a=side*Mathf.PI/3, b=(side+1)*Mathf.PI/3;
                var p=new Vector3(Mathf.Cos(a),.3f,Mathf.Sin(a));
                var q=new Vector3(Mathf.Cos(b),.3f,Mathf.Sin(b));
                int n=vertices.Count;
                vertices.Add(p);vertices.Add(Vector3.up);vertices.Add(q);
                vertices.Add(q);vertices.Add(Vector3.zero);vertices.Add(p);
                for(int j=0;j<6;j++)indices.Add(n+j);
            }
            crystalMesh=new Mesh { name="Entropy six-sided crystal" };
            crystalMesh.SetVertices(vertices);crystalMesh.SetTriangles(indices,0);crystalMesh.RecalculateNormals();crystalMesh.RecalculateBounds();
        }
        var go=new GameObject("Void crystal",typeof(MeshFilter),typeof(MeshRenderer));
        go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.transform.localRotation=Quaternion.Euler(tilt,0,tilt*.5f);
        go.GetComponent<MeshFilter>().sharedMesh=crystalMesh;go.GetComponent<MeshRenderer>().sharedMaterial=crystal;
    }

    Transform Part(Transform parent,PrimitiveType type,Vector3 pos,Vector3 scale,Material material)
    { var go=GameObject.CreatePrimitive(type); go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material; var c=go.GetComponent<Collider>(); c.enabled=false; if(Application.isPlaying) Destroy(c); else DestroyImmediate(c); return go.transform; }
    Material MaterialFor(Color color) { var shader=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard"); if(!shader) shader=Shader.Find("Standard"); return new Material(shader){color=color}; }
    void RestoreAtmosphere() { if(!captured)return; RenderSettings.ambientLight=initialAmbient; RenderSettings.fogColor=initialFogColor; RenderSettings.fogDensity=initialFog; if(sun){sun.intensity=initialIntensity;sun.transform.rotation=initialRotation;} captured=false; }
    void OnDestroy() { RestoreAtmosphere(); if(crystalMesh) Destroy(crystalMesh); foreach(var material in new[]{stone,crystal,darkness,ash,gold}) if(material) Destroy(material); }
}
