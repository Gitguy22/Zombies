Shader "Hidden/TonicPostProcessing/TonicSharpness"
{
    Properties
    {
        _MainTex  ("Texture",   2D)              = "white" {}
        _Strength ("Strength",  Range(0.0, 3.0)) = 1.5
        _Fineness ("Fineness",  Range(0.5, 2.0)) = 1.0 
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Overlay" }
        LOD 100

        Pass
        {
            Name "TonicSharpness"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex  vert
            #pragma fragment frag

            #include "UnityCG.cginc"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }


            sampler2D _MainTex;
            float4    _MainTex_TexelSize; 
            float     _Strength;       
            float     _Fineness;      

            static const float3 _Luma = float3(0.299, 0.587, 0.114);


            float4 frag (v2f i) : SV_Target
            {
                if (_Strength <= 0.001) return tex2D(_MainTex, i.uv);

                float2 baseOffset = _MainTex_TexelSize.xy * _Fineness;


                float3 blur = tex2D(_MainTex, i.uv).rgb * 0.227027f;

                        blur += tex2D(_MainTex, i.uv + baseOffset *  float2( 1,  0)).rgb * 0.1945946f;
                        blur += tex2D(_MainTex, i.uv + baseOffset *  float2(-1,  0)).rgb * 0.1945946f;
                        blur += tex2D(_MainTex, i.uv + baseOffset *  float2( 0,  1)).rgb * 0.1216216f;
                        blur += tex2D(_MainTex, i.uv + baseOffset *  float2( 0, -1)).rgb * 0.1216216f;
                        blur += tex2D(_MainTex, i.uv + baseOffset *  float2( 1,  1)).rgb * 0.054054f;
                        blur += tex2D(_MainTex, i.uv + baseOffset *  float2(-1,  1)).rgb * 0.054054f;
                        blur += tex2D(_MainTex, i.uv + baseOffset *  float2( 1, -1)).rgb * 0.054054f;
                        blur += tex2D(_MainTex, i.uv + baseOffset *  float2(-1, -1)).rgb * 0.054054f;

                float3 centre = tex2D(_MainTex, i.uv).rgb;

                float3 detail = centre - blur;

 
                float  edge  = abs(dot(detail, _Luma));      
                float  mask  = saturate(edge * 24.0);      

                mask *= saturate(_Strength);


                float3 result = centre + detail * (_Strength * mask);

                return float4(saturate(result), 1.0);
            }
            ENDCG
        }
    }

    FallBack Off
}