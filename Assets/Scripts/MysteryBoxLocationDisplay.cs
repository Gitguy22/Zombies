using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LocationLightMapping
{
    [Header("Location Mapping")]
    [Tooltip("The mystery box location GameObject")]
    public GameObject locationObject;

    [Tooltip("The light/indicator on the map that represents this location")]
    public GameObject lightObject;

    [Tooltip("Optional: Renderer component if different from lightObject")]
    public Renderer lightRenderer;

    [Header("Optional Audio")]
    [Tooltip("Sound to play when light turns on")]
    public AudioClip lightOnSound;

    [Tooltip("Sound to play when light turns off")]
    public AudioClip lightOffSound;
}

public class MysteryBoxLocationDisplay : MonoBehaviour
{
    [Header("Light Materials")]
    [SerializeField] Material lightOnMaterial;
    [SerializeField] Material lightOffMaterial;

    [Header("Location Mappings")]
    [SerializeField] LocationLightMapping[] locationMappings;

    [Header("Audio Settings")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] bool playLightSounds = true;
    [SerializeField] float lightSoundVolume = 0.5f;

    [Header("Visual Effects")]
    [SerializeField] bool enableLightEffects = true;
    [SerializeField] float lightTransitionTime = 0.5f;
    [SerializeField] AnimationCurve lightIntensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug")]
    [SerializeField] bool debugMode = true;

    // Internal state
    private Dictionary<GameObject, LocationLightMapping> locationToMapping;
    private GameObject currentActiveLocation;
    private Coroutine lightTransitionCoroutine;

    void Awake()
    {
        InitializeLocationMappings();
        SetupAudioSource();
        ValidateMappings();
    }

    void Start()
    {
        // Initialize all lights to off state
        SetAllLightsOff();
        DebugLog("Location display system initialized");
    }

    #region Initialization

    private void InitializeLocationMappings()
    {
        locationToMapping = new Dictionary<GameObject, LocationLightMapping>();

        foreach (var mapping in locationMappings)
        {
            if (mapping.locationObject != null)
            {
                locationToMapping[mapping.locationObject] = mapping;

                // Auto-assign renderer if not set
                if (mapping.lightRenderer == null && mapping.lightObject != null)
                {
                    mapping.lightRenderer = mapping.lightObject.GetComponent<Renderer>();
                }
            }
        }

        DebugLog($"Initialized {locationToMapping.Count} location mappings");
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.volume = lightSoundVolume;
        audioSource.playOnAwake = false;
    }

