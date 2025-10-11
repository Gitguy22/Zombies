Shader "Hidden/TonicPostProcessing/RenderScale"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Scale   ("Render Scale (0.1-4)", Range(0.1,4)) = 1
        _FilterMode ("Filter Mode", Int) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }

        Pass
        {
            Name "RenderScale"
            ZTest  Always
            ZWrite Off
            Cull   Off
            Blend  Off

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f     { float2 uv:TEXCOORD0; float4 vertex:SV_POSITION; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Scale;
            int _FilterMode;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 SampleBicubic(sampler2D tex, float2 uv, float4 texelSize)
            {
                float2 samplePos = uv * texelSize.zw;
                float2 texPos1 = floor(samplePos - 0.5) + 0.5;
                float2 f = samplePos - texPos1;
                
                // Cubic weights
                float2 w0 = f * (-0.5 + f * (1.0 - 0.5 * f));
                float2 w1 = 1.0 + f * f * (-2.5 + 1.5 * f);
                float2 w2 = f * (0.5 + f * (2.0 - 1.5 * f));
                float2 w3 = f * f * (-0.5 + 0.5 * f);
                
                float2 g0 = w0 + w1;
                float2 g1 = w2 + w3;
                float2 h0 = (w1 / g0) - 1.0;
                float2 h1 = (w3 / g1) + 1.0;
                
                float2 tex00 = (texPos1 + h0) * texelSize.xy;
                float2 tex10 = (texPos1 + float2(h1.x, h0.y)) * texelSize.xy;
                float2 tex01 = (texPos1 + float2(h0.x, h1.y)) * texelSize.xy;
                float2 tex11 = (texPos1 + h1) * texelSize.xy;
                
                float4 col00 = tex2D(tex, tex00);
                float4 col10 = tex2D(tex, tex10);
                float4 col01 = tex2D(tex, tex01);
                float4 col11 = tex2D(tex, tex11);
                
                float4 col0 = lerp(col00, col10, g1.x / (g0.x + g1.x));
                float4 col1 = lerp(col01, col11, g1.x / (g0.x + g1.x));
                
                return lerp(col0, col1, g1.y / (g0.y + g1.y));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (_Scale < 1.0)
                {
                    float lodMul = max(1.0, 1.0 / _Scale);
                    float2 dx = ddx(i.uv) * lodMul;
                    float2 dy = ddy(i.uv) * lodMul;
                    return tex2Dgrad(_MainTex, i.uv, dx, dy);
                }
                else if (_Scale > 1.0)
                {
                    return SampleBicubic(_MainTex, i.uv, _MainTex_TexelSize);
                }
                else
                {
                    return tex2D(_MainTex, i.uv);
                }
            }
            ENDCG
        }
    }
    Fallback Off
}