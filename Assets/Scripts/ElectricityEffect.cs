using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class LightningBranch
{
    public List<Vector3> points = new List<Vector3>();
    public float width;
    public int generation;
}

public class ElectricityEffect : MonoBehaviour
{
    public enum GrowthMethod
    {
        Simple,      // Original procedural method
        Laplacian,   // Physics-based method
        Hybrid       // Laplacian main path with procedural branches
    }

    [Header("Growth Method")]
    public GrowthMethod growthMethod = GrowthMethod.Hybrid;

    [Header("Components")]
    private LineRenderer mainBoltRenderer;
    private List<LineRenderer> branchRenderers = new List<LineRenderer>();
    private Light mainLight;
    private List<Light> branchLights = new List<Light>();

    [Header("Lightning Settings")]
    public int segmentCount = 50;
    public float branchProbability = 0.3f;
    public int maxBranches = 10;
    public int maxGenerations = 3;
    public float branchAngleRange = 45f;
    public float widthDecayPerGeneration = 0.6f;
    public float noiseFrequency = 10f;
    public float noiseAmplitude = 0.3f;

    [Header("Laplacian Settings")]
    public float eta = 6.3f;
    public float gridCellSize = 0.1f;
    public bool considerEnvironment = true;
    public int maxLaplacianSites = 500;

    [Header("Visual Settings")]
    public AnimationCurve intensityCurve;
    public float glowIntensity = 2f;
    public float pulseSpeed = 3f;

    [Header("Light Settings")]
    public float lightIntensity = 5f;
    public float lightRange = 20f;
    public float lightBranchIntensityMultiplier = 0.5f;
    public bool enableShadows = false;

    [Header("Audio Settings")]
    public AudioClip thunderClip;
    public AudioClip electricityLoop;
    public float thunderVolume = 1f;
    public float electricityVolume = 0.5f;
    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;
    public float soundDelay = 0f;
    public float thunderMaxDistance = 30f;
    public float electricityMaxDistance = 20f;

    private AudioSource thunderSource;
    private AudioSource electricitySource;

    private Transform startPoint;
    private Transform endPoint;
    private TeslaCoilManager manager;
    private float timeAlive;
    private List<LightningBranch> branches = new List<LightningBranch>();
    private Material lightningMaterial;
    private LaplacianLightningEffect laplacianGenerator;

    void Awake()
    {
        mainBoltRenderer = GetComponent<LineRenderer>();
        if (mainBoltRenderer == null)
        {
            mainBoltRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lightningMaterial = mainBoltRenderer.material;

        // Create main light
        mainLight = GetComponent<Light>();
        if (mainLight == null)
        {
            mainLight = gameObject.AddComponent<Light>();
        }

        // Configure light
        mainLight.type = LightType.Point;
        mainLight.color = new Color(0.5f, 0.8f, 1.0f);
        mainLight.intensity = 0;
        mainLight.range = lightRange;
        mainLight.shadows = enableShadows ? LightShadows.Soft : LightShadows.None;

        // Create audio sources
        SetupAudioSources();

        // Add Laplacian generator if using physics-based growth
        if (growthMethod != GrowthMethod.Simple)
        {
            laplacianGenerator = GetComponent<LaplacianLightningEffect>();
            if (laplacianGenerator == null)
            {
                laplacianGenerator = gameObject.AddComponent<LaplacianLightningEffect>();
            }
        }
    }

    void SetupAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            thunderSource = sources[0];
            electricitySource = sources[1];
        }
        else
        {
            // Create thunder source
            thunderSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(thunderSource, thunderMaxDistance, false);

            // Create electricity loop source
            electricitySource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(electricitySource, electricityMaxDistance, true);
        }
    }

    void ConfigureAudioSource(AudioSource source, float maxDistance, bool loop)
    {
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Custom;
        source.maxDistance = maxDistance;
        source.minDistance = loop ? 2f : 3f;
        source.dopplerLevel = 0f;
        source.loop = loop;

        // Custom rolloff curve
        AnimationCurve rolloff = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.1f, loop ? 0.9f : 0.8f),
            new Keyframe(0.5f, loop ? 0.3f : 0.5f),
            new Keyframe(1f, 0f)
        );
        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloff);
    }

    public void Initialize(Transform start, Transform end, TeslaCoilManager mgr)
    {
        startPoint = start;
        endPoint = end;
        manager = mgr;
        timeAlive = 0;

        // Get the material from the manager or use existing
        if (manager.electricityMaterial != null)
        {
            lightningMaterial = manager.electricityMaterial;
            mainBoltRenderer.material = lightningMaterial;
        }
        else if (mainBoltRenderer.material != null)
        {
            lightningMaterial = mainBoltRenderer.material;
        }

        // Configure main bolt renderer
        mainBoltRenderer.textureMode = LineTextureMode.Stretch;
        mainBoltRenderer.alignment = LineAlignment.View;

        // Generate lightning pattern
        GenerateLightning();

        // Configure and play audio
        ConfigureAudio();

        StartCoroutine(AnimateLightning());
    }

    void GenerateLightning()
    {
        branches.Clear();

        switch (growthMethod)
        {
            case GrowthMethod.Simple:
                GenerateSimpleLightning();
                break;
            case GrowthMethod.Laplacian:
                GenerateLaplacianLightning();
                break;
            case GrowthMethod.Hybrid:
                GenerateHybridLightning();
                break;
        }

        UpdateRenderers();
    }

    void GenerateSimpleLightning()
    {
        // Create main branch using original method
        LightningBranch mainBranch = new LightningBranch();
        mainBranch.generation = 0;
        mainBranch.width = manager.maxLineWidth;

        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        mainBranch.points.Add(start);

        for (int i = 1; i < segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 targetPoint = Vector3.Lerp(start, end, t);

            // Add controlled randomness
            float noise = Mathf.PerlinNoise(t * noiseFrequency, timeAlive * pulseSpeed);
            Vector3 offset = Random.insideUnitSphere * (noise * noiseAmplitude * distance);
            offset = Vector3.ProjectOnPlane(offset, direction);

            float biasStrength = Mathf.Pow(1f - Mathf.Abs(t - 0.5f) * 2f, 2f);
            Vector3 point = targetPoint + offset * biasStrength;

            mainBranch.points.Add(point);

            // Chance to create branch
            if (Random.value < branchProbability && branches.Count < maxBranches)
            {
                CreateBranch(point, direction, mainBranch.generation + 1, mainBranch.width * widthDecayPerGeneration);
            }
        }

        mainBranch.points.Add(end);
        branches.Add(mainBranch);
    }

    void GenerateLaplacianLightning()
    {
        if (laplacianGenerator == null) return;

        // Configure Laplacian settings
        ConfigureLaplacianGenerator();

        // Generate main path using Laplacian growth
        List<Vector3> mainPath = laplacianGenerator.GenerateLightning(startPoint.position, endPoint.position);

        // Create main branch from Laplacian path
        LightningBranch mainBranch = new LightningBranch();
        mainBranch.generation = 0;
        mainBranch.width = manager.maxLineWidth;
        mainBranch.points = mainPath;
        branches.Add(mainBranch);

        // Add some procedural branches for detail
        AddProceduralBranches(mainBranch);
    }

    void GenerateHybridLightning()
    {
        if (laplacianGenerator == null)
        {
            GenerateSimpleLightning();
            return;
        }

        // Configure Laplacian settings
        ConfigureLaplacianGenerator();

        // Generate main path using Laplacian growth
        List<Vector3> mainPath = laplacianGenerator.GenerateLightning(startPoint.position, endPoint.position);

        // Enhance path with procedural noise
        List<Vector3> enhancedPath = EnhancePathWithNoise(mainPath);

        // Create main branch
        LightningBranch mainBranch = new LightningBranch();
        mainBranch.generation = 0;
        mainBranch.width = manager.maxLineWidth;
        mainBranch.points = enhancedPath;
        branches.Add(mainBranch);

        // Add detailed procedural branches
        AddDetailedProceduralBranches(mainBranch);
    }

    void ConfigureLaplacianGenerator()
    {
        laplacianGenerator.eta = eta;
        laplacianGenerator.gridCellSize = gridCellSize;
        laplacianGenerator.useGroundPlane = considerEnvironment;
        laplacianGenerator.maxGrowthSites = maxLaplacianSites;

        // Set environmental influences
        if (manager != null)
        {
            laplacianGenerator.groundHeight = manager.groundHeight;
            laplacianGenerator.groundChargeStrength = manager.groundChargeStrength;

            // Add tesla coils as repulsors (except start and end)
            laplacianGenerator.ClearInfluences();
            foreach (var coil in manager.teslaCoils)
            {
                if (coil != startPoint && coil != endPoint)
                {
                    laplacianGenerator.AddRepulsor(coil);
                }
            }
        }
    }

    List<Vector3> EnhancePathWithNoise(List<Vector3> originalPath)
    {
        if (originalPath.Count < 3) return originalPath;

        List<Vector3> enhanced = new List<Vector3>();
        enhanced.Add(originalPath[0]);

        for (int i = 1; i < originalPath.Count - 1; i++)
        {
            Vector3 point = originalPath[i];
            Vector3 direction = (originalPath[i + 1] - originalPath[i - 1]).normalized;

            // Add small perpendicular noise
            float noise = Mathf.PerlinNoise(i * 0.1f, timeAlive);
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
            if (perpendicular.magnitude < 0.1f)
                perpendicular = Vector3.Cross(direction, Vector3.right).normalized;

            point += perpendicular * noise * noiseAmplitude * 0.3f;
            enhanced.Add(point);
        }

        enhanced.Add(originalPath[originalPath.Count - 1]);
        return enhanced;
    }

    void AddProceduralBranches(LightningBranch mainBranch)
    {
        for (int i = 1; i < mainBranch.points.Count - 1; i++)
        {
            if (Random.value < branchProbability * 0.5f && branches.Count < maxBranches)
            {
                Vector3 branchStart = mainBranch.points[i];
                Vector3 direction = GetDirectionAtPoint(mainBranch.points, i);
                CreateBranch(branchStart, direction, 1, mainBranch.width * widthDecayPerGeneration);
            }
        }
    }

    void AddDetailedProceduralBranches(LightningBranch mainBranch)
    {
        int branchCount = 0;
        float adaptiveBranchProbability = branchProbability;

        for (int i = 1; i < mainBranch.points.Count - 1; i++)
        {
            float progress = i / (float)mainBranch.points.Count;
            // More branches in the middle, fewer at ends
            float probabilityModifier = Mathf.Sin(progress * Mathf.PI);

            if (Random.value < adaptiveBranchProbability * probabilityModifier && branchCount < maxBranches)
            {
                Vector3 branchStart = mainBranch.points[i];
                Vector3 direction = GetDirectionAtPoint(mainBranch.points, i);

                // Vary branch characteristics based on position
                float branchScale = Random.Range(0.5f, 1f) * (1f - progress * 0.5f);
                CreateDetailedBranch(branchStart, direction, 1, mainBranch.width * widthDecayPerGeneration, branchScale);
                branchCount++;
            }
        }
    }

    Vector3 GetDirectionAtPoint(List<Vector3> points, int index)
    {
        if (index >= points.Count - 1)
            return (points[index] - points[index - 1]).normalized;
        else if (index <= 0)
            return (points[1] - points[0]).normalized;
        else
            return (points[index + 1] - points[index - 1]).normalized;
    }

    void CreateBranch(Vector3 startPos, Vector3 parentDirection, int generation, float width)
    {
        if (generation >= maxGenerations) return;

        LightningBranch branch = new LightningBranch();
        branch.generation = generation;
        branch.width = width;
        branch.points.Add(startPos);

        Vector3 branchDirection = Quaternion.Euler(
            Random.Range(-branchAngleRange, branchAngleRange),
            Random.Range(-branchAngleRange, branchAngleRange),
            0
        ) * parentDirection;

        float branchLength = Random.Range(0.1f, 0.3f) * Vector3.Distance(startPoint.position, endPoint.position);
        int branchSegments = Mathf.Max(3, segmentCount / (generation + 2));

        for (int i = 1; i <= branchSegments; i++)
        {
            float t = i / (float)branchSegments;
            Vector3 point = startPos + branchDirection * (t * branchLength);

            float noise = Mathf.PerlinNoise(t * noiseFrequency * 2f, generation);
            point += Random.insideUnitSphere * (noise * noiseAmplitude * 0.5f);

            branch.points.Add(point);

            if (Random.value < branchProbability * 0.5f && branches.Count < maxBranches)
            {
                CreateBranch(point, branchDirection, generation + 1, width * widthDecayPerGeneration);
            }
        }

        branches.Add(branch);
    }

    void CreateDetailedBranch(Vector3 startPos, Vector3 parentDirection, int generation, float width, float scale)
    {
        if (generation >= maxGenerations) return;

        LightningBranch branch = new LightningBranch();
        branch.generation = generation;
        branch.width = width;
        branch.points.Add(startPos);

        // More varied branch angles for detailed branches
        float angleVariation = branchAngleRange * (1f + generation * 0.2f);
        Vector3 branchDirection = Quaternion.Euler(
            Random.Range(-angleVariation, angleVariation),
            Random.Range(-angleVariation, angleVariation),
            Random.Range(-angleVariation * 0.5f, angleVariation * 0.5f)
        ) * parentDirection;

        float branchLength = Random.Range(0.1f, 0.4f) * scale * Vector3.Distance(startPoint.position, endPoint.position);
        int branchSegments = Mathf.Max(5, segmentCount / (generation + 1));

        for (int i = 1; i <= branchSegments; i++)
        {
            float t = i / (float)branchSegments;

            // Add curve to branch
            float curve = Mathf.Sin(t * Mathf.PI * 0.5f);
            Vector3 point = startPos + branchDirection * (t * branchLength * curve);

            // Multi-octave noise for more detail
            float noise1 = Mathf.PerlinNoise(t * noiseFrequency * 2f, generation);
            float noise2 = Mathf.PerlinNoise(t * noiseFrequency * 8f, generation + 100f) * 0.3f;
            float combinedNoise = noise1 + noise2;

            point += Random.insideUnitSphere * (combinedNoise * noiseAmplitude * 0.5f * scale);

            branch.points.Add(point);

            // Reduced sub-branching for performance
            if (Random.value < branchProbability * 0.3f && branches.Count < maxBranches && generation < maxGenerations - 1)
            {
                CreateDetailedBranch(point, branchDirection, generation + 1, width * widthDecayPerGeneration, scale * 0.7f);
            }
        }

        branches.Add(branch);
    }

    void UpdateRenderers()
    {
        if (branches.Count > 0)
        {
            var mainBranch = branches[0];
            mainBoltRenderer.positionCount = mainBranch.points.Count;
            mainBoltRenderer.SetPositions(mainBranch.points.ToArray());

            if (mainBranch.points.Count > 1)
            {
                int midPoint = mainBranch.points.Count / 2;
                mainLight.transform.position = mainBranch.points[midPoint];
            }
        }

        while (branchRenderers.Count < branches.Count - 1)
        {
            GameObject branchObj = new GameObject("LightningBranch");
            branchObj.transform.SetParent(transform);

            LineRenderer renderer = branchObj.AddComponent<LineRenderer>();
            renderer.material = lightningMaterial;
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.alignment = LineAlignment.View;
            branchRenderers.Add(renderer);

            Light branchLight = branchObj.AddComponent<Light>();
            branchLight.type = LightType.Point;
            branchLight.color = mainLight.color;
            branchLight.intensity = 0;
            branchLight.range = lightRange * 0.5f;
            branchLight.shadows = LightShadows.None;
            branchLights.Add(branchLight);
        }

        for (int i = 1; i < branches.Count; i++)
        {
            var branch = branches[i];
            var renderer = branchRenderers[i - 1];
            var light = branchLights[i - 1];

            renderer.gameObject.SetActive(true);
            renderer.positionCount = branch.points.Count;
            renderer.SetPositions(branch.points.ToArray());

            if (branch.points.Count > 1)
            {
                int midPoint = branch.points.Count / 2;
                light.transform.position = branch.points[midPoint];
            }
        }

        for (int i = branches.Count - 1; i < branchRenderers.Count; i++)
        {
            branchRenderers[i].gameObject.SetActive(false);
        }
    }

    void ConfigureAudio()
    {
        if (thunderClip == null && manager.thunderClip != null)
            thunderClip = manager.thunderClip;
        if (electricityLoop == null && manager.electricityLoop != null)
            electricityLoop = manager.electricityLoop;

        float distance = Vector3.Distance(startPoint.position, endPoint.position);
        float normalizedDistance = Mathf.Clamp01(distance / 50f);

        if (thunderSource != null && thunderClip != null)
        {
            thunderSource.clip = thunderClip;
            thunderSource.volume = thunderVolume * manager.soundVolume;

            float basePitch = Mathf.Lerp(1.1f, 0.9f, normalizedDistance);
            float randomVariation = Random.Range(-0.15f, 0.15f);
            thunderSource.pitch = Mathf.Clamp(basePitch + randomVariation, minPitch, maxPitch);

            thunderSource.maxDistance = thunderMaxDistance;

            if (branches.Count > 0 && branches[0].points.Count > 0)
            {
                int midPoint = branches[0].points.Count / 2;
                thunderSource.transform.position = branches[0].points[midPoint];
            }

            if (soundDelay > 0)
                StartCoroutine(PlayDelayedSound(thunderSource, soundDelay));
            else
                thunderSource.Play();
        }

        if (electricitySource != null && electricityLoop != null)
        {
            electricitySource.clip = electricityLoop;
            electricitySource.volume = electricityVolume * manager.soundVolume;

            float electricityPitch = Mathf.Lerp(1.3f, 0.85f, normalizedDistance);
            electricityPitch += Random.Range(-0.1f, 0.1f);
            electricitySource.pitch = Mathf.Clamp(electricityPitch, minPitch * 1.1f, maxPitch * 1.1f);

            electricitySource.maxDistance = electricityMaxDistance;
            electricitySource.transform.position = startPoint.position;
            electricitySource.Play();
        }
    }

    IEnumerator PlayDelayedSound(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (source != null && source.clip != null)
            source.Play();
    }

    IEnumerator AnimateLightning()
    {
        while (timeAlive < manager.electricityDuration)
        {
            timeAlive += Time.deltaTime;
            float normalizedTime = timeAlive / manager.electricityDuration;

            float intensity = manager.electricityIntensityCurve.Evaluate(normalizedTime);

            float pulse = 1f + Mathf.Sin(timeAlive * pulseSpeed * Mathf.PI * 2f) * 0.2f;
            intensity *= pulse;

            lightningMaterial.SetFloat("_Intensity", intensity * glowIntensity);
            lightningMaterial.SetFloat("_EdgeGlow", intensity);
            lightningMaterial.SetFloat("_CenterBrightness", intensity * 1.5f);

            float mainWidth = manager.maxLineWidth * intensity;
            mainBoltRenderer.startWidth = mainWidth;
            mainBoltRenderer.endWidth = mainWidth * 0.3f;

            Color glowColor = manager.electricityColor * intensity * glowIntensity;
            mainBoltRenderer.startColor = glowColor;
            mainBoltRenderer.endColor = glowColor;

            mainLight.intensity = intensity * lightIntensity;
            mainLight.color = manager.electricityColor;

            if (Random.value < 0.1f)
            {
                mainLight.intensity *= Random.Range(0.8f, 1.5f);
            }

            if (electricitySource != null && electricitySource.isPlaying)
            {
                electricitySource.volume = electricityVolume * intensity * manager.soundVolume;

                if (branches.Count > 0 && branches[0].points.Count > 1)
                {
                    int midPoint = branches[0].points.Count / 2;
                    electricitySource.transform.position = branches[0].points[midPoint];
                }
            }

            for (int i = 0; i < branchRenderers.Count && i < branches.Count - 1; i++)
            {
                var branch = branches[i + 1];
                var renderer = branchRenderers[i];
                var light = branchLights[i];

                float branchWidth = mainWidth * Mathf.Pow(widthDecayPerGeneration, branch.generation);
                renderer.startWidth = branchWidth;
                renderer.endWidth = branchWidth * 0.3f;
                renderer.startColor = glowColor;
                renderer.endColor = glowColor;

                light.intensity = intensity * lightIntensity * lightBranchIntensityMultiplier *
                                 Mathf.Pow(0.7f, branch.generation);
                light.color = manager.electricityColor;
            }

            // Only regenerate for simple method
            if (growthMethod == GrowthMethod.Simple && Random.value < 0.3f)
            {
                RegenerateLightning();
            }

            yield return null;
        }

        DisableVisuals();
    }

    void RegenerateLightning()
    {
        for (int b = 0; b < branches.Count; b++)
        {
            var branch = branches[b];
            Vector3 start = branch.points[0];
            Vector3 end = branch.points[branch.points.Count - 1];

            for (int i = 1; i < branch.points.Count - 1; i++)
            {
                float t = i / (float)(branch.points.Count - 1);
                Vector3 targetPoint = Vector3.Lerp(start, end, t);

                float noise = Mathf.PerlinNoise(t * noiseFrequency, timeAlive * pulseSpeed);
                Vector3 offset = Random.insideUnitSphere * (noise * noiseAmplitude * 0.5f);

                branch.points[i] = targetPoint + offset;
            }
        }

        UpdateRenderers();
    }

    void DisableVisuals()
    {
        mainBoltRenderer.enabled = false;
        mainLight.intensity = 0;

        foreach (var renderer in branchRenderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }

        foreach (var light in branchLights)
        {
            if (light != null)
                light.intensity = 0;
        }

        if (electricitySource != null && electricitySource.isPlaying)
        {
            electricitySource.Stop();
        }
    }

    public bool IsAudioPlaying()
    {
        return thunderSource != null && thunderSource.isPlaying;
    }

    void StopAudio()
    {
        if (electricitySource != null && electricitySource.isPlaying)
        {
            electricitySource.Stop();
        }

        if (thunderSource != null && thunderSource.isPlaying)
        {
            thunderSource.Stop();
        }
    }

    void OnDisable()
    {
        foreach (var renderer in branchRenderers)
        {
            if (renderer != null)
            {
                renderer.gameObject.SetActive(false);
            }
        }

        StopAudio();
    }

    public void PrepareForReuse()
    {
        mainBoltRenderer.enabled = true;

        foreach (var renderer in branchRenderers)
        {
            if (renderer != null)
                renderer.enabled = true;
        }

        timeAlive = 0;
        branches.Clear();
    }
}