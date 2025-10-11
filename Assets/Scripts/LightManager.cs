using System.Collections.Generic;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    [Header("Power System")]
    [SerializeField] PowerSystemData powerSystemData;

    [Header("Light Configuration")]
    [SerializeField] GameObject[] lightPrefabs;
    [SerializeField] Material lightOffMaterial;
    [SerializeField] string powerLightTag = "PowerLight";
    [SerializeField] string emergencyLightTag = "EmergencyLight";

    [Header("Lightmap Settings")]
    [SerializeField] Texture2D[] darkLightmaps; // Array of dark lightmaps (no lighting data)
    [SerializeField] bool swapLightmaps = true;

    [Header("Settings")]
    [SerializeField] bool affectRealtimeLights = true;
    [SerializeField] bool changeEmissiveMaterials = true;

    // Separate lists for regular and emergency lights
    List<GameObject> managedLights = new List<GameObject>();
    List<GameObject> emergencyLights = new List<GameObject>();

    // Store original states for restoration
    Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    Dictionary<Renderer, int[]> lightMaterialIndices = new Dictionary<Renderer, int[]>();
    Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();

    // Lightmap storage
    LightmapData[] originalLightmaps;
    bool lightmapsStored = false;

    void Start()
    {
        // Store original lightmaps
        StoreLightmaps();

        // Find all lights in scene
        FindAllLights();

        // Subscribe to power events
        if (powerSystemData != null)
        {
            powerSystemData.OnPowerStateChanged.AddListener(OnPowerStateChanged);
            // Apply initial state
            ApplyPowerState(powerSystemData.IsPowerOn);
        }
    }

    void OnDestroy()
    {
        if (powerSystemData != null)
        {
            powerSystemData.OnPowerStateChanged.RemoveListener(OnPowerStateChanged);
        }

        // Restore original lightmaps on destroy
        RestoreLightmaps();
    }

    void StoreLightmaps()
    {
        if (!swapLightmaps || lightmapsStored) return;

        // Store the original lightmaps
        originalLightmaps = new LightmapData[LightmapSettings.lightmaps.Length];
        for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
        {
            originalLightmaps[i] = new LightmapData();
            originalLightmaps[i].lightmapColor = LightmapSettings.lightmaps[i].lightmapColor;
            originalLightmaps[i].lightmapDir = LightmapSettings.lightmaps[i].lightmapDir;
            originalLightmaps[i].shadowMask = LightmapSettings.lightmaps[i].shadowMask;
        }

        lightmapsStored = true;
    }

    void FindAllLights()
    {
        managedLights.Clear();
        emergencyLights.Clear();
        originalMaterials.Clear();
        lightMaterialIndices.Clear();
        originalIntensities.Clear();

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

        // Also find lights by name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains(powerLightTag) && !managedLights.Contains(obj))
            {
                RegisterLight(obj, false);
            }
            else if (obj.name.Contains(emergencyLightTag) && !emergencyLights.Contains(obj))
            {
                RegisterLight(obj, true);
            }
        }

        // Find instances of prefabs
        if (lightPrefabs != null && lightPrefabs.Length > 0)
        {
            foreach (GameObject prefab in lightPrefabs)
            {
                if (prefab == null) continue;

                string prefabName = prefab.name;
                GameObject[] instances = FindObjectsOfType<GameObject>();

                foreach (GameObject obj in instances)
                {
                    if (obj.name.StartsWith(prefabName) && !managedLights.Contains(obj) && !emergencyLights.Contains(obj))
                    {
                        // Check if it's tagged as emergency
                        bool isEmergency = obj.tag == emergencyLightTag || obj.name.Contains(emergencyLightTag);
                        RegisterLight(obj, isEmergency);
                    }
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
            managedLights.Add(lightObj);
        }

        // Store original materials and find "Light" materials in the object and all children
        Renderer[] renderers = lightObj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.materials != null && renderer.materials.Length > 0)
            {
                // Store the actual current materials (not a clone)
                originalMaterials[renderer] = renderer.materials;

                // Find indices of materials that start with "Light"
                List<int> lightMatIndices = new List<int>();
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (renderer.materials[i] != null && renderer.materials[i].name.StartsWith("Light"))
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

    void OnPowerStateChanged(bool isPowerOn)
    {
        ApplyPowerState(isPowerOn);
    }

    void ApplyPowerState(bool isPowerOn)
    {
        // Swap lightmaps based on power state
        if (swapLightmaps)
        {
            if (isPowerOn)
            {
                RestoreLightmaps();
            }
            else
            {
                ApplyDarkLightmaps();
            }
        }

        // Handle regular lights (affected when power changes)
        foreach (GameObject lightObj in managedLights)
        {
            ApplyLightState(lightObj, isPowerOn, false);
        }

        // Handle emergency lights (only affected when power is ON - they turn off)
        foreach (GameObject lightObj in emergencyLights)
        {
            // Emergency lights are ON (unchanged) when power is OFF
            // Emergency lights are OFF when power is ON
            if (isPowerOn)
            {
                ApplyLightState(lightObj, false, true);
            }
            else
            {
                // When power is off, emergency lights remain in their original state
                RestoreLightState(lightObj);
            }
        }
    }

    void ApplyDarkLightmaps()
    {
        if (!swapLightmaps || darkLightmaps == null || darkLightmaps.Length == 0) return;

        LightmapData[] newLightmaps = new LightmapData[LightmapSettings.lightmaps.Length];

        for (int i = 0; i < newLightmaps.Length; i++)
        {
            newLightmaps[i] = new LightmapData();

            // Use dark lightmap if available, otherwise use original
            if (i < darkLightmaps.Length && darkLightmaps[i] != null)
            {
                newLightmaps[i].lightmapColor = darkLightmaps[i];
            }
            else if (originalLightmaps != null && i < originalLightmaps.Length)
            {
                newLightmaps[i].lightmapColor = originalLightmaps[i].lightmapColor;
            }

            // Keep directional and shadow data if needed
            if (originalLightmaps != null && i < originalLightmaps.Length)
            {
                newLightmaps[i].lightmapDir = originalLightmaps[i].lightmapDir;
                newLightmaps[i].shadowMask = originalLightmaps[i].shadowMask;
            }
        }

        LightmapSettings.lightmaps = newLightmaps;
    }

    void RestoreLightmaps()
    {
        if (!swapLightmaps || !lightmapsStored || originalLightmaps == null) return;

        LightmapSettings.lightmaps = originalLightmaps;
    }

    void ApplyLightState(GameObject lightObj, bool shouldBeOn, bool isEmergency)
    {
        if (lightObj == null) return;

        // Handle materials for object and all children
        if (changeEmissiveMaterials)
        {
            Renderer[] renderers = lightObj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (!originalMaterials.ContainsKey(renderer)) continue;

                Material[] currentMaterials = new Material[renderer.materials.Length];

                if (shouldBeOn)
                {
                    // Restore original materials
                    currentMaterials = originalMaterials[renderer];
                }
                else if (lightOffMaterial != null)
                {
                    // Start with original materials
                    for (int i = 0; i < currentMaterials.Length; i++)
                    {
                        currentMaterials[i] = originalMaterials[renderer][i];
                    }

                    // Replace only the materials that start with "Light"
                    if (lightMaterialIndices.ContainsKey(renderer))
                    {
                        foreach (int index in lightMaterialIndices[renderer])
                        {
                            if (index < currentMaterials.Length)
                            {
                                currentMaterials[index] = lightOffMaterial;
                            }
                        }
                    }
                }

                renderer.materials = currentMaterials;
            }
        }

        // Handle all light components in children
        if (affectRealtimeLights)
        {
            Light[] lights = lightObj.GetComponentsInChildren<Light>(true);
            foreach (Light light in lights)
            {
                // In builds, all runtime Light components should be affected
                // (baked lights don't exist as Light components at runtime)
                if (shouldBeOn)
                {
                    // Restore original intensity
                    if (originalIntensities.ContainsKey(light))
                    {
                        light.intensity = originalIntensities[light];
                    }
                }
                else
                {
                    light.intensity = 0;
                }
            }
        }
    }

    void RestoreLightState(GameObject lightObj)
    {
        if (lightObj == null) return;

        // Restore all materials to originals
        Renderer[] renderers = lightObj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (originalMaterials.ContainsKey(renderer))
            {
                renderer.materials = originalMaterials[renderer];
            }
        }

        // Restore all light intensities
        Light[] lights = lightObj.GetComponentsInChildren<Light>(true);
        foreach (Light light in lights)
        {
            // In builds, restore all runtime lights
            if (originalIntensities.ContainsKey(light))
            {
                light.intensity = originalIntensities[light];
            }
        }
    }
}