    Shader "Hidden/TonicPostProcessing/TonicSSAO" {
    Properties {
        _MainTex ("", 2D) = "" {}
        _SSAO ("", 2D) = "" {}
    }
    Subshader {
        ZTest Always Cull Off ZWrite Off

    CGINCLUDE

    #include "UnityCG.cginc"

    struct v2f {
        float4 pos : SV_POSITION;
        float4 uv : TEXCOORD0;
    };

    v2f vert (appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos (v.vertex);
        o.uv = float4(v.texcoord.xy, 1, 1);     
        return o;
    }

    sampler2D _MainTex;
    sampler2D _CameraDepthTexture;
    sampler2D _CameraDepthNormalsTexture;
    sampler2D _SSAO;
    sampler2D _ColorBuffer;

    float4 _CameraDepthNormalsTexture_TexelSize;
    float4 _SSAO_TexelSize;

    float4 _Params; 
    float4 _Params2;
    float4 _Params3; 
    float4 _Params4; 
    float4 _OcclusionColor;

    float2 _InputSize;
    float3 _FarCorner;

    ENDCG

    // ---- SSAO pass
    Pass {
            
        CGPROGRAM
        #pragma vertex vert_ao
        #pragma fragment frag
        #pragma target 3.0
        #pragma multi_compile SAMPLES_2 SAMPLES_4 SAMPLES_6 SAMPLES_8
        #pragma multi_compile __ COLORBLEEDING_ON

        struct v2f_ao {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            float2 uvr : TEXCOORD1;
        };
        
        uniform float2 _InterleavePatternScale;
        
        v2f_ao vert_ao (appdata_img v)
        {
            v2f_ao o;
            o.pos = UnityObjectToClipPos (v.vertex);
            o.uv = v.texcoord;
            o.uvr = v.texcoord.xy * _InterleavePatternScale;
            return o;
        }

        sampler2D _AxisTexture;

        inline void samplePositionAndNormal(float2 uv, out float3 p, out float3 n)
        {
            float depth;
            if (unity_OrthoParams.w < 0.5){
                depth = LinearEyeDepth(tex2D(_CameraDepthTexture, uv).x);
            }else{
                depth = _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * tex2D(_CameraDepthTexture, uv).x;
            }

            n = DecodeViewNormalStereo(tex2D(_CameraDepthNormalsTexture, uv));
        
            if (unity_OrthoParams.w < 0.5){ 
                float3 ray = (half3(-0.5,-0.5,0) + half3(uv,-1)) * _FarCorner;
                p = ray * depth / _FarCorner.z;
            }else{                      
                p = (half3(-0.5,-0.5,0) + half3(uv * _FarCorner.xy,-depth));
            }
        }

        inline half4 calculateGI(float3 fragP, float3 fragN, float3 sampleP, float3 sampleN, float3 bleedColor)
        {
            float3 v = sampleP - fragP;
            float vvDot = dot(v,v);
            float oneOverSqrt = rsqrt(vvDot);
            float fvDot = dot(fragN,v) * oneOverSqrt;

            float falloff = saturate(1.0 - _Params.w * vvDot);
            float ao = max(-_Params.z,fvDot - _Params.y) * falloff;

            #if COLORBLEEDING_ON
                float nvDot = dot(sampleN,-v) * oneOverSqrt;
                float3 gi = bleedColor * max(0,fvDot) * max(0,nvDot) * falloff;
                return half4(gi,ao);
            #else
                return half4(0,0,0,ao);
            #endif
        }

        half4 frag (v2f_ao i) : SV_Target
        {   
            float4 gi = float4(0,0,0,0);
        
            float3 fragP, sampleP;
            float3 fragN, sampleN;
            
            half luminance = dot(tex2D (_ColorBuffer, i.uv).rgb, half3(0.299, 0.587, 0.114));
            samplePositionAndNormal(i.uv,fragP,fragN);

            float radius;
            if (unity_OrthoParams.w < 0.5){
                radius = clamp(_Params.x / -fragP.z,_Params2.w,_Params4.x);
            }else{
                radius = clamp(_Params.x / _FarCorner.y,_Params2.w,_Params4.x);
            }
            half2 aspect_ratio = half2(_CameraDepthNormalsTexture_TexelSize.w / _CameraDepthNormalsTexture_TexelSize.z,1);
        
            half3 pattern = tex2D (_AxisTexture, i.uvr).xyz;
            half2 axis1 = pattern.xy * radius;

            #if defined(SAMPLES_8)
            axis1 /= 8;
            #elif defined(SAMPLES_6)
            axis1 /= 6;
            #elif defined(SAMPLES_4)
            axis1 /= 4;
            #elif defined(SAMPLES_2)
            axis1 /= 2;
            #endif

            half2 axis2 = half2(-axis1.y,axis1.x);

            #if defined(SAMPLES_8)
            for (int s1 = 1; s1 <= 8; ++s1)
            #elif defined(SAMPLES_6)
            for (int s1 = 1; s1 <= 6; ++s1)
            #elif defined(SAMPLES_4)
            for (int s1 = 1; s1 <= 4; ++s1)
            #elif defined(SAMPLES_2)
            for (int s1 = 1; s1 <= 2; ++s1)
            #else 
            for (int s1 = 1; s1 <= 1; ++s1)
            #endif
            {
                float2 uv = float2(i.uv - axis1.xy * (s1 - pattern.z) * aspect_ratio);
                samplePositionAndNormal(uv,sampleP, sampleN);
                #if COLORBLEEDING_ON
                    gi += calculateGI(fragP, fragN, sampleP, sampleN, tex2D (_ColorBuffer, uv));
                #else 
                    gi += calculateGI(fragP, fragN, sampleP, sampleN, 0);
                #endif
            }

            #if defined(SAMPLES_8)
            for (int s2 = 1; s2 <= 8; ++s2)
            #elif defined(SAMPLES_6)
            for (int s2 = 1; s2 <= 6; ++s2)
            #elif defined(SAMPLES_4)
            for (int s2 = 1; s2 <= 4; ++s2)
            #elif defined(SAMPLES_2)
            for (int s2 = 1; s2 <= 2; ++s2)
            #else 
            for (int s2 = 1; s2 <= 1; ++s2)
            #endif
            {
                float2 uv = float2(i.uv + axis1.xy * (s2 - pattern.z) * aspect_ratio);
                samplePositionAndNormal(uv, sampleP, sampleN);
                #if COLORBLEEDING_ON
                    gi += calculateGI(fragP, fragN, sampleP, sampleN, tex2D (_ColorBuffer, uv));
                #else 
                    gi += calculateGI(fragP, fragN, sampleP, sampleN, 0);
                #endif
            }

            #if defined(SAMPLES_8)
            for (int s3 = 1; s3 <= 8; ++s3)
            #elif defined(SAMPLES_6)
            for (int s3 = 1; s3 <= 6; ++s3)
            #elif defined(SAMPLES_4)
            for (int s3 = 1; s3 <= 4; ++s3)
            #elif defined(SAMPLES_2)
            for (int s3 = 1; s3 <= 2; ++s3)
            #else 
            for (int s3 = 1; s3 <= 1; ++s3)
            #endif
            {
                float2 uv = float2(i.uv - axis2.xy * (s3 - pattern.z) * aspect_ratio);
                samplePositionAndNormal(uv, sampleP, sampleN);
                #if COLORBLEEDING_ON
                    gi += calculateGI(fragP, fragN, sampleP, sampleN, tex2D (_ColorBuffer, uv));
                #else 
                    gi += calculateGI(fragP, fragN, sampleP, sampleN, 0);
                #endif
            }

            #if defined(SAMPLES_8)
            for (int s4 = 1; s4 <= 8; ++s4)
            #elif defined(SAMPLES_6)
            for (int s4 = 1; s4 <= 6; ++s4)
            #elif defined(SAMPLES_4)
            for (int s4 = 1; s4 <= 4; ++s4)
            #elif defined(SAMPLES_2)
            for (int s4 = 1; s4 <= 2; ++s4)
            #else 
            for (int s4 = 1; s4 <= 1; ++s4)
            #endif
            {
                float2 uv = float2(i.uv + axis2.xy * (s4 - pattern.z) * aspect_ratio);
                samplePositionAndNormal(uv, sampleP, sampleN);
                #if COLORBLEEDING_ON
                    gi += calculateGI(fragP, fragN, sampleP, sampleN, tex2D (_ColorBuffer, uv));
                #else 
                    gi += calculateGI(fragP, fragN, sampleP, sampleN, 0);
                #endif
            }

            #if defined(SAMPLES_8)
            gi /= 32;
            #elif defined(SAMPLES_6)
            gi /= 24;
            #elif defined(SAMPLES_4)
            gi /= 16;
            #elif defined(SAMPLES_2)
            gi /= 8;
            #else 
            gi /= 4;
            #endif

            gi *= half4(_Params3.xxx,_Params2.x);
            gi.a += _Params.z;

            gi.a = lerp(pow(saturate(1-gi.a),_Params2.y), 1.0, luminance * _Params4.y);

            return half4(gi.rgb,gi.a);
        }
        ENDCG

    }   

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 3.0
        #include "UnityCG.cginc"
        
        float3 _TexelOffsetScale;
        
        inline half CheckSame (half4 n, half4 nn)
        {
            // difference in normals
            half2 diff = abs(n.xy - nn.xy);
            half sn = (diff.x + diff.y) < _Params3.w;
            // difference in depth
            float z = DecodeFloatRG (n.zw);
            float zz = DecodeFloatRG (nn.zw);
            float zdiff = abs(z-zz) * _ProjectionParams.z;
            half sz = zdiff < _Params3.z;
            return sn * sz;
        }
        
        
        half4 frag( v2f i ) : SV_Target
        {
            #define NUM_BLUR_SAMPLES 3
            
            float2 o = _TexelOffsetScale.xy;
            
            half4 sum = tex2D(_SSAO, i.uv) * (NUM_BLUR_SAMPLES + 1);
            half denom = NUM_BLUR_SAMPLES + 1;
            
            half4 geom = tex2D (_CameraDepthNormalsTexture, i.uv);
            
            for (int s_blur1 = 0; s_blur1 < NUM_BLUR_SAMPLES; ++s_blur1)
            {
                float2 nuv = i.uv + o * (s_blur1+1);
                half4 ngeom = tex2D (_CameraDepthNormalsTexture, nuv.xy);
                half coef = (NUM_BLUR_SAMPLES - s_blur1) * CheckSame (geom, ngeom);
                sum += tex2D (_SSAO, nuv.xy) * coef;
                denom += coef;
            }
            for (int s_blur2 = 0; s_blur2 < NUM_BLUR_SAMPLES; ++s_blur2) 
            {
                float2 nuv = i.uv - o * (s_blur2+1);
                half4 ngeom = tex2D (_CameraDepthNormalsTexture, nuv.xy);
                half coef = (NUM_BLUR_SAMPLES - s_blur2) * CheckSame (geom, ngeom);
                sum += tex2D (_SSAO, nuv.xy) * coef;
                denom += coef;
            }
            return sum / denom;
        }
        ENDCG
    }

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
        
        half4 frag( v2f i ) : SV_Target
        {

            half4 c = tex2D (_MainTex, i.uv.xy);
            half4 gi = tex2D (_SSAO, i.uv.xy);

        
            c.rgb *= LinearToGammaSpace(gi.a + _OcclusionColor.rgb);
            c.rgb += gi.rgb;

            return c;
        }
        ENDCG
    }

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        half4 frag( v2f i ) : SV_Target
        {
            return half4(LinearToGammaSpace(tex2D (_SSAO, i.uv).a + _OcclusionColor.rgb),1);
        }
        ENDCG
    }

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        half4 frag( v2f i ) : SV_Target
        {
            return half4(tex2D (_SSAO, i.uv).rgb,1);
        }
        ENDCG
    }

    }

    Fallback off
    }
