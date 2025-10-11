using UnityEditor;
using UnityEngine;

namespace BloodEffectsPack
{
    [CustomEditor(typeof(BloodModifier))]
    public class BloodModifierEditor : Editor
    {

        private SerializedProperty effectType;
        private SerializedProperty color;
        private SerializedProperty colorIntensity;
        private SerializedProperty albedoPower;
        private SerializedProperty ambientColorIntensity;
        private SerializedProperty hueShift;
        private SerializedProperty useSpecularity;
        private SerializedProperty gravityScale;
        private SerializedProperty smoothness;
        private SerializedProperty decalPresets;
        private SerializedProperty splashPresets;

        private void OnEnable()
        {
            decalPresets = serializedObject.FindProperty("decalPresets");
            splashPresets = serializedObject.FindProperty("splashPresets");
            effectType = serializedObject.FindProperty("effectType");
            color = serializedObject.FindProperty("color");
            colorIntensity = serializedObject.FindProperty("colorIntensity");
            albedoPower = serializedObject.FindProperty("albedoPower");
            ambientColorIntensity = serializedObject.FindProperty("ambientColorIntensity");
            hueShift = serializedObject.FindProperty("hueShift");
            smoothness = serializedObject.FindProperty("smoothness");
            useSpecularity = serializedObject.FindProperty("useSpecularity");
            gravityScale = serializedObject.FindProperty("gravityScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(decalPresets);
            EditorGUILayout.PropertyField(splashPresets);


            BloodModifier bloodModifier = (BloodModifier)target;
            EditorGUILayout.Space(10);
            GUILayout.BeginHorizontal();


            if(bloodModifier.effectType == BloodModifier.EffectType.Decal)
            {
                if(bloodModifier.decalPresets != null)
                {
                    for (int i = 0; i < bloodModifier.decalPresets.Length; i++)
                    {
                        if(bloodModifier.decalPresets[i] != null)
                        {
                            if (GUILayout.Button(bloodModifier.decalPresets[i].presetName))
                            {
                                LoadPreset(bloodModifier.decalPresets[i]);
                            }
                        }
                   
                    }
                }
            }
            else if (bloodModifier.effectType == BloodModifier.EffectType.Splash)
            {
                if (bloodModifier.splashPresets != null)
                {
                    for (int i = 0; i < bloodModifier.splashPresets.Length; i++)
                    {
                        if(bloodModifier.splashPresets[i] != null)
                        {
                            if (GUILayout.Button(bloodModifier.splashPresets[i].presetName))
                            {
                                LoadPreset(bloodModifier.splashPresets[i]);
                            }
                        }
                      
                    }
                }
       
            }



            GUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(effectType);
            EditorGUILayout.PropertyField(color);
            EditorGUILayout.PropertyField(colorIntensity);
            EditorGUILayout.PropertyField(albedoPower);
            EditorGUILayout.PropertyField(ambientColorIntensity);
            EditorGUILayout.PropertyField(hueShift);
            EditorGUILayout.PropertyField(smoothness);
            EditorGUILayout.PropertyField(useSpecularity);
            if(bloodModifier.effectType == BloodModifier.EffectType.Splash)
                EditorGUILayout.PropertyField(gravityScale);


            EditorGUILayout.Space(10);
            if (GUILayout.Button("Apply"))
            {
                Apply();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void LoadPreset(BloodPreset preset)
        {
            color.colorValue = preset.color;
            colorIntensity.floatValue = preset.colorIntensity;
            albedoPower.floatValue = preset.albedoPower;
            ambientColorIntensity.floatValue = preset.ambientColorIntensity;
            hueShift.floatValue = preset.hueshift;
            smoothness.floatValue = preset.smoothness;
            useSpecularity.boolValue = preset.useSpecularity;
            gravityScale.floatValue = preset.gravityScale;

            EditorUtility.SetDirty(target);
        }
        private void Apply()
        {
            BloodModifier bloodModifier = (BloodModifier)target;
            bloodModifier.Apply();
            EditorUtility.SetDirty(bloodModifier);
        }
    }
}
