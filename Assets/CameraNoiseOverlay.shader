Shader "Horror/CameraNoiseOverlay"
{
    Properties
    {
        _NoiseLevel ("Noise Level", Range(0, 1)) = 0.2
        _NoiseScale ("Noise Scale", Float) = 160
        _FlickerSpeed ("Flicker Speed", Float) = 22
        _BlockNoiseLevel ("Block Noise Level", Range(0, 1)) = 0.25
        _BlockNoiseCenter ("Block Noise Center", Vector) = (0.5, 0.5, 0, 0)
        _BlockNoiseRegionSize ("Block Noise Region Size", Vector) = (0.24, 0.12, 0, 0)
        _BlockNoiseGrid ("Block Noise Grid", Vector) = (8, 5, 0, 0)
        _RgbGlitchLevel ("RGB Glitch Level", Range(0, 1)) = 0.18
        _RgbGlitchOffset ("RGB Glitch Offset", Float) = 0.012
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "CameraNoiseOverlay"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float _NoiseLevel;
                float _NoiseScale;
                float _FlickerSpeed;
                float _BlockNoiseLevel;
                float2 _BlockNoiseCenter;
                float2 _BlockNoiseRegionSize;
                float2 _BlockNoiseGrid;
                float _RgbGlitchLevel;
                float _RgbGlitchOffset;
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
                float frameSeed = floor(_Time.y * _FlickerSpeed);
                float grain = Hash21(floor(input.uv * _NoiseScale) + frameSeed);
                float scanline = sin((input.uv.y + _Time.y * 0.35) * 900.0) * 0.5 + 0.5;
                float2 blockDistance = abs(input.uv - _BlockNoiseCenter);
                float2 blockMaskAxis = step(blockDistance, _BlockNoiseRegionSize * 0.5);
                float blockMask = blockMaskAxis.x * blockMaskAxis.y;

                float2 regionUv = (input.uv - (_BlockNoiseCenter - _BlockNoiseRegionSize * 0.5)) / max(_BlockNoiseRegionSize, 0.0001);
                float2 blockCell = floor(saturate(regionUv) * max(_BlockNoiseGrid, 1.0));
                float blockRandom = Hash21(blockCell + frameSeed * 3.71);
                float blockStripe = step(0.38, Hash21(float2(blockCell.y, frameSeed)));
                float blockNoise = blockMask * step(0.42, blockRandom) * lerp(0.35, 1.0, blockRandom) * blockStripe;

                float rgbWave = Hash21(floor(float2(input.uv.y * 28.0, frameSeed))) * 2.0 - 1.0;
                float rgbShift = rgbWave * _RgbGlitchOffset;
                float redNoise = Hash21(floor((input.uv + float2(rgbShift, 0.0)) * _NoiseScale) + frameSeed);
                float blueNoise = Hash21(floor((input.uv - float2(rgbShift, 0.0)) * _NoiseScale) + frameSeed + 19.13);

                float baseIntensity = saturate((grain * 0.75 + scanline * 0.25) * _NoiseLevel);
                float blockIntensity = blockNoise * _BlockNoiseLevel;
                float rgbMask = saturate((_RgbGlitchLevel + blockNoise * _BlockNoiseLevel) * step(0.55, abs(rgbWave)));

                float red = saturate(baseIntensity + blockIntensity + redNoise * rgbMask);
                float green = saturate(baseIntensity + blockIntensity * 0.55);
                float blue = saturate(baseIntensity + blockIntensity + blueNoise * rgbMask);
                float alpha = saturate(max(max(red, green), blue));
                return half4(red, green, blue, alpha);
            }
            ENDHLSL
        }
    }
}
