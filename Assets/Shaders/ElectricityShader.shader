Shader "Effects/ElectricityShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (0.5, 0.8, 1.0, 1.0)
        _Intensity ("Intensity", Range(0, 10)) = 2.0
        _EdgeGlow ("Edge Glow", Range(0, 5)) = 1.0
        _CenterBrightness ("Center Brightness", Range(0, 10)) = 3.0
        _NoiseScale ("Noise Scale", Range(0, 10)) = 3.0
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 2.0
        _EdgeFalloff ("Edge Falloff", Range(0.1, 5)) = 2.0
        _GlowRadius ("Glow Radius", Range(0, 2)) = 0.5
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                UNITY_FOG_COORDS(1)
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Intensity;
            float _EdgeGlow;
            float _CenterBrightness;
            float _NoiseScale;
            float _NoiseSpeed;
            float _EdgeFalloff;
            float _GlowRadius;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            float smoothNoise(float2 uv)
            {
                float2 id = floor(uv);
                float2 fractPart = frac(uv);
                
                float a = noise(id);
                float b = noise(id + float2(1.0, 0.0));
                float c = noise(id + float2(0.0, 1.0));
                float d = noise(id + float2(1.0, 1.0));
                
                float2 u = fractPart * fractPart * (3.0 - 2.0 * fractPart);
                
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Calculate distance from center of the line (0.5)
                float dist = abs(uv.y - 0.5) * 2.0;
                
                // Multi-layered edge falloff for realistic glow
                float coreFalloff = pow(1.0 - dist, _EdgeFalloff * 2.0);
                float glowFalloff = pow(1.0 - saturate(dist / (1.0 + _GlowRadius)), _EdgeFalloff);
                float outerGlow = pow(1.0 - saturate(dist / (1.0 + _GlowRadius * 2.0)), _EdgeFalloff * 0.5);
                
                // Animated noise for flickering
                float n1 = smoothNoise(uv * _NoiseScale + _Time.y * _NoiseSpeed);
                float n2 = smoothNoise(uv * _NoiseScale * 0.5 - _Time.y * _NoiseSpeed * 0.7);
                float combinedNoise = (n1 + n2) * 0.5;
                
                // Core lightning bolt (bright center)
                float core = coreFalloff * _CenterBrightness;
                core *= (1.0 + combinedNoise * 0.3);
                
                // Inner glow
                float innerGlow = glowFalloff * _EdgeGlow;
                innerGlow *= (1.0 + combinedNoise * 0.2);
                
                // Outer glow
                float outer = outerGlow * _EdgeGlow * 0.3;
                
                // Combine all layers
                float lightning = max(core, innerGlow) + outer;
                
                // Add extra bright streaks
                float streaks = smoothNoise(float2(uv.x * 20.0, _Time.y * 10.0));
                lightning += streaks * coreFalloff * 0.5;
                
                // Apply color and intensity
                fixed4 col = _Color * i.color;
                
                // HDR color for bloom
                col.rgb *= lightning * _Intensity;
                col.a = saturate(lightning * 2.0);
                
                // Add subtle color variation
                float colorVariation = smoothNoise(uv * 5.0 + _Time.y);
                col.rgb = lerp(col.rgb, col.rgb * float3(1.2, 0.9, 1.3), colorVariation * 0.3);
                
                // Final HDR boost for bloom
                col.rgb *= (1.0 + _Intensity);
                
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}