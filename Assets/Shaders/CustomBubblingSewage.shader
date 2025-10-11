Shader "Custom/RealisticSewage"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _FoamTex ("Foam/Scum Texture", 2D) = "white" {}
        
        // Sewage Colors
        _BaseColor ("Base Sewage Color", Color) = (0.15, 0.12, 0.08, 1)
        _MurkyColor ("Murky Color", Color) = (0.1, 0.09, 0.06, 1)
        _ScumColor ("Scum Color", Color) = (0.25, 0.22, 0.15, 1)
        _DebrisColor ("Debris Color", Color) = (0.08, 0.06, 0.04, 1)
        
        // Surface Properties
        _Glossiness ("Surface Glossiness", Range(0,1)) = 0.15
        _ScumGloss ("Scum Glossiness", Range(0,1)) = 0.05
        _Opacity ("Opacity", Range(0.5,1)) = 0.95
        
        // Flow
        _FlowSpeed ("Flow Speed", Range(0,1)) = 0.1
        _FlowDistortion ("Flow Distortion", Range(0,0.5)) = 0.1
        
        // Occasional Bubbles
        _BubbleThreshold ("Bubble Spawn Threshold", Range(0.9,0.999)) = 0.995
        _BubbleSize ("Max Bubble Size", Range(0.01,0.1)) = 0.03
        _BubbleLifetime ("Bubble Lifetime", Range(0.5,3)) = 1.5
        _BubbleHeight ("Bubble Height", Range(0,0.1)) = 0.02
        
        // Scum and Debris
        _ScumAmount ("Scum Amount", Range(0,1)) = 0.6
        _DebrisScale ("Debris Scale", Range(1,10)) = 3
        
        // Surface Movement
        _RippleScale ("Ripple Scale", Range(0,0.1)) = 0.02
        _RippleSpeed ("Ripple Speed", Range(0,1)) = 0.3
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _FoamTex;
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float3 worldPos;
            float3 viewDir;
        };

        half _Glossiness, _ScumGloss;
        fixed4 _BaseColor, _MurkyColor, _ScumColor, _DebrisColor;
        float _Opacity;
        float _FlowSpeed, _FlowDistortion;
        float _BubbleThreshold, _BubbleSize, _BubbleLifetime, _BubbleHeight;
        float _ScumAmount, _DebrisScale;
        float _RippleScale, _RippleSpeed;

        // Better hash function for randomness
        float hash(float2 p)
        {
            p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
            return frac(sin(p.x) * 43758.5453 + sin(p.y) * 23421.631);
        }
        
        // Smooth noise
        float noise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            
            float a = hash(i);
            float b = hash(i + float2(1.0, 0.0));
            float c = hash(i + float2(0.0, 1.0));
            float d = hash(i + float2(1.0, 1.0));
            
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }
        
        // Individual bubble function - creates occasional popping bubbles
        float singleBubble(float2 pos, float2 center, float time, float seed)
        {
            float dist = distance(pos, center);
            float maxSize = _BubbleSize * (0.5 + hash(center + seed) * 0.5);
            
            // Bubble lifecycle
            float life = fmod(time * (0.5 + hash(center + seed + 0.5)), _BubbleLifetime);
            float growth = saturate(life / 0.3); // Quick growth
            float pop = 1.0 - saturate((life - _BubbleLifetime + 0.1) / 0.1); // Quick pop
            float size = maxSize * growth * pop;
            
            // Bubble shape
            float bubble = 1.0 - smoothstep(0.0, size, dist);
            bubble *= smoothstep(size * 0.2, 0.0, dist); // Inner highlight
            
            return bubble * growth * pop;
        }
        
        // Spawns bubbles occasionally at random positions
        float sparseBubbles(float2 uv, float time)
        {
            float bubble = 0;
            float2 cellSize = 0.2; // Grid cells for bubble spawning
            
            // Check neighboring cells for bubbles
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    float2 cellPos = floor(uv / cellSize) + float2(x, y);
                    float2 cellCenter = (cellPos + 0.5) * cellSize;
                    
                    // Determine if this cell should spawn a bubble
                    float spawnValue = hash(cellPos + floor(time / _BubbleLifetime));
                    
                    if (spawnValue > _BubbleThreshold)
                    {
                        // Offset bubble position within cell
                        float2 offset = (hash(cellPos + 100.0) - 0.5) * cellSize * 0.8;
                        float2 bubblePos = cellCenter + offset;
                        
                        bubble = max(bubble, singleBubble(uv, bubblePos, time, spawnValue));
                    }
                }
            }
            
            return bubble;
        }
        
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            
            // Gentle ripples
            float ripple = sin(worldPos.x * 10.0 + _Time.y * _RippleSpeed) * _RippleScale;
            ripple += sin(worldPos.z * 8.0 - _Time.y * _RippleSpeed * 0.8) * _RippleScale * 0.7;
            
            // Bubble displacement (only where bubbles exist)
            float2 bubbleCheckUV = v.texcoord.xy;
            float bubbleHeight = sparseBubbles(bubbleCheckUV, _Time.y) * _BubbleHeight;
            
            v.vertex.y += ripple + bubbleHeight;
            
            o.uv_MainTex = v.texcoord.xy;
            o.uv_NormalMap = v.texcoord.xy;
            o.worldPos = worldPos;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Slow flow animation
            float2 flow1 = IN.uv_MainTex + float2(_Time.x * _FlowSpeed * 0.05, 0);
            float2 flow2 = IN.uv_MainTex + float2(0, _Time.x * _FlowSpeed * 0.03);
            
            // Base sewage texture
            fixed4 base1 = tex2D(_MainTex, flow1);
            fixed4 base2 = tex2D(_MainTex, flow2 * 1.1);
            fixed4 baseColor = lerp(base1, base2, 0.5);
            
            // Color variation
            float colorVar = noise(IN.worldPos.xz * 0.3 + _Time.y * 0.01);
            fixed4 sewageColor = lerp(_BaseColor, _MurkyColor, colorVar);
            sewageColor *= baseColor;
            
            // Scum and floating debris
            float2 scumUV = IN.uv_MainTex * _DebrisScale;
            float scum = tex2D(_FoamTex, scumUV + _Time.x * _FlowSpeed * 0.02).r;
            scum *= noise(scumUV * 2.0 + _Time.y * 0.05);
            float scumMask = smoothstep(1.0 - _ScumAmount, 1.0, scum);
            
            // Debris patches
            float debris = noise(IN.worldPos.xz * 0.5);
            debris = smoothstep(0.7, 0.8, debris);
            
            // Combine colors
            sewageColor = lerp(sewageColor, _ScumColor, scumMask * 0.8);
            sewageColor = lerp(sewageColor, _DebrisColor, debris * 0.3);
            
            // Occasional bubbles
            float bubbles = sparseBubbles(IN.uv_MainTex, _Time.y);
            
            // Bubble rim (slightly lighter)
            if (bubbles > 0)
            {
                fixed4 bubbleColor = sewageColor * 1.3;
                bubbleColor.a = lerp(0.8, 0.95, bubbles);
                sewageColor = lerp(sewageColor, bubbleColor, bubbles * 0.6);
            }
            
            // Normal mapping
            fixed3 normal = UnpackNormal(tex2D(_NormalMap, flow1));
            fixed3 normal2 = UnpackNormal(tex2D(_NormalMap, flow2 * 0.8));
            o.Normal = normalize(lerp(normal, normal2, 0.5));
            
            // Subtle bubble normal distortion
            if (bubbles > 0)
            {
                o.Normal = lerp(o.Normal, float3(0, 0, 1), bubbles * 0.7);
            }
            
            // Output
            o.Albedo = sewageColor.rgb;
            o.Metallic = 0;
            o.Smoothness = lerp(_Glossiness, _ScumGloss, scumMask);
            o.Smoothness = lerp(o.Smoothness, 0.4, bubbles); // Bubbles are slightly glossier
            o.Alpha = _Opacity;
        }
        ENDCG
    }
    FallBack "Diffuse"
}