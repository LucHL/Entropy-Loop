Shader "Void/IslandGPU"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.31, 0.55, 0.28, 1)
        _IslandRadius("Island Radius", Float) = 60
        _IslandEdgeFalloff("Island Edge Falloff", Float) = 8
        _IslandMaxHeight("Island Max Height", Float) = 6
        _IslandSeed("Island Seed", Float) = 12345
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
            };

            float4 _BaseColor;
            float  _IslandRadius;
            float  _IslandEdgeFalloff;
            float  _IslandMaxHeight;
            float  _IslandSeed;

            // hash + value noise simple
            float2 hash2(float2 p)
            {
                float x = dot(p, float2(127.1, 311.7));
                float y = dot(p, float2(269.5, 183.3));
                return frac(sin(float2(x, y)) * 43758.5453);
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float2 a = hash2(i);
                float2 b = hash2(i + float2(1, 0));
                float2 c = hash2(i + float2(0, 1));
                float2 d = hash2(i + float2(1, 1));

                float v1 = lerp(a.x, b.x, u.x);
                float v2 = lerp(c.x, d.x, u.x);
                return lerp(v1, v2, u.y);
            }

            float ComputeHeight(float3 posWS)
            {
                float2 p = posWS.xz;
                float r = length(p);

                float edge = saturate((r - (_IslandRadius - _IslandEdgeFalloff)) / _IslandEdgeFalloff);

                float2 noisePos = (p + float2(_IslandSeed, -_IslandSeed)) * 0.045;
                float baseH = noise2D(noisePos) * _IslandMaxHeight;

                baseH *= (1.0 - edge * 0.95);
                return baseH;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                float h = ComputeHeight(positionWS);
                positionWS.y = h;

                float3 normalWS = float3(0, 1, 0);

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = normalWS;
                OUT.positionWS = positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                float3 l = normalize(float3(0.4, 1.0, 0.3));
                float ndotl = saturate(dot(n, l));

                float3 col = _BaseColor.rgb * (0.3 + 0.7 * ndotl);
                return half4(col, 1.0);
            }

            ENDHLSL
        }
    }
}