    private void ValidateMappings()
    {
        bool hasErrors = false;

        if (lightOnMaterial == null)
        {
            ////Debug.LogError("Light On Material is not assigned!");
            hasErrors = true;
        }

        if (lightOffMaterial == null)
        {
            ////Debug.LogError("Light Off Material is not assigned!");
            hasErrors = true;
        }

        foreach (var mapping in locationMappings)
        {
            if (mapping.locationObject == null)
            {
                ////Debug.LogWarning("Location mapping has null location object!");
                hasErrors = true;
            }

            if (mapping.lightObject == null)
            {
                ////Debug.LogWarning($"Location {mapping.locationObject?.name} has no light object assigned!");
                hasErrors = true;
            }

            if (mapping.lightRenderer == null && mapping.lightObject != null)
            {
                ////Debug.LogWarning($"Location {mapping.locationObject?.name} light has no renderer component!");
            }
        }

        if (hasErrors)
        {
            ////Debug.LogWarning("Location display system has configuration errors. Check the inspector!");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Updates the light state for a specific location
    /// </summary>
    /// <param name="location">The mystery box location GameObject</param>
    /// <param name="isActive">Whether the box is at this location</param>
    public void UpdateLocationLight(GameObject location, bool isActive)
    {
        if (location == null) return;

        DebugLog($"Updating location light: {location.name} -> {(isActive ? "ON" : "OFF")}");

        if (locationToMapping.TryGetValue(location, out LocationLightMapping mapping))
        {
            if (isActive)
            {
                SetLightActive(mapping);
                currentActiveLocation = location;
            }
            else
            {
                SetLightInactive(mapping);
                if (currentActiveLocation == location)
                {
                    currentActiveLocation = null;
                }
            }
        }
        else
        {
            DebugLog($"Warning: No mapping found for location {location.name}");
        }
    }
    public void SetAllLightsOff()
    {
        DebugLog("Setting all lights to OFF state");

        foreach (var mapping in locationMappings)
        {
            SetLightInactive(mapping);
        }

        currentActiveLocation = null;
    }

    public GameObject GetCurrentActiveLocation()
    {
        return currentActiveLocation;
    }
    public bool IsLocationActive(GameObject location)
    {
        return currentActiveLocation == location;
    }

    #endregion

    #region Light Control

    private void SetLightActive(LocationLightMapping mapping)
    {
        if (mapping?.lightRenderer == null || lightOnMaterial == null) return;

        if (enableLightEffects)
        {
            if (lightTransitionCoroutine != null)
            {
                StopCoroutine(lightTransitionCoroutine);
            }
            lightTransitionCoroutine = StartCoroutine(TransitionLightMaterial(mapping, lightOnMaterial));
        }
        else
        {
            mapping.lightRenderer.material = lightOnMaterial;
        }

        // Play sound effect
        if (playLightSounds && mapping.lightOnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(mapping.lightOnSound);
        }

        DebugLog($"Light activated for {mapping.locationObject?.name}");
    }

    private void SetLightInactive(LocationLightMapping mapping)
    {
        if (mapping?.lightRenderer == null || lightOffMaterial == null) return;

        if (enableLightEffects)
        {
            if (lightTransitionCoroutine != null)
            {
                StopCoroutine(lightTransitionCoroutine);
            }
            lightTransitionCoroutine = StartCoroutine(TransitionLightMaterial(mapping, lightOffMaterial));
        }
        else
        {
            mapping.lightRenderer.material = lightOffMaterial;
        }

        // Play sound effect
        if (playLightSounds && mapping.lightOffSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(mapping.lightOffSound);
        }

        DebugLog($"Light deactivated for {mapping.locationObject?.name}");
    }

    private IEnumerator TransitionLightMaterial(LocationLightMapping mapping, Material targetMaterial)
    {
        if (mapping.lightRenderer == null || targetMaterial == null) yield break;

        Material startMaterial = mapping.lightRenderer.material;
        float elapsed = 0f;

        // Create a material instance for smooth transition
        Material transitionMaterial = new Material(targetMaterial);
        mapping.lightRenderer.material = transitionMaterial;

        while (elapsed < lightTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lightTransitionTime;
            float intensity = lightIntensityCurve.Evaluate(t);

            // Blend between materials (simplified approach using alpha/emission)
            if (targetMaterial == lightOnMaterial)
            {
                // Transitioning to ON - increase intensity
                Color emissionColor = targetMaterial.GetColor("_EmissionColor") * intensity;
                transitionMaterial.SetColor("_EmissionColor", emissionColor);
            }
            else
            {
                // Transitioning to OFF - decrease intensity
                Color emissionColor = startMaterial.GetColor("_EmissionColor") * (1f - intensity);
                transitionMaterial.SetColor("_EmissionColor", emissionColor);
            }

            yield return null;
        }

        // Set final material
        mapping.lightRenderer.material = targetMaterial;

        // Clean up transition material
        if (transitionMaterial != targetMaterial)
        {
            Destroy(transitionMaterial);
        }

        lightTransitionCoroutine = null;
    }

    #endregion

    #region Debug and Utility

    private void DebugLog(string message)
    {
        if (debugMode)
        {
            ////Debug.Log($"💡 LocationDisplay: {message}");
        }
    }

    [ContextMenu("Test All Lights ON")]
    private void TestAllLightsOn()
    {
        foreach (var mapping in locationMappings)
        {
            if (mapping.lightRenderer != null && lightOnMaterial != null)
            {
                mapping.lightRenderer.material = lightOnMaterial;
            }
        }
    }

    [ContextMenu("Test All Lights OFF")]
    private void TestAllLightsOff()
    {
        SetAllLightsOff();
    }

    void OnValidate()
    {
        // Auto-assign renderers in editor when mappings change
        if (locationMappings != null)
        {
            foreach (var mapping in locationMappings)
            {
                if (mapping.lightRenderer == null && mapping.lightObject != null)
                {
                    mapping.lightRenderer = mapping.lightObject.GetComponent<Renderer>();
                }
            }
        }
    }

    #endregion

    #region Public Utility Methods

    public void AddLocationMapping(GameObject location, GameObject light, AudioClip onSound = null, AudioClip offSound = null)
    {
        if (location == null || light == null) return;

        LocationLightMapping newMapping = new LocationLightMapping
        {
            locationObject = location,
            lightObject = light,
            lightRenderer = light.GetComponent<Renderer>(),
            lightOnSound = onSound,
            lightOffSound = offSound
        };

        // Add to dictionary
        locationToMapping[location] = newMapping;

        // Add to array (for inspector visibility)
        System.Array.Resize(ref locationMappings, locationMappings.Length + 1);
        locationMappings[locationMappings.Length - 1] = newMapping;

        DebugLog($"Added runtime mapping: {location.name} -> {light.name}");
    }
    public void RemoveLocationMapping(GameObject location)
    {
        if (location == null) return;

        if (locationToMapping.ContainsKey(location))
        {
            locationToMapping.Remove(location);
            DebugLog($"Removed mapping for {location.name}");
        }
    }

    #endregion
}