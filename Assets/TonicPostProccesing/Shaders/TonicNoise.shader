Shader "Hidden/TonicPostProcessing/TonicNoise"
{
     Properties
    {
        _MainTex        ("Source", 2D) = "white" {}
        _NoiseIntensity ("Noise Intensity", Range(0,1)) = 0.35
        _NoiseSpeed     ("Noise Speed",     Range(0,1)) = 1.0
        _Monochrome     ("Monochrome",      Range(0,1))  = 1.0 
        _GrainSize      ("Grain Size",      Range(1, 32)) = 1.0 
    }

    SubShader
    {
        Tags {
            "RenderType"="Opaque"
            "Queue"="Overlay"
            "ForceNoShadowCasting"="True"
            "IgnoreProjector"="True"
        }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "FILMGRAIN_PASS"
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;     float4 _MainTex_TexelSize;
            float   _NoiseIntensity;  
            float   _NoiseSpeed;     
            float   _Monochrome;    
            float   _GrainSize;     

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float2 uv       : TEXCOORD0;
                float4 vertex   : SV_POSITION;
            };

   
            inline float Luminance(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }

            inline float Hash12(float2 p)
            {
                float3 p3  = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 19.19); 
                return frac((p3.x + p3.y) * p3.z); 
            }

            float3 SampleProceduralGrain(float2 uv)
            {
              
                float2 grainCoords = floor( (uv * _ScreenParams.xy) / _GrainSize );

                float2 seed = grainCoords + _Time.yy * _NoiseSpeed; 

               
                float nR = Hash12(seed);
                float nG = Hash12(seed + float2(113.1, 113.1)); 
                float nB = Hash12(seed + float2(367.3, 367.3));

                return float3(nR, nG, nB); 
            }

            float3 ApplyFilmGrain(float3 color, float3 noise)
            {
                if (_Monochrome >= 0.5)
                {
                    float lumin = dot(noise, float3(0.333333, 0.333333, 0.333333));
                    noise = lumin.xxx; // Set R, G, and B to the same luminance value
                }

                float luma   = Luminance(color);
                float weight = saturate(4.0 * luma * (1.0 - luma)); 

           
                float3 offset = (noise - 0.5) * (_NoiseIntensity * weight);
                
           
                offset = clamp(offset, -color, 1.0 - color); 

                return color + offset; 
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); 
                o.uv     = v.uv;                        
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv   = i.uv;                       
                float3 src  = tex2D(_MainTex, uv).rgb;      
                float3 grain = SampleProceduralGrain(uv);   
                float3 col  = ApplyFilmGrain(src, grain);   
                return float4(col, 1.0);                 
            }
            ENDCG
        }
    }
    FallBack Off
}