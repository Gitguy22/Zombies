using UnityEngine;
using UnityEditor;
namespace BloodEffectsPack
{
    [CustomEditor(typeof(BloodEffectsPack.ContinuousProjectorSpawner))]
    public class ContinuousProjectorSpawner_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            var spawner = (BloodEffectsPack.ContinuousProjectorSpawner)target;
            EditorGUILayout.LabelField("Ignore Layers", EditorStyles.boldLabel);

            // Get the current layer names
            string[] layerNames = GetNonEmptyLayerNames();

            // Use the filtered layer names for the MaskField
            spawner.ignoreLayerMask = EditorGUILayout.MaskField("Ignore Layer Mask", spawner.ignoreLayerMask, layerNames);

            // Draw default inspector
            DrawDefaultInspector();

            // Apply changes
            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        private string[] GetNonEmptyLayerNames()
        {
            var nonEmptyLayers = new System.Collections.Generic.List<string>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);

                // Skip the layer if it's empty (i.e., no objects are assigned to this layer)
                if (!string.IsNullOrEmpty(layerName))
                {
                    nonEmptyLayers.Add(layerName);
                }
            }

            return nonEmptyLayers.ToArray();
        }

    }
}
