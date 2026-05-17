Shader "Horror/GrasslandGround"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.18, 0.34, 0.13, 1)
        _NoiseColor ("Noise Color", Color) = (0.08, 0.19, 0.07, 1)
        _NoiseScale ("Noise Scale", Float) = 42
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "GrasslandGround"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _NoiseColor;
                float _NoiseScale;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float patch = Hash21(floor(input.uv * _NoiseScale));
                float fine = Hash21(floor(input.uv * _NoiseScale * 4.0));
                float mixValue = saturate(patch * 0.55 + fine * 0.25);
                return half4(lerp(_NoiseColor.rgb, _BaseColor.rgb, mixValue), 1);
            }
            ENDHLSL
        }
    }
}
