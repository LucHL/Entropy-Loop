Shader "Custom/SeamlessHeatWaveEffect"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _DistortionTex("Distortion Texture", 2D) = "white" {}
        _Distortion("Distortion", Range(0, 0.2)) = 0.05
        _Speed("Speed", Float) = 0.5
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100
 
            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"
 
                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };
 
                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    float2 distortionUV : TEXCOORD1;
                };
 
                sampler2D _MainTex;
                sampler2D _DistortionTex;
                float _Distortion;
                float _Speed;
 
                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    o.distortionUV = v.uv + float2(0, -_Time.y * _Speed);
                    return o;
                }
 
                fixed4 frag(v2f i) : SV_Target
                {
                    float2 uv = i.distortionUV;
                    uv.y = frac(uv.y);
                    float2 distortion = tex2D(_DistortionTex, uv).rg * _Distortion;
                    fixed4 col = tex2D(_MainTex, i.uv + distortion);
                    return col;
                }
                ENDCG
            }
        }
            FallBack "Diffuse"
}
