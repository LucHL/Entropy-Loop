Shader "Void/BlackHoleLens"
{
    // Lentille gravitationnelle : déforme l'arrière-plan (GrabPass) autour de la
    // silhouette d'une coquille sphérique — l'image se courbe et tourbillonne
    // près du bord, comme la lumière happée par un trou noir. Pipeline Built-in.
    Properties
    {
        _Strength ("Distortion Strength", Range(0, 0.4)) = 0.14
        _Power    ("Edge Power",          Range(0.5, 8)) = 2.5
        _Swirl    ("Swirl",               Range(-3, 3))  = 0.8
        _Tint     ("Edge Tint",           Color)         = (0.55, 0.35, 0.85, 1)
        _TintAmt  ("Tint Amount",         Range(0, 1))   = 0.30
    }

    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }

        // Capture de l'écran rendu jusqu'ici (le trou noir et le décor)
        GrabPass { "_BHLensGrab" }

        Pass
        {
            ZWrite Off
            Cull Back
            Blend Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _BHLensGrab;
            float  _Strength, _Power, _Swirl, _TintAmt;
            fixed4 _Tint;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f
            {
                float4 pos     : SV_POSITION;
                float4 grab    : TEXCOORD0;
                float3 vnormal : TEXCOORD1;
                float3 vdir    : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos     = UnityObjectToClipPos(v.vertex);
                o.grab    = ComputeGrabScreenPos(o.pos);
                o.vnormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                o.vdir    = normalize(UnityObjectToViewPos(v.vertex));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n  = normalize(i.vnormal);
                float3 vd = normalize(i.vdir);
                // 0 au centre face caméra, 1 sur la silhouette (le bord)
                float rim = pow(saturate(1.0 - abs(dot(n, -vd))), _Power);

                float2 radial  = normalize(n.xy + 1e-5);
                float2 tangent = float2(-radial.y, radial.x);
                float2 offset  = (radial * _Strength + tangent * (_Strength * _Swirl)) * rim;

                float2 uv  = i.grab.xy / i.grab.w + offset;
                fixed4 col = tex2D(_BHLensGrab, uv);
                col.rgb    = lerp(col.rgb, col.rgb * _Tint.rgb, _TintAmt * rim);
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
