using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PowerSystemPreviewWindow : EditorWindow
{
    PowerSystemData powerSystemData;
    LightManager lightManager;

    // Preview state
    bool isPreviewingPower = false;
    bool lastPreviewState = false;

    // Light management - separate lists for regular and emergency
    List<GameObject> previewLights = new List<GameObject>();
    List<GameObject> emergencyLights = new List<GameObject>();
    Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    Dictionary<Renderer, int[]> lightMaterialIndices = new Dictionary<Renderer, int[]>();
    Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();

    [MenuItem("Window/Power System Preview")]
    public static void ShowWindow()
    {
        GetWindow<PowerSystemPreviewWindow>("Power System Preview");
    }

    void OnEnable()
    {
        LoadPowerSystemData();
        FindLightManager();
    }

    void OnDisable()
    {
        // Always restore original state when window closes
        ForceRestoreOriginalState();
    }

    void LoadPowerSystemData()
    {
        // Try to find PowerSystemData in the project
        string[] guids = AssetDatabase.FindAssets("t:PowerSystemData");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            powerSystemData = AssetDatabase.LoadAssetAtPath<PowerSystemData>(path);
        }
    }

    void FindLightManager()
    {
        lightManager = FindObjectOfType<LightManager>();
    }

    void OnGUI()
    {
        GUILayout.Label("Power System Preview", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // PowerSystemData field
        EditorGUI.BeginChangeCheck();
        powerSystemData = (PowerSystemData)EditorGUILayout.ObjectField("Power System Data",
            powerSystemData, typeof(PowerSystemData), false);
        if (EditorGUI.EndChangeCheck())
        {
            ForceRestoreOriginalState();
            isPreviewingPower = false;
        }

        GUILayout.Space(10);

        if (powerSystemData == null)
        {
            EditorGUILayout.HelpBox("Please assign a PowerSystemData ScriptableObject", MessageType.Warning);
            return;
        }

        // Find LightManager button
        if (GUILayout.Button("Find Light Manager in Scene"))
        {
            FindLightManager();
        }

        if (lightManager != null)
        {
            EditorGUILayout.HelpBox($"Light Manager found: {lightManager.name}", MessageType.Info);
        }

        GUILayout.Space(20);

        // Direct light controls
        EditorGUILayout.LabelField("Direct Light Controls", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("All Lights ON", GUILayout.Height(30)))
        {
            if (!isPreviewingPower)
            {
                StoreOriginalState();
                isPreviewingPower = true;
            }
            SetAllLights(true);
        }

        if (GUILayout.Button("All Lights OFF", GUILayout.Height(30)))
        {
            if (!isPreviewingPower)
            {
                StoreOriginalState();
                isPreviewingPower = true;
            }
            SetAllLights(false);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Preview controls
        EditorGUILayout.LabelField("Power System Preview", EditorStyles.boldLabel);

        // Power state toggle
        bool newPreviewState = EditorGUILayout.Toggle("Power On", isPreviewingPower ? lastPreviewState : false);

        if (newPreviewState != lastPreviewState || (!isPreviewingPower && newPreviewState))
        {
            if (!isPreviewingPower)
            {
                // First time previewing - store original state
                StoreOriginalState();
                isPreviewingPower = true;
            }

            ApplyPreviewState(newPreviewState);
            lastPreviewState = newPreviewState;
        }

        GUILayout.Space(10);

        // Reset button
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("RESET TO ORIGINAL STATE", GUILayout.Height(30)))
        {
            ForceRestoreOriginalState();
            isPreviewingPower = false;
            lastPreviewState = false;
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(20);

        // Info section
        EditorGUILayout.LabelField("Information", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox($"Regular lights: {previewLights.Count}\nEmergency lights: {emergencyLights.Count}", MessageType.Info);

        // Show light state preview
        EditorGUILayout.LabelField("Light States:", EditorStyles.miniBoldLabel);
        if (isPreviewingPower)
        {
            string powerState = lastPreviewState ? "ON" : "OFF";
            EditorGUILayout.LabelField($"Power: {powerState}");
            EditorGUILayout.LabelField($"Regular lights: {(lastPreviewState ? "ON" : "OFF")}");
            EditorGUILayout.LabelField($"Emergency lights: {(!lastPreviewState ? "Unchanged (ON)" : "OFF")}");
        }
        else
        {
            EditorGUILayout.LabelField("Not in preview mode");
        }

        GUILayout.Space(10);

        // Manual refresh button
        if (GUILayout.Button("Refresh Light List"))
        {
            ForceRestoreOriginalState();
            StoreOriginalState();
            if (isPreviewingPower)
            {
                ApplyPreviewState(lastPreviewState);
            }
        }
    }

    void StoreOriginalState()
    {
        previewLights.Clear();
        emergencyLights.Clear();
        originalMaterials.Clear();
        lightMaterialIndices.Clear();
        originalIntensities.Clear();

        string powerLightTag = "PowerLight";
        string emergencyLightTag = "EmergencyLight";

        // Find regular power lights by tag
        GameObject[] powerTaggedLights = GameObject.FindGameObjectsWithTag(powerLightTag);
        foreach (GameObject light in powerTaggedLights)
        {
            RegisterLight(light, false);
        }

        // Find emergency lights by tag
        GameObject[] emergencyTaggedLights = GameObject.FindGameObjectsWithTag(emergencyLightTag);
        foreach (GameObject light in emergencyTaggedLights)
        {
            RegisterLight(light, true);
        }

        // Find by name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (!previewLights.Contains(obj) && !emergencyLights.Contains(obj))
            {
                if (obj.name.Contains(powerLightTag))
                {
                    RegisterLight(obj, false);
                }
                else if (obj.name.Contains(emergencyLightTag))
                {
                    RegisterLight(obj, true);
                }
            }
        }
    }

    void RegisterLight(GameObject lightObj, bool isEmergency)
    {
        if (isEmergency)
        {
            emergencyLights.Add(lightObj);
        }
        else
        {
            previewLights.Add(lightObj);
        }

        // Store original materials from object and all children
        Renderer[] renderers = lightObj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0)
            {
                // Store the actual shared materials (not a clone)
                originalMaterials[renderer] = renderer.sharedMaterials;

                // Find materials that start with "Light"
                List<int> lightMatIndices = new List<int>();
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] != null &&
                        renderer.sharedMaterials[i].name.StartsWith("Light"))
                    {
                        lightMatIndices.Add(i);
                    }
                }

                if (lightMatIndices.Count > 0)
                {
                    lightMaterialIndices[renderer] = lightMatIndices.ToArray();
                }
            }
        }

        // Store original light intensities from all children
        Light[] lights = lightObj.GetComponentsInChildren<Light>(true);
        foreach (Light light in lights)
        {
            originalIntensities[light] = light.intensity;
        }
    }

    void SetAllLights(bool on)
    {
        Material lightOffMaterial = GetLightOffMaterial();

        // Apply to all lights regardless of type
        foreach (GameObject lightObj in previewLights)
        {
            ApplyLightPreviewState(lightObj, on, false, lightOffMaterial);
        }

        foreach (GameObject lightObj in emergencyLights)
        {
            ApplyLightPreviewState(lightObj, on, true, lightOffMaterial);
        }

        SceneView.RepaintAll();
    }

    void ApplyPreviewState(bool powerOn)
    {
        Material lightOffMaterial = GetLightOffMaterial();

        // Apply states to regular lights
        foreach (GameObject lightObj in previewLights)
        {
            ApplyLightPreviewState(lightObj, powerOn, false, lightOffMaterial);
        }

        // Apply states to emergency lights
        foreach (GameObject lightObj in emergencyLights)
        {
            if (powerOn)
            {
                // Emergency lights turn off when power is on
                ApplyLightPreviewState(lightObj, false, true, lightOffMaterial);
            }
            else
            {
                // Emergency lights remain unchanged (on) when power is off
                RestoreLightPreviewState(lightObj);
            }
        }

        SceneView.RepaintAll();
    }

    Material GetLightOffMaterial()
    {
        if (lightManager != null)
        {
            var offMaterialField = lightManager.GetType().GetField("lightOffMaterial",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (offMaterialField != null)
                return offMaterialField.GetValue(lightManager) as Material;
        }
        return null;
    }

    void ApplyLightPreviewState(GameObject lightObj, bool shouldBeOn, bool isEmergency, Material offMaterial)
    {
        if (lightObj == null) return;

        // Handle materials in object and all children
        Renderer[] renderers = lightObj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (!originalMaterials.ContainsKey(renderer)) continue;

            Material[] newMaterials = new Material[renderer.sharedMaterials.Length];

            if (shouldBeOn)
            {
                // Restore original materials
                newMaterials = originalMaterials[renderer];
            }
            else if (offMaterial != null)
            {
                // Start with original materials
                for (int i = 0; i < newMaterials.Length; i++)
                {
                    newMaterials[i] = originalMaterials[renderer][i];
                }

                // Replace only the materials that start with "Light"
                if (lightMaterialIndices.ContainsKey(renderer))
                {
                    foreach (int index in lightMaterialIndices[renderer])
                    {
                        if (index < newMaterials.Length)
                        {
                            newMaterials[index] = offMaterial;
                        }
                    }
                }
            }

            renderer.sharedMaterials = newMaterials;
            EditorUtility.SetDirty(renderer);
        }

        // Handle all light components in children
        Light[] lights = lightObj.GetComponentsInChildren<Light>(true);
        foreach (Light light in lights)
        {
            if (light.lightmapBakeType != LightmapBakeType.Baked)
            {
                if (shouldBeOn && originalIntensities.ContainsKey(light))
                {
                    light.intensity = originalIntensities[light];
                }
                else
                {
                    light.intensity = 0;
                }

                EditorUtility.SetDirty(light);
            }
        }
    }

    void RestoreLightPreviewState(GameObject lightObj)
    {
        if (lightObj == null) return;

        // Restore materials
        Renderer[] renderers = lightObj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (originalMaterials.ContainsKey(renderer))
            {
                renderer.sharedMaterials = originalMaterials[renderer];
                EditorUtility.SetDirty(renderer);
            }
        }

        // Restore light intensities
        Light[] lights = lightObj.GetComponentsInChildren<Light>(true);
        foreach (Light light in lights)
        {
            if (originalIntensities.ContainsKey(light) && light.lightmapBakeType != LightmapBakeType.Baked)
            {
                light.intensity = originalIntensities[light];
                EditorUtility.SetDirty(light);
            }
        }
    }

    void ForceRestoreOriginalState()
    {
        if (!isPreviewingPower) return;

        // Restore all lights to original state
        List<GameObject> allLights = new List<GameObject>();
        allLights.AddRange(previewLights);
        allLights.AddRange(emergencyLights);

        foreach (GameObject lightObj in allLights)
        {
            RestoreLightPreviewState(lightObj);
        }

        SceneView.RepaintAll();
    }
}