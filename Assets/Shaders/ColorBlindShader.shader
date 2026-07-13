Shader "Hidden/ColorBlindFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mode ("Mode", Int) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

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
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            int _Mode; // 0 = none, 1 = protanopia, 2 = deuteranopia, 3 = tritanopia

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 c = col.rgb;
                float3 result = c;

                if (_Mode == 1) // Protanopia
                {
                    result.r = 0.567*c.r + 0.433*c.g + 0.0*c.b;
                    result.g = 0.558*c.r + 0.442*c.g + 0.0*c.b;
                    result.b = 0.0*c.r   + 0.242*c.g + 0.758*c.b;
                }
                else if (_Mode == 2) // Deuteranopia
                {
                    result.r = 0.625*c.r + 0.375*c.g + 0.0*c.b;
                    result.g = 0.70*c.r  + 0.30*c.g  + 0.0*c.b;
                    result.b = 0.0*c.r   + 0.30*c.g  + 0.70*c.b;
                }
                else if (_Mode == 3) // Tritanopia
                {
                    result.r = 0.95*c.r  + 0.05*c.g  + 0.0*c.b;
                    result.g = 0.0*c.r   + 0.433*c.g + 0.567*c.b;
                    result.b = 0.0*c.r   + 0.475*c.g + 0.525*c.b;
                }

                return fixed4(result, col.a);
            }
            ENDCG
        }
    }
}