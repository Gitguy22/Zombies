Shader "Hidden/TonicPostProcessing/TonicTonemapper"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        // LUT Properties
        _LutTex1 ("LUT Texture 1", 2D) = "black" {}
        _LutTex2 ("LUT Texture 2", 2D) = "black" {}
        _LutBlend ("LUT Blend", Range(0, 1)) = 0.0
        _LutDim ("LUT Dimension (N for NxNxN)", Float) = 32.0 // For 1024x32 LUT, N=32


    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" "ForceNoShadowCasting"="True" "IgnoreProjector"="True" }
        LOD 150
        ZTest Always    ZWrite Off  Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv     : TEXCOORD0;
                float4 pos    : SV_POSITION;
                float4 scrPos : TEXCOORD1; 
            };


            sampler2D _MainTex;
            sampler2D _LutTex1;
            sampler2D _LutTex2;

            float     _Exposure;
            float     _FilmicContrast;
            float     _Saturation;
            float4    _ShadowTint;
            float4    _HighlightTint;
            float     _Temperature;
            float     _Tint;
            float     _DitherStrength;
            float     _LutBlend;
            float     _LutDim;


            inline float Luma(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }


            inline float3 TonemapStylised(float3 x)
            {
                const float a = 2.51;
                const float b = 0.03;
                const float c = 2.43;
                const float d = 0.59;
                const float e = 0.14;
                float3 y = saturate((x * (a * x + b)) / (x * (c * x + d) + e));

                float  l        = Luma(y);
                float  liftAmt  = (1.0 - smoothstep(0.0, 0.4, l)) * 0.08; 
                y += liftAmt;

                return saturate(y);
            }

            inline float3 ApplyFilmicContrast(float3 c, float contrast)
            {
                const float pivot = 0.18f;
                c = (c - pivot) * contrast + pivot;
                float k = max(0.0f, contrast - 1.0f);
                c = (c * (1.0f + k)) / (1.0f + k * abs(c));
                return saturate(c);
            }

            inline float3 ApplyTints(float3 c, float4 sh, float4 hi)
            {
                float l = Luma(c);
                float shMask = 1.0f - smoothstep(0.04f, 0.45f, l);
                float hiMask =       smoothstep(0.55f, 1.00f, l);
                c = lerp(c, c * sh.rgb, sh.a * shMask);
                c = lerp(c, c * hi.rgb, hi.a * hiMask);
                return c;
            }

            inline float3 ApplyWhiteBalance(float3 c, float temp, float tnt)
            {
                float3 wb = float3(
                    1.0f + temp * 0.10f + tnt * 0.05f,
                    1.0f                 - tnt * 0.10f,
                    1.0f - temp * 0.10f + tnt * 0.05f);
                return c * wb;
            }

            inline float3 ApplySaturation(float3 c, float sat)
            {
                float l = Luma(c);
                return lerp(float3(l, l, l), c, sat);
            }

  
            float3 SampleLUT(sampler2D lut, float3 color, float lutDimN)
            {
                color = saturate(color);
                float N = lutDimN;
                float N1 = N - 1.0f;
                float3 coords = color * N1;
                float b0 = floor(coords.b);
                float b1 = ceil(coords.b);
                float f  = coords.b - b0;
                float u0 = (b0 * N + coords.r + 0.5f) / (N * N);
                float u1 = (min(b1, N1) * N + coords.r + 0.5f) / (N * N);
                float v  = (coords.g + 0.5f) / N;
                float3 c0 = tex2D(lut, float2(u0, v)).rgb;
                float3 c1 = tex2D(lut, float2(u1, v)).rgb;
                return lerp(c0, c1, f);
            }

 
            inline float2 R2(uint i)
            {
                i = (i << 16u) | (i >> 16u);
                i = ((i & 0x55555555u) << 1u) | ((i & 0xAAAAAAAAu) >> 1u);
                i = ((i & 0x33333333u) << 2u) | ((i & 0xCCCCCCCCu) >> 2u);
                i = ((i & 0x0F0F0F0Fu) << 4u) | ((i & 0xF0F0F0F0u) >> 4u);
                i = ((i & 0x00FF00FFu) << 8u) | ((i & 0xFF00FF00u) >> 8u);
                uint vdc = i;
                return float2((float)vdc * 2.3283064365386963e-10f, frac((float)i * 0.61803398875f));
            }

            inline float3 ApplyDither(float3 c, float4 scrPos, float str)
            {
                if (str <= 0.001f) return c;
                uint2 pix = (uint2)scrPos.xy;
                uint  id  = pix.x + pix.y * (uint)_ScreenParams.x;
                float2 n  = R2(id);
                float tpdf = (n.x + n.y) - 1.0f;
                float amt = tpdf * str * (1.0f / 255.0f);
                float preserve = smoothstep(0.0f, 0.02f, Luma(c));
                return c + amt * preserve;
            }


            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.pos    = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                o.scrPos = ComputeScreenPos(o.pos);
                return o;
            }


            float4 Frag(Varyings i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).rgb * _Exposure;

                col = TonemapStylised(col);

                col = ApplyFilmicContrast(col, _FilmicContrast);
                col = ApplyTints          (col, _ShadowTint, _HighlightTint);
                col = ApplyWhiteBalance   (col, _Temperature, _Tint);
                col = ApplySaturation     (col, _Saturation);

                if (_LutDim >= 2.0f)
                {
                    float3 colLut1 = SampleLUT(_LutTex1, col, _LutDim);
                    float3 colLut2 = SampleLUT(_LutTex2, col, _LutDim);
                    col = lerp(colLut1, colLut2, saturate(_LutBlend));
                }

                col = ApplyDither(col, i.scrPos, _DitherStrength);

                return float4(saturate(col), 1.0f);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
