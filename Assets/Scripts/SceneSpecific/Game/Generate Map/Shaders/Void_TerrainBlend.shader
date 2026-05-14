Shader "Void/TerrainBlend"
{
    Properties
    {
        _GrassTex        ("Grass Albedo",      2D)            = "white" {}
        _DirtTex         ("Dirt Albedo",       2D)            = "white" {}
        _RockTex         ("Rock Albedo",       2D)            = "white" {}
        _SandTex         ("Sand Albedo",       2D)            = "white" {}
        _GrassNormal     ("Grass Normal Map",  2D)            = "bump"  {}
        _RockNormal      ("Rock Normal Map",   2D)            = "bump"  {}
        _GrassColor      ("Grass Tint",        Color)         = (0.30, 0.55, 0.20, 1)
        _DirtColor       ("Dirt Tint",         Color)         = (0.50, 0.35, 0.20, 1)
        _RockColor       ("Rock Tint",         Color)         = (0.45, 0.42, 0.38, 1)
        _SandColor       ("Sand Tint",         Color)         = (0.76, 0.70, 0.50, 1)
        _MaxHeight       ("Max Height",        Float)         = 10.0
        _TextureScale    ("Texture Scale",     Float)         = 0.15
        _NormalStrength  ("Normal Strength",   Range(0,2))    = 1.0
        _Glossiness      ("Smoothness",        Range(0,1))    = 0.25
        _Metallic        ("Metallic",          Range(0,1))    = 0.0
        _SlopeSharpness  ("Slope Sharpness",   Range(1,16))   = 4.0
        _HeightSharpness ("Height Sharpness",  Range(1,16))   = 3.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        // Built-in Render Pipeline surface shader (PBR Standard)
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        #include "UnityCG.cginc"

        sampler2D _GrassTex, _DirtTex, _RockTex, _SandTex;
        sampler2D _GrassNormal, _RockNormal;

        fixed4 _GrassColor, _DirtColor, _RockColor, _SandColor;

        float _MaxHeight, _TextureScale, _NormalStrength;
        float _Glossiness, _Metallic;
        float _SlopeSharpness, _HeightSharpness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        // Vertex function required so worldNormal is available
        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Reconstruct world-space normal (needs INTERNAL_DATA + vertex:vert)
            float3 wNormal = WorldNormalVector(IN, float3(0, 0, 1));

            // Top-down UV for terrain textures
            float2 uv = IN.worldPos.xz * _TextureScale;

            // Sample textures
            fixed4 gCol = tex2D(_GrassTex, uv) * _GrassColor;
            fixed4 dCol = tex2D(_DirtTex,  uv) * _DirtColor;
            fixed4 rCol = tex2D(_RockTex,  uv) * _RockColor;
            fixed4 sCol = tex2D(_SandTex,  uv) * _SandColor;

            // Height-based weights (0=low/sand, 1=high/rock)
            float h = saturate(IN.worldPos.y / max(_MaxHeight, 0.001));
            float wSand  = pow(saturate(1.0 - h * _HeightSharpness), _HeightSharpness);
            float wGrass = pow(saturate(1.0 - abs(h - 0.35) * _HeightSharpness * 1.5), 2.0);
            float wRock  = pow(saturate((h - 0.55) * _HeightSharpness), _HeightSharpness);
            float wDirt  = max(0, 1.0 - (wSand + wGrass + wRock));

            // Slope override — steep faces become rocky / dirty
            float slope     = 1.0 - saturate(dot(wNormal, float3(0, 1, 0)));
            float slopeRock = pow(saturate((slope - 0.3) * _SlopeSharpness), 2.0);
            wGrass *= (1.0 - slopeRock);
            wRock  += slopeRock * 0.7;
            wDirt  += slopeRock * 0.3;

            // Normalise
            float total = wSand + wGrass + wRock + wDirt + 1e-5;
            wSand /= total; wGrass /= total; wRock /= total; wDirt /= total;

            // Blend albedo
            fixed4 albedo = sCol * wSand + gCol * wGrass + rCol * wRock + dCol * wDirt;

            // Blend normals
            fixed4 gN = tex2D(_GrassNormal, uv);
            fixed4 rN = tex2D(_RockNormal,  uv);
            fixed4 bN = gN * (wGrass + wSand + wDirt) + rN * wRock;
            float3 n  = UnpackNormal(bN);
            n.xy *= _NormalStrength;

            o.Albedo     = albedo.rgb;
            o.Normal     = normalize(n);
            o.Smoothness = _Glossiness * (1.0 - slope * 0.5);
            o.Metallic   = _Metallic;
            o.Occlusion  = 1.0;
        }
        ENDCG
    }

    FallBack "Nature/Terrain/Diffuse"
}
