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
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        Pass
        {
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
                float3 normal : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _VolumeCenter;
            float4 _VolumeSize;
            float _MaskingActive;
            float4 _CameraPosition;
            
            v2f vert (appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // Check if vertex is inside masking volume
                if (_MaskingActive > 0.5)
                {
                    float3 localPos = worldPos - _VolumeCenter.xyz;
                    float3 absPos = abs(localPos);
                    
                    // If vertex is inside the volume, move it away from camera
                    if (absPos.x < _VolumeSize.x * 0.5 && 
                        absPos.y < _VolumeSize.y * 0.5 && 
                        absPos.z < _VolumeSize.z * 0.5)
                    {
                        // Move vertex far away or make it invisible
                        v.vertex = float4(0, 0, -1000, 1);
                    }
                }
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = worldPos;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}