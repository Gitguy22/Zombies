using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthDisplay : MonoBehaviour
{
    [Header("Health Data")]
    [SerializeField] private PlayerHealthData healthData;

    [Header("UI References")]
    [SerializeField] private Image frontHealthBar;
    [SerializeField] private Image backHealthBar;

    [Header("Animation Settings")]
    [SerializeField] private float backBarDelay = 1.0f;
    [SerializeField] private float backBarDrainSpeed = 2.0f;

    // Private variables
    private float previousHealth;
    private float frontBarFillTarget;
    private float backBarFillTarget;
    private float backBarDrainTimer;
    private bool isBackBarAnimating = false;

    // Cached values
    private float maxHealth;

    private void Start()
    {
        if (healthData == null)
        {
            Debug.LogError("PlayerHealthDisplay: No PlayerHealthData assigned!");
            enabled = false;
            return;
        }

        // Get initial values
        maxHealth = healthData.MaxHealth;
        previousHealth = healthData.PlayerHealth;

        // Set initial fill amounts
        float initialFill = previousHealth / maxHealth;
        frontHealthBar.fillAmount = initialFill;
        backHealthBar.fillAmount = initialFill;

        frontBarFillTarget = initialFill;
        backBarFillTarget = initialFill;
    }

    private void Update()
    {
        // Check for health changes
        float currentHealth = healthData.PlayerHealth;

        // If health changed
        if (currentHealth != previousHealth)
        {
            // Calculate new fill target
            frontBarFillTarget = currentHealth / maxHealth;

            // Update front bar immediately
            frontHealthBar.fillAmount = frontBarFillTarget;

            // Set back bar to drain after delay (only if health decreased)
            if (currentHealth < previousHealth)
            {
                // Store the original fill amount to animate from
                backBarFillTarget = frontBarFillTarget;

                // Start tracking time for delay
                backBarDrainTimer = backBarDelay;
                isBackBarAnimating = true;
            }
            else
            {
                // If health increased, update back bar immediately too
                backHealthBar.fillAmount = frontBarFillTarget;
                isBackBarAnimating = false;
            }

            // Update previous health
            previousHealth = currentHealth;
        }

        // Handle delayed back bar animation
        if (isBackBarAnimating)
        {
            // Count down delay timer
            if (backBarDrainTimer > 0)
            {
                backBarDrainTimer -= Time.deltaTime;
            }
            else
            {
                // After delay, gradually reduce back bar fill
                if (backHealthBar.fillAmount > frontBarFillTarget)
                {
                    // Gradually drain fill amount
                    backHealthBar.fillAmount -= backBarDrainSpeed * Time.deltaTime;

                    // Stop when target is reached
                    if (backHealthBar.fillAmount <= frontBarFillTarget)
                    {
                        backHealthBar.fillAmount = frontBarFillTarget;
                        isBackBarAnimating = false;
                    }
                }
                else
                {
                    isBackBarAnimating = false;
                }
            }
        }
    }

    // Method to set the health data reference at runtime if needed
    public void SetHealthData(PlayerHealthData newHealthData)
    {
        healthData = newHealthData;

        if (healthData != null)
        {
            // Reset values based on new health data
            maxHealth = healthData.MaxHealth;
            previousHealth = healthData.PlayerHealth;

            // Reset fill amounts
            float initialFill = previousHealth / maxHealth;
            frontHealthBar.fillAmount = initialFill;
            backHealthBar.fillAmount = initialFill;

            frontBarFillTarget = initialFill;
            backBarFillTarget = initialFill;
        }
    }
}