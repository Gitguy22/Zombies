using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Events;

namespace PlayerStats
{
    public class PlayerStatsManager : MonoBehaviour
    {
        [Header("Stats Configuration")]
        [SerializeField] bool initializeOnStart = true;
        [SerializeField] bool updateTemporaryModifiers = true;

        [Header("Base Health Stats")]
        [SerializeField] float baseMaxHealth = 100f;
        [SerializeField] float baseHealthRegen = 5f;
        [SerializeField] float baseHealthRegenDelay = 1f;
        [SerializeField] float baseHealthRegenCooldown = 3f;
        [SerializeField] float baseDamageResistance = 0f;
        [SerializeField] float baseIncomingDamageMultiplier = 1f;

        [Header("Base Stamina Stats")]
        [SerializeField] float baseMaxStamina = 100f;
        [SerializeField] float baseStaminaRegen = 20f;
        [SerializeField] float baseStaminaRegenDelay = 0.5f;
        [SerializeField] float baseSprintStaminaCost = 20f;
        [SerializeField] float baseJumpStaminaCost = 10f;

        [Header("Base Movement Stats")]
        [SerializeField] float baseMovementSpeed = 5f;
        [SerializeField] float baseSprintSpeed = 10f;
        [SerializeField] float baseSprintAcceleration = 8f;
        [SerializeField] float baseMovementAcceleration = 10f;
        [SerializeField] float baseJumpHeight = 2f;
        [SerializeField] float baseJumpPower = 1f;
        [SerializeField] int baseJumpCount = 1;
        [SerializeField] float baseGravityMultiplier = 1f;
        [SerializeField] float baseAirControl = 0.3f;
        [SerializeField] float baseMomentumPreservation = 0.8f;
        [SerializeField] float baseMomentumDecay = 2f;
        [SerializeField] float baseMaxAirSpeed = 12f;
        [SerializeField] float baseGroundDrag = 5f;

        [Header("Base Weapon Stats")]
        [SerializeField] int baseMaxWeaponSlots = 2;
        [SerializeField] float baseWeaponDamageMultiplier = 1f;
        [SerializeField] float baseWeaponFireRateMultiplier = 1f;
        [SerializeField] float baseWeaponReloadSpeedMultiplier = 1f;
        [SerializeField] float baseWeaponSpreadMultiplier = 1f;
        [SerializeField] float baseWeaponRecoilMultiplier = 1f;
        [SerializeField] float baseWeaponSwapSpeedMultiplier = 1f;
        [SerializeField] float baseADSSpeedMultiplier = 1f;
        [SerializeField] float baseADSMovementMultiplier = 0.6f;

        [Header("Base Combat Stats")]
        [SerializeField] float baseMeleeDamageMultiplier = 1f;
        [SerializeField] float baseMeleeSpeedMultiplier = 1f;
        [SerializeField] float baseMeleeRange = 2f;
        [SerializeField] float baseCriticalChance = 0f;
        [SerializeField] float baseCriticalDamageMultiplier = 2f;

        [Header("Base Utility Stats")]
        [SerializeField] float basePointsMultiplier = 1f;
        [SerializeField] float baseInteractionRange = 4f;
        [SerializeField] float baseInteractionSpeedMultiplier = 1f;

        [Header("Events")]
        public UnityEvent<StatType, float, float> OnStatChanged;
        public UnityEvent<StatModifier> OnModifierAdded;
        public UnityEvent<StatModifier> OnModifierRemoved;

        // Stat dictionary
        private Dictionary<StatType, ModifiableStatValue> stats;

        // Player references
        private PlayerHealthData healthData;
        private PlayerStaminaData staminaData;
        private PlayerMovement playerMovement;
        private WeaponManager weaponManager;

        // Singleton per player
        private static Dictionary<GameObject, PlayerStatsManager> instances = new Dictionary<GameObject, PlayerStatsManager>();

        // Properties
        public static PlayerStatsManager GetInstance(GameObject player)
        {
            if (player == null) return null;
            instances.TryGetValue(player, out PlayerStatsManager instance);
            return instance;
        }

        private void Awake()
        {
            // Register this instance
            instances[gameObject] = this;

            // Get references
            CacheReferences();

            if (initializeOnStart)
            {
                InitializeStats();
            }
        }

        private void OnDestroy()
        {
            // Unregister this instance
            instances.Remove(gameObject);
        }

