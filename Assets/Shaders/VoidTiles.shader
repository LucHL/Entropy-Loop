Shader "Void/VoidTiles"
{
    Properties
    {
        _Color("Base Color", Color) = (0.3, 0.0, 0.5, 1.0)
        _PortalColor("Portal Color", Color) = (1.0, 0.2, 1.0, 1.0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            StructuredBuffer<float4> _Tiles; // xyz = pos, w = isPortal

            struct appdata
            {
                float3 vertex : POSITION;
                float3 normal : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float  isPortal : TEXCOORD1;
            };

            float4 _Color;
            float4 _PortalColor;

            v2f vert(appdata v)
            {
                v2f o;

                float4 tile = _Tiles[v.instanceID];
                float3 worldPos = v.vertex + tile.xyz;

                o.pos = UnityObjectToClipPos(float4(worldPos, 1.0));
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.isPortal = tile.w;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float ndotl = saturate(dot(normalize(i.normal), normalize(float3(0.3, 1.0, 0.2))));
                fixed4 baseCol = _Color;
                fixed4 portalCol = _PortalColor;

                fixed4 col = lerp(baseCol, portalCol, step(0.5, i.isPortal));
                col.rgb *= (0.3 + ndotl * 0.7);

                return col;
            }
            ENDCG
        }
    }
}
