Shader "Hidden/TonicPostProcessing/TonicVignetteShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _VignetteIntensity;
            float _VignetteSmoothness;
            float _VignetteSize; 
            float4 _VignetteColor;
            float _VignetteType; 
            float _ScreenAspect; 

            float4 frag (v2f i) : SV_Target
            {
                float4 originalColor = tex2D(_MainTex, i.uv);
                
                float2 fixedCenter = float2(0.5, 0.5);
                float2 centeredUV = i.uv - fixedCenter;
                float vignetteMask = 0.0; 

                if (_VignetteType < 0.5) 
                {
                    float2 aspectScaledUV = centeredUV;
                    if (_ScreenAspect > 1.0) 
                    {
                        aspectScaledUV.x *= sqrt(_ScreenAspect);
                    }
                    else 
                    {
                        aspectScaledUV.y /= sqrt(_ScreenAspect); 
                    }
                    float dist = length(aspectScaledUV);
                    vignetteMask = saturate(dist / _VignetteSize); 
                }
                else if (_VignetteType < 1.5)
                {
                    float2 aspectScaledUV = centeredUV;
                     if (_ScreenAspect > 1.0)
                    {
                        aspectScaledUV.x *= sqrt(_ScreenAspect);
                    }
                    else 
                    {
                        aspectScaledUV.y /= sqrt(_ScreenAspect);
                    }
                    float dist = max(abs(aspectScaledUV.x), abs(aspectScaledUV.y));
                    vignetteMask = saturate(dist / _VignetteSize);
                }
                else 
                {
                    float dist = abs(centeredUV.y); 
                    vignetteMask = saturate(dist / _VignetteSize); 
                }
                
                vignetteMask = pow(vignetteMask, _VignetteSmoothness);
                
                float blendFactor = vignetteMask * _VignetteIntensity * _VignetteColor.a;
                blendFactor = saturate(blendFactor);

                float3 blendedColor = lerp(originalColor.rgb, _VignetteColor.rgb, blendFactor);
                
                return float4(blendedColor, originalColor.a);
            }
            ENDCG
        }
    }
}