        private void Update()
        {
            if (updateTemporaryModifiers)
            {
                UpdateAllTemporaryModifiers(Time.deltaTime);
            }
        }

        private void CacheReferences()
        {
            // Get health reference
            var healthRef = GetComponent<PlayerHealthReference>();
            if (healthRef != null)
            {
                healthData = healthRef.HealthData;
            }

            // Get stamina reference
            var staminaRef = GetComponent<PlayerStaminaReference>();
            if (staminaRef != null)
            {
                staminaData = staminaRef.StaminaData;
            }

            // Get other components
            playerMovement = GetComponent<PlayerMovement>();
            weaponManager = GetComponent<WeaponManager>();
        }

        private void InitializeStats()
        {
            stats = new Dictionary<StatType, ModifiableStatValue>();

            // Initialize health stats
            InitializeStat(StatType.MaxHealth, baseMaxHealth);
            InitializeStat(StatType.HealthRegen, baseHealthRegen);
            InitializeStat(StatType.HealthRegenDelay, baseHealthRegenDelay);
            InitializeStat(StatType.HealthRegenCooldown, baseHealthRegenCooldown);
            InitializeStat(StatType.DamageResistance, baseDamageResistance);
            InitializeStat(StatType.IncomingDamageMultiplier, baseIncomingDamageMultiplier);

            // Initialize stamina stats
            InitializeStat(StatType.MaxStamina, baseMaxStamina);
            InitializeStat(StatType.StaminaRegen, baseStaminaRegen);
            InitializeStat(StatType.StaminaRegenDelay, baseStaminaRegenDelay);
            InitializeStat(StatType.SprintStaminaCost, baseSprintStaminaCost);
            InitializeStat(StatType.JumpStaminaCost, baseJumpStaminaCost);

            // Initialize movement stats
            InitializeStat(StatType.MovementSpeed, baseMovementSpeed);
            InitializeStat(StatType.SprintSpeed, baseSprintSpeed);
            InitializeStat(StatType.SprintAcceleration, baseSprintAcceleration);
            InitializeStat(StatType.MovementAcceleration, baseMovementAcceleration);
            InitializeStat(StatType.JumpHeight, baseJumpHeight);
            InitializeStat(StatType.JumpPower, baseJumpPower);
            InitializeStat(StatType.JumpCount, baseJumpCount);
            InitializeStat(StatType.GravityMultiplier, baseGravityMultiplier);
            InitializeStat(StatType.AirControl, baseAirControl);
            InitializeStat(StatType.MomentumPreservation, baseMomentumPreservation);
            InitializeStat(StatType.GroundDrag, baseGroundDrag);
            InitializeStat(StatType.MomentumDecay, 2f);
            InitializeStat(StatType.MaxAirSpeed, 12f);

            // Initialize weapon stats
            InitializeStat(StatType.MaxWeaponSlots, baseMaxWeaponSlots);
            InitializeStat(StatType.WeaponDamage, baseWeaponDamageMultiplier);
            InitializeStat(StatType.WeaponFireRate, baseWeaponFireRateMultiplier);
            InitializeStat(StatType.WeaponReloadSpeed, baseWeaponReloadSpeedMultiplier);
            InitializeStat(StatType.WeaponSpread, baseWeaponSpreadMultiplier);
            InitializeStat(StatType.WeaponRecoil, baseWeaponRecoilMultiplier);
            InitializeStat(StatType.WeaponSwapSpeed, baseWeaponSwapSpeedMultiplier);
            InitializeStat(StatType.ADSSpeed, baseADSSpeedMultiplier);
            InitializeStat(StatType.ADSMovementMultiplier, baseADSMovementMultiplier);

            // Initialize combat stats
            InitializeStat(StatType.MeleeDamage, baseMeleeDamageMultiplier);
            InitializeStat(StatType.MeleeSpeed, baseMeleeSpeedMultiplier);
            InitializeStat(StatType.MeleeRange, baseMeleeRange);
            InitializeStat(StatType.CriticalChance, baseCriticalChance);
            InitializeStat(StatType.CriticalDamage, baseCriticalDamageMultiplier);

            // Initialize utility stats
            InitializeStat(StatType.PointsMultiplier, basePointsMultiplier);
            InitializeStat(StatType.InteractionRange, baseInteractionRange);
            InitializeStat(StatType.InteractionSpeed, baseInteractionSpeedMultiplier);

            // Apply initial values to components
            ApplyAllStats();
        }

        private void InitializeStat(StatType type, float baseValue)
        {
            var stat = new ModifiableStatValue(type, baseValue);
            stat.OnValueChanged += (oldValue, newValue) => HandleStatChanged(type, oldValue, newValue);
            stats[type] = stat;
        }

        // Public methods for adding/removing modifiers
        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null) return;

            if (stats.TryGetValue(modifier.StatType, out ModifiableStatValue stat))
            {
                stat.AddModifier(modifier);
                OnModifierAdded?.Invoke(modifier);
            }
        }

        public bool RemoveModifier(StatModifier modifier)
        {
            if (modifier == null) return false;

            if (stats.TryGetValue(modifier.StatType, out ModifiableStatValue stat))
            {
                bool removed = stat.RemoveModifier(modifier);
                if (removed)
                {
                    OnModifierRemoved?.Invoke(modifier);
                }
                return removed;
            }
            return false;
        }

        public void RemoveModifiersFromSource(string source)
        {
            foreach (var stat in stats.Values)
            {
                stat.RemoveModifiersFromSource(source);
            }
        }

        // Get stat value
        public float GetStatValue(StatType type)
        {
            return stats.TryGetValue(type, out ModifiableStatValue stat) ? stat.Value : 0f;
        }

        // Get base stat value
        public float GetBaseStatValue(StatType type)
        {
            return stats.TryGetValue(type, out ModifiableStatValue stat) ? stat.BaseValue : 0f;
        }

        // Set base stat value
        public void SetBaseStatValue(StatType type, float value)
        {
            if (stats.TryGetValue(type, out ModifiableStatValue stat))
            {
                stat.BaseValue = value;
            }
        }

        // Update temporary modifiers
        private void UpdateAllTemporaryModifiers(float deltaTime)
        {
            foreach (var stat in stats.Values)
            {
                stat.UpdateModifiers(deltaTime);
            }
        }

        // Handle stat changes
        private void HandleStatChanged(StatType type, float oldValue, float newValue)
        {
            OnStatChanged?.Invoke(type, oldValue, newValue);
            ApplyStat(type, newValue);
        }

        // Apply stat changes to components
        private void ApplyStat(StatType type, float value)
        {
            switch (type)
            {
                case StatType.MaxHealth:
                    if (healthData != null)
                    {
                        float healthPercent = healthData.GetHealthPercentage();
                        healthData.SetMaxHealth(value);
                        healthData.SetPlayerHealth(value * healthPercent);
                    }
                    break;

                case StatType.MaxStamina:
                    if (staminaData != null)
                    {
                        float staminaPercent = staminaData.GetStaminaPercentage();
                        staminaData.SetMaxStamina(value);
                        staminaData.SetStamina(value * staminaPercent);
                    }
                    break;

                case StatType.MaxWeaponSlots:
                    if (weaponManager != null)
                    {
                        weaponManager.maxWeapons = Mathf.RoundToInt(value);
                    }
                    break;

                    // Movement stats are handled by PlayerMovement through OnStatChanged event
                    // Weapon stats are handled by WeaponStatsReceiver through OnStatChanged event
            }
        }

        // Apply all stats
        private void ApplyAllStats()
        {
            foreach (var kvp in stats)
            {
                ApplyStat(kvp.Key, kvp.Value.Value);
            }
        }

        // Create modifier helper methods
        public StatModifier CreateFlatModifier(StatType stat, float value, string source, float duration = 0f)
        {
            return new StatModifier(Guid.NewGuid().ToString(), stat, ModifierType.Flat,
                value, 0, true, duration, source);
        }

        public StatModifier CreatePercentModifier(StatType stat, float percent, string source,
            float duration = 0f, bool multiplicative = false)
        {
            var type = multiplicative ? ModifierType.PercentMult : ModifierType.PercentAdd;
            return new StatModifier(Guid.NewGuid().ToString(), stat, type,
                percent, 0, true, duration, source);
        }

        // Debug method to log all current stat values
        [ContextMenu("Log All Stats")]
        public void LogAllStats()
        {
            Debug.Log("=== Current Stat Values ===");
            foreach (var kvp in stats)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value.Value:F2} (Base: {kvp.Value.BaseValue:F2}, Modifiers: {kvp.Value.ModifierCount})");
            }
        }
    }
}