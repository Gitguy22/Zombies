using UnityEngine;

namespace BloodEffectsPack
{
    [CreateAssetMenu(fileName = "BloodPreset", menuName = "BloodEffectsPack/BloodPreset", order = 1)]
    public class BloodPreset : ScriptableObject
    {
        public string presetName;
        public Color color;
        public float colorIntensity;
        public float albedoPower;
        public float ambientColorIntensity;
        [Range(-180,180)] public float hueshift;
        public bool useSpecularity;
        public float gravityScale;
        public float smoothness;
    }
}