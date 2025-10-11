Shader "Hidden/TonicPostProcessing/TonicDithering"
{
    Properties
    {
        _MainTex       ("Source",    2D)   = "white" {}
        _DitherStrength("Strength",  Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay"
               "ForceNoShadowCasting"="True" "IgnoreProjector"="True" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0
            #include "UnityCG.cginc"

         
            sampler2D _MainTex;
            float4    _MainTex_TexelSize;      
            float     _DitherStrength;

          
            static const float _Bayer[16] = {
                 1.0/17.0,  9.0/17.0,  3.0/17.0, 11.0/17.0,
                13.0/17.0,  5.0/17.0, 15.0/17.0,  7.0/17.0,
                 4.0/17.0, 12.0/17.0,  2.0/17.0, 10.0/17.0,
                16.0/17.0,  8.0/17.0, 14.0/17.0,  6.0/17.0
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv        : TEXCOORD0;
                float4 vertex    : SV_POSITION;
                float4 scrPos    : TEXCOORD1; 
            };

            float Luminance(float3 c) { return dot(c, float3(0.2126,0.7152,0.0722)); }

            float3 ApplyBayer(float3 c, float4 clipPos, float strength)
            {
                if (strength <= 0.0) return c;

                float2 pix = (clipPos.xy / clipPos.w * 0.5 + 0.5) * _ScreenParams.xy;

                int2  idx = int2(fmod(pix, 4.0));
                float b   = _Bayer[idx.y * 4 + idx.x];

                float offset = (b - 0.5/17.0) * (1.0/255.0) * strength;

                float l     = Luminance(c);
                float fade  = smoothstep(0.0, 0.05, l);

                return c + offset * fade;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                o.scrPos = o.vertex;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).rgb;

                col = ApplyBayer(col, i.scrPos, _DitherStrength);

                return float4(saturate(col), 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
