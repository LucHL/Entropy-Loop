Shader "Void/GrassWind"
{
    Properties
    {
        _MainTex       ("Grass Texture (RGBA)",  2D)          = "white" {}
        _Cutoff        ("Alpha Cutoff",          Range(0,1))  = 0.35
        _TopColor      ("Tip Color",             Color)       = (0.45, 0.72, 0.20, 1)
        _BottomColor   ("Base Color",            Color)       = (0.18, 0.38, 0.10, 1)
        _WindStrength  ("Wind Strength",         Range(0,1))  = 0.25
        _WindSpeed     ("Wind Speed",            Float)       = 1.2
        _WindFrequency ("Wind Frequency",        Float)       = 0.8
        _Glossiness    ("Smoothness",            Range(0,1))  = 0.1
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "DisableBatching"="True" }
        LOD 200
        Cull Off   // two-sided

        CGPROGRAM
        // Built-in Render Pipeline — alphatest (cutout) surface shader
        #pragma surface surf Standard alphatest:_Cutoff addshadow fullforwardshadows vertex:vert
        #pragma target 3.0
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        fixed4    _TopColor, _BottomColor;
        float     _WindStrength, _WindSpeed, _WindFrequency;
        float     _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            // Wind displacement: only affects upper part of blade (texcoord.y ~ height)
            float height = v.texcoord.y;   // 0=root, 1=tip
            float phase  = dot(v.vertex.xyz, float3(1,0,1)) * _WindFrequency
                         + _Time.y * _WindSpeed;
            float swing  = sin(phase) * _WindStrength * height * height;

            // Displace in XZ, keep root planted
            v.vertex.x += swing * 0.6;
            v.vertex.z += swing * 0.4;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);

            // Height gradient: bottom colour -> tip colour
            fixed4 col = lerp(_BottomColor, _TopColor, IN.uv_MainTex.y);
            col.rgb   *= tex.rgb;

            // Subtle ambient occlusion at blade base
            float ao = lerp(0.55, 1.0, IN.uv_MainTex.y);

            o.Albedo     = col.rgb;
            o.Alpha      = tex.a;
            o.Smoothness = _Glossiness;
            o.Metallic   = 0.0;
            o.Occlusion  = ao;
        }
        ENDCG
    }

    FallBack "Nature/Soft Occlusion Leaves"
}
