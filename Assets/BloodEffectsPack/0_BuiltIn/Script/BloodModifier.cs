using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BloodEffectsPack
{
    public class BloodModifier : MonoBehaviour
    {
        public enum EffectType
        { Splash, Decal}

        public EffectType effectType = EffectType.Splash;
        public Color color;
        public float colorIntensity;
        public float albedoPower;
        public float ambientColorIntensity;
        [Range(-180, 180)] public float hueShift = 0.0f;
        public float smoothness;
        public bool useSpecularity = true;
        public float gravityScale = 0.0f;
        public BloodPreset[] decalPresets;
        public BloodPreset[] splashPresets;
        private List<Material> mats = new List<Material>();



        public void Apply()
        {
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
            MeshRenderer[] meshRends = GetComponentsInChildren<MeshRenderer>();
          



            for(int i =0; i < particleSystems.Length; i++)
            {
                var mainModule = particleSystems[i].main;
                if(effectType == EffectType.Splash)
                    mainModule.gravityModifierMultiplier = gravityScale;
                ParticleSystemRenderer rend = particleSystems[i].GetComponent<ParticleSystemRenderer>();


      
                Material sharedMat = rend.sharedMaterial;
                sharedMat.SetFloat("_Smoothness", smoothness);
                int specularityInt = useSpecularity ? 1 : 0;
                sharedMat.SetColor("_Color", color);
                sharedMat.SetInt("_UseSpecularity", specularityInt);

                sharedMat.SetFloat("_ColorIntensity", colorIntensity);
                sharedMat.SetFloat("_AlbedoPower", albedoPower);
                sharedMat.SetFloat("_AmbientColorIntensity", ambientColorIntensity);
                sharedMat.SetFloat("_HueShift", hueShift);
            }
            for (int i = 0; i < meshRends.Length; i++)
            {
                Material sharedMat = meshRends[i].sharedMaterial;
                int specularityInt = useSpecularity ? 1 : 0;
                sharedMat.SetColor("_Color", color);
                sharedMat.SetInt("_UseSpecularity", specularityInt);
                sharedMat.SetFloat("_Smoothness", smoothness);

                sharedMat.SetFloat("_ColorIntensity", colorIntensity);
                sharedMat.SetFloat("_AlbedoPower", albedoPower);
                sharedMat.SetFloat("_AmbientColorIntensity", ambientColorIntensity);
                sharedMat.SetFloat("_HueShift", hueShift);

            }
            Projector[] projectors = GetComponentsInChildren<Projector>();
            for (int i = 0; i < projectors.Length; i++)
            {
                Material sharedMat = projectors[i].material;
                int specularityInt = useSpecularity ? 1 : 0;
                sharedMat.SetColor("_Color", color);
                sharedMat.SetInt("_UseSpecularity", specularityInt);
                sharedMat.SetFloat("_Smoothness", smoothness);
                sharedMat.SetFloat("_ColorIntensity", colorIntensity);
                sharedMat.SetFloat("_AlbedoPower", albedoPower);
                sharedMat.SetFloat("_AmbientColorIntensity", ambientColorIntensity);
                sharedMat.SetFloat("_HueShift", hueShift);
            }
         
    
        }
    }
}
