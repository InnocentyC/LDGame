Shader "Custom/EchoGoldFlashSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _FlashColor ("Flash Color", Color) = (1.0, 0.84, 0.2, 1.0)
        _FlashStrength ("Flash Strength", Range(0, 3)) = 1.2
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0
        _BlinkFrequency ("Blink Frequency", Range(0, 30)) = 8
        _BlinkMin ("Blink Min", Range(0, 1)) = 0.25
        _BaseKeep ("Base Keep", Range(0, 1)) = 0.85
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
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _FlashColor;
                float  _FlashStrength;
                float  _FlashAmount;
                float  _BlinkFrequency;
                float  _BlinkMin;
                float  _BaseKeep;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col = tex * IN.color;

                // 保持 alpha 不变，只处理可见像素
                if (col.a <= 0.001h)
                    return col;

                float pulse = sin(_Time.y * _BlinkFrequency * 6.2831853) * 0.5 + 0.5;
                pulse = lerp(_BlinkMin, 1.0, pulse);

                float flash = saturate(_FlashAmount * pulse);

                float3 baseCol = col.rgb;
                float3 goldCol = baseCol * _BaseKeep + _FlashColor.rgb * _FlashStrength;

                col.rgb = lerp(baseCol, goldCol, flash);

                return col;
            }
            ENDHLSL
        }
    }
}