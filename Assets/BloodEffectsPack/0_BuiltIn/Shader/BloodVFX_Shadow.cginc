// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#if !defined(BLOODVFX_SHADOW_INCLUDED)
#define BLOODVFX_SHADOW_INCLUDED

#include "UnityCG.cginc"

struct appdata {
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : NORMAL;
};

struct v2f
{
	float2 uv : TEXCOORD0;
	float4 vertex : SV_POSITION;
};

sampler2D _MainTex;

v2f vert(appdata v)
{	
	v2f o;

	float4 vertex = UnityClipSpaceShadowCasterPos(v.vertex.xyz, v.normal);
	o.vertex = UnityApplyLinearShadowBias(vertex);
	o.uv = v.uv;
	return o;
}

half4 frag(v2f i) : SV_TARGET{
	float alpha = tex2D(_MainTex, i.uv).a;
	clip(alpha - 0.1);
	return 0;
}

#endif
