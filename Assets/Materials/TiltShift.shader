Shader "Hidden/URP/TiltShift"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _BlurTex ("BlurTex", 2D) = "white" {}
        _BlurAmount ("Blur Amount", Float) = 2.5
        _FocusPos ("Focus Position", Float) = 0.5
        _FocusRange ("Focus Range", Float) = 0.15
        _Samples ("Samples", Int) = 8
        _TiltDir ("Tilt Direction", Vector) = (0,1,0,0)
        _BlurDir ("Blur Direction", Vector) = (1,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        // --- shared definitions ---
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // Vertex input / output
        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
        };

        Varyings VertFullScreen(Attributes IN)
        {
            Varyings OUT;
            OUT.positionCS = TransformObjectToHClip(IN.positionOS);
            OUT.uv = IN.uv;
            return OUT;
        }

        // helper gaussian
        float gaussian(float x, float sigma)
        {
            return exp(- (x * x) / (2.0 * sigma * sigma));
        }

        // safe normalize for floats
        float2 safe_normalize(float2 v)
        {
            float len = length(v);
            if (len > 1e-6) return v / len;
            return float2(0,1);
        }

        ENDHLSL

        // --- pass 0: separable blur (reads _MainTex) ---
        Pass
        {
            Name "SeparableBlur"
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex VertFullScreen
            #pragma fragment FragBlur
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _BlurAmount;
            int _Samples;
            float4 _BlurDir;

            float4 FragBlur(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                int samples = max(1, _Samples);
                float sigma = max(0.5, _BlurAmount);

                // axis takes aspect into account (so blur looks consistent)
                float2 axis = safe_normalize(_BlurDir.xy);
                axis *= float2(1.0, _MainTex_TexelSize.x / _MainTex_TexelSize.y);

                float weightSum = 0.0;
                float4 col = 0;

                // sample symmetric taps
                for (int s = -samples; s <= samples; ++s)
                {
                    float offs = (float)s;
                    float w = gaussian(offs, sigma);
                    float2 sampleUV = uv + axis * offs * (_BlurAmount / 100.0);
                    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV) * w;
                    weightSum += w;
                }

                col /= weightSum;
                return col;
            }
            ENDHLSL
        }

        // --- pass 1: composite original + blurred using focus mask ---
        Pass
        {
            Name "Composite"
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex VertFullScreen
            #pragma fragment FragComposite
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BlurTex);
            SAMPLER(sampler_BlurTex);
            float _FocusPos;
            float _FocusRange;
            float4 _TiltDir;

            float4 FragComposite(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 centerUV = uv - 0.5;
                float2 tilt = safe_normalize(_TiltDir.xy);
                float signedDist = dot(centerUV, tilt);
                float focusSigned = (_FocusPos - 0.5);
                float dist = abs(signedDist - focusSigned);
                float edge = saturate((dist - _FocusRange * 0.5) / (_FocusRange * 0.5 + 1e-6));

                float4 original = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float4 blurred  = SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, uv);

                return lerp(original, blurred, edge);
            }
            ENDHLSL
        } // Pass Composite
    } // SubShader
} // Shader
