Shader "Custom/EchoWaveSector"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _OriginWS ("Origin WS", Vector) = (0,0,0,0)
        _ForwardWS ("Forward WS", Vector) = (1,0,0,0)

        _Radius ("Radius", Float) = 5
        _AngleDeg ("Angle Deg", Float) = 60

        _Progress ("Progress", Range(0,1)) = 0
        _FrontWidth ("Front Width", Float) = 0.3
        _TrailWidth ("Trail Width", Float) = 1.2
        _TrailAlpha ("Trail Alpha", Range(0,1)) = 0.25

        _EdgeSoftDeg ("Edge Soft Deg", Float) = 3
        _EdgeAlphaMin ("Edge Alpha Min", Range(0,1)) = 0.85

        _ArcLineWidth ("Arc Line Width", Float) = 0.18
        _ArcLineAlpha ("Arc Line Alpha", Range(0,2)) = 1.2

        _GlobalAlpha ("Global Alpha", Range(0,1)) = 1
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

                float4 _OriginWS;
                float4 _ForwardWS;

                float _Radius;
                float _AngleDeg;

                float _Progress;
                float _FrontWidth;
                float _TrailWidth;
                float _TrailAlpha;

                float _EdgeSoftDeg;
                float _EdgeAlphaMin;

                float _ArcLineWidth;
                float _ArcLineAlpha;

                float _GlobalAlpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionCS = posInputs.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                OUT.worldPos = posInputs.positionWS.xy;

                return OUT;
            }

            float ComputeEdgeAlpha(float dotVal, float hardCos, float softCos, float edgeMinAlpha)
            {
                if (dotVal < hardCos)
                    return 0.0;

                if (dotVal >= softCos)
                    return 1.0;

                float t = saturate((dotVal - hardCos) / max(softCos - hardCos, 0.0001));
                return lerp(edgeMinAlpha, 1.0, t);
            }

            float ComputeWaveFillAlpha(float dist, float waveRadius,
                                       float frontWidth, float trailWidth, float trailAlpha)
            {
                if (dist > waveRadius)
                    return 0.0;

                float front = 1.0 - saturate(abs(dist - waveRadius) / max(frontWidth, 0.0001));

                float behind = waveRadius - dist;
                float trail = 0.0;

                if (behind >= 0.0)
                {
                    float t = 1.0 - saturate(behind / max(trailWidth, 0.0001));
                    trail = t * trailAlpha;
                }

                return max(front, trail);
            }

            float ComputeArcLineAlpha(float dist, float waveRadius, float arcLineWidth, float arcLineAlpha)
            {
                float arcMask = 1.0 - saturate(abs(dist - waveRadius) / max(arcLineWidth, 0.0001));
                return arcMask * arcLineAlpha;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col = tex * IN.color;

                float2 origin = _OriginWS.xy;
                float2 forward = normalize(_ForwardWS.xy);

                if (length(forward) < 0.0001)
                    forward = float2(1.0, 0.0);

                float2 toPixel = IN.worldPos - origin;
                float dist = length(toPixel);

                // ³¬³ö×î´ó°ë¾¶
                if (dist > _Radius)
                {
                    col.a = 0;
                    return col;
                }

                float2 dir = (dist > 0.0001) ? normalize(toPixel) : forward;

                float halfAngle = radians(_AngleDeg * 0.5);
                float hardCos = cos(halfAngle);

                float innerHalfAngle = max(0.0, halfAngle - radians(_EdgeSoftDeg));
                float softCos = cos(innerHalfAngle);

                float d = dot(dir, forward);

                float edgeAlpha = ComputeEdgeAlpha(d, hardCos, softCos, _EdgeAlphaMin);
                if (edgeAlpha <= 0.0)
                {
                    col.a = 0;
                    return col;
                }

                float waveRadius = saturate(_Progress) * _Radius;

                float fillAlpha = ComputeWaveFillAlpha(
                    dist,
                    waveRadius,
                    _FrontWidth,
                    _TrailWidth,
                    _TrailAlpha
                );

                float arcAlpha = 0.0;
                if (dist <= waveRadius)
                {
                    arcAlpha = ComputeArcLineAlpha(
                        dist,
                        waveRadius,
                        _ArcLineWidth,
                        _ArcLineAlpha
                    );
                }

                float finalWaveAlpha = max(fillAlpha, arcAlpha);

                if (finalWaveAlpha <= 0.0)
                {
                    col.a = 0;
                    return col;
                }

                col.a *= edgeAlpha * finalWaveAlpha * _GlobalAlpha;
                return col;
            }
            ENDHLSL
        }
    }
}