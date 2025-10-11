using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using GameAudio;
using System.Linq;

[CustomEditor(typeof(SoundDataGenerator))]
public class SoundDataGeneratorEditor : Editor
{
    private SoundDataGenerator generator;
    private SerializedProperty targetFolderPath;

    void OnEnable()
    {
        generator = (SoundDataGenerator)target;
        targetFolderPath = serializedObject.FindProperty("targetFolderPath");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sound Data Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool scans components for AudioClip fields and creates SoundEffectData assets.", MessageType.Info);

        EditorGUILayout.Space();

        // Folder selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(targetFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Target Folder", generator.targetFolderPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                // Convert to relative path
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                generator.targetFolderPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        // Settings
        EditorGUILayout.Space();
        generator.autoAssignToComponents = EditorGUILayout.Toggle("Auto Assign to Components", generator.autoAssignToComponents);
        generator.useDefaultSettings = EditorGUILayout.Toggle("Use Default Settings", generator.useDefaultSettings);

        if (generator.useDefaultSettings)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Settings", EditorStyles.boldLabel);
            generator.defaultVolume = EditorGUILayout.Slider("Volume", generator.defaultVolume, 0f, 1f);
            generator.defaultVolumeVariation = EditorGUILayout.Slider("Volume Variation", generator.defaultVolumeVariation, 0f, 0.2f);
            generator.defaultPitch = EditorGUILayout.Slider("Pitch", generator.defaultPitch, 0.1f, 3f);
            generator.defaultPitchVariation = EditorGUILayout.Slider("Pitch Variation", generator.defaultPitchVariation, 0f, 0.2f);
            generator.defaultIs3D = EditorGUILayout.Toggle("3D Sound", generator.defaultIs3D);

            if (generator.defaultIs3D)
            {
                generator.defaultMinDistance = EditorGUILayout.FloatField("Min Distance", generator.defaultMinDistance);
                generator.defaultMaxDistance = EditorGUILayout.FloatField("Max Distance", generator.defaultMaxDistance);
            }

            generator.defaultPoolSize = EditorGUILayout.IntSlider("Pool Size", generator.defaultPoolSize, 1, 20);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        // Action buttons
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Scan for Audio Clips", GUILayout.Height(30)))
        {
            ScanForAudioClips();
        }

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Generate Sound Data", GUILayout.Height(30)))
        {
            GenerateSoundData();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // Show found sounds
        if (generator.foundSounds.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Found {generator.foundSounds.Count} Audio Fields:", EditorStyles.boldLabel);

            foreach (var sound in generator.foundSounds)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);

                string label = sound.fieldName;
                if (sound.isArray)
                {
                    label += $" (Array[{sound.audioClips?.Length ?? 0}])";
                    EditorGUILayout.ObjectField(label, null, typeof(AudioClip), false);
                }
                else
                {
                    EditorGUILayout.ObjectField(label, sound.audioClip, typeof(AudioClip), false);
                }

                EditorGUI.EndDisabledGroup();

                if (sound.generatedData != null)
                {
                    GUI.color = Color.green;
                    EditorGUILayout.ObjectField(sound.generatedData, typeof(SoundEffectData), false, GUILayout.Width(150));
                    GUI.color = Color.white;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // Quick actions
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Clear All"))
        {
            generator.foundSounds.Clear();
            generator.generatedSoundData.Clear();
        }

        if (GUILayout.Button("Select All Generated"))
        {
            Selection.objects = generator.generatedSoundData.ToArray();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void ScanForAudioClips()
    {
        generator.foundSounds.Clear();

        // Get all components on the GameObject
        Component[] components = generator.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null || component == generator) continue;

            // Use SerializedObject to properly read arrays
            SerializedObject serializedComponent = new SerializedObject(component);
            SerializedProperty property = serializedComponent.GetIterator();

            // Iterate through all serialized properties
            while (property.NextVisible(true))
            {
                // Check for single AudioClip
                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                    property.objectReferenceValue != null &&
                    property.objectReferenceValue.GetType() == typeof(AudioClip))
                {
                    AudioClip clip = (AudioClip)property.objectReferenceValue;
                    generator.foundSounds.Add(new SoundMapping(property.name, clip));
                    Debug.Log($"Found AudioClip: {property.name} = {clip.name}");
                }
                // Check for AudioClip array
                else if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
                {
                    // Check if it's an array of AudioClips
                    if (property.arraySize > 0)
                    {
                        SerializedProperty firstElement = property.GetArrayElementAtIndex(0);
                        if (firstElement != null &&
                            firstElement.propertyType == SerializedPropertyType.ObjectReference &&
                            firstElement.objectReferenceValue is AudioClip)
                        {
                            // It's an AudioClip array - collect all non-null clips
                            List<AudioClip> clips = new List<AudioClip>();

                            for (int i = 0; i < property.arraySize; i++)
                            {
                                SerializedProperty element = property.GetArrayElementAtIndex(i);
                                if (element.objectReferenceValue != null && element.objectReferenceValue is AudioClip)
                                {
                                    clips.Add((AudioClip)element.objectReferenceValue);
                                }
                            }

                            if (clips.Count > 0)
                            {
                                generator.foundSounds.Add(new SoundMapping(property.name, clips.ToArray()));
                                Debug.Log($"Found AudioClip Array: {property.name} with {clips.Count} clips");
                            }
                        }
                    }
                }
            }

            // Also try reflection for non-serialized fields or edge cases
            FieldInfo[] fields = component.GetType().GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            foreach (FieldInfo field in fields)
            {
                // Skip if we already found this field via SerializedProperty
                if (generator.foundSounds.Any(s => s.fieldName == field.Name))
                    continue;

                // Check if field has SerializeField attribute or is public
                bool isSerializedField = field.IsPublic ||
                    field.GetCustomAttribute<SerializeField>() != null;

                if (!isSerializedField) continue;

                // Check for AudioClip
                if (field.FieldType == typeof(AudioClip))
                {
                    AudioClip clip = (AudioClip)field.GetValue(component);
                    if (clip != null)
                    {
                        generator.foundSounds.Add(new SoundMapping(field.Name, clip));
                        Debug.Log($"Found AudioClip via reflection: {field.Name}");
                    }
                }
                // Check for AudioClip array
                else if (field.FieldType == typeof(AudioClip[]))
                {
                    AudioClip[] clips = (AudioClip[])field.GetValue(component);
                    if (clips != null && clips.Length > 0)
                    {
                        // Filter out null clips
                        AudioClip[] validClips = clips.Where(c => c != null).ToArray();
                        if (validClips.Length > 0)
                        {
                            generator.foundSounds.Add(new SoundMapping(field.Name, validClips));
                            Debug.Log($"Found AudioClip Array via reflection: {field.Name} with {validClips.Length} clips");
                        }
                    }
                }
            }
        }

        Debug.Log($"Scan complete! Found {generator.foundSounds.Count} audio fields on {generator.gameObject.name}");
    }

    void GenerateSoundData()
    {
        if (generator.foundSounds.Count == 0)
        {
            EditorUtility.DisplayDialog("No Audio Found", "Please scan for audio clips first!", "OK");
            return;
        }

        // Ensure folder exists
        if (!Directory.Exists(generator.targetFolderPath))
        {
            Directory.CreateDirectory(generator.targetFolderPath);
        }

        generator.generatedSoundData.Clear();

        foreach (var sound in generator.foundSounds)
        {
            // Generate name
            string assetName = $"{generator.gameObject.name}_{CleanFieldName(sound.fieldName)}_SoundData.asset";
            string assetPath = Path.Combine(generator.targetFolderPath, assetName);

            // Create or load existing
            SoundEffectData soundData = AssetDatabase.LoadAssetAtPath<SoundEffectData>(assetPath);

            if (soundData == null)
            {
                soundData = ScriptableObject.CreateInstance<SoundEffectData>();

                // Set clips
                if (sound.isArray)
                {
                    soundData.clips = sound.audioClips;
                }
                else
                {
                    soundData.clips = new AudioClip[] { sound.audioClip };
                }

                // Apply default settings
                if (generator.useDefaultSettings)
                {
                    soundData.volume = generator.defaultVolume;
                    soundData.volumeVariation = generator.defaultVolumeVariation;
                    soundData.pitch = generator.defaultPitch;
                    soundData.pitchVariation = generator.defaultPitchVariation;
                    soundData.is3D = generator.defaultIs3D;
                    soundData.minDistance = generator.defaultMinDistance;
                    soundData.maxDistance = generator.defaultMaxDistance;
                    soundData.poolSize = generator.defaultPoolSize;
                    soundData.spatialBlend = generator.defaultIs3D ? 1f : 0f;
                }

                AssetDatabase.CreateAsset(soundData, assetPath);
            }
            else
            {
                // Update existing
                if (sound.isArray)
                {
                    soundData.clips = sound.audioClips;
                }
                else
                {
                    soundData.clips = new AudioClip[] { sound.audioClip };
                }

                EditorUtility.SetDirty(soundData);
            }

            sound.generatedData = soundData;
            generator.generatedSoundData.Add(soundData);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (generator.autoAssignToComponents)
        {
            AutoAssignToComponents();
        }

        EditorUtility.DisplayDialog("Success",
            $"Generated {generator.generatedSoundData.Count} SoundEffectData assets!", "OK");
    }

    void AutoAssignToComponents()
    {
        Component[] components = generator.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null || component == generator) continue;

            System.Type componentType = component.GetType();
            bool modified = false;

            foreach (var sound in generator.foundSounds)
            {
                if (sound.generatedData == null) continue;

                // Try to find a corresponding SoundEffectData field
                string dataFieldName = sound.fieldName + "Data";
                FieldInfo dataField = componentType.GetField(dataFieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (dataField != null && dataField.FieldType == typeof(SoundEffectData))
                {
                    dataField.SetValue(component, sound.generatedData);
                    modified = true;
                    Debug.Log($"Assigned {sound.generatedData.name} to {component.GetType().Name}.{dataFieldName}");
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(component);
            }
        }
    }

    string CleanFieldName(string fieldName)
    {
        // Remove common prefixes/suffixes
        string clean = fieldName;
        clean = clean.Replace("sounds", "");
        clean = clean.Replace("Sounds", "");
        clean = clean.Replace("Sound", "");
        clean = clean.Replace("Audio", "");
        clean = clean.Replace("Clip", "");
        clean = clean.Replace("clips", "");
        clean = clean.Replace("Clips", "");
        clean = clean.Replace("_", "");
        clean = clean.Replace("m_", ""); // Remove Unity's internal prefix

        // Capitalize first letter
        if (!string.IsNullOrEmpty(clean))
        {
            clean = char.ToUpper(clean[0]) + clean.Substring(1);
        }

        return clean;
    }
}