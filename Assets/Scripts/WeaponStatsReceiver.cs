using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerStats;


public class WeaponStatsReceiver : MonoBehaviour
{
    [Header("Stat Application")]
    [SerializeField] bool applyDamageModifiers = true;
    [SerializeField] bool applyFireRateModifiers = true;
    [SerializeField] bool applyReloadSpeedModifiers = true;
    [SerializeField] bool applySpreadModifiers = true;
    [SerializeField] bool applyRecoilModifiers = true;

    // References
    private Weapon weapon;
    private PlayerStatsManager statsManager;
    private GameObject owner;

    // Original values for restoration
    private int originalDamage;
    private float originalFireRate;
    private float originalReloadSpeed;
    private Vector3 originalSpreadVariance;
    private float originalRecoilVertical;
    private float originalRecoilHorizontal;

    // Cached multipliers
    private float damageMultiplier = 1f;
    private float fireRateMultiplier = 1f;
    private float reloadSpeedMultiplier = 1f;
    private float spreadMultiplier = 1f;
    private float recoilMultiplier = 1f;

    private void Awake()
    {
        weapon = GetComponent<Weapon>();
        if (weapon == null)
        {
            Debug.LogError("WeaponStatsReceiver requires a Weapon component!");
            enabled = false;
        }
    }

    private void Start()
    {
        // Store original values
        StoreOriginalValues();

        // Find owner and stats manager
        FindOwnerAndStatsManager();
    }

    private void OnEnable()
    {
        // Subscribe to stat changes if we have a stats manager
        if (statsManager != null)
        {
            SubscribeToStatChanges();
            ApplyAllModifiers();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from stat changes
        if (statsManager != null)
        {
            UnsubscribeFromStatChanges();
        }
    }

    private void FindOwnerAndStatsManager()
    {
        owner = transform.root.gameObject;
        statsManager = PlayerStatsManager.GetInstance(owner);

        if (statsManager != null)
        {
            SubscribeToStatChanges();
            ApplyAllModifiers();
        }
    }

    private void StoreOriginalValues()
    {
        if (weapon == null) return;

        // Use reflection to get private fields
        originalDamage = GetWeaponField<int>("weaponDamage");
        originalFireRate = GetWeaponField<float>("fireRate");
        originalReloadSpeed = GetWeaponField<float>("reloadSpeed");
        originalSpreadVariance = GetWeaponField<Vector3>("bulletSpreadVariance");
        originalRecoilVertical = GetWeaponField<float>("recoilVerticalStrength");
        originalRecoilHorizontal = GetWeaponField<float>("recoilHorizontalStrength");
    }

    private T GetWeaponField<T>(string fieldName)
    {
        var field = weapon.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            return (T)field.GetValue(weapon);
        }

        Debug.LogWarning($"Field {fieldName} not found in Weapon component");
        return default(T);
    }

    private void SetWeaponField<T>(string fieldName, T value)
    {
        var field = weapon.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(weapon, value);
        }
    }

    private void SubscribeToStatChanges()
    {
        statsManager.OnStatChanged.AddListener(OnStatChanged);
    }

    private void UnsubscribeFromStatChanges()
    {
        statsManager.OnStatChanged.RemoveListener(OnStatChanged);
    }

    private void OnStatChanged(StatType statType, float oldValue, float newValue)
    {
        switch (statType)
        {
            case StatType.WeaponDamage:
                if (applyDamageModifiers)
                {
                    damageMultiplier = newValue;
                    ApplyDamageModifier();
                }
                break;

            case StatType.WeaponFireRate:
                if (applyFireRateModifiers)
                {
                    fireRateMultiplier = newValue;
                    ApplyFireRateModifier();
                }
                break;

            case StatType.WeaponReloadSpeed:
                if (applyReloadSpeedModifiers)
                {
                    reloadSpeedMultiplier = newValue;
                    ApplyReloadSpeedModifier();
                }
                break;

            case StatType.WeaponSpread:
                if (applySpreadModifiers)
                {
                    spreadMultiplier = newValue;
                    ApplySpreadModifier();
                }
                break;

            case StatType.WeaponRecoil:
                if (applyRecoilModifiers)
                {
                    recoilMultiplier = newValue;
                    ApplyRecoilModifier();
                }
                break;
        }
    }

    private void ApplyAllModifiers()
    {
        if (statsManager == null) return;

        // Get current multipliers
        damageMultiplier = statsManager.GetStatValue(StatType.WeaponDamage);
        fireRateMultiplier = statsManager.GetStatValue(StatType.WeaponFireRate);
        reloadSpeedMultiplier = statsManager.GetStatValue(StatType.WeaponReloadSpeed);
        spreadMultiplier = statsManager.GetStatValue(StatType.WeaponSpread);
        recoilMultiplier = statsManager.GetStatValue(StatType.WeaponRecoil);

        // Apply all modifiers
        ApplyDamageModifier();
        ApplyFireRateModifier();
        ApplyReloadSpeedModifier();
        ApplySpreadModifier();
        ApplyRecoilModifier();
    }

    private void ApplyDamageModifier()
    {
        int modifiedDamage = Mathf.RoundToInt(originalDamage * damageMultiplier);
        SetWeaponField("weaponDamage", modifiedDamage);
    }

    private void ApplyFireRateModifier()
    {
        float modifiedFireRate = originalFireRate * fireRateMultiplier;
        SetWeaponField("fireRate", modifiedFireRate);
    }

    private void ApplyReloadSpeedModifier()
    {
        // Reload speed multiplier works inversely (higher multiplier = faster reload)
        float modifiedReloadSpeed = originalReloadSpeed / Mathf.Max(0.1f, reloadSpeedMultiplier);
        SetWeaponField("reloadSpeed", modifiedReloadSpeed);
    }

    private void ApplySpreadModifier()
    {
        // Spread multiplier works inversely (higher multiplier = less spread)
        Vector3 modifiedSpread = originalSpreadVariance / Mathf.Max(0.1f, spreadMultiplier);
        SetWeaponField("bulletSpreadVariance", modifiedSpread);
    }

    private void ApplyRecoilModifier()
    {
        // Recoil multiplier works inversely (higher multiplier = less recoil)
        float modifiedVerticalRecoil = originalRecoilVertical / Mathf.Max(0.1f, recoilMultiplier);
        float modifiedHorizontalRecoil = originalRecoilHorizontal / Mathf.Max(0.1f, recoilMultiplier);

        SetWeaponField("recoilVerticalStrength", modifiedVerticalRecoil);
        SetWeaponField("recoilHorizontalStrength", modifiedHorizontalRecoil);
    }

    // Public method to refresh stats (useful when weapon is picked up)
    public void RefreshStats()
    {
        FindOwnerAndStatsManager();
    }
}