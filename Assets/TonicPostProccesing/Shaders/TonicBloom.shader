Shader "Hidden/TonicBloomShader" // Final Name
{
    Properties
    {
        // _MainTex is implicitly used by Graphics.Blit as the input to a pass.
        // These properties are primarily for C# to set and for the shader to declare the uniforms.
        _MainTex ("Main Input (Set by Blit)", 2D) = "white" {}
        _SourceTex ("Original Scene (For Final Composite)", 2D) = "white" {}
        _BloomTex ("Accumulated Bloom (For Final Composite)", 2D) = "white" {}
        _LowResTex ("Low Res Texture (For Upsampling Pass)", 2D) = "black" {}

        _Intensity ("Bloom Intensity", Range(0, 20)) = 1.0
        _Threshold ("Bloom Threshold", Range(0, 5)) = 1.0
        _SoftKneeWidth ("Soft Knee Width (Threshold * SoftKneeRatio)", Range(0, 5)) = 0.5 // This is effectively the 'knee' value
        _Scatter ("Blur Scatter Scale", Range(0.1, 5)) = 1.0
        _TexelSize ("Texel Size (1/W, 1/H, W, H of _MainTex for current pass)", Vector) = (0,0,0,0)
        _BloomExposure ("Bloom Exposure (Before Tonemapping)", Range(0.1, 10)) = 1.0
        _DistanceScatterFactor ("Distance Scatter Factor", Range(0, 2)) = 0.5 // This will be unused for now in blur passes
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Extract Bright Pixels & Initial 2x Downsample with Enhanced Anti-Flicker & Firefly Clamp
        // Input: Full-resolution scene texture (_MainTex)
        // Output: Half-resolution texture containing only bright parts, prefiltered for stability
        Pass
        {
            Name "ExtractBrightDownsampleInit"
            CGPROGRAM
            #pragma vertex vert_img // Uses appdata_img and v2f_img from UnityCG.cginc
            #pragma fragment frag_extract_bright
            #include "UnityCG.cginc"

            sampler2D _MainTex; // Full-res source from Graphics.Blit
            float4 _MainTex_TexelSize; // Texel size of the full-res _MainTex
            float _Threshold;
            float _SoftKneeWidth; 

            float Luminance(float3 linearRgb) {
                // Using Rec.709 luminance weights
                return dot(linearRgb, float3(0.2126h, 0.7152h, 0.0722h));
            }

            // Anti-flicker by averaging a 2x2 neighborhood
            float3 AntiFlickerSample(float2 uv)
            {
                float2 ofs  = _MainTex_TexelSize.xy * 0.5h;      // half-pixel in UV
                float3 box0 = tex2D(_MainTex, uv + float2(-ofs.x, -ofs.y)).rgb;
                float3 box1 = tex2D(_MainTex, uv + float2( ofs.x, -ofs.y)).rgb;
                float3 box2 = tex2D(_MainTex, uv + float2(-ofs.x,  ofs.y)).rgb;
                float3 box3 = tex2D(_MainTex, uv +  ofs).rgb;
                return (box0 + box1 + box2 + box3) * 0.25h;
            }
            
            float4 frag_extract_bright(v2f_img i) : SV_Target
            {
                // 1. Average neighbourhood first (anti-flicker for color source)
                float3 neighbourhood_color = AntiFlickerSample(i.uv);

                // 2. Get luminance of the direct center pixel and the neighborhood average
                float  lum_centre = Luminance(tex2D(_MainTex, i.uv).rgb);
                float  lum_box    = Luminance(neighbourhood_color);
                
                // 3. Fire-fly clamp: prevent 1-pixel spikes dominating the thresholding decision
                //    The luminance used for thresholding is clamped.
                float  lum_for_threshold = min(lum_centre, lum_box * 4.0f); // 4x factor is empirical

                // 4. Use the clamped luminance for soft-knee thresholding (smoothstep)
                //    _SoftKneeWidth is (threshold_from_csharp * softKneeRatio_from_csharp)
                //    The smoothstep region is [threshold - knee, threshold + knee]
                float  contribution = smoothstep(
                                        _Threshold - _SoftKneeWidth,
                                        _Threshold + _SoftKneeWidth,
                                        lum_for_threshold);

                // 5. Apply the contribution to the anti-flicker sampled neighborhood color
                float3 final_bright_color = neighbourhood_color * contribution;
                
                // Alpha can be taken from the original center pixel or the neighborhood average
                float alpha = tex2D(_MainTex, i.uv).a; 
                return float4(final_bright_color, alpha);
            }
            ENDCG
        }

        // Pass 1: Downsample (Improved Quality - 4-tap bilinear style)
        Pass
        {
            Name "Downsample"
            CGPROGRAM
            #pragma vertex vert_img 
            #pragma fragment frag_downsample
            #include "UnityCG.cginc"

            sampler2D _MainTex; 
            float4 _TexelSize;  

            float4 frag_downsample(v2f_img i) : SV_Target {
                float2 d = _TexelSize.xy * 0.5f; 
                float3 s00 = tex2D(_MainTex, i.uv - d).rgb;                         
                float3 s10 = tex2D(_MainTex, i.uv + float2(d.x, -d.y)).rgb;        
                float3 s01 = tex2D(_MainTex, i.uv + float2(-d.x, d.y)).rgb;        
                float3 s11 = tex2D(_MainTex, i.uv + d).rgb;                        
                float3 downsampledColor = (s00 + s10 + s01 + s11) * 0.25f;
                return float4(downsampledColor, tex2D(_MainTex, i.uv).a); 
            }
            ENDCG
        }

        // Pass 2: Horizontal Separable Gaussian Blur (7-tap example)
        Pass
        {
            Name "BlurHorizontal"
            CGPROGRAM
            #pragma vertex vert_img 
            #pragma fragment frag_blur_horizontal
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            // sampler2D _CameraDepthTexture; // Temporarily removed for stability test
            float4 _TexelSize; 
            float _Scatter;    
            // float _DistanceScatterFactor; // Temporarily unused
            static const float weights[4] = { 0.382928f, 0.241732f, 0.060598f, 0.005977f }; 

            float4 frag_blur_horizontal(v2f_img i) : SV_Target {
                // Reverted to use only _Scatter for stability testing
                float effectiveScatter = _Scatter; 

                float3 blurredColor = tex2D(_MainTex, i.uv).rgb * weights[0]; 
                float2 hstep = float2(_TexelSize.x * effectiveScatter, 0.0f);
                blurredColor += tex2D(_MainTex, i.uv + hstep * 1.0f).rgb * weights[1];
                blurredColor += tex2D(_MainTex, i.uv - hstep * 1.0f).rgb * weights[1];
                blurredColor += tex2D(_MainTex, i.uv + hstep * 2.0f).rgb * weights[2];
                blurredColor += tex2D(_MainTex, i.uv - hstep * 2.0f).rgb * weights[2];
                blurredColor += tex2D(_MainTex, i.uv + hstep * 3.0f).rgb * weights[3];
                blurredColor += tex2D(_MainTex, i.uv - hstep * 3.0f).rgb * weights[3];
                return float4(blurredColor, tex2D(_MainTex, i.uv).a);
            }
            ENDCG
        }

        // Pass 3: Vertical Separable Gaussian Blur (7-tap example)
        Pass
        {
            Name "BlurVertical"
            CGPROGRAM
            #pragma vertex vert_img 
            #pragma fragment frag_blur_vertical
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            // sampler2D _CameraDepthTexture; // Temporarily removed for stability test
            float4 _TexelSize; 
            float _Scatter;
            // float _DistanceScatterFactor; // Temporarily unused
            static const float weights[4] = { 0.382928f, 0.241732f, 0.060598f, 0.005977f };

            float4 frag_blur_vertical(v2f_img i) : SV_Target {
                // Reverted to use only _Scatter for stability testing
                float effectiveScatter = _Scatter;

                float3 blurredColor = tex2D(_MainTex, i.uv).rgb * weights[0]; 
                float2 vstep = float2(0.0f, _TexelSize.y * effectiveScatter);
                blurredColor += tex2D(_MainTex, i.uv + vstep * 1.0f).rgb * weights[1];
                blurredColor += tex2D(_MainTex, i.uv - vstep * 1.0f).rgb * weights[1];
                blurredColor += tex2D(_MainTex, i.uv + vstep * 2.0f).rgb * weights[2];
                blurredColor += tex2D(_MainTex, i.uv - vstep * 2.0f).rgb * weights[2];
                blurredColor += tex2D(_MainTex, i.uv + vstep * 3.0f).rgb * weights[3];
                blurredColor += tex2D(_MainTex, i.uv - vstep * 3.0f).rgb * weights[3];
                return float4(blurredColor, tex2D(_MainTex, i.uv).a);
            }
            ENDCG
        }

        // Pass 4: Upsample Additive (Improved Quality - Manual 4-tap Bilinear & Weighted Mip Blending)
        Pass
        {
            Name "UpsampleAdditive"
            CGPROGRAM
            #pragma vertex vert_img 
            #pragma fragment frag_upsample_additive 
            #include "UnityCG.cginc"

            sampler2D _MainTex;     // Higher-resolution texture (current accumulation target)
            sampler2D _LowResTex;   // Lower-resolution texture from smaller mip level
            float4 _TexelSize;      // Texel size of _MainTex (the higher-res target this pass writes to)

            // Fixed intensity scale for upsampled lower mip levels to control energy
            // Values slightly less than 1.0 can prevent lower mips from being too overpowering.
            // This is a simple form of "weighted mip blending".
            static const float _LowMipContributionScale = 0.925f; // Tune this value (0.8 to 1.0)

            float3 SampleBilinearUpsample(sampler2D tex, float2 uv_highRes, float2 texelSize_highRes) {
                float2 lowResTexelInHighResUVSpace = texelSize_highRes * 2.0f; 
                float2 lowResTexelUnits = uv_highRes / lowResTexelInHighResUVSpace;
                float2 fracPart = frac(lowResTexelUnits - 0.5f); 
                float2 uv00_center_low = (floor(lowResTexelUnits - 0.5f) + 0.5f) * lowResTexelInHighResUVSpace;
                float2 uv10_center_low = uv00_center_low + float2(lowResTexelInHighResUVSpace.x, 0.0f);
                float2 uv01_center_low = uv00_center_low + float2(0.0f, lowResTexelInHighResUVSpace.y);
                float2 uv11_center_low = uv00_center_low + lowResTexelInHighResUVSpace;
                float3 s00 = tex2D(tex, uv00_center_low).rgb;
                float3 s10 = tex2D(tex, uv10_center_low).rgb;
                float3 s01 = tex2D(tex, uv01_center_low).rgb;
                float3 s11 = tex2D(tex, uv11_center_low).rgb;
                float3 interpX1 = lerp(s00, s10, fracPart.x);
                float3 interpX2 = lerp(s01, s11, fracPart.x);
                return lerp(interpX1, interpX2, fracPart.y);
            }
            
            float4 frag_upsample_additive(v2f_img i) : SV_Target { 
                float3 highResColor = tex2D(_MainTex, i.uv).rgb; 
                float3 lowResBlurred_Upsampled = SampleBilinearUpsample(_LowResTex, i.uv, _TexelSize.xy);
                
                // Apply a fixed weight to the contribution from the lower (more blurred) mip level
                return float4(highResColor + lowResBlurred_Upsampled * _LowMipContributionScale, 1.0f);
            }
            ENDCG
        }

        // Pass 5: Final Composite (with Tonemapping for Bloom)
        Pass
        {
            Name "CompositeFinal"
            CGPROGRAM
            #pragma vertex vert_img 
            #pragma fragment frag_composite_final
            #include "UnityCG.cginc"

            sampler2D _SourceTex; 
            sampler2D _BloomTex;  
            float _Intensity;
            float _BloomExposure;

            // Reinhard Tonemapping (Simple)
            float3 Tonemap_Reinhard(float3 color) {
                color = max(0.0f, color); // Ensure positive
                return color / (color + 1.0f);
            }

            float4 frag_composite_final(v2f_img i) : SV_Target {
                float4 baseColor = tex2D(_SourceTex, i.uv);
                float3 bloomColorLinear = tex2D(_BloomTex, i.uv).rgb;
                
                bloomColorLinear *= _Intensity * _BloomExposure;
                float3 bloomTonemapped = Tonemap_Reinhard(bloomColorLinear);
                float3 finalColor = baseColor.rgb + bloomTonemapped;
                return float4(saturate(finalColor), baseColor.a);
            }
            ENDCG
        }
    }
    Fallback "Hidden/Internal-Error"
}
