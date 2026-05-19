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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
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
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float patch = Hash21(floor(input.uv * _NoiseScale));
                float fine = Hash21(floor(input.uv * _NoiseScale * 4.0));
                float mixValue = saturate(patch * 0.55 + fine * 0.25);
                float3 albedo = lerp(_NoiseColor.rgb, _BaseColor.rgb, mixValue);
                float3 normalWS = normalize(input.normalWS);

                Light mainLight = GetMainLight();
                float mainNdotL = saturate(dot(normalWS, mainLight.direction));
                float3 lighting = mainLight.color * mainNdotL;

                #if defined(_ADDITIONAL_LIGHTS)
                uint additionalLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < additionalLightCount; lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    float ndotl = saturate(dot(normalWS, light.direction));
                    lighting += light.color * ndotl * light.distanceAttenuation * light.shadowAttenuation;
                }
                #endif

                float3 ambient = albedo * 0.035;
                float3 litColor = ambient + albedo * lighting;
                return half4(litColor, 1);
            }
            ENDHLSL
        }
    }
}
