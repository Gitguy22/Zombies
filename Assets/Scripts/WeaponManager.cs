using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Settings")]
    public Transform weaponHolder;
    public int maxWeapons = 2;
    public WeaponData starterWeapon;

    [Header("UI References")]
    public WeaponUI weaponUI;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Weapon Switching")]
    [SerializeField] float weaponSwitchDelay = 0.5f; // Delay to ensure proper ammo saving

    [Header("Events")]
    public UnityEvent<GameObject> onWeaponAdded;
    public UnityEvent<GameObject> onWeaponRemoved;
    public UnityEvent<GameObject> onWeaponSwitched;

    // Event for external systems to listen to weapon changes
    public System.Action<string> OnWeaponChanged; // Passes weapon name
    public System.Action<int, int> OnAmmoChanged; // Passes current ammo and reserves

    // Private variables
    private List<GameObject> equippedWeapons = new List<GameObject>();
    private List<WeaponData> ownedWeaponData = new List<WeaponData>();
    private int currentWeaponIndex = 0;

    // Reference to the player's ammo data
    private PlayerAmmoReference playerAmmoRef;
    private PlayerAmmoData playerAmmoData;

    // Reference to the WeaponAmmoTracker
    private WeaponAmmoTracker ammoTracker;

    // Input system
    private PlayerInputs playerInput;
    private InputAction switchWeaponAction;
    private InputAction scrollWeaponAction;
    private InputAction[] numberKeyActions;

    // Flag to prevent ammo tracker interference during pickup
    private bool isPickingUpWeapon = false;

    // Flag to prevent weapon ammo initialization during switching
    private bool isSwitchingWeapons = false;

    // Coroutine for weapon switching
    private Coroutine weaponSwitchCoroutine = null;

    private void Awake()
    {
        playerInput = new PlayerInputs();
        InitializeInputs();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Get the player's ammo reference from this same GameObject
        playerAmmoRef = GetComponent<PlayerAmmoReference>();
        if (playerAmmoRef != null)
        {
            playerAmmoData = playerAmmoRef.AmmoData;
        }
        else
        {
            //////Debug.LogWarning("PlayerAmmoReference not found on the player. Creating one.");
            // Create one if it doesn't exist
            playerAmmoRef = gameObject.AddComponent<PlayerAmmoReference>();
            playerAmmoData = playerAmmoRef.AmmoData;
        }

        // Get or add the WeaponAmmoTracker
        ammoTracker = GetComponent<WeaponAmmoTracker>();
        if (ammoTracker == null)
        {
            ammoTracker = gameObject.AddComponent<WeaponAmmoTracker>();
        }
    }

    private void OnEnable()
    {
        playerInput.Enable();
        RegisterInputCallbacks(true);

        // Set up a listener for ammo changes if we have valid ammo data
        if (playerAmmoData != null)
        {
            // Here we would ideally set up a listener for changes in the ScriptableObject
            // Since ScriptableObjects don't have built-in events, we'll implement a polling approach
            StartCoroutine(PollAmmoData());
        }
    }

    private void OnDisable()
    {
        RegisterInputCallbacks(false);
        playerInput.Disable();
        StopAllCoroutines();
    }

    // Since ScriptableObjects don't have built-in change events, we'll poll for changes
    private IEnumerator PollAmmoData()
    {
        int lastAmmoInMag = -1;
        int lastAmmoInReserve = -1;

        while (true)
        {
            if (playerAmmoData != null)
            {
                if (playerAmmoData.AmmoInMag != lastAmmoInMag ||
                    playerAmmoData.AmmoInReserve != lastAmmoInReserve)
                {
                    lastAmmoInMag = playerAmmoData.AmmoInMag;
                    lastAmmoInReserve = playerAmmoData.AmmoInReserve;
                    OnAmmoChanged?.Invoke(lastAmmoInMag, lastAmmoInReserve);
                }
            }

            yield return new WaitForSeconds(0.1f); // Poll every 0.1 seconds
        }
    }

    private void InitializeInputs()
    {
        switchWeaponAction = playerInput.OnFoot.SwitchWeapon;
        scrollWeaponAction = playerInput.OnFoot.ScrollWeapon;
        numberKeyActions = new InputAction[maxWeapons];

        for (int i = 0; i < maxWeapons && i < 10; i++)
        {
            numberKeyActions[i] = playerInput.asset.FindAction($"Weapon{i + 1}");
            if (numberKeyActions[i] == null)
            {
                //////Debug.LogWarning($"Weapon{i + 1} action not found in Input System.");
            }
        }
    }

    private void RegisterInputCallbacks(bool register)
    {
        if (register)
        {
            switchWeaponAction.performed += SwitchWeaponInputAction;
            scrollWeaponAction.performed += ScrollWeaponInputAction;

            for (int i = 0; i < numberKeyActions.Length; i++)
            {
                int weaponIndex = i;
                if (numberKeyActions[i] != null)
                {
                    numberKeyActions[i].performed += ctx => EquipWeapon(weaponIndex);
                }
            }
        }
        else
        {
            switchWeaponAction.performed -= SwitchWeaponInputAction;
            scrollWeaponAction.performed -= ScrollWeaponInputAction;

            for (int i = 0; i < numberKeyActions.Length; i++)
            {
                if (numberKeyActions[i] != null)
                {
                    int weaponIndex = i;
                    numberKeyActions[i].performed -= ctx => EquipWeapon(weaponIndex);
                }
            }
        }
    }

    private void SwitchWeaponInputAction(InputAction.CallbackContext ctx)
    {
        SwitchWeapon(1);
    }

    private void ScrollWeaponInputAction(InputAction.CallbackContext ctx)
    {
        SwitchWeapon(ctx.ReadValue<float>() > 0 ? 1 : -1);
    }

    private void InitializeStarterWeaponAmmo()
    {
        if (starterWeapon == null || ammoTracker == null) return;

        //////Debug.Log($"*** EXPLICIT INITIALIZATION for starter weapon: {starterWeapon.weaponName} ***");

        // Force tracker to initialize the weapon to defaults
        ammoTracker.InitializeWeaponAmmo(starterWeapon);

        // Double-check the tracker's state directly
        //////Debug.Log($"Verifying starter weapon initialization: {starterWeapon.weaponName}");

        // Force reset the PlayerAmmoData to match the starter weapon's defaults
        if (playerAmmoData != null)
        {
            playerAmmoData.SetAmmoInMag(starterWeapon.defaultAmmoInMag);
            playerAmmoData.SetAmmoInReserve(starterWeapon.defaultReserveAmmo);
            playerAmmoData.SetMaxAmmoInMag(starterWeapon.maxAmmoInMag);
            playerAmmoData.SetMaxAmmoInReserve(starterWeapon.maxReserveAmmo);

            //////Debug.Log($"Forced PlayerAmmoData for starter weapon: " +
            // $"{playerAmmoData.AmmoInMag}/{playerAmmoData.AmmoInReserve}");
        }
    }

    // Modify the Start method in WeaponManager.cs to call this first
    private void Start()
    {
        // Initialize the starter weapon ammo explicitly FIRST
        InitializeStarterWeaponAmmo();

        // Make sure ammo tracker is ready
        if (ammoTracker == null)
        {
            ammoTracker = gameObject.GetComponent<WeaponAmmoTracker>();
            if (ammoTracker == null)
            {
                ammoTracker = gameObject.AddComponent<WeaponAmmoTracker>();
            }
        }

        // Now handle the starter weapon pickup
        bool addedStarterWeapon = false;
        if (starterWeapon != null)
        {
            addedStarterWeapon = PickupWeapon(starterWeapon);
        }

        // Check all weapons (including the starter weapon that may have just been added)
        if (equippedWeapons.Count > 0)
        {
            // Apply transforms to all equipped weapons
            for (int i = 0; i < equippedWeapons.Count; i++)
            {
                WeaponData weaponData = GetWeaponDataFromGameObject(equippedWeapons[i]);
                if (weaponData != null)
                {
                    ApplyWeaponTransformSafe(equippedWeapons[i], weaponData);
                }
            }

            // Equip the first weapon (or current one if already set)
            int indexToEquip = (currentWeaponIndex < equippedWeapons.Count) ? currentWeaponIndex : 0;
            EquipWeapon(indexToEquip);
        }
    }

    private void SwitchWeapon(int direction)
    {
        if (equippedWeapons.Count == 0) return;

        // Prevent multiple simultaneous switches
        if (isSwitchingWeapons) return;

        int newIndex = (currentWeaponIndex + direction + equippedWeapons.Count) % equippedWeapons.Count;
        EquipWeapon(newIndex);
    }

    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= equippedWeapons.Count) return;
        if (index == currentWeaponIndex && !isSwitchingWeapons) return; // Already equipped

        // Prevent multiple simultaneous switches
        if (weaponSwitchCoroutine != null)
        {
            //////Debug.Log("Weapon switch already in progress, ignoring new switch request");
            return;
        }

        // Start the weapon switch coroutine
        weaponSwitchCoroutine = StartCoroutine(SwitchWeaponCoroutine(index));
    }

    private IEnumerator SwitchWeaponCoroutine(int targetIndex)
    {
        isSwitchingWeapons = true;
        //////Debug.Log($"Starting weapon switch from index {currentWeaponIndex} to {targetIndex}");

        // STEP 1: Save current weapon's ammo explicitly
        if (currentWeaponIndex >= 0 && currentWeaponIndex < equippedWeapons.Count)
        {
            GameObject currentWeapon = equippedWeapons[currentWeaponIndex];
            WeaponData currentWeaponData = GetWeaponDataFromGameObject(currentWeapon);

            if (currentWeaponData != null && ammoTracker != null)
            {
                //////Debug.Log($"Explicitly saving ammo for current weapon {currentWeaponData.weaponName}");

                // Force save current weapon's ammo state using tracker
                ammoTracker.ForceSaveCurrentWeaponAmmo();
            }
        }

        // STEP 2: Disable all weapons and set skip flags
        foreach (GameObject weaponObj in equippedWeapons)
        {
            if (weaponObj != null)
            {
                weaponObj.SetActive(false);

                Weapon weaponComponent = weaponObj.GetComponent<Weapon>();
                if (weaponComponent != null)
                {
                    weaponComponent.SetFlag_SkipAmmoInit(true);
                }
            }
        }

        // STEP 3: Wait for the specified delay to ensure ammo is properly saved
        //////Debug.Log($"Waiting {weaponSwitchDelay} seconds for ammo save completion...");
        yield return new WaitForSeconds(weaponSwitchDelay);

        // STEP 4: Get target weapon data and load its ammo
        WeaponData targetWeaponData = GetWeaponDataFromGameObject(equippedWeapons[targetIndex]);
        if (targetWeaponData != null && ammoTracker != null)
        {
            //////Debug.Log($"Loading ammo for target weapon {targetWeaponData.weaponName}");

            // Force load target weapon's ammo into PlayerAmmoData
            ammoTracker.ForceLoadWeaponAmmo(targetWeaponData.weaponID);
        }

        // STEP 5: Update current index and activate target weapon
        currentWeaponIndex = targetIndex;
        GameObject targetWeapon = equippedWeapons[targetIndex];

        // Apply transform safely
        if (targetWeaponData != null)
        {
            ApplyWeaponTransformSafe(targetWeapon, targetWeaponData);
        }

        // Activate the target weapon
        targetWeapon.SetActive(true);

        // STEP 6: Wait a brief moment then re-enable ammo initialization
        yield return new WaitForSeconds(0.1f);

        // Re-enable ammo initialization on all weapons
        foreach (GameObject weaponObj in equippedWeapons)
        {
            if (weaponObj != null)
            {
                Weapon weaponComponent = weaponObj.GetComponent<Weapon>();
                if (weaponComponent != null)
                {
                    weaponComponent.SetFlag_SkipAmmoInit(false);
                }
            }
        }

        // STEP 7: Update UI and notify listeners
        if (weaponUI != null)
        {
            UpdateUI();
        }

        // Notify listeners about weapon change
        NotifyWeaponChanged();
        onWeaponSwitched?.Invoke(targetWeapon);

        //////Debug.Log($"Weapon switch completed to {targetWeaponData?.weaponName}");

        // Clear flags and coroutine reference
        isSwitchingWeapons = false;
        weaponSwitchCoroutine = null;
    }

    private void NotifyWeaponChanged()
    {
        if (OnWeaponChanged == null || equippedWeapons.Count == 0 ||
            currentWeaponIndex < 0 || currentWeaponIndex >= equippedWeapons.Count)
            return;

        WeaponData weaponData = GetWeaponDataFromGameObject(equippedWeapons[currentWeaponIndex]);
        if (weaponData != null)
        {
            OnWeaponChanged(weaponData.weaponName);
        }
    }

    // Update UI
    private void UpdateUI()
    {
        if (weaponUI == null) return;

        // Update weapon icons
        weaponUI.UpdateWeaponIcons(equippedWeapons, currentWeaponIndex);

        // Update weapon name
        if (equippedWeapons.Count > 0 && currentWeaponIndex >= 0 && currentWeaponIndex < equippedWeapons.Count)
        {
            WeaponData weaponData = GetWeaponDataFromGameObject(equippedWeapons[currentWeaponIndex]);
            if (weaponData != null)
            {
                weaponUI.UpdateWeaponName(weaponData.weaponName);
            }
            else
            {
                weaponUI.UpdateWeaponName("Unknown Weapon");
            }
        }
        else
        {
            weaponUI.UpdateWeaponName("No Weapon");
        }

        // Also update ammo display
        if (playerAmmoData != null)
        {
            OnAmmoChanged?.Invoke(playerAmmoData.AmmoInMag, playerAmmoData.AmmoInReserve);
        }
    }

    public bool HasWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return false;

        //////Debug.Log($"Checking if player has weapon: {weaponData.weaponName} (ID: {weaponData.weaponID})");

        // First check the ownedWeaponData list 
        bool foundInList = false;
        foreach (WeaponData ownedWeapon in ownedWeaponData)
        {
            if (ownedWeapon != null && ownedWeapon.weaponID == weaponData.weaponID)
            {
                foundInList = true;
                break;
            }
        }

        // Verify by checking actual GameObjects in equippedWeapons
        bool foundInEquipped = false;
        foreach (GameObject weaponObj in equippedWeapons)
        {
            if (weaponObj == null) continue;

            WeaponData objWeaponData = GetWeaponDataFromGameObject(weaponObj);
            if (objWeaponData != null && objWeaponData.weaponID == weaponData.weaponID)
            {
                foundInEquipped = true;
                break;
            }
        }

        // Log a warning if there's a mismatch
        if (foundInList != foundInEquipped)
        {
            //////Debug.LogWarning($"Inconsistency for weapon {weaponData.weaponName}: " +
            // $"In list: {foundInList}, In equipped: {foundInEquipped}");

            // If it's in the list but not equipped, clean up the list
            if (foundInList && !foundInEquipped)
            {
                CleanupOwnedWeaponsList();
                return false;
            }
        }

        return foundInEquipped;
    }

    private void CleanupOwnedWeaponsList()
    {
        //////Debug.Log("Cleaning up owned weapons list");

        // Create a new list with only weapons that are actually equipped
        List<WeaponData> validWeapons = new List<WeaponData>();

        foreach (GameObject weaponObj in equippedWeapons)
        {
            if (weaponObj == null) continue;

            WeaponData objWeaponData = GetWeaponDataFromGameObject(weaponObj);
            if (objWeaponData != null)
            {
                validWeapons.Add(objWeaponData);
            }
        }

        // Replace the list
        ownedWeaponData = validWeapons;

        //////Debug.Log($"Owned weapons list cleaned up: {ownedWeaponData.Count} weapons remaining");
    }

    // Called when purchasing ammo for an already owned weapon - FIXED VERSION
    public bool RefillAmmo(WeaponData weaponData)
    {
        if (weaponData == null) return false;

        // Find the specific weapon to refill
        foreach (GameObject weaponObj in equippedWeapons)
        {
            WeaponData objWeaponData = GetWeaponDataFromGameObject(weaponObj);

            if (objWeaponData != null && objWeaponData.weaponID == weaponData.weaponID)
            {
                // Debug log
                //////Debug.Log($"Refilling ammo for {weaponData.weaponName} (ID: {weaponData.weaponID})");

                // Update the ammo tracker with the DEFAULT ammo values from WeaponData
                if (ammoTracker != null)
                {
                    ammoTracker.SetWeaponAmmo(
                        weaponData.weaponID,
                        weaponData.defaultAmmoInMag,
                        weaponData.defaultReserveAmmo
                    );
                }

                // If this is the current weapon, also update the ammo data directly
                GameObject currentWeapon = GetCurrentWeapon();
                if (currentWeapon != null && currentWeapon == weaponObj && playerAmmoData != null)
                {
                    playerAmmoData.SetAmmoInMag(weaponData.defaultAmmoInMag);
                    playerAmmoData.SetAmmoInReserve(weaponData.defaultReserveAmmo);
                }

                // REMOVED: weapon.RefillAmmo() call to prevent refilling all weapons
                // Individual weapon RefillAmmo methods may be causing all weapons to refill

                if (weaponData.pickupSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(weaponData.pickupSound);
                }

                return true;
            }
        }

        return false;
    }

    public bool RefillSpecificWeaponAmmo(WeaponData weaponData)
    {
        if (weaponData == null) return false;

        //////Debug.Log($"Refilling ammo for specific weapon: {weaponData.weaponName} (ID: {weaponData.weaponID})");

        bool weaponFound = false;

        // First, check if the player actually has this weapon
        foreach (GameObject weaponObj in equippedWeapons)
        {
            WeaponData objWeaponData = GetWeaponDataFromGameObject(weaponObj);

            if (objWeaponData != null && objWeaponData.weaponID == weaponData.weaponID)
            {
                weaponFound = true;
                break;
            }
        }

        if (!weaponFound)
        {
            //////Debug.LogWarning($"Cannot refill ammo - player doesn't have weapon: {weaponData.weaponName}");
            return false;
        }

        // Update ammo in the tracker for ONLY this specific weapon
        if (ammoTracker != null)
        {
            ammoTracker.SetWeaponAmmo(
                weaponData.weaponID,
                weaponData.defaultAmmoInMag,
                weaponData.defaultReserveAmmo
            );

            //////Debug.Log($"Set ammo in tracker for {weaponData.weaponID}: {weaponData.defaultAmmoInMag}/{weaponData.defaultReserveAmmo}");
        }

        // If this weapon is currently equipped, update the player ammo data directly
        GameObject currentWeapon = GetCurrentWeapon();
        if (currentWeapon != null)
        {
            WeaponData currentWeaponData = GetWeaponDataFromGameObject(currentWeapon);
            if (currentWeaponData != null && currentWeaponData.weaponID == weaponData.weaponID && playerAmmoData != null)
            {
                playerAmmoData.SetAmmoInMag(weaponData.defaultAmmoInMag);
                playerAmmoData.SetAmmoInReserve(weaponData.defaultReserveAmmo);
                //////Debug.Log($"Updated current weapon ammo directly: {weaponData.defaultAmmoInMag}/{weaponData.defaultReserveAmmo}");

                // Trigger the ammo changed event
                OnAmmoChanged?.Invoke(weaponData.defaultAmmoInMag, weaponData.defaultReserveAmmo);
            }
        }

        // Play pickup sound if available
        if (weaponData.pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(weaponData.pickupSound);
        }

        return true;
    }

    // Helper method to apply transform with animator handling
    private void ApplyWeaponTransformSafe(GameObject weapon, WeaponData weaponData)
    {
        if (weapon == null || weaponData == null) return;

        Transform weaponTransform = weapon.transform;
        Animator animator = weapon.GetComponent<Animator>();

        if (animator != null)
        {
            // Temporarily disable animator
            bool wasEnabled = animator.enabled;
            animator.enabled = false;

            // Set transform values when animator is disabled
            weaponTransform.localPosition = weaponData.positionOffset;
            weaponTransform.localRotation = Quaternion.Euler(weaponData.rotationOffset);
            weaponTransform.localScale = weaponData.scale;

            // Re-enable animator if it was enabled
            animator.enabled = wasEnabled;
        }
        else
        {
            // No animator - just set the transform directly
            weaponTransform.localPosition = weaponData.positionOffset;
            weaponTransform.localRotation = Quaternion.Euler(weaponData.rotationOffset);
            weaponTransform.localScale = weaponData.scale;
        }
    }

    // Adds a new weapon to the player's inventory - FIXED VERSION
    public bool PickupWeapon(WeaponData weaponData)
    {
        if (weaponData == null || weaponData.weaponPrefab == null)
        {
            //////Debug.LogError("Cannot pick up null weaponData or weapon with null prefab");
            return false;
        }

        //////Debug.Log($"Picking up weapon: {weaponData.weaponName} (ID: {weaponData.weaponID})");

        // Check if player already has this weapon
        if (HasWeapon(weaponData))
        {
            return RefillAmmo(weaponData);
        }

        // Set flag to prevent ammo tracker interference during pickup
        isPickingUpWeapon = true;

        // STEP 1: Initialize the weapon's ammo state in tracker FIRST
        if (ammoTracker != null)
        {
            //////Debug.Log($"Pre-initializing ammo for {weaponData.weaponName} in tracker");
            ammoTracker.InitializeWeaponAmmo(weaponData);
        }

        // STEP 2: Instantiate the weapon prefab
        GameObject newWeapon = Instantiate(weaponData.weaponPrefab);
        newWeapon.transform.SetParent(weaponHolder, false);

        // STEP 3: Configure the weapon BEFORE enabling it
        WeaponPickup pickup = newWeapon.GetComponent<WeaponPickup>();
        if (pickup != null)
        {
            pickup.weaponData = weaponData;
        }

        Weapon weaponComponent = newWeapon.GetComponent<Weapon>();
        if (weaponComponent != null)
        {
            weaponComponent.weaponData = weaponData;
            // Tell the weapon not to initialize its own ammo during pickup
            weaponComponent.SetFlag_SkipAmmoInit(true);
        }

        // STEP 4: Apply weapon transforms
        ApplyWeaponTransformSafe(newWeapon, weaponData);

        // STEP 5: Handle inventory management
        if (equippedWeapons.Count >= maxWeapons)
        {
            ReplaceCurrentWeapon(newWeapon, weaponData);
        }
        else
        {
            equippedWeapons.Add(newWeapon);
            ownedWeaponData.Add(weaponData);
        }

        // STEP 6: Set up the PlayerAmmoData with the weapon's values
        if (playerAmmoData != null)
        {
            //////Debug.Log($"Setting PlayerAmmoData for picked up weapon: {weaponData.defaultAmmoInMag}/{weaponData.defaultReserveAmmo}");
            playerAmmoData.SetAmmoInMag(weaponData.defaultAmmoInMag);
            playerAmmoData.SetAmmoInReserve(weaponData.defaultReserveAmmo);
            playerAmmoData.SetMaxAmmoInMag(weaponData.maxAmmoInMag);
            playerAmmoData.SetMaxAmmoInReserve(weaponData.maxReserveAmmo);
        }

        // STEP 7: Equip the weapon (this will trigger ammo loading from tracker)
        if (equippedWeapons.Count <= maxWeapons)
        {
            EquipWeapon(equippedWeapons.Count - 1);
        }

        // STEP 8: Clear the pickup flag and allow weapon to initialize normally
        isPickingUpWeapon = false;
        if (weaponComponent != null)
        {
            weaponComponent.SetFlag_SkipAmmoInit(false);
        }

        // Play pickup sound
        if (weaponData.pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(weaponData.pickupSound);
        }

        onWeaponAdded?.Invoke(newWeapon);
        return true;
    }

    private void ReplaceCurrentWeapon(GameObject newWeapon, WeaponData weaponData)
    {
        if (equippedWeapons.Count == 0) return;

        // Get the current weapon
        GameObject oldWeapon = equippedWeapons[currentWeaponIndex];
        WeaponPickup oldPickup = oldWeapon.GetComponent<WeaponPickup>();

        // Remove the old weapon from both lists
        equippedWeapons.RemoveAt(currentWeaponIndex);
        if (oldPickup != null && oldPickup.weaponData != null)
        {
            ownedWeaponData.Remove(oldPickup.weaponData);
        }

        // Trigger weapon removed event before destroying
        onWeaponRemoved?.Invoke(oldWeapon);

        // Destroy the old weapon gameobject
        Destroy(oldWeapon);

        // Add the new weapon
        equippedWeapons.Insert(currentWeaponIndex, newWeapon);
        if (weaponData != null)
        {
            ownedWeaponData.Add(weaponData);
        }

        // Apply transform safely (handles animator if present)
        if (weaponData != null)
        {
            ApplyWeaponTransformSafe(newWeapon, weaponData);
        }

        // Make sure the new weapon is active
        newWeapon.SetActive(true);
    }

    // For dropping/discarding weapons without picking up a new one
    public void DropCurrentWeapon()
    {
        if (equippedWeapons.Count == 0) return;

        GameObject weaponToDrop = equippedWeapons[currentWeaponIndex];
        WeaponPickup pickup = weaponToDrop.GetComponent<WeaponPickup>();

        equippedWeapons.RemoveAt(currentWeaponIndex);

        if (pickup != null && pickup.weaponData != null)
        {
            ownedWeaponData.Remove(pickup.weaponData);
        }

        // Trigger event before destroying
        onWeaponRemoved?.Invoke(weaponToDrop);

        Destroy(weaponToDrop);

        // Refresh the weapons in the ammo tracker
        if (ammoTracker != null)
        {
            ammoTracker.RefreshWeapons();
        }

        // Switch to the next available weapon if any
        if (equippedWeapons.Count > 0)
        {
            currentWeaponIndex = Mathf.Clamp(currentWeaponIndex, 0, equippedWeapons.Count - 1);
            EquipWeapon(currentWeaponIndex);
        }
        else
        {
            // Update UI for no weapons
            if (weaponUI != null)
            {
                weaponUI.UpdateWeaponIcons(equippedWeapons, -1);
                weaponUI.UpdateWeaponName("No Weapon");
            }

            // Notify listeners about no weapon
            OnWeaponChanged?.Invoke("No Weapon");
            OnAmmoChanged?.Invoke(0, 0);
        }
    }

    // Gets the currently equipped weapon GameObject
    public GameObject GetCurrentWeapon()
    {
        if (equippedWeapons.Count == 0 || currentWeaponIndex < 0 || currentWeaponIndex >= equippedWeapons.Count)
            return null;

        return equippedWeapons[currentWeaponIndex];
    }

    // Get all equipped weapons (needed by the WeaponAmmoTracker)
    public List<GameObject> GetEquippedWeapons()
    {
        return equippedWeapons;
    }

    // Get the current ammo status
    public (int currentAmmo, int reserves) GetAmmoStatus()
    {
        if (playerAmmoData != null)
        {
            return (playerAmmoData.AmmoInMag, playerAmmoData.AmmoInReserve);
        }
        return (0, 0);
    }

    // Add this helper method to get WeaponData from a GameObject
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

    // Public getter for pickup state (for ammo tracker)
    public bool IsPickingUpWeapon()
    {
        return isPickingUpWeapon;
    }

    // Public getter for switching state (for ammo tracker)
    public bool IsSwitchingWeapons()
    {
        return isSwitchingWeapons;
    }
}