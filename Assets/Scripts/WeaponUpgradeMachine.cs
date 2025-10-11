using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WeaponUpgradeMachine : MonoBehaviour, IInteractable
{
    [Header("Machine Settings")]
    [SerializeField] private int upgradeCost = 5000;
    [SerializeField] private float upgradeTime = 8f;
    [SerializeField] private Transform weaponEntryPoint;
    [SerializeField] private Transform weaponExitPoint;

    [Header("Upgrade Mappings")]
    [SerializeField] private WeaponUpgradeData upgradeData;

    [Header("Effects and Animation")]
    [SerializeField] private GameObject processingEffectPrefab;
    [SerializeField] private Animation machineAnimation;
    [SerializeField] private string activateAnimationName = "UpgradeMachine_Activate";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip startUpgradeSound;
    [SerializeField] private AudioClip upgradeCompleteSound;
    [SerializeField] private AudioClip notEnoughPointsSound;
    [SerializeField] private AudioClip noUpgradeAvailableSound;

    [Header("Events")]
    [SerializeField] private UnityEvent onUpgradeStart;
    [SerializeField] private UnityEvent onUpgradeComplete;

    private bool isProcessing = false;
    private GameObject currentPlayerInteracting;
    private PointManager pointManager;

    private void Awake()
    {
        // Find the point manager in the scene
        pointManager = FindObjectOfType<PointManager>();

        if (pointManager == null)
        {
            Debug.LogError("PointManager not found in scene! WeaponUpgradeMachine will not function correctly.");
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Validate required components
        if (weaponEntryPoint == null)
        {
            Debug.LogError("Weapon Entry Point not assigned on WeaponUpgradeMachine!");
        }

        if (weaponExitPoint == null)
        {
            Debug.LogError("Weapon Exit Point not assigned on WeaponUpgradeMachine!");
        }

        if (upgradeData == null)
        {
            Debug.LogError("WeaponUpgradeData not assigned on WeaponUpgradeMachine!");
        }
    }

    #region IInteractable Implementation

    public void BuyItem(GameObject player)
    {
        Debug.Log("Upgrade machine interaction started");

        if (isProcessing)
        {
            Debug.Log("Machine is already processing");
            return;
        }

        currentPlayerInteracting = player;

        // Check if player has enough points
        int playerPoints = pointManager.GetPoints(player);
        Debug.Log($"Player has {playerPoints} points, need {upgradeCost}");

        if (playerPoints < upgradeCost)
        {
            Debug.Log("Not enough points");
            // Not enough points
            if (audioSource != null && notEnoughPointsSound != null)
            {
                audioSource.PlayOneShot(notEnoughPointsSound);
            }
            return;
        }

        // Get player's current weapon
        WeaponManager weaponManager = player.GetComponent<WeaponManager>();
        if (weaponManager == null)
        {
            Debug.LogError("Player does not have a WeaponManager component!");
            return;
        }

        GameObject currentWeapon = weaponManager.GetCurrentWeapon();
        if (currentWeapon == null)
        {
            Debug.Log("No weapon equipped");
            return;
        }

        // Find the weapon data of the current weapon
        WeaponData currentWeaponData = GetWeaponDataFromGameObject(currentWeapon);
        if (currentWeaponData == null)
        {
            Debug.LogError("Could not find WeaponData for current weapon!");
            return;
        }

        // Check if this weapon is already upgraded
        if (upgradeData.IsUpgradedWeapon(currentWeaponData))
        {
            Debug.Log($"Weapon {currentWeaponData.weaponName} is already upgraded.");

            if (audioSource != null && noUpgradeAvailableSound != null)
            {
                audioSource.PlayOneShot(noUpgradeAvailableSound);
            }
            return;
        }

        // Find the upgrade for this weapon using our scriptable object
        WeaponData upgradedWeaponData = upgradeData.GetUpgradedVersion(currentWeaponData);
        if (upgradedWeaponData == null)
        {
            Debug.Log($"No upgrade mapping found for weapon: {currentWeaponData.weaponName}");

            if (audioSource != null && noUpgradeAvailableSound != null)
            {
                audioSource.PlayOneShot(noUpgradeAvailableSound);
            }
            return;
        }

        // Deduct points
        pointManager.TakePoints(player, upgradeCost);

        // Start the upgrade process
        StartCoroutine(ProcessWeaponUpgrade(weaponManager, currentWeaponData, upgradedWeaponData));
    }

    public string GetItemName()
    {
        if (isProcessing)
        {
            return "Upgrading weapon...";
        }
        return $"Upgrade Weapon ({upgradeCost} points)";
    }

    public int GetCost()
    {
        return upgradeCost;
    }

    public bool IsPaidFor()
    {
        return false; // Always requires payment for each use
    }

    #endregion

    private WeaponData GetWeaponDataFromGameObject(GameObject weaponObj)
    {
        if (weaponObj == null) return null;

        // First check WeaponPickup
        WeaponPickup pickup = weaponObj.GetComponent<WeaponPickup>();
        if (pickup != null && pickup.weaponData != null)
            return pickup.weaponData;

        // Then check Weapon component 
        Weapon weapon = weaponObj.GetComponent<Weapon>();
        if (weapon != null && weapon.weaponData != null)
            return weapon.weaponData;

        return null;
    }

    private IEnumerator ProcessWeaponUpgrade(WeaponManager weaponManager, WeaponData currentWeaponData, WeaponData upgradedWeaponData)
    {
        isProcessing = true;

        // Play start sound
        if (audioSource != null && startUpgradeSound != null)
        {
            audioSource.PlayOneShot(startUpgradeSound);
        }

        // Play machine animation
        if (machineAnimation != null && !string.IsNullOrEmpty(activateAnimationName))
        {
            machineAnimation.Play(activateAnimationName);
        }

        // Remove current weapon from player
        weaponManager.DropCurrentWeapon();

        // Create processing effect if available
        GameObject processingEffect = null;
        if (processingEffectPrefab != null && weaponEntryPoint != null)
        {
            processingEffect = Instantiate(processingEffectPrefab, weaponEntryPoint.position, weaponEntryPoint.rotation);
        }

        // Trigger upgrade start event
        onUpgradeStart?.Invoke();

        // Wait for upgrade time
        float elapsed = 0f;
        while (elapsed < upgradeTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Clean up processing effect
        if (processingEffect != null)
        {
            Destroy(processingEffect);
        }

        // Spawn upgraded weapon at exit point
        if (upgradedWeaponData != null && upgradedWeaponData.weaponPrefab != null && weaponExitPoint != null)
        {
            // Create the upgraded weapon pickup
            GameObject upgradedWeapon = Instantiate(upgradedWeaponData.weaponPrefab, weaponExitPoint.position, weaponExitPoint.rotation);

            // Configure the weapon pickup
            WeaponPickup weaponPickup = upgradedWeapon.GetComponent<WeaponPickup>();
            if (weaponPickup == null)
            {
                weaponPickup = upgradedWeapon.AddComponent<WeaponPickup>();
            }

            weaponPickup.weaponData = upgradedWeaponData;

            // Start pickup timer to make it disappear after a while if not picked up
            weaponPickup.StartPickupTimer();

            // Set up visual effects for the pickup
            weaponPickup.rotationSpeed = 45f;
            weaponPickup.floatAmplitude = 0.15f;
            weaponPickup.floatFrequency = 1.5f;
        }

        // Play completion sound
        if (audioSource != null && upgradeCompleteSound != null)
        {
            audioSource.PlayOneShot(upgradeCompleteSound);
        }

        // Trigger upgrade complete event
        onUpgradeComplete?.Invoke();

        // Reset processing state
        isProcessing = false;
    }
}