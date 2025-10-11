Shader "Hidden/TonicLensEffectsShader"
{
    Properties
    {
        _MainTex                      ("Texture",                       2D)            = "white" {}
        _DistortionAmount             ("Distortion Amount",             Range(-1,1))   = 0
        _DistortionCenter             ("Distortion Center (UV)",        Vector)        = (0.5, 0.5, 0, 0)
        _ChromaticAberrationAmount    ("Chromatic Aberration Amount",   Range(0,0.2))  = 0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "LensEffectsPass"

            CGPROGRAM
            #pragma vertex   vert_img
            #pragma fragment frag_lens_effects
            #include "UnityCG.cginc"

            // ────────── uniforms ──────────
            sampler2D _MainTex;
            float4    _MainTex_TexelSize;      
            float     _DistortionAmount;
            float2    _DistortionCenter;
            float     _ChromaticAberrationAmount;


            float2 DistortUV_BrownConrady(float2 uv, float2 center, float k1)
            {
                if (abs(k1) < 1e-6f) return uv;          

                float2 d      = uv - center;             
                float  aspect = _ScreenParams.x / _ScreenParams.y;

                float2 dCorr = d;
                dCorr.y *= (aspect > 1.0) ? aspect : 1.0;
                dCorr.x /= (aspect <= 1.0) ? aspect : 1.0;

                float r2       = dot(dCorr, dCorr);     
                float radial   = 1.0 + k1 * r2;        

                if (k1 > 0.0f)
                {
                    float2 cornerCorr  = float2(0.5,0.5);
                    cornerCorr.y      *= (aspect > 1.0) ? aspect : 1.0;
                    cornerCorr.x      /= (aspect <= 1.0) ? aspect : 1.0;
                    float cornerScale  = 1.0 + k1 * dot(cornerCorr, cornerCorr);
                    radial            /= max(cornerScale, 1e-5f);
                    return center + d * radial;
                }
                else
                {

                    float2 hCorr   = float2(0.5, 0.0);
                    float2 vCorr   = float2(0.0, 0.5);
                    hCorr.y       *= (aspect > 1.0) ? aspect : 1.0;
                    vCorr.x       /= (aspect <= 1.0) ? aspect : 1.0;

                    float scaleH   = 1.0 + k1 * dot(hCorr, hCorr);   // shrink along X
                    float scaleV   = 1.0 + k1 * dot(vCorr, vCorr);   // shrink along Y

                    scaleH = max(scaleH, 1e-5f);
                    scaleV = max(scaleV, 1e-5f);

                    float2 dDist;
                    dDist.x = d.x * radial / scaleH;
                    dDist.y = d.y * radial / scaleV;
                    return center + dDist;
                }
            }

            float2 GetChromaticShiftVector(float2 baseUV, float2 center, float chromaAmt)
            {
                float2 aspect      = float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                float2 dirAspect   = (baseUV - center) * aspect;
                float  r2          = dot(dirAspect, dirAspect);
                if (r2 < 1e-8f) return 0;

                float2 dirNorm     = normalize(dirAspect);
                float  shiftMag    = chromaAmt * pow(r2, 2.5f);     
                float2 dirUV       = dirNorm / aspect;
                return dirUV * shiftMag;
            }

            float SampleSoftChannel(sampler2D tex, float2 uvShifted,
                                    float2 shiftDirUV, int channel)
            {
                float  step       = 0.5f;
                float2 texelStep  = shiftDirUV * _MainTex_TexelSize.xy * step;
                float s1 = tex2D(tex, saturate(uvShifted - texelStep))[channel];
                float s2 = tex2D(tex, saturate(uvShifted))[channel];
                float s3 = tex2D(tex, saturate(uvShifted + texelStep))[channel];
                return s1 * 0.25f + s2 * 0.5f + s3 * 0.25f;
            }

            float4 frag_lens_effects (v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                if (abs(_DistortionAmount) > 1e-6f)
                    uv = DistortUV_BrownConrady(uv, _DistortionCenter, _DistortionAmount);

                float3 rgb;
                if (_ChromaticAberrationAmount > 1e-6f)
                {
                    float2 shift = GetChromaticShiftVector(uv, _DistortionCenter,
                                                           _ChromaticAberrationAmount);

                    rgb.g = tex2D(_MainTex, saturate(uv)).g;  

                    rgb.r = SampleSoftChannel(_MainTex, uv - shift,
                                              (shift != 0) ? -normalize(shift) : 0, 0);
                    rgb.b = SampleSoftChannel(_MainTex, uv + shift,
                                              (shift != 0) ?  normalize(shift) : 0, 2);
                }
                else
                {
                    rgb = tex2D(_MainTex, saturate(uv)).rgb;
                }

                float a = tex2D(_MainTex, saturate(uv)).a;
                return float4(rgb, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
