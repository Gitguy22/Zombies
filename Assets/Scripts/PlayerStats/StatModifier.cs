using UnityEngine;
using System;

namespace PlayerStats
{
    [Serializable]
    public class StatModifier
    {
        [SerializeField] string modifierID;
        [SerializeField] StatType statType;
        [SerializeField] ModifierType modifierType;
        [SerializeField] float value;
        [SerializeField] int priority;
        [SerializeField] bool isStackable;
        [SerializeField] float duration;
        [SerializeField] string source;

        // Properties
        public string ModifierID => modifierID;
        public StatType StatType => statType;
        public ModifierType ModifierType => modifierType;
        public float Value => value;
        public int Priority => priority;
        public bool IsStackable => isStackable;
        public float Duration => duration;
        public string Source => source;
        public bool IsPermanent => duration <= 0;
        public float TimeRemaining { get; private set; }

        // Constructor
        public StatModifier(string id, StatType stat, ModifierType type, float value,
            int priority = 0, bool stackable = true, float duration = 0f, string source = "Unknown")
        {
            this.modifierID = id;
            this.statType = stat;
            this.modifierType = type;
            this.value = value;
            this.priority = priority;
            this.isStackable = stackable;
            this.duration = duration;
            this.source = source;
            this.TimeRemaining = duration;
        }

        // Update the time remaining for temporary modifiers
        public bool UpdateDuration(float deltaTime)
        {
            if (IsPermanent) return false;

            TimeRemaining -= deltaTime;
            return TimeRemaining <= 0;
        }

        // Create a copy of this modifier
        public StatModifier Clone()
        {
            return new StatModifier(modifierID, statType, modifierType, value,
                priority, isStackable, duration, source);
        }
    }

    public enum StatType
    {
        // Health
        Health,
        MaxHealth,
        HealthRegen,
        HealthRegenDelay,
        HealthRegenCooldown,
        DamageResistance,
        IncomingDamageMultiplier,

        // Stamina  
        Stamina,
        MaxStamina,
        StaminaRegen,
        StaminaRegenDelay,  
        SprintStaminaCost, 
        JumpStaminaCost, 

        // Movement
        MovementSpeed,
        SprintSpeed,
        SprintAcceleration,   
        MovementAcceleration,
        JumpHeight,
        JumpPower,    //controls horizontal jump distance
        JumpCount,
        GravityMultiplier,
        AirControl,    
        MomentumPreservation,
        MomentumDecay,
        MaxAirSpeed,
        GroundDrag, 

        // Weapon
        WeaponDamage,
        WeaponFireRate,
        WeaponReloadSpeed,
        WeaponSpread,
        WeaponRecoil,
        WeaponSwapSpeed,
        ADSSpeed,       
        ADSMovementMultiplier,

        // Combat
        MeleeDamage,
        MeleeSpeed,
        MeleeRange, 
        ExplosiveDamage,
        ExplosiveRadius,
        CriticalChance,
        CriticalDamage,  

        // Other
        MaxWeaponSlots,
        PointsMultiplier,
        InteractionRange,   
        InteractionSpeed 
    }

    // Enum for modifier calculation types
    public enum ModifierType
    {
        Flat,           // Add flat value
        PercentAdd,     // Add percentage of base value
        PercentMult     // Multiply by percentage
    }
}