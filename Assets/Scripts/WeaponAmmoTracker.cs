using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAmmoTracker : MonoBehaviour
{
    [System.Serializable]
    public class WeaponAmmoState
    {
        public string weaponID;
        public string weaponName;
        public int ammoInMag;
        public int ammoInReserve;
    }

    [Header("Debug")]
    [SerializeField] private List<WeaponAmmoState> debugWeaponStates = new List<WeaponAmmoState>();

    // Simple dictionary to store ammo per weapon
    private Dictionary<string, WeaponAmmoState> weaponAmmo = new Dictionary<string, WeaponAmmoState>();

    // References
    private WeaponManager weaponManager;
    private PlayerAmmoData ammoData;
    private string currentWeaponID = "";

    // Flag to prevent saving during ammo refill operations
    private bool isRefillInProgress = false;

    void Awake()
    {
        weaponManager = GetComponent<WeaponManager>();
        PlayerAmmoReference ammoRef = GetComponent<PlayerAmmoReference>();
        if (ammoRef != null)
        {
            ammoData = ammoRef.AmmoData;
        }
    }

    void Start()
    {
        // Wait a frame for weapon manager to initialize
        StartCoroutine(InitializeAfterFrame());
    }

    IEnumerator InitializeAfterFrame()
    {
        yield return null;

        // Register for weapon changes
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged += OnWeaponChanged;
        }

        // Initialize current weapon
        UpdateCurrentWeapon();
    }

    void Update()
    {
        // Only save current weapon's ammo if we're not in the middle of a refill operation
        if (!isRefillInProgress)
        {
            SaveCurrentWeaponAmmo();
        }

        // Update debug list
        UpdateDebugList();
    }

    void OnDestroy()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged -= OnWeaponChanged;
        }
    }

    // Called when weapon changes - save old, load new
    void OnWeaponChanged(string weaponName)
    {
        // Don't interfere during pickup/switching operations
        if (weaponManager.IsPickingUpWeapon() || weaponManager.IsSwitchingWeapons())
            return;

        UpdateCurrentWeapon();
    }

    // Update current weapon ID and load its ammo
    void UpdateCurrentWeapon()
    {
        GameObject currentWeapon = weaponManager.GetCurrentWeapon();
        if (currentWeapon == null)
        {
            currentWeaponID = "";
            return;
        }

        WeaponData weaponData = GetWeaponData(currentWeapon);
        if (weaponData == null)
        {
            currentWeaponID = "";
            return;
        }

        string newWeaponID = weaponData.weaponID;

        // If this is a different weapon, load its saved ammo
        if (newWeaponID != currentWeaponID)
        {
            currentWeaponID = newWeaponID;
            LoadWeaponAmmo(weaponData);
        }
    }

    // Save current weapon's ammo values in real-time
    void SaveCurrentWeaponAmmo()
    {
        if (string.IsNullOrEmpty(currentWeaponID) || ammoData == null)
            return;

        // Create or update the ammo state
        if (!weaponAmmo.ContainsKey(currentWeaponID))
        {
            weaponAmmo[currentWeaponID] = new WeaponAmmoState();
        }

        WeaponAmmoState state = weaponAmmo[currentWeaponID];
        state.weaponID = currentWeaponID;
        state.ammoInMag = ammoData.AmmoInMag;
        state.ammoInReserve = ammoData.AmmoInReserve;

        // Get weapon name for debugging
        GameObject currentWeapon = weaponManager.GetCurrentWeapon();
        if (currentWeapon != null)
        {
            WeaponData weaponData = GetWeaponData(currentWeapon);
            if (weaponData != null)
            {
                state.weaponName = weaponData.weaponName;
            }
        }
    }

    // FIXED: Load saved ammo for a weapon with CORRECT LOADING ORDER
    void LoadWeaponAmmo(WeaponData weaponData)
    {
        if (weaponData == null || ammoData == null)
            return;

        string weaponID = weaponData.weaponID;

        // Set flag to prevent saving during load
        isRefillInProgress = true;

        // CRITICAL FIX: Set max values FIRST before setting current values
        ammoData.SetMaxAmmoInMag(weaponData.maxAmmoInMag);
        ammoData.SetMaxAmmoInReserve(weaponData.maxReserveAmmo);

        if (weaponAmmo.ContainsKey(weaponID))
        {
            // Load saved ammo - now that max values are set correctly
            WeaponAmmoState state = weaponAmmo[weaponID];
            ammoData.SetAmmoInMag(state.ammoInMag);
            ammoData.SetAmmoInReserve(state.ammoInReserve);

            //Debug.Log($"Loaded saved ammo for {weaponData.weaponName}: {state.ammoInMag}/{state.ammoInReserve} (max: {weaponData.maxAmmoInMag}/{weaponData.maxReserveAmmo})");
        }
        else
        {
            // First time using this weapon - use defaults
            ammoData.SetAmmoInMag(weaponData.defaultAmmoInMag);
            ammoData.SetAmmoInReserve(weaponData.defaultReserveAmmo);

            //Debug.Log($"Using default ammo for {weaponData.weaponName}: {weaponData.defaultAmmoInMag}/{weaponData.defaultReserveAmmo} (max: {weaponData.maxAmmoInMag}/{weaponData.maxReserveAmmo})");
        }

        // Clear flag after load is complete
        isRefillInProgress = false;
    }

    // Initialize a weapon with default ammo (called by weapon manager)
    public void InitializeWeaponAmmo(WeaponData weaponData)
    {
        if (weaponData == null)
            return;

        string weaponID = weaponData.weaponID;

        // Create new state with defaults
        WeaponAmmoState state = new WeaponAmmoState
        {
            weaponID = weaponID,
            weaponName = weaponData.weaponName,
            ammoInMag = weaponData.defaultAmmoInMag,
            ammoInReserve = weaponData.defaultReserveAmmo
        };

        weaponAmmo[weaponID] = state;

        //Debug.Log($"Initialized {weaponData.weaponName} with default ammo: {state.ammoInMag}/{state.ammoInReserve}");
    }

    // FIXED: Set specific ammo for a weapon (for refills, etc.) with proper safeguards
    public void SetWeaponAmmo(string weaponID, int ammoInMag, int ammoInReserve)
    {
        if (string.IsNullOrEmpty(weaponID))
            return;

        //Debug.Log($"Setting ammo for weapon {weaponID}: {ammoInMag}/{ammoInReserve}");

        // Set flag to prevent saving during refill
        isRefillInProgress = true;

        if (!weaponAmmo.ContainsKey(weaponID))
        {
            weaponAmmo[weaponID] = new WeaponAmmoState { weaponID = weaponID };
        }

        WeaponAmmoState state = weaponAmmo[weaponID];
        state.ammoInMag = ammoInMag;
        state.ammoInReserve = ammoInReserve;

        // If this is the current weapon, also update player ammo data with correct max values first
        if (weaponID == currentWeaponID && ammoData != null)
        {
            WeaponData weaponData = FindWeaponDataByID(weaponID);
            if (weaponData != null)
            {
                // Set max values first
                ammoData.SetMaxAmmoInMag(weaponData.maxAmmoInMag);
                ammoData.SetMaxAmmoInReserve(weaponData.maxReserveAmmo);

                // Then set current values
                ammoData.SetAmmoInMag(ammoInMag);
                ammoData.SetAmmoInReserve(ammoInReserve);

                //Debug.Log($"Updated PlayerAmmoData for current weapon {weaponID}: {ammoInMag}/{ammoInReserve}");
            }
        }

        // Clear flag after refill is complete
        isRefillInProgress = false;

        //Debug.Log($"Successfully set ammo for weapon {weaponID}: {ammoInMag}/{ammoInReserve}");
    }

    // Force save current weapon ammo (called before weapon switching)
    public void ForceSaveCurrentWeaponAmmo()
    {
        // Don't save if we're in the middle of a refill
        if (isRefillInProgress)
            return;

        SaveCurrentWeaponAmmo();

        if (!string.IsNullOrEmpty(currentWeaponID))
        {
            //Debug.Log($"Force saved current weapon ammo: {currentWeaponID}");
        }
    }

    // FIXED: Force load specific weapon ammo with correct loading order
    public void ForceLoadWeaponAmmo(string weaponID)
    {
        WeaponData weaponData = FindWeaponDataByID(weaponID);
        if (weaponData != null)
        {
            currentWeaponID = weaponID;
            LoadWeaponAmmo(weaponData);
            //Debug.Log($"Force loaded weapon ammo: {weaponID}");
        }
    }

    // Helper method to get weapon data from GameObject
    WeaponData GetWeaponData(GameObject weaponObj)
    {
        if (weaponObj == null) return null;

        WeaponPickup pickup = weaponObj.GetComponent<WeaponPickup>();
        if (pickup != null && pickup.weaponData != null)
            return pickup.weaponData;

        Weapon weapon = weaponObj.GetComponent<Weapon>();
        if (weapon != null && weapon.weaponData != null)
            return weapon.weaponData;

        return null;
    }

    // Helper method to find weapon data by ID
    WeaponData FindWeaponDataByID(string weaponID)
    {
        if (string.IsNullOrEmpty(weaponID)) return null;

        var equippedWeapons = weaponManager.GetEquippedWeapons();
        foreach (GameObject weaponObj in equippedWeapons)
        {
            WeaponData weaponData = GetWeaponData(weaponObj);
            if (weaponData != null && weaponData.weaponID == weaponID)
                return weaponData;
        }

        return null;
    }

    // Update debug list for inspector
    void UpdateDebugList()
    {
        debugWeaponStates.Clear();
        foreach (var kvp in weaponAmmo)
        {
            debugWeaponStates.Add(kvp.Value);
        }
    }

    // Debug method
    public void DebugListAllWeaponStates()
    {
        //Debug.Log("=== WEAPON AMMO STATES ===");
        foreach (var kvp in weaponAmmo)
        {
            var state = kvp.Value;
            string current = (kvp.Key == currentWeaponID) ? " [CURRENT]" : "";
            //Debug.Log($"{state.weaponName} ({state.weaponID}): {state.ammoInMag}/{state.ammoInReserve}{current}");
        }
        //Debug.Log("=========================");
    }

    // Cleanup/reset methods (for compatibility)
    public void RefreshWeapons() { UpdateCurrentWeapon(); }
    public void ResetWeaponAmmo(string weaponID, WeaponData weaponData)
    {
        if (weaponData != null)
            SetWeaponAmmo(weaponID, weaponData.defaultAmmoInMag, weaponData.defaultReserveAmmo);
    }
    public void ResetWeaponToDefaults(string weaponID)
    {
        WeaponData weaponData = FindWeaponDataByID(weaponID);
        if (weaponData != null)
            ResetWeaponAmmo(weaponID, weaponData);
    }
}