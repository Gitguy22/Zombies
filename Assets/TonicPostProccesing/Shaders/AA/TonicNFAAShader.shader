Shader "Hidden/TonicNFAA" // Normal-based Anti-Aliasing
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EdgeThresholdNormal ("Normal Edge Threshold", Range(0.01, 1.0)) = 0.1 
        _EdgeThresholdDepth ("Depth Edge Threshold", Range(0.001, 0.1)) = 0.01 
        _BlendFactor ("Blend Factor", Range(0.1, 1.0)) = 0.5 
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

            sampler2D _CameraDepthNormalsTexture;

            float _EdgeThresholdNormal;
            float _EdgeThresholdDepth;
            float _BlendFactor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float3 SampleViewNormal(float2 uv) {
                return DecodeViewNormalStereo(tex2D(_CameraDepthNormalsTexture, uv));
            }

            float SampleLinear01Depth(float2 uv) {
                return DecodeFloatRG(tex2D(_CameraDepthNormalsTexture, uv).zw);
            }


            float4 frag(v2f i) : SV_Target
            {
                float2 rcpFrame = _MainTex_TexelSize.xy; // 1/width, 1/height
                float3 centerColor = tex2D(_MainTex, i.uv).rgb;

                float3 centerNormal = SampleViewNormal(i.uv);
                float centerDepth = SampleLinear01Depth(i.uv);

                float2 uvN = i.uv + float2(0.0,  rcpFrame.y);
                float2 uvS = i.uv + float2(0.0, -rcpFrame.y);
                float2 uvE = i.uv + float2( rcpFrame.x, 0.0);
                float2 uvW = i.uv + float2(-rcpFrame.x, 0.0);

                float3 normalN = SampleViewNormal(uvN);
                float3 normalS = SampleViewNormal(uvS);
                float3 normalE = SampleViewNormal(uvE);
                float3 normalW = SampleViewNormal(uvW);

                float depthN = SampleLinear01Depth(uvN);
                float depthS = SampleLinear01Depth(uvS);
                float depthE = SampleLinear01Depth(uvE);
                float depthW = SampleLinear01Depth(uvW);

                float normalDiffN = 1.0 - saturate(dot(centerNormal, normalN));
                float normalDiffS = 1.0 - saturate(dot(centerNormal, normalS));
                float normalDiffE = 1.0 - saturate(dot(centerNormal, normalE));
                float normalDiffW = 1.0 - saturate(dot(centerNormal, normalW));
                
                float depthDiffN = abs(centerDepth - depthN);
                float depthDiffS = abs(centerDepth - depthS);
                float depthDiffE = abs(centerDepth - depthE);
                float depthDiffW = abs(centerDepth - depthW);

                float edgeN = max(step(_EdgeThresholdNormal, normalDiffN), step(_EdgeThresholdDepth, depthDiffN));
                float edgeS = max(step(_EdgeThresholdNormal, normalDiffS), step(_EdgeThresholdDepth, depthDiffS));
                float edgeE = max(step(_EdgeThresholdNormal, normalDiffE), step(_EdgeThresholdDepth, depthDiffE));
                float edgeW = max(step(_EdgeThresholdNormal, normalDiffW), step(_EdgeThresholdDepth, depthDiffW));

                float totalEdge = edgeN + edgeS + edgeE + edgeW;

                if (totalEdge < 0.5) 
                {
                    return float4(centerColor, 1.0);
                }

            
                float3 accumColor = centerColor;
                float numSamples = 1.0;     

                if (edgeN > 0.0) { accumColor += tex2D(_MainTex, uvN).rgb; numSamples += 1.0; }
                if (edgeS > 0.0) { accumColor += tex2D(_MainTex, uvS).rgb; numSamples += 1.0; }
                if (edgeE > 0.0) { accumColor += tex2D(_MainTex, uvE).rgb; numSamples += 1.0; }
                if (edgeW > 0.0) { accumColor += tex2D(_MainTex, uvW).rgb; numSamples += 1.0; }
                
                float3 blendedColor;
                if (numSamples > 1.0) {
                    blendedColor = accumColor / numSamples;
                } else {
                    blendedColor = centerColor; 
                }
                
                blendedColor = lerp(centerColor, blendedColor, _BlendFactor);

                return float4(saturate(blendedColor), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
