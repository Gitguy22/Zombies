using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace PlayerStats
{
    [Serializable]
    public class ModifiableStatValue
    {
        [SerializeField] float baseValue;
        [SerializeField] StatType statType;
        [SerializeField] bool allowNegative;

        private List<StatModifier> modifiers = new List<StatModifier>();
        private float lastCalculatedValue;
        private bool isDirty = true;

        // Events
        public event Action<float, float> OnValueChanged; // oldValue, newValue

        // Properties
        public float BaseValue
        {
            get => baseValue;
            set
            {
                if (Mathf.Approximately(baseValue, value)) return;
                baseValue = value;
                isDirty = true;
                RecalculateValue();
            }
        }

        public float Value
        {
            get
            {
                if (isDirty) RecalculateValue();
                return lastCalculatedValue;
            }
        }

        public StatType StatType => statType;
        public int ModifierCount => modifiers.Count;

        // Constructor
        public ModifiableStatValue(StatType type, float baseValue, bool allowNegative = false)
        {
            this.statType = type;
            this.baseValue = baseValue;
            this.allowNegative = allowNegative;
            RecalculateValue();
        }

        // Add a modifier
        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null || modifier.StatType != statType) return;

            // Check if stackable or if modifier already exists
            if (!modifier.IsStackable)
            {
                RemoveModifiersFromSource(modifier.Source);
            }

            modifiers.Add(modifier);
            isDirty = true;
            RecalculateValue();
        }

        // Remove a specific modifier
        public bool RemoveModifier(StatModifier modifier)
        {
            if (modifier == null) return false;

            bool removed = modifiers.Remove(modifier);
            if (removed)
            {
                isDirty = true;
                RecalculateValue();
            }
            return removed;
        }

        // Remove modifier by ID
        public bool RemoveModifierByID(string modifierID)
        {
            var modifier = modifiers.FirstOrDefault(m => m.ModifierID == modifierID);
            return modifier != null && RemoveModifier(modifier);
        }

        // Remove all modifiers from a specific source
        public void RemoveModifiersFromSource(string source)
        {
            int removedCount = modifiers.RemoveAll(m => m.Source == source);
            if (removedCount > 0)
            {
                isDirty = true;
                RecalculateValue();
            }
        }

        // Remove all modifiers
        public void ClearModifiers()
        {
            modifiers.Clear();
            isDirty = true;
            RecalculateValue();
        }

        // Update temporary modifiers
        public void UpdateModifiers(float deltaTime)
        {
            bool anyExpired = false;

            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (modifiers[i].UpdateDuration(deltaTime))
                {
                    modifiers.RemoveAt(i);
                    anyExpired = true;
                }
            }

            if (anyExpired)
            {
                isDirty = true;
                RecalculateValue();
            }
        }

        // Recalculate the final value with all modifiers
        private void RecalculateValue()
        {
            float oldValue = lastCalculatedValue;
            float finalValue = baseValue;

            // Sort modifiers by priority (lower priority first)
            var sortedModifiers = modifiers.OrderBy(m => m.Priority).ToList();

            // Apply flat modifiers first
            float flatBonus = 0f;
            foreach (var mod in sortedModifiers.Where(m => m.ModifierType == ModifierType.Flat))
            {
                flatBonus += mod.Value;
            }
            finalValue += flatBonus;

            // Apply percentage additions
            float percentAdd = 0f;
            foreach (var mod in sortedModifiers.Where(m => m.ModifierType == ModifierType.PercentAdd))
            {
                percentAdd += mod.Value;
            }
            finalValue += baseValue * percentAdd;

            // Apply percentage multipliers
            foreach (var mod in sortedModifiers.Where(m => m.ModifierType == ModifierType.PercentMult))
            {
                finalValue *= (1f + mod.Value);
            }

            // Clamp if necessary
            if (!allowNegative && finalValue < 0)
            {
                finalValue = 0;
            }

            lastCalculatedValue = finalValue;
            isDirty = false;

            // Trigger event if value changed
            if (!Mathf.Approximately(oldValue, finalValue))
            {
                OnValueChanged?.Invoke(oldValue, finalValue);
            }
        }

        // Get all modifiers (readonly)
        public IReadOnlyList<StatModifier> GetModifiers()
        {
            return modifiers.AsReadOnly();
        }

        // Get modifiers from specific source
        public List<StatModifier> GetModifiersFromSource(string source)
        {
            return modifiers.Where(m => m.Source == source).ToList();
        }
    }
}