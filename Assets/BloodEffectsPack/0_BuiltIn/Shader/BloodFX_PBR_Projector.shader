Shader "BloodEffectsPack/BuiltIn/BloodFX_PBR_Projector"
{
    Properties
    {
        [Header(ViewDirMask)]
        _ViewDirMaskThreshold("ViewDirMaskThreshold", range(0,1)) = 0.1
        
        [Header(Color)]
        _Color("Color", color) = (1,1,1,1)
        _ColorIntensity("ColorIntensity", float) = 1.0
        _AlbedoPower("AlbedoPower", float) = 1.0
        [HideInInspector] _Opacity("Opacity", range(0,1)) = 1.0
       

        _HueShift("HueShift", Range(-180,180)) = 0
        _AmbientColorIntensity("AmbientColorIntensity", float) = 1.0

        [Header(Main)]
        [NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = 0


        [NoScaleOffset] _NormalMap("NormalMap", 2D) = "bump"{}
        _BumpScale("BumpScale", float) = 1.0

        [Header(Specularity)]
        [Toggle] _UseSpecularity("UseSpecularity", float) = 1.0


        //CustomProjector
        [Header(Projector)]
        [Toggle(_ISPROJECTOR)] _ISPROJECTOR("IsProjector", float) = 0
        [Toggle(_ISSPRITESHEET)] _ISSPRITESHEET("IsSpriteSheet", float) = 0
        _Columns("Columns", Range(1, 100)) = 1
        _Rows("Rows", Range(1, 100)) = 1
        _FrameLength("FrameLength", float) = 1
        _Frame("Frame Index", Float) = 0
  
   

    }
        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "true"}
            Pass
            {
                Tags {"LightMode" = "ForwardBase"}

                ZWrite Off
                Blend SrcAlpha OneMinusSrcAlpha
                Offset -1,-1

            

                CGPROGRAM
                #pragma target 3.0
                #pragma multi_compile _ VERTEXLIGHT_ON
                #pragma shader_feature_local _ _ISPROJECTOR
                #pragma shader_feature_local _ _ISSPRITESHEET

                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_fog

                #define FORWARD_BASE_PASS
                #include "BloodVFX_Lighting.cginc"
                ENDCG
            }
            Pass
            {
                Tags{"LightMode" = "ForwardAdd"}
          
                Blend One One
                ZWrite Off
                Offset -1,-1

                CGPROGRAM
                #pragma target 3.0
            
                #pragma multi_compile_fwdadd
                #pragma shader_feature_local _ _ISPROJECTOR
                #pragma shader_feature_local _ _ISSPRITESHEET

                #pragma vertex vert
                #pragma fragment frag


   
                #pragma multi_compile_fog
                #include "BloodVFX_Lighting.cginc"
                ENDCG
            }
  
        }
}
