using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerHealthData", menuName = "Game Data/Player Health Data")]
public class PlayerHealthData : ScriptableObject
{
    [Header("Health Settings")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float playerHealth = 100f;
    [SerializeField] bool isDead = false;

    [Header("Damage Settings")]
    [SerializeField] float damageMultiplier = 1f;
    [SerializeField] bool isInvulnerable = false;

    // Properties
    public float PlayerHealth => playerHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;

    // Events for health changes
    public System.Action<float> OnHealthChanged;
    public System.Action OnDeath;

    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth); // Ensure max health is at least 1

        // If current health exceeds new max, clamp it
        if (playerHealth > maxHealth)
        {
            playerHealth = maxHealth;
            OnHealthChanged?.Invoke(playerHealth);
        }
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public void ResetHealth()
    {
        playerHealth = maxHealth;
        isDead = false;
        OnHealthChanged?.Invoke(playerHealth);
    }

    public void SetPlayerHealth(float health)
    {
        playerHealth = Mathf.Clamp(health, 0, maxHealth);
        CheckDeath();
        OnHealthChanged?.Invoke(playerHealth);
    }

    public void TakeDamage(float damageAmount)
    {
        if (isInvulnerable || isDead) return;

        float finalDamage = damageAmount * damageMultiplier;
        playerHealth = Mathf.Max(0, playerHealth - finalDamage);

        CheckDeath();
        OnHealthChanged?.Invoke(playerHealth);
    }

    public void Heal(float healAmount)
    {
        if (isDead) return;

        playerHealth = Mathf.Min(maxHealth, playerHealth + healAmount);

        if (playerHealth > 0)
        {
            isDead = false;
        }

        OnHealthChanged?.Invoke(playerHealth);
    }

    private void CheckDeath()
    {
        if (playerHealth <= 0 && !isDead)
        {
            isDead = true;
            playerHealth = 0;
            OnDeath?.Invoke();
        }
    }

    public void SetInvulnerable(bool invulnerable) => isInvulnerable = invulnerable;
    public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;
    public float GetHealthPercentage() => playerHealth / maxHealth;
}