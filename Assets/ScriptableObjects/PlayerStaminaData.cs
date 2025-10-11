using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerStaminaData", menuName = "Game Data/Player Stamina Data")]
public class PlayerStaminaData : ScriptableObject
{
    [Header("Stamina Settings")]
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float currentStamina = 100f;
    [SerializeField] bool canSprint = true;

    [Header("Consumption Settings")]
    [SerializeField] float sprintCostPerSecond = 20f;
    [SerializeField] float jumpCost = 10f;
    [SerializeField] float minimumStaminaToSprint = 10f;

    [Header("Regeneration Settings")]
    [SerializeField] float regenRate = 20f;
    [SerializeField] float regenDelay = 1f;
    [SerializeField] bool regenWhileMoving = false;

    // Properties
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool CanSprint => canSprint && currentStamina >= minimumStaminaToSprint;
    public float StaminaPercentage => maxStamina > 0 ? currentStamina / maxStamina : 0f;

    // Events
    public System.Action<float> OnStaminaChanged;
    public System.Action OnStaminaDepleted;
    public System.Action OnStaminaRestored;

    // Reset stamina to full
    public void ResetStamina()
    {
        currentStamina = maxStamina;
        canSprint = true;
        OnStaminaChanged?.Invoke(currentStamina);
    }

    // Set stamina value
    public void SetStamina(float value)
    {
        float previousStamina = currentStamina;
        currentStamina = Mathf.Clamp(value, 0, maxStamina);

        CheckStaminaState(previousStamina);
        OnStaminaChanged?.Invoke(currentStamina);
    }

    // Set max stamina
    public void SetMaxStamina(float value)
    {
        maxStamina = Mathf.Max(0, value);
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina);
    }

    // Consume stamina
    public bool ConsumeStamina(float amount)
    {
        if (currentStamina < amount) return false;

        float previousStamina = currentStamina;
        currentStamina = Mathf.Max(0, currentStamina - amount);

        CheckStaminaState(previousStamina);
        OnStaminaChanged?.Invoke(currentStamina);

        return true;
    }

    public bool DrainStaminaContinuous(float drainRate, float deltaTime)
    {
        float drainAmount = drainRate * deltaTime;

        if (currentStamina < drainAmount)
        {
            // Not enough stamina
            currentStamina = 0;
            canSprint = false;
            OnStaminaChanged?.Invoke(currentStamina);
            OnStaminaDepleted?.Invoke();
            return false;
        }

        currentStamina -= drainAmount;
        OnStaminaChanged?.Invoke(currentStamina);

        // Check if we dropped below minimum sprint threshold
        if (currentStamina < minimumStaminaToSprint)
        {
            canSprint = false;
        }

        return true;
    }

    // Restore stamina
    public void RestoreStamina(float amount)
    {
        float previousStamina = currentStamina;
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);

        CheckStaminaState(previousStamina);
        OnStaminaChanged?.Invoke(currentStamina);
    }

    // Check stamina state changes
    private void CheckStaminaState(float previousStamina)
    {
        // Check if depleted
        if (previousStamina > 0 && currentStamina <= 0)
        {
            OnStaminaDepleted?.Invoke();
        }

        // Check if restored from depleted
        if (previousStamina <= 0 && currentStamina > 0)
        {
            OnStaminaRestored?.Invoke();
        }

        // Update sprint availability
        canSprint = currentStamina >= minimumStaminaToSprint;
    }

    // Get stamina percentage
    public float GetStaminaPercentage()
    {
        return maxStamina > 0 ? currentStamina / maxStamina : 0f;
    }

    // Configuration getters
    public float GetSprintCostPerSecond() => sprintCostPerSecond;
    public float GetJumpCost() => jumpCost;
    public float GetRegenRate() => regenRate;
    public float GetRegenDelay() => regenDelay;
    public bool GetRegenWhileMoving() => regenWhileMoving;
}