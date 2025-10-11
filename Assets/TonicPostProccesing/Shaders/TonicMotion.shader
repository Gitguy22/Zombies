Shader "Hidden/TonicPostProcessing/TonicMotionBlur"
{
    Properties
    {
        _MainTex ("-", 2D) = "white" {}
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

            float2 _ScreenSpaceVelocity; 
            int _SampleCount;

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

            float4 frag(v2f i) : SV_Target
            {
                float3 accumulatedColor = float3(0.0, 0.0, 0.0);
                
                if (_SampleCount <= 1 || length(_ScreenSpaceVelocity) < 1e-5f) 
                {
                    return tex2D(_MainTex, i.uv);
                }


                float2 uvStep = _ScreenSpaceVelocity / max(1, _SampleCount - 1);

                for (int s = 0; s < _SampleCount; ++s)
                {

                    float t = ((float)s / (float)max(1, _SampleCount - 1)) - 0.5f;
                    if (_SampleCount == 1) t = 0.0f; 

                    float2 sampleUv = i.uv + uvStep * t * (_SampleCount -1) ; 
                                                                       

                    accumulatedColor += tex2D(_MainTex, sampleUv).rgb;
                }
                
                return float4(accumulatedColor / _SampleCount, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
