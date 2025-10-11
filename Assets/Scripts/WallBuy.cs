using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBuy : MonoBehaviour, IInteractable
{
    [Header("Weapon Configuration")]
    public WeaponData weaponData;

    [Header("Wall Buy Settings")]
    public string promptText;
    [Tooltip("Reference to the weapon upgrade data scriptable object")]
    public WeaponUpgradeData upgradeData;

    [Header("Visual Elements")]
    public ParticleSystem purchaseParticles;
    public SpriteRenderer chalkOutline;

    [Header("Audio")]
    public AudioSource audioSource;

    private WeaponManager playerWeaponManager;
    private PointManager pointManager;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (weaponData == null)
        {
            Debug.LogError("WallBuy is missing WeaponData!", this);
        }
    }

    private void Start()
    {
        // Find point manager system
        pointManager = FindObjectOfType<PointManager>();
        if (pointManager == null)
        {
            Debug.LogError("PointManager not found in scene!");
        }

        if (weaponData != null)
        {
            UpdatePromptText();
        }
        else
        {
            Debug.LogError("WallBuy is missing WeaponData!");
            promptText = "Missing Weapon Data";
        }
    }

    private void UpdatePromptText()
    {
        if (playerWeaponManager == null)
        {
            PlayerInteraction playerInteraction = FindObjectOfType<PlayerInteraction>();
            if (playerInteraction != null)
            {
                playerWeaponManager = playerInteraction.GetComponent<WeaponManager>();
            }
        }

        if (playerWeaponManager != null && weaponData != null)
        {
            // Check if player has this weapon or its upgraded version
            bool hasBaseWeapon = playerWeaponManager.HasWeapon(weaponData);

            // Check for upgraded weapon if we have upgrade data
            bool hasUpgradedWeapon = false;
            WeaponData upgradedVersion = null;

            if (upgradeData != null)
            {
                upgradedVersion = upgradeData.GetUpgradedVersion(weaponData);
                if (upgradedVersion != null)
                {
                    hasUpgradedWeapon = playerWeaponManager.HasWeapon(upgradedVersion);
                }
            }

            if (hasBaseWeapon)
            {
                // Player has the base weapon, offer ammo at normal cost
                promptText = $"Buy Ammo for {weaponData.weaponName} for {weaponData.ammoCost} points";
            }
            else if (hasUpgradedWeapon && upgradedVersion != null)
            {
                // Player has the upgraded version, use the upgraded weapon's own ammo cost
                promptText = $"Buy Ammo for {upgradedVersion.weaponName} for {upgradedVersion.ammoCost} points";
            }
            else
            {
                // Normal purchase prompt
                promptText = $"Buy {weaponData.weaponName} for {weaponData.purchaseCost} points";
            }
        }
        else if (weaponData != null)
        {
            promptText = $"Buy {weaponData.weaponName} for {weaponData.purchaseCost} points";
        }
    }

    public void BuyItem(GameObject player)
    {
        if (weaponData == null) return;

        // Get or update weapon manager reference
        if (playerWeaponManager == null)
        {
            playerWeaponManager = player.GetComponent<WeaponManager>();
        }

        // Check if player has this weapon or its upgraded version
        bool hasBaseWeapon = playerWeaponManager != null && playerWeaponManager.HasWeapon(weaponData);

        // Check for upgraded weapon if we have upgrade data
        bool hasUpgradedWeapon = false;
        WeaponData upgradedVersion = null;

        if (upgradeData != null)
        {
            upgradedVersion = upgradeData.GetUpgradedVersion(weaponData);
            if (upgradedVersion != null)
            {
                hasUpgradedWeapon = playerWeaponManager != null && playerWeaponManager.HasWeapon(upgradedVersion);
            }
        }

        // Calculate cost based on which weapon the player has
        int cost;
        if (hasBaseWeapon)
        {
            // Normal ammo cost for base weapon
            cost = weaponData.ammoCost;
        }
        else if (hasUpgradedWeapon && upgradedVersion != null)
        {
            // Use the upgraded weapon's own flat ammo cost
            cost = upgradedVersion.ammoCost;
        }
        else
        {
            // Normal weapon purchase cost
            cost = weaponData.purchaseCost;
        }

        // Check if player has enough points
        bool hasEnoughPoints = pointManager != null && pointManager.GetPoints(player) >= cost;

        if (hasEnoughPoints)
        {
            // Deduct points
            pointManager.TakePoints(player, cost);

            bool success = false;

            if (hasBaseWeapon)
            {
                // Refill ammo for the base weapon
                success = playerWeaponManager.RefillSpecificWeaponAmmo(weaponData);

                if (success)
                {
                    Debug.Log($"Player {player.name} bought ammo for {weaponData.weaponName} for {cost} points");
                }
            }
            else if (hasUpgradedWeapon && upgradedVersion != null)
            {
                // Refill ammo for the upgraded weapon
                success = playerWeaponManager.RefillSpecificWeaponAmmo(upgradedVersion);

                if (success)
                {
                    Debug.Log($"Player {player.name} bought ammo for {upgradedVersion.weaponName} for {cost} points");
                }
            }
            else
            {
                // Give the new weapon to the player
                success = playerWeaponManager.PickupWeapon(weaponData);

                if (success)
                {
                    Debug.Log($"Player {player.name} bought {weaponData.weaponName} for {cost} points");
                }
            }

            if (success)
            {
                // Play effects
                PlayPurchaseEffects();

                // Update prompt for next interaction
                UpdatePromptText();
            }
        }
        else
        {
            Debug.Log($"Not enough points to buy {weaponData.weaponName}!");

            // Flash red for insufficient funds
            if (chalkOutline != null)
            {
                StartCoroutine(FlashChalkOutline(Color.red, 0.4f));
            }

            // Play "can't afford" sound
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.PlayOneShot(audioSource.clip);
            }
        }
    }

    private void PlayPurchaseEffects()
    {
        // Play sound effect
        if (audioSource != null && weaponData != null && weaponData.purchaseSound != null)
        {
            audioSource.PlayOneShot(weaponData.purchaseSound);
        }

        // Play particles
        if (purchaseParticles != null)
        {
            purchaseParticles.Play();
        }

        // Flash the chalk outline
        if (chalkOutline != null)
        {
            StartCoroutine(FlashChalkOutline(Color.green, 0.5f));
        }
    }

    private IEnumerator FlashChalkOutline(Color flashColor, float duration)
    {
        if (chalkOutline == null) yield break;

        Color originalColor = chalkOutline.color;

        // Change to flash color
        chalkOutline.color = flashColor;

        // Wait for duration
        yield return new WaitForSeconds(duration);

        // Change back to original color
        chalkOutline.color = originalColor;
    }

    public string GetItemName()
    {
        UpdatePromptText(); // Make sure text is up to date
        return promptText;
    }

    public int GetCost()
    {
        if (playerWeaponManager != null && weaponData != null)
        {
            // Check for base weapon
            bool hasBaseWeapon = playerWeaponManager.HasWeapon(weaponData);
            if (hasBaseWeapon)
            {
                return weaponData.ammoCost;
            }

            // Check for upgraded weapon
            if (upgradeData != null)
            {
                WeaponData upgradedVersion = upgradeData.GetUpgradedVersion(weaponData);
                if (upgradedVersion != null && playerWeaponManager.HasWeapon(upgradedVersion))
                {
                    // Use the upgraded weapon's own flat ammo cost
                    return upgradedVersion.ammoCost;
                }
            }

            // Default is purchase cost
            return weaponData.purchaseCost;
        }
        return weaponData != null ? weaponData.purchaseCost : 0;
    }

    public bool IsPaidFor()
    {
        // Always return false to allow repeated interactions
        return false;
    }
}