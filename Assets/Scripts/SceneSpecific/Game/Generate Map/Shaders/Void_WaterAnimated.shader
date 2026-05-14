Shader "Void/WaterAnimated"
{
    Properties
    {
        _ShallowColor  ("Shallow Color",      Color)       = (0.15, 0.55, 0.75, 0.75)
        _DeepColor     ("Deep Color",         Color)       = (0.03, 0.18, 0.40, 0.95)
        _DepthFade     ("Depth Fade",         Float)       = 3.0
        _FresnelPow    ("Fresnel Power",      Range(1,8))  = 4.0
        _Glossiness    ("Smoothness",         Range(0,1))  = 0.92
        _Metallic      ("Metallic",           Range(0,1))  = 0.0

        _NormalMapA    ("Normal Map A",       2D)          = "bump" {}
        _NormalMapB    ("Normal Map B",       2D)          = "bump" {}
        _NormalScale   ("Normal Scale",       Float)       = 0.6
        _SpeedA        ("Wave Speed A (XY)",  Vector)      = (0.04, 0.02, 0, 0)
        _SpeedB        ("Wave Speed B (XY)",  Vector)      = (-0.02, 0.03, 0, 0)
        _WaveTiling    ("Wave Tiling",        Float)       = 0.35

        _FoamTex       ("Foam Texture",       2D)          = "white" {}
        _FoamAmount    ("Foam Amount",        Range(0,1))  = 0.35
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 300

        // Grab pass for refraction-like depth fade
        GrabPass { "_GrabTexture" }

        CGPROGRAM
        // Built-in Render Pipeline — transparent + alpha
        #pragma surface surf Standard alpha:fade fullforwardshadows vertex:vert
        #pragma target 3.0
        #include "UnityCG.cginc"

        fixed4 _ShallowColor, _DeepColor;
        float  _DepthFade, _FresnelPow, _Glossiness, _Metallic;

        sampler2D _NormalMapA, _NormalMapB;
        float  _NormalScale, _WaveTiling;
        float4 _SpeedA, _SpeedB;

        sampler2D _FoamTex;
        float  _FoamAmount;

        // Built-in depth texture for soft edge fade
        sampler2D _CameraDepthTexture;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
            float4 screenPos;
        };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            // Gentle sine wave displacement
            float phase = v.vertex.x * 0.8 + v.vertex.z * 0.6 + _Time.y * 1.2;
            v.vertex.y += sin(phase) * 0.08 + cos(phase * 1.7 + 1.0) * 0.04;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float t = _Time.y;

            // Scrolling UVs for two normal map layers
            float2 uvA = IN.worldPos.xz * _WaveTiling + _SpeedA.xy * t;
            float2 uvB = IN.worldPos.xz * _WaveTiling * 1.4 + _SpeedB.xy * t;

            float3 nA = UnpackNormal(tex2D(_NormalMapA, uvA));
            float3 nB = UnpackNormal(tex2D(_NormalMapB, uvB));
            float3 n  = normalize(float3((nA.xy + nB.xy) * _NormalScale, 1.0));

            // Fresnel factor
            float NdotV   = saturate(dot(n, normalize(IN.viewDir)));
            float fresnel  = pow(1.0 - NdotV, _FresnelPow);

            // Soft depth edge fade (scene depth – water surface)
            float4 sp     = UNITY_PROJ_COORD(IN.screenPos);
            float sceneZ  = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, sp));
            float surfZ   = IN.screenPos.w;
            float depthFade = saturate((sceneZ - surfZ) / max(_DepthFade, 0.01));

            // Color blend shallow<->deep by depth + fresnel
            fixed4 col = lerp(_ShallowColor, _DeepColor, saturate(depthFade + fresnel * 0.4));

            // Foam along shallow edges
            float2 uvF = IN.worldPos.xz * 0.5 + t * 0.05;
            float foam = tex2D(_FoamTex, uvF).r;
            col.rgb   = lerp(col.rgb, float3(1,1,1), foam * _FoamAmount * (1.0 - depthFade));

            o.Albedo     = col.rgb;
            o.Normal     = n;
            o.Smoothness = _Glossiness;
            o.Metallic   = _Metallic;
            o.Alpha      = col.a;
        }
        ENDCG
    }

    FallBack "Transparent/Diffuse"
}
