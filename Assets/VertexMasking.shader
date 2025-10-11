Shader "Custom/VertexMasking"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _VolumeCenter ("Volume Center", Vector) = (0,0,0,0)
        _VolumeSize ("Volume Size", Vector) = (1,1,1,0)
        _MaskingActive ("Masking Active", Float) = 1
        _CameraPosition ("Camera Position", Vector) = (0,0,0,0)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        
        Pass
        {
            Name "VertexMasking"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float maskingFactor : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;  // Fixed the syntax error here
            fixed4 _Color;
            float4 _VolumeCenter;
            float4 _VolumeSize;
            float _MaskingActive;
            float4 _CameraPosition;
            
            v2f vert (appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float maskingFactor = 0.0;
                
                // Check if vertex is inside masking volume
                if (_MaskingActive > 0.5)
                {
                    float3 localPos = worldPos - _VolumeCenter.xyz;
                    float3 absPos = abs(localPos);
                    float3 halfSize = _VolumeSize.xyz * 0.5;
                    
                    // If vertex is inside the volume, hide it
                    if (absPos.x < halfSize.x && absPos.y < halfSize.y && absPos.z < halfSize.z)
                    {
                        // Move vertex far away to hide it
                        v.vertex = float4(0, 0, -1000, 1);
                        maskingFactor = 1.0;
                    }
                }
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = worldPos;
                o.maskingFactor = maskingFactor;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Discard fragment if vertex was masked
                if (i.maskingFactor > 0.5)
                {
                    discard;
                }
                
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}