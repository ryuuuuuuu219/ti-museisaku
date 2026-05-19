Shader "Phase1/StarrySkyTransition"
{
    Properties
    {
        _SkyTopColor ("Sky Top Color", Color) = (0.005, 0.012, 0.035, 1)
        _SkyHorizonColor ("Sky Horizon Color", Color) = (0.018, 0.026, 0.055, 1)
        _StarColor ("Star Color", Color) = (0.86, 0.92, 1, 1)
        _StarDensity ("Star Density", Range(0, 1)) = 0.72
        _StarSharpness ("Star Sharpness", Range(8, 80)) = 42
        _TwinkleSpeed ("Twinkle Speed", Range(0, 8)) = 1.4
        _FillColor ("Transition Fill Color", Color) = (0, 0, 0, 1)
        _FillAmount ("Transition Fill Amount", Range(0, 1)) = 0
        _TransitionOriginWS ("Transition Origin WS", Vector) = (0, 1, 0, 0)
        _TransitionExpansionRate ("Transition Expansion Rate", Range(0.1, 100)) = 1
        _FillEdgeSoftness ("Transition Edge Softness", Range(0.001, 0.35)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 directionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _SkyTopColor;
                float4 _SkyHorizonColor;
                float4 _StarColor;
                float _StarDensity;
                float _StarSharpness;
                float _TwinkleSpeed;
                float4 _FillColor;
                float _FillAmount;
                float3 _TransitionOriginWS;
                float _TransitionExpansionRate;
                float _FillEdgeSoftness;
            CBUFFER_END

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.directionWS = normalize(TransformObjectToWorld(input.positionOS.xyz));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.directionWS);
                float horizon = saturate(dir.y * 0.5 + 0.5);
                float3 sky = lerp(_SkyHorizonColor.rgb, _SkyTopColor.rgb, pow(horizon, 0.72));

                float3 starCell = floor(dir * 180.0);
                float starSeed = Hash31(starCell);
                float starMask = step(1.0 - _StarDensity * 0.055, starSeed);
                float starCore = pow(Hash31(starCell + 19.73), _StarSharpness);
                float twinkle = lerp(0.55, 1.25, Hash31(starCell + floor(_Time.y * _TwinkleSpeed)));
                float horizonFade = smoothstep(0.08, 0.45, dir.y);
                float stars = saturate(starMask * starCore * twinkle * horizonFade);

                float3 color = sky + _StarColor.rgb * stars;

                float3 originDir = normalize(_TransitionOriginWS);
                float originDistance = distance(dir, originDir);
                float fillRadius = saturate(_FillAmount) * _TransitionExpansionRate;
                float fillMask = 1.0 - smoothstep(fillRadius - _FillEdgeSoftness, fillRadius + _FillEdgeSoftness, originDistance);
                color = lerp(color, _FillColor.rgb, fillMask);

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
