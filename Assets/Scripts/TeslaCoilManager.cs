using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeslaCoilManager : MonoBehaviour
{
    [Header("Tesla Coils")]
    public List<Transform> teslaCoils = new List<Transform>();

    [Header("Electricity Settings")]
    public GameObject electricityPrefab;
    public float minTimeBetweenStrikes = 0.5f;
    public float maxTimeBetweenStrikes = 2.0f;
    public float electricityDuration = 0.3f;
    public int maxSimultaneousStrikes = 3;

    [Header("Growth Method")]
    public ElectricityEffect.GrowthMethod defaultGrowthMethod = ElectricityEffect.GrowthMethod.Hybrid;

    [Header("Visual Settings")]
    public Material electricityMaterial;
    public AnimationCurve electricityIntensityCurve;
    public float maxLineWidth = 0.5f;
    public Color electricityColor = new Color(0.5f, 0.8f, 1.0f);
    public float electricityNoise = 0.2f;

    [Header("Simple Lightning Settings")]
    public int segmentCount = 50;
    public float branchProbability = 0.3f;
    public int maxBranches = 10;
    public float glowIntensity = 2f;

    [Header("Laplacian Lightning Settings")]
    public float eta = 6.3f;
    public float gridCellSize = 0.1f;
    public int maxLaplacianSites = 500;
    public bool considerEnvironment = true;

    [Header("Environmental Settings")]
    public float groundHeight = 0f;
    public float groundChargeStrength = -1f;
    public List<Transform> attractors = new List<Transform>();
    public List<Transform> repulsors = new List<Transform>();

    [Header("Light Settings")]
    public float lightIntensity = 5f;
    public float lightRange = 20f;
    public bool enableShadows = false;

    [Header("Audio Settings")]
    public AudioClip thunderClip;
    public AudioClip electricityLoop;
    public float soundVolume = 1f;
    public float soundDelay = 0.05f;
    public float maxAudioWaitTime = 5f;
    public float thunderMaxDistance = 30f;
    public float electricityMaxDistance = 20f;
    public float minPitch = 0.7f;
    public float maxPitch = 1.3f;

    private List<ElectricityEffect> activeEffects = new List<ElectricityEffect>();
    private ElectricityObjectPool electricityPool;

    void Start()
    {
        // Initialize electricity object pool
        electricityPool = new ElectricityObjectPool(
            () => Instantiate(electricityPrefab),
            effect => effect.SetActive(true),
            effect => effect.SetActive(false),
            effect => Destroy(effect),
            false, 10, 20
        );

        // Set up default intensity curve if not configured
        if (electricityIntensityCurve == null || electricityIntensityCurve.keys.Length == 0)
        {
            CreateDefaultIntensityCurve();
        }

        StartCoroutine(ElectricityController());
    }

    void CreateDefaultIntensityCurve()
    {
        electricityIntensityCurve = new AnimationCurve();

        // Quick strike pattern for realistic lightning
        electricityIntensityCurve.AddKey(new Keyframe(0.0f, 0.0f, 0, 50));
        electricityIntensityCurve.AddKey(new Keyframe(0.02f, 1.0f, 0, 0));
        electricityIntensityCurve.AddKey(new Keyframe(0.05f, 0.9f, -2, -2));
        electricityIntensityCurve.AddKey(new Keyframe(0.1f, 0.95f, 0, -5));
        electricityIntensityCurve.AddKey(new Keyframe(0.2f, 0.7f, -3, -3));
        electricityIntensityCurve.AddKey(new Keyframe(0.4f, 0.3f, -2, -2));
        electricityIntensityCurve.AddKey(new Keyframe(0.6f, 0.1f, -1, -1));
        electricityIntensityCurve.AddKey(new Keyframe(1.0f, 0.0f, -1, 0));
    }

    IEnumerator ElectricityController()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minTimeBetweenStrikes, maxTimeBetweenStrikes));

            if (activeEffects.Count < maxSimultaneousStrikes && teslaCoils.Count >= 2)
            {
                CreateRandomElectricityStrike();
            }
        }
    }

    void CreateRandomElectricityStrike()
    {
        // Select two random different coils
        int coil1Index = Random.Range(0, teslaCoils.Count);
        int coil2Index;
        do
        {
            coil2Index = Random.Range(0, teslaCoils.Count);
        } while (coil2Index == coil1Index);

        Transform startCoil = teslaCoils[coil1Index];
        Transform endCoil = teslaCoils[coil2Index];

        CreateElectricityStrike(startCoil, endCoil);
    }

    void CreateElectricityStrike(Transform startCoil, Transform endCoil)
    {
        GameObject electricityGO = electricityPool.Get();
        ElectricityEffect effect = electricityGO.GetComponent<ElectricityEffect>();

        if (effect == null)
        {
            effect = electricityGO.AddComponent<ElectricityEffect>();
        }

        ConfigureElectricityEffect(effect);

        effect.Initialize(startCoil, endCoil, this);
        activeEffects.Add(effect);

        StartCoroutine(ReturnToPool(electricityGO, effect));
    }

    void ConfigureElectricityEffect(ElectricityEffect effect)
    {
        // Growth method
        effect.growthMethod = defaultGrowthMethod;

        // Simple lightning settings
        effect.segmentCount = segmentCount;
        effect.branchProbability = branchProbability;
        effect.maxBranches = maxBranches;
        effect.glowIntensity = glowIntensity;
        effect.intensityCurve = electricityIntensityCurve;

        // Laplacian settings
        effect.eta = eta;
        effect.gridCellSize = gridCellSize;
        effect.maxLaplacianSites = maxLaplacianSites;
        effect.considerEnvironment = considerEnvironment;

        // Light settings
        effect.lightIntensity = lightIntensity;
        effect.lightRange = lightRange;
        effect.enableShadows = enableShadows;

        // Audio settings
        effect.thunderClip = thunderClip;
        effect.electricityLoop = electricityLoop;
        effect.thunderVolume = 1f;
        effect.electricityVolume = 0.5f;
        effect.soundDelay = soundDelay;
        effect.thunderMaxDistance = thunderMaxDistance;
        effect.electricityMaxDistance = electricityMaxDistance;
        effect.minPitch = minPitch;
        effect.maxPitch = maxPitch;
    }

    IEnumerator ReturnToPool(GameObject electricityGO, ElectricityEffect effect)
    {
        yield return new WaitForSeconds(electricityDuration);

        // Wait for thunder audio to finish (with maximum wait time)
        float audioWaitTime = 0f;
        while (effect.IsAudioPlaying() && audioWaitTime < maxAudioWaitTime)
        {
            yield return new WaitForSeconds(0.1f);
            audioWaitTime += 0.1f;
        }

        activeEffects.Remove(effect);
        effect.PrepareForReuse();
        electricityPool.Release(electricityGO);
    }

    // Public methods for manual control
    public void TriggerStrike(Transform startCoil, Transform endCoil)
    {
        CreateElectricityStrike(startCoil, endCoil);
    }

    public void TriggerRandomStrike()
    {
        if (teslaCoils.Count >= 2)
        {
            CreateRandomElectricityStrike();
        }
    }

    public void SetGrowthMethod(ElectricityEffect.GrowthMethod method)
    {
        defaultGrowthMethod = method;
    }

    public void AddAttractor(Transform attractor)
    {
        if (!attractors.Contains(attractor))
        {
            attractors.Add(attractor);
        }
    }

    public void RemoveAttractor(Transform attractor)
    {
        attractors.Remove(attractor);
    }

    public void AddRepulsor(Transform repulsor)
    {
        if (!repulsors.Contains(repulsor))
        {
            repulsors.Add(repulsor);
        }
    }

    public void RemoveRepulsor(Transform repulsor)
    {
        repulsors.Remove(repulsor);
    }

    // Helper method to visualize environmental influences in editor
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw ground plane
        if (considerEnvironment)
        {
            Gizmos.color = Color.green;
            float size = 50f;
            Vector3 groundPos = new Vector3(transform.position.x, groundHeight, transform.position.z);
            Gizmos.DrawWireCube(groundPos, new Vector3(size, 0.1f, size));
        }

        // Draw attractors
        Gizmos.color = Color.blue;
        foreach (var attractor in attractors)
        {
            if (attractor != null)
            {
                Gizmos.DrawWireSphere(attractor.position, 1f);
                Gizmos.DrawLine(attractor.position, attractor.position + Vector3.up * 2f);
            }
        }

        // Draw repulsors
        Gizmos.color = Color.red;
        foreach (var repulsor in repulsors)
        {
            if (repulsor != null)
            {
                Gizmos.DrawWireSphere(repulsor.position, 1f);
                Gizmos.DrawLine(repulsor.position + Vector3.left, repulsor.position + Vector3.right);
                Gizmos.DrawLine(repulsor.position + Vector3.forward, repulsor.position + Vector3.back);
            }
        }
    }
}