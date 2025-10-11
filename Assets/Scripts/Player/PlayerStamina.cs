using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using PlayerStats;

public class PlayerStamina : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerStaminaReference staminaReference;

    [Header("Stamina Settings")]
    [SerializeField] bool enableStamina = true;
    [SerializeField] bool debugStamina = false;

    [Header("UI Events")]
    public UnityEvent<float> OnStaminaChanged;
    public UnityEvent<float> OnStaminaPercentageChanged;
    public UnityEvent OnStaminaDepleted;
    public UnityEvent OnStaminaRestored;

    // Component references
    private PlayerStaminaData staminaData => staminaReference?.StaminaData;
    private PlayerMovement playerMovement;
    private PlayerStatsManager statsManager;

    // Stamina state
    private bool isRegenerating = false;
    private float timeSinceLastUse = 0f;
    private float lastStaminaPercentage = 1f;
    private Coroutine regenCoroutine;

    // Cached stat values
    private float cachedRegenRate;
    private float cachedMaxStamina;
    private float cachedSprintCost;

    private void Awake()
    {
        SetupReferences();

        if (staminaData != null)
        {
            staminaData.OnStaminaChanged += HandleStaminaChanged;
            staminaData.OnStaminaDepleted += HandleStaminaDepleted;
            staminaData.OnStaminaRestored += HandleStaminaRestored;
        }
    }

    private void Start()
    {
        InitializeStamina();
        UpdateCachedStats();
    }

    private void OnDestroy()
    {
        if (staminaData != null)
        {
            staminaData.OnStaminaChanged -= HandleStaminaChanged;
            staminaData.OnStaminaDepleted -= HandleStaminaDepleted;
            staminaData.OnStaminaRestored -= HandleStaminaRestored;
        }
    }

    private void Update()
    {
        if (!enableStamina || staminaData == null) return;

        // Check pause state
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused()) return;

        // Handle stamina consumption
        HandleStaminaConsumption();

        // Handle stamina regeneration
        HandleStaminaRegeneration();

        // Check for percentage changes
        CheckStaminaPercentageChange();
    }

    private void SetupReferences()
    {
        if (staminaReference == null)
        {
            staminaReference = GetComponent<PlayerStaminaReference>();
            if (staminaReference == null)
            {
                staminaReference = gameObject.AddComponent<PlayerStaminaReference>();
            }
        }

        playerMovement = GetComponent<PlayerMovement>();
        statsManager = GetComponent<PlayerStatsManager>();
    }

    private void InitializeStamina()
    {
        if (staminaData != null)
        {
            staminaData.ResetStamina();
            OnStaminaChanged?.Invoke(staminaData.CurrentStamina);
            OnStaminaPercentageChanged?.Invoke(1f);
        }
    }

    private void UpdateCachedStats()
    {
        if (statsManager != null)
        {
            cachedRegenRate = statsManager.GetStatValue(StatType.StaminaRegen);
            cachedMaxStamina = statsManager.GetStatValue(StatType.MaxStamina);
            cachedSprintCost = staminaData.GetSprintCostPerSecond();
        }
        else
        {
            cachedRegenRate = staminaData.GetRegenRate();
            cachedMaxStamina = staminaData.MaxStamina;
            cachedSprintCost = staminaData.GetSprintCostPerSecond();
        }
    }

    private void HandleStaminaConsumption()
    {
        bool consumedStamina = false;

        // Sprint consumption
        if (playerMovement != null && playerMovement.IsRunning())
        {
            float sprintCost = cachedSprintCost * Time.deltaTime;

            if (!staminaData.ConsumeStamina(sprintCost))
            {
                // Out of stamina - force stop sprinting
                playerMovement.StopRunning();

                if (debugStamina)
                {
                    Debug.Log("Out of stamina - stopping sprint");
                }
            }
            else
            {
                consumedStamina = true;
            }
        }

        // Track time since last use
        if (consumedStamina)
        {
            timeSinceLastUse = 0f;

            if (isRegenerating)
            {
                StopRegeneration();
            }
        }
        else
        {
            timeSinceLastUse += Time.deltaTime;
        }
    }

    private void HandleStaminaRegeneration()
    {
        // Check if we should start regenerating
        bool shouldRegen = staminaData.CurrentStamina < cachedMaxStamina &&
                          timeSinceLastUse >= staminaData.GetRegenDelay();

        // Modified: Only stop regen if sprinting (allow regen while walking/jumping)
        if (shouldRegen && playerMovement != null && playerMovement.IsRunning())
        {
            shouldRegen = false; // Can't regen while sprinting
        }

        if (shouldRegen && !isRegenerating)
        {
            StartRegeneration();
        }
        else if (!shouldRegen && isRegenerating)
        {
            StopRegeneration();
        }
    }


    private void StartRegeneration()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }

        isRegenerating = true;
        regenCoroutine = StartCoroutine(RegenerateStamina());
    }

    private void StopRegeneration()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }

        isRegenerating = false;
    }

    private IEnumerator RegenerateStamina()
    {
        while (isRegenerating && staminaData.CurrentStamina < cachedMaxStamina)
        {
            float regenAmount = cachedRegenRate * Time.deltaTime;
            staminaData.RestoreStamina(regenAmount);

            if (debugStamina)
            {
                Debug.Log($"Regenerating stamina: {staminaData.CurrentStamina:F1}/{cachedMaxStamina}");
            }

            yield return null;
        }

        isRegenerating = false;
    }

    private void CheckStaminaPercentageChange()
    {
        float currentPercentage = staminaData.GetStaminaPercentage();

        if (Mathf.Abs(currentPercentage - lastStaminaPercentage) > 0.001f)
        {
            OnStaminaPercentageChanged?.Invoke(currentPercentage);
            lastStaminaPercentage = currentPercentage;
        }
    }

    // Public methods
    public void ConsumeStaminaForJump()
    {
        if (!enableStamina || staminaData == null) return;

        float jumpCost = staminaData.GetJumpCost();
        staminaData.ConsumeStamina(jumpCost);
        timeSinceLastUse = 0f;
    }

    public bool HasEnoughStamina(float amount)
    {
        return staminaData != null && staminaData.CurrentStamina >= amount;
    }

    public bool CanSprint()
    {
        return staminaData != null && staminaData.CanSprint;
    }

    public void ModifyStamina(float amount)
    {
        if (staminaData == null) return;

        if (amount > 0)
        {
            staminaData.RestoreStamina(amount);
        }
        else
        {
            staminaData.ConsumeStamina(-amount);
        }
    }

    // Event handlers
    private void HandleStaminaChanged(float newStamina)
    {
        OnStaminaChanged?.Invoke(newStamina);
        UpdateCachedStats();
    }

    private void HandleStaminaDepleted()
    {
        OnStaminaDepleted?.Invoke();

        if (playerMovement != null)
        {
            playerMovement.StopRunning();
        }
    }

    private void HandleStaminaRestored()
    {
        OnStaminaRestored?.Invoke();
    }

    // Getters
    public float GetCurrentStamina() => staminaData?.CurrentStamina ?? 0f;
    public float GetMaxStamina() => staminaData?.MaxStamina ?? 0f;
    public float GetStaminaPercentage() => staminaData?.GetStaminaPercentage() ?? 0f;
    public bool IsRegenerating() => isRegenerating;
}