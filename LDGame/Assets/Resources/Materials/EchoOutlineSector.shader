Shader "Custom/EchoOutlineSector"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _AlphaMul ("Alpha Multiplier", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "UniversalMaterialType"="Unlit"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SpriteUnlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_SCANS 16

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                float2 worldPos   : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _AlphaMul;
            CBUFFER_END

            int    _EchoScanCount;
            float4 _EchoOrigins[MAX_SCANS];
            float4 _EchoForwards[MAX_SCANS];
            float  _EchoBirthTimes[MAX_SCANS];

            float _EchoRadius;
            float _EchoCosHalfAngle;

            float _EchoHoldTime;
            float _EchoFadeTime;

            float _EchoEdgeSoftCos;     // halfAngle - 3° 对应的 cos
            float _EchoEdgeAlphaMin;    // 边缘最低 alpha，例如 0.85

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionCS = posInputs.positionCS;
                OUT.worldPos = posInputs.positionWS.xy;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;

                return OUT;
            }

            float ComputeLifeAlpha(float age, float holdTime, float fadeTime)
            {
                if (age < 0) return 0;

                if (age <= holdTime)
                    return 1.0;

                float t = (age - holdTime) / max(fadeTime, 0.0001);
                return saturate(1.0 - t);
            }

            float ComputeEdgeAlpha(float dotVal, float hardCos, float softCos, float edgeMinAlpha)
            {
                // dotVal 越大越靠扇形中心，越小越靠边
                // hardCos = 真正边界
                // softCos = 内缩 3° 后的边界（更大）
                //
                // 中间区域 alpha = 1
                // 在 softCos ~ hardCos 之间平滑降到 edgeMinAlpha

                if (dotVal < hardCos)
                    return 0.0;

                if (dotVal >= softCos)
                    return 1.0;

                float t = saturate((dotVal - hardCos) / max(softCos - hardCos, 0.0001));
                return lerp(edgeMinAlpha, 1.0, t);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col = tex * IN.color;

                float bestAlpha = 0.0;
                float2 worldPos = IN.worldPos;
                float now = _Time.y;

                [unroll]
                for (int i = 0; i < MAX_SCANS; i++)
                {
                    if (i >= _EchoScanCount) break;

                    float2 origin = _EchoOrigins[i].xy;
                    float2 forward = normalize(_EchoForwards[i].xy);

                    float2 toPixel = worldPos - origin;
                    float dist = length(toPixel);

                    if (dist > _EchoRadius)
                        continue;

                    float2 dir = (dist > 0.0001) ? (toPixel / dist) : forward;
                    float d = dot(dir, forward);

                    if (d < _EchoCosHalfAngle)
                        continue;

                    float age = now - _EchoBirthTimes[i];
                    float lifeAlpha = ComputeLifeAlpha(age, _EchoHoldTime, _EchoFadeTime);
                    if (lifeAlpha <= 0.0)
                        continue;

                    float edgeAlpha = ComputeEdgeAlpha(d, _EchoCosHalfAngle, _EchoEdgeSoftCos, _EchoEdgeAlphaMin);

                    float a = lifeAlpha * edgeAlpha;
                    bestAlpha = max(bestAlpha, a);
                }

                col.a *= _AlphaMul * bestAlpha;
                return col;
            }
            ENDHLSL
        }
    }
}