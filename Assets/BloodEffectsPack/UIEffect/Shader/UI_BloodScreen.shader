Shader "BloodEffectsPack/UI_BloodScreen"
{
    Properties
    {
        [Header(ColorControl)]
        _ColorIntensity("ColorIntensity", range(0,10)) = 1.0
        _Saturation("Saturation", Range(0, 20)) = 1.0
        _HueShift("HueShift", range(-180, 180)) = 0.0
        [Space(10)]

        [Header(Background)]
        _BackgroundColor("BackgroundColor", color) = (1,1,1,1)




        [HideInInspector] _MainTex("Texture", 2D) = "white" {}
        [Header(AlphaControl)]
        _Alpha_Power("Alpha_Power", float) = 1.0
        _Alpha_Intensity("Alpha_Intensity", float) = 1.0
        
        [Header(EdgeMask)]
        _EdgeMaskThicknessX("EdgeMaskThicknessX", range(0,1)) = 0.1
        _EdgeMaskThicknessX_Blur("EdgeMaskThicknessX_Blur", range(0,1)) = 0.1
        _EdgeMaskThicknessY("EdgeMaskThicknessY", range(0,1)) = 0.1
        _EdgeMaskThicknessY_Blur("EdgeMaskThicknessY_Blur", range(0,1)) = 0.1


        [Header(EdgeColor)]
        [HDR] _EdgeColor("EdgeColor", color) = (1,1,1,1)
        _EdgeColorThicknessX("EdgeColorThicknessX", range(0,1)) = 0.1
        _EdgeColorThicknessX_Blur("EdgeColorThicknessX_Blur", range(0,1)) = 0.1
        _EdgeColorThicknessY("EdgeColorThicknessY", range(0,1)) = 0.1
        _EdgeColorThicknessY_Blur("EdgeColorThicknessY_Blur", range(0,1)) = 0.1


        [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask("Color Mask", Float) = 15
    }
        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
            ZTest[unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            Stencil
            {
                Ref[_Stencil]
                Comp[_StencilComp]
            }

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile __ UNITY_UI_CLIP_RECT
                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    float4 color : COLOR;
                    #if UNITY_UI_CLIP_RECT
                        float4	rectMask    : TEXCOORD1;
                    #endif
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    float2 screenUV : TEXCOORD1;
                    float4 color : COLOR;

                    #if UNITY_UI_CLIP_RECT
                        float4	rectMask    : TEXCOORD2;
                    #endif
                };

                float _ColorIntensity;
                float _HueShift;
                float _Saturation;

                float4 _BackgroundColor;


                sampler2D _MainTex;
                float _Alpha_Intensity;
                float _Alpha_Power;

                float _EdgeMaskThicknessX;
                float _EdgeMaskThicknessY;
                float _EdgeMaskThicknessX_Blur;
                float _EdgeMaskThicknessY_Blur;


                float4 _EdgeColor;
                float _EdgeColorThicknessX;
                float _EdgeColorThicknessY;
                float _EdgeColorThicknessX_Blur;
                float _EdgeColorThicknessY_Blur;

                #ifdef UNITY_UI_CLIP_RECT
                    float4 _ClipRect;
                    float _UIMaskSoftnessX, _UIMaskSoftnessY;
                #endif


                float3 ApplyHueShift(float3 aColor, float hue)
                {
                    float angle = radians(hue);
                    float3 k = float3(0.57735, 0.57735, 0.57735);
                    float cosAngle = cos(angle);
                    return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
                }
                float3 AdjustSaturation(float3 color, float saturation)
                {
                    float gray = dot(color, float3(0.299, 0.587, 0.114));
                    return lerp(float3(gray.rrr), color, saturation);
                }


                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;

                    // Calculate screen UV coordinates (0 to 1 range)
                    o.screenUV = o.vertex.xy / o.vertex.w * 0.5 + 0.5;
                    o.color = v.color;


                    #ifdef UNITY_UI_CLIP_RECT
                        float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                        o.rectMask = half4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * float2((_UIMaskSoftnessX + 1), (_UIMaskSoftnessY + 1))));
                    #endif


                    return o;
                }

                float4 frag(v2f i) : SV_Target
                {
              \
                
                    float4 col = tex2D(_MainTex, i.uv);
                    col.a *= pow(i.color.a, 1.0);
                    float4 backgroundColor = _BackgroundColor;
                    backgroundColor.a *= pow(i.color.a, 2);
                    float4 edgeColor = _EdgeColor;
                    edgeColor.a *= pow(i.color.a, 0.5);





                    col = lerp(col, backgroundColor, (1.0 - col.a));
                    col.rgb *= _ColorIntensity;
                    col.a = pow(col.a, _Alpha_Power) * _Alpha_Intensity;
           



                    float edgeDistX = abs((i.screenUV.x - 0.5) * 2.0);
                    float edgeMaskX = smoothstep(1.0 - _EdgeMaskThicknessX, 1.0 - _EdgeMaskThicknessX + _EdgeMaskThicknessX_Blur, edgeDistX);
                    col.a *= edgeMaskX;

                    float edgeDistY = abs((i.screenUV.y - 0.5) * 2.0);
                    float edgeMaskY = smoothstep(1.0 - _EdgeMaskThicknessY, 1.0 - _EdgeMaskThicknessY + _EdgeMaskThicknessY_Blur, edgeDistY);
                    col.a *= edgeMaskY;


                    float edgeColorX = smoothstep(1.0 - _EdgeColorThicknessX, 1.0 - _EdgeColorThicknessX + _EdgeColorThicknessX_Blur, edgeDistX);
                    float edgeColorY = smoothstep(1.0 - _EdgeColorThicknessY, 1.0 - _EdgeColorThicknessY + _EdgeColorThicknessY_Blur, edgeDistY);
                
        

                    col = lerp(col, edgeColor, saturate(edgeColorX + edgeColorY));



                  

                    col.rgb = ApplyHueShift(col.rgb, _HueShift);
                    col.rgb = AdjustSaturation(col.rgb, _Saturation);

                    #if UNITY_UI_CLIP_RECT	
                        half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.rectMask.xy)) * i.rectMask.zw);
                        col.a *= m.x * m.y;
                    #endif

                    return col;
            }
            ENDCG
        }
        }
}
