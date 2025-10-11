Shader "Hidden/TonicFXAA"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FxaaSubpix("Subpixel AA", Range(0.0, 1.0)) = 0.75
        _FxaaEdgeThreshold("Edge Threshold", Range(0.0, 1.0)) = 0.166 
        _FxaaEdgeThresholdMin("Edge Threshold Min", Range(0.0, 1.0)) = 0.0833
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; 

            float _FxaaSubpix;
            float _FxaaEdgeThreshold;
            float _FxaaEdgeThresholdMin;

            #define FXAA_REDUCE_MIN   (1.0/128.0)
            #define FXAA_REDUCE_MUL   (1.0/8.0)
            #define FXAA_SPAN_MAX     8.0

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 uv_luma : TEXCOORD1; 
                float4 uv_edge : TEXCOORD2; 
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                float2 rcpFrame = _MainTex_TexelSize.xy;

                o.uv_luma.xy = v.uv; 
                o.uv_luma.zw = float2(0.0, 0.0); 

                o.uv_edge.xy = v.uv + float2(-0.5, -0.5) * rcpFrame; // NW
                o.uv_edge.zw = v.uv + float2( 0.5, -0.5) * rcpFrame; // NE
                return o;
            }

            float FxaaLuma(float3 rgb)
            {
                return dot(rgb, float3(0.2126, 0.7152, 0.0722));
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 rcpFrame = _MainTex_TexelSize.xy;

                float3 colorCenter = tex2D(_MainTex, i.uv).rgb;
                float lumaCenter = FxaaLuma(colorCenter);

                float lumaN = FxaaLuma(tex2D(_MainTex, i.uv + float2(0.0,  rcpFrame.y)).rgb);
                float lumaS = FxaaLuma(tex2D(_MainTex, i.uv + float2(0.0, -rcpFrame.y)).rgb);
                float lumaE = FxaaLuma(tex2D(_MainTex, i.uv + float2( rcpFrame.x, 0.0)).rgb);
                float lumaW = FxaaLuma(tex2D(_MainTex, i.uv + float2(-rcpFrame.x, 0.0)).rgb);

                float lumaMin = min(lumaCenter, min(min(lumaN, lumaS), min(lumaE, lumaW)));
                float lumaMax = max(lumaCenter, max(max(lumaN, lumaS), max(lumaE, lumaW)));

                float lumaRange = lumaMax - lumaMin;

                if (lumaRange < max(_FxaaEdgeThresholdMin, _FxaaEdgeThreshold * lumaMax))
                {
                    return float4(colorCenter, 1.0);
                }

                float edgeVert = abs(lumaN - lumaCenter) + abs(lumaS - lumaCenter);
                float edgeHorz = abs(lumaE - lumaCenter) + abs(lumaW - lumaCenter);
                
                bool isHorizontal = (edgeHorz >= edgeVert);
                
                float lumaPositive, lumaNegative;
                if (isHorizontal) {
                    lumaPositive = lumaE;
                    lumaNegative = lumaW;
                } else {
                    lumaPositive = lumaN;
                    lumaNegative = lumaS;
                }

                float gradientScaled = max(abs(lumaCenter - lumaPositive), abs(lumaCenter - lumaNegative));
                gradientScaled = saturate(gradientScaled / lumaRange);

                float2 stepDirection = isHorizontal ? float2(rcpFrame.x, 0.0) : float2(0.0, rcpFrame.y);

                float blendFactor = 0.5 + gradientScaled * (_FxaaSubpix - 0.5); 
                blendFactor = saturate(blendFactor);

                float3 blendedColor;
                float lumaEdgeAverage = (lumaPositive + lumaNegative) * 0.5;

                if (abs(lumaCenter - lumaEdgeAverage) / lumaRange < 0.1) { 
                    blendedColor = colorCenter; 
                } else if (lumaCenter < lumaEdgeAverage) {
                    blendedColor = lerp(colorCenter, tex2D(_MainTex, i.uv + stepDirection * blendFactor).rgb, 0.5);
                } else { 
                    blendedColor = lerp(colorCenter, tex2D(_MainTex, i.uv - stepDirection * blendFactor).rgb, 0.5);
                }

                return float4(blendedColor, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
