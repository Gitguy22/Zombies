Shader "Custom/WeaponCamo"
{
    Properties
    {
        [Toggle] _UseTexture ("Use Texture for Albedo", Float) = 1
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.5, 0.5, 0.5, 1.0)
        
        [Toggle] _UseMetallicMap ("Use Texture for Metallic", Float) = 1
        _MetallicMap ("Metallic", 2D) = "white" {}
        _MetallicValue ("Metallic Value", Range(0.0, 1.0)) = 0.5
        _SmoothnessValue ("Smoothness Value", Range(0.0, 1.0)) = 0.5
        
        _NormalMap ("Normal Map", 2D) = "bump" {}
        
        [Toggle] _UseCustomPatternMap ("Use Custom Pattern Instead of Perlin", Float) = 0
        _GlowMap ("Glow/Pattern Texture", 2D) = "white" {}
        _ScrollSpeed ("Pattern Scroll Speed", Range(0.0, 2.0)) = 0.3
        
        [Toggle] _GooEffect ("Enable Goo Effect", Float) = 0
        _GooDirection ("Goo Direction", Vector) = (0, -1, 0, 0)
        _GooSpeed ("Goo Flow Speed", Range(0.1, 3.0)) = 1.0
        _GooStretch ("Goo Stretch", Range(1.0, 10.0)) = 2.0
        _GooNoise ("Goo Noise", Range(0.0, 1.0)) = 0.5
        
        _GlowColor1 ("Glow Color 1", Color) = (0.0, 0.8, 1.0, 1.0)
        _GlowColor2 ("Glow Color 2", Color) = (1.0, 0.2, 0.9, 1.0)
        _PulseSpeed ("Pulse Speed", Range(0.1, 5.0)) = 1.0
        _GlowIntensity ("Glow Intensity", Range(0.1, 10.0)) = 2.0
        _MinGlowIntensity ("Minimum Glow", Range(0.0, 1.0)) = 0.3
        
        _PatternScale ("Pattern Scale", Range(0.1, 10.0)) = 2.0
        _EdgeSharpness ("Edge Sharpness", Range(1.0, 10.0)) = 5.0
        _NoiseInfluence ("Noise Influence", Range(0.0, 1.0)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _MetallicMap;
        sampler2D _GlowMap;
        
        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
        };
        
        // Properties
        float _UseTexture;
        float4 _BaseColor;
        float _UseMetallicMap;
        float _MetallicValue;
        float _SmoothnessValue;
        float _UseCustomPatternMap;
        float _ScrollSpeed;
        float _GooEffect;
        float4 _GooDirection;
        float _GooSpeed;
        float _GooStretch;
        float _GooNoise;
        float4 _GlowColor1;
        float4 _GlowColor2;
        float _PulseSpeed;
        float _GlowIntensity;
        float _MinGlowIntensity;
        float _PatternScale;
        float _EdgeSharpness;
        float _NoiseInfluence;
        
        // Noise functions
        float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
        float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
        float4 mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
        float4 permute(float4 x) { return mod289(((x*34.0)+1.0)*x); }
        float3 permute(float3 x) { return mod289(((x*34.0)+1.0)*x); }
        
        // Simplex noise
        float snoise(float2 v) 
        {
            const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
            float2 i  = floor(v + dot(v, C.yy));
            float2 x0 = v - i + dot(i, C.xx);
            float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
            float4 x12 = x0.xyxy + C.xxzz;
            x12.xy -= i1;
            i = mod289(i);
            float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
            float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
            m = m * m;
            m = m * m;
            float3 x = 2.0 * frac(p * C.www) - 1.0;
            float3 h = abs(x) - 0.5;
            float3 ox = floor(x + 0.5);
            float3 a0 = x - ox;
            m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
            float3 g;
            g.x = a0.x * x0.x + h.x * x0.y;
            g.yz = a0.yz * x12.xz + h.yz * x12.yw;
            return 130.0 * dot(m, g);
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o) 
        {
            // Get object space position by transforming world position
            float3 objectPos = mul(unity_WorldToObject, float4(IN.worldPos, 1.0)).xyz;
            
            // Base color
            fixed4 c;
            if (_UseTexture > 0.5) {
                c = tex2D(_MainTex, IN.uv_MainTex);
            } else {
                c = _BaseColor;
            }
            
            // Metallic and smoothness
            float metalVal;
            float smoothVal;
            if (_UseMetallicMap > 0.5) {
                fixed4 metallic = tex2D(_MetallicMap, IN.uv_MainTex);
                metalVal = metallic.r;
                smoothVal = metallic.a;
            } else {
                metalVal = _MetallicValue;
                smoothVal = _SmoothnessValue;
            }
            
            o.Albedo = c.rgb;
            o.Metallic = metalVal;
            o.Smoothness = smoothVal;
            o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
            
            // Time-based animation values
            float time = _Time.y;
            
            // Sine wave pulse with slight variation
            float pulse = (sin(time * _PulseSpeed) + 1.0) * 0.5;
            
            // Create pattern using noise in object space
            float noise1 = snoise(objectPos.xy * _PatternScale * 0.5 + time * 0.2);
            float noise2 = snoise(objectPos.yz * _PatternScale * 0.3 - time * 0.15);
            float noise3 = snoise(objectPos.xz * _PatternScale * 0.7 + time * 0.25);
            
            float noiseCombined = (noise1 + noise2 + noise3) * 0.33;
            
            // Calculate texture coordinates for custom pattern with scrolling or goo effect
            float2 effectUV = IN.uv_MainTex;
            
            // Apply goo effect if enabled
            if (_GooEffect > 0.5) {
                // Direction vector for goo flow (normalized)
                float2 gooDir = normalize(_GooDirection.xy);
                
                // Create dripping effect by distorting UVs
                float gooDistortion = snoise(float2(
                    objectPos.x * 10.0, 
                    objectPos.y * 10.0 - time * _GooSpeed
                )) * _GooNoise;
                
                // Create stretched, flowing pattern
                float gooPattern = snoise(float2(
                    objectPos.x * 5.0, 
                    objectPos.y * _GooStretch - time * _GooSpeed
                ));
                
                // Apply directional flow with noise
                effectUV += gooDir * gooPattern * 0.2;
                effectUV += gooDir * gooDistortion * 0.1;
                
                // Add downward scrolling
                effectUV += gooDir * time * _GooSpeed * 0.1;
            }
            // Regular scrolling if goo effect is disabled but scroll speed > 0
            else if (_ScrollSpeed > 0.001) {
                effectUV += float2(time * _ScrollSpeed * 0.1, time * _ScrollSpeed * 0.15);
            }
            
            // Sample the glow pattern texture with modified UVs
            float4 glowPattern = tex2D(_GlowMap, effectUV * _PatternScale);
            
            // Combine noise and pattern based on toggle
            float glowMask;
            if (_UseCustomPatternMap > 0.5) {
                // Use the custom texture pattern
                glowMask = glowPattern.r;
            } else {
                // Use perlin noise with optional texture influence
                glowMask = lerp(glowPattern.r, noiseCombined, _NoiseInfluence);
            }
            
            // Create sharp edges in the pattern
            glowMask = pow(glowMask, _EdgeSharpness);
            
            // Add fresnel effect (edges glow more)
            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), 5.0);
            glowMask = saturate(glowMask + fresnel * 0.3);
            
            // Interpolate between glow colors based on pulse
            float4 finalGlowColor = lerp(_GlowColor1, _GlowColor2, pulse);
            
            // Apply glow to emission with a minimum intensity
            float adjustedPulse = lerp(_MinGlowIntensity, 1.0, pulse);
            o.Emission = finalGlowColor.rgb * glowMask * _GlowIntensity * adjustedPulse;
            
            // Add a subtle influence to the albedo
            o.Albedo = lerp(o.Albedo, finalGlowColor.rgb, glowMask * 0.2 * adjustedPulse);
        }
        ENDCG
    }
    FallBack "Diffuse"
}