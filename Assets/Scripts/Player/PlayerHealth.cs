using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using Cinemachine;
using PlayerStats;

public class PlayerHealth : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerHealthReference healthReference;

    [Header("Health Settings")]
    [SerializeField] bool enableHealthSystem = true;
    [SerializeField] bool useStatBasedRegen = true;

    [Header("Base Regeneration Settings")]
    [SerializeField] float baseRegenDelay = 3f;
    [SerializeField] float baseRegenCooldown = 5f;
    [SerializeField] float baseRegenRate = 10f;

    [Header("Damage Feedback")]
    [SerializeField] bool enableDamageEffects = true;
    [SerializeField] AudioClip[] damageSounds;
    [SerializeField] AudioClip deathSound;
    [SerializeField] GameObject bloodSplatterPrefab;
    [SerializeField] [Range(0f, 1f)] float damageVolume = 0.8f;

    [Header("Camera Shake")]
    [SerializeField] bool enableCameraShake = true;
    [SerializeField] float baseShakeIntensity = 2.0f;
    [SerializeField] float maxShakeIntensity = 5.0f;
    [SerializeField] float shakeDuration = 0.3f;

    [Header("Death")]
    [SerializeField] string menuSceneName = "Menu";
    [SerializeField] float deathDelay = 0f;

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<float> OnDamageTaken;
    public UnityEvent<float> OnHealed;
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent<float> OnHealthPercentageChanged;
    public UnityEvent OnRegenStarted;
    public UnityEvent OnRegenStopped;

    [Header("Debug")]
    [SerializeField] bool showDebugInfo = false;

    // Component references
    private PlayerHealthData healthData => healthReference?.HealthData;
    private PlayerStatsManager statsManager;
    private AudioSource audioSource;

    // Camera shake
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineBasicMultiChannelPerlin cinemachineNoise;

    // Health tracking - FIXED: Track previous health to detect damage
    private float lastHealthPercentage = 1f;
    private float previousHealth = 100f;
    private float currentMaxHealth;
    private bool isDead = false;

    // Regeneration state
    private bool isWaitingToRegen = false;
    private float regenWaitStartTime = 0f;
    private float regenWaitDuration = 0f;
    private bool isRegenerating = false;
    private string regenWaitReason = ""; // For debugging

    // Stat-cached values
    private float cachedRegenRate;
    private float cachedRegenDelay;
    private float cachedRegenCooldown;
    private float cachedDamageResistance;
    private float cachedIncomingDamageMultiplier;

    private void Awake()
    {
        SetupComponents();
        SetupHealthReference();
        SetupCameraShake();
    }

    private void Start()
    {
        statsManager = GetComponent<PlayerStatsManager>();
        InitializeHealth();
        UpdateCachedStats();

        // Subscribe to stat changes
        if (statsManager != null)
        {
            statsManager.OnStatChanged.AddListener(OnStatChanged);
        }
    }

    private void OnDestroy()
    {
        // Cleanup
        if (healthData != null)
        {
            healthData.OnHealthChanged -= HandleHealthDataChanged;
            healthData.OnDeath -= HandleHealthDataDeath;
        }

        if (statsManager != null)
        {
            statsManager.OnStatChanged.RemoveListener(OnStatChanged);
        }
    }

    private void Update()
    {
        if (!enableHealthSystem || isDead) return;

        // Check pause state
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused()) return;

        // Handle regeneration
        HandleRegeneration();

        // Update health percentage
        CheckHealthPercentageChange();
    }

    private void SetupComponents()
    {
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = damageVolume;
    }

    private void SetupHealthReference()
    {
        if (healthReference == null)
        {
            healthReference = GetComponent<PlayerHealthReference>();
            if (healthReference == null)
            {
                healthReference = gameObject.AddComponent<PlayerHealthReference>();
            }
        }
    }

    private void SetupCameraShake()
    {
        virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        if (virtualCamera != null)
        {
            cinemachineNoise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (cinemachineNoise == null)
            {
                cinemachineNoise = virtualCamera.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            }
            cinemachineNoise.m_AmplitudeGain = 0f;
            cinemachineNoise.m_FrequencyGain = 1f;
        }
    }

    private void InitializeHealth()
    {
        if (healthData == null) return;

        // Subscribe to health data events
        healthData.OnHealthChanged += HandleHealthDataChanged;
        healthData.OnDeath += HandleHealthDataDeath;

        // Get initial max health from stats if available
        UpdateCachedStats();

        if (statsManager != null)
        {
            currentMaxHealth = statsManager.GetStatValue(StatType.MaxHealth);
            healthData.SetMaxHealth(currentMaxHealth);
        }
        else
        {
            currentMaxHealth = healthData.MaxHealth;
        }

        // Reset to full health and initialize tracking
        healthData.ResetHealth();
        previousHealth = healthData.PlayerHealth; // Initialize tracking
        UpdateHealthEvents();

        if (showDebugInfo)
        {
            Debug.Log($"Health initialized: {healthData.PlayerHealth}/{currentMaxHealth}");
        }
    }

    private void UpdateCachedStats()
    {
        if (statsManager != null)
        {
            cachedRegenRate = statsManager.GetStatValue(StatType.HealthRegen);
            cachedRegenDelay = statsManager.GetStatValue(StatType.HealthRegenDelay);
            cachedRegenCooldown = statsManager.GetStatValue(StatType.HealthRegenCooldown);
            cachedDamageResistance = statsManager.GetStatValue(StatType.DamageResistance);
            cachedIncomingDamageMultiplier = statsManager.GetStatValue(StatType.IncomingDamageMultiplier);
        }
        else
        {
            cachedRegenRate = baseRegenRate;
            cachedRegenDelay = baseRegenDelay;
            cachedRegenCooldown = baseRegenCooldown;
            cachedDamageResistance = 0f;
            cachedIncomingDamageMultiplier = 1f;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Updated cached stats - Regen Rate: {cachedRegenRate}, Delay: {cachedRegenDelay}, Cooldown: {cachedRegenCooldown}");
        }
    }

    private void OnStatChanged(StatType statType, float oldValue, float newValue)
    {
        switch (statType)
        {
            case StatType.MaxHealth:
                HandleMaxHealthChanged(oldValue, newValue);
                break;

            case StatType.HealthRegen:
            case StatType.HealthRegenDelay:
            case StatType.HealthRegenCooldown:
            case StatType.DamageResistance:
            case StatType.IncomingDamageMultiplier:
                UpdateCachedStats();
                break;
        }
    }

    private void HandleMaxHealthChanged(float oldMax, float newMax)
    {
        if (healthData == null) return;

        // Calculate health percentage before change
        float healthPercent = healthData.GetHealthPercentage();

        // Update max health
        currentMaxHealth = newMax;
        healthData.SetMaxHealth(newMax);

        // Scale current health to maintain percentage
        float newHealth = newMax * healthPercent;
        healthData.SetPlayerHealth(newHealth);

        // Update our tracking
        previousHealth = newHealth;

        // Only start delay if not at full health and not already waiting from damage
        if (healthData.PlayerHealth < currentMaxHealth && !isWaitingToRegen)
        {
            StartRegenWait(cachedRegenDelay, "max health change");
        }

        if (showDebugInfo)
        {
            Debug.Log($"Max health changed: {oldMax} -> {newMax}, Current: {newHealth}");
        }
    }

    // FIXED: Observer pattern - detect damage from health changes
    private void HandleHealthDataChanged(float newHealth)
    {
        // Compare with previous health to detect what happened
        float healthDifference = newHealth - previousHealth;

        if (healthDifference < 0)
        {
            // Health decreased = damage taken
            float damageAmount = -healthDifference;

            if (showDebugInfo)
            {
                Debug.Log($"Damage detected: {damageAmount:F1}. Health: {previousHealth:F1} -> {newHealth:F1}");
            }

            // Start damage cooldown
            StartRegenWait(cachedRegenCooldown, "damage taken");

            // Trigger damage events
            OnDamageTaken?.Invoke(damageAmount);

            if (enableDamageEffects)
            {
                PlayDamageEffects(damageAmount);
            }
        }
        else if (healthDifference > 0)
        {
            // Health increased = healing
            float healAmount = healthDifference;

            if (showDebugInfo)
            {
                Debug.Log($"Healing detected: {healAmount:F1}. Health: {previousHealth:F1} -> {newHealth:F1}");
            }

            OnHealed?.Invoke(healAmount);
            // Healing doesn't interrupt regeneration timers
        }

        // Update tracking
        previousHealth = newHealth;

        // Update health events
        UpdateHealthEvents();
    }

    private void HandleRegeneration()
    {
        if (!useStatBasedRegen || healthData == null || healthData.IsDead) return;

        // Check if at full health
        if (healthData.PlayerHealth >= currentMaxHealth)
        {
            if (isRegenerating)
            {
                StopRegeneration();
            }
            return;
        }

        // Handle waiting period
        if (isWaitingToRegen)
        {
            float timeSinceWaitStart = Time.time - regenWaitStartTime;

            if (timeSinceWaitStart >= regenWaitDuration)
            {
                // Wait is over, start regenerating
                isWaitingToRegen = false;
                StartRegeneration();

                if (showDebugInfo)
                {
                    Debug.Log($"Wait complete ({regenWaitReason}): {timeSinceWaitStart:F1}s. Starting regeneration.");
                }
            }
            else if (showDebugInfo)
            {
                float timeLeft = regenWaitDuration - timeSinceWaitStart;
                Debug.Log($"Waiting ({regenWaitReason}): {timeLeft:F1}s remaining");
            }
            return;
        }

        // Apply regeneration if active
        if (isRegenerating)
        {
            float regenAmount = cachedRegenRate * Time.deltaTime;
            healthData.Heal(regenAmount);

            // Check if we reached full health
            if (healthData.PlayerHealth >= currentMaxHealth)
            {
                StopRegeneration();
            }

            if (showDebugInfo)
            {
                Debug.Log($"Regenerating: {healthData.PlayerHealth:F1}/{currentMaxHealth} (+{regenAmount:F2}/s)");
            }
        }
    }

    private void StartRegenWait(float duration, string reason)
    {
        isWaitingToRegen = true;
        regenWaitStartTime = Time.time;
        regenWaitDuration = duration;
        regenWaitReason = reason;

        // Stop current regeneration
        if (isRegenerating)
        {
            StopRegeneration();
        }

        if (showDebugInfo)
        {
            Debug.Log($"Starting regen wait: {duration}s ({reason})");
        }
    }

    private void StartRegeneration()
    {
        isRegenerating = true;
        OnRegenStarted?.Invoke();

        if (showDebugInfo)
        {
            Debug.Log("Health regeneration started");
        }
    }

    private void StopRegeneration()
    {
        if (isRegenerating)
        {
            isRegenerating = false;
            OnRegenStopped?.Invoke();

            if (showDebugInfo)
            {
                Debug.Log("Health regeneration stopped");
            }
        }
    }

    // REMOVED: No longer needed since we observe health changes
    // public void TakeDamage() - This breaks the observer pattern

    // Manual healing method (for health packs, etc.)
    public void Heal(float healAmount, bool triggerEvent = true)
    {
        if (!enableHealthSystem || healthData == null || healthData.IsDead) return;

        healthData.Heal(healAmount);
        // The healing will be detected automatically in HandleHealthDataChanged
    }

    private void PlayDamageEffects(float damageAmount)
    {
        // Play damage sound
        if (audioSource != null && damageSounds != null && damageSounds.Length > 0)
        {
            AudioClip randomSound = damageSounds[Random.Range(0, damageSounds.Length)];
            if (randomSound != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(randomSound, damageVolume);
            }
        }

        // Blood splatter
        if (bloodSplatterPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f;
            Instantiate(bloodSplatterPrefab, spawnPos, Quaternion.identity);
        }

        // Camera shake
        if (enableCameraShake)
        {
            TriggerCameraShake(damageAmount);
        }
    }

    private void TriggerCameraShake(float damageAmount)
    {
        if (cinemachineNoise == null) return;

        float healthPercentage = healthData.GetHealthPercentage();
        float damagePercentage = damageAmount / currentMaxHealth;

        // Scale shake based on damage and current health
        float intensityMultiplier = (1f - healthPercentage * 0.3f) + (damagePercentage * 3f);
        float shakeIntensity = Mathf.Clamp(
            baseShakeIntensity * intensityMultiplier,
            baseShakeIntensity,
            maxShakeIntensity
        );

        StartCoroutine(CameraShakeCoroutine(shakeIntensity));
    }

    private IEnumerator CameraShakeCoroutine(float intensity)
    {
        if (cinemachineNoise == null) yield break;

        cinemachineNoise.m_AmplitudeGain = intensity;
        cinemachineNoise.m_FrequencyGain = 2f;

        yield return new WaitForSeconds(shakeDuration);

        // Fade out shake
        float fadeTime = 0.2f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            cinemachineNoise.m_AmplitudeGain = Mathf.Lerp(intensity, 0f, t);
            yield return null;
        }

        cinemachineNoise.m_AmplitudeGain = 0f;
    }

    private void HandleHealthDataDeath()
    {
        if (isDead) return;

        isDead = true;

        if (showDebugInfo)
        {
            Debug.Log("Player died!");
        }

        // Stop regeneration
        if (isRegenerating)
        {
            StopRegeneration();
        }

        // Stop waiting
        isWaitingToRegen = false;

        // Reset round data
        RoundManager roundManager = FindObjectOfType<RoundManager>();
        if (roundManager != null && roundManager.roundData != null)
        {
            roundManager.roundData.ResetRoundData();
        }

        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        OnDeath?.Invoke();
        StartCoroutine(LoadMenuAfterDelay(deathDelay));
    }

    private IEnumerator LoadMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(menuSceneName);
    }

    private void CheckHealthPercentageChange()
    {
        if (healthData == null) return;

        float currentPercentage = healthData.GetHealthPercentage();
        if (Mathf.Abs(currentPercentage - lastHealthPercentage) > 0.001f)
        {
            OnHealthPercentageChanged?.Invoke(currentPercentage);
            lastHealthPercentage = currentPercentage;
        }
    }

    private void UpdateHealthEvents()
    {
        if (healthData != null)
        {
            OnHealthChanged?.Invoke(healthData.PlayerHealth);
            OnHealthPercentageChanged?.Invoke(healthData.GetHealthPercentage());
        }
    }

    // Public getters
    public float GetCurrentHealth() => healthData?.PlayerHealth ?? 0f;
    public float GetMaxHealth() => currentMaxHealth;
    public float GetHealthPercentage() => healthData?.GetHealthPercentage() ?? 0f;
    public bool IsAlive => !isDead && healthData != null && !healthData.IsDead;
    public bool IsDead => isDead;
    public bool IsRegenerating => isRegenerating;
    public bool IsWaitingToRegen => isWaitingToRegen;
}