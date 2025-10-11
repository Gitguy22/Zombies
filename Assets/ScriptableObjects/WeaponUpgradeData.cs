using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponUpgradeData", menuName = "Weapons/Upgrade Data", order = 2)]
public class WeaponUpgradeData : ScriptableObject
{
    [System.Serializable]
    public class WeaponUpgradeMapping
    {
        public WeaponData baseWeapon;
        public WeaponData upgradedWeapon;
        [Tooltip("How much more expensive ammo is for upgraded weapons (multiplier)")]
        public float upgradedAmmoCostMultiplier = 1.0f;
    }

    [SerializeField] private List<WeaponUpgradeMapping> upgradeMappings = new List<WeaponUpgradeMapping>();

    // Get the upgraded version of a weapon
    public WeaponData GetUpgradedVersion(WeaponData baseWeapon)
    {
        if (baseWeapon == null) return null;

        foreach (var mapping in upgradeMappings)
        {
            if (mapping.baseWeapon != null && mapping.baseWeapon.weaponID == baseWeapon.weaponID)
            {
                return mapping.upgradedWeapon;
            }
        }
        return null;
    }

    // Get the base version of a weapon (if this is an upgraded weapon)
    public WeaponData GetBaseVersion(WeaponData upgradedWeapon)
    {
        if (upgradedWeapon == null) return null;

        foreach (var mapping in upgradeMappings)
        {
            if (mapping.upgradedWeapon != null && mapping.upgradedWeapon.weaponID == upgradedWeapon.weaponID)
            {
                return mapping.baseWeapon;
            }
        }
        return null;
    }

    // Check if a weapon is an upgraded version
    public bool IsUpgradedWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return false;

        foreach (var mapping in upgradeMappings)
        {
            if (mapping.upgradedWeapon != null && mapping.upgradedWeapon.weaponID == weaponData.weaponID)
            {
                return true;
            }
        }
        return false;
    }

    // Get the cost multiplier for an upgraded weapon's ammo
    public float GetUpgradedAmmoCostMultiplier(WeaponData weaponData)
    {
        if (weaponData == null) return 1f;

        foreach (var mapping in upgradeMappings)
        {
            if (mapping.upgradedWeapon != null && mapping.upgradedWeapon.weaponID == weaponData.weaponID)
            {
                return mapping.upgradedAmmoCostMultiplier;
            }
        }
        return 1f;
    }

    // Find the wall buy cost for an upgraded weapon based on its base version
    public int GetBaseWeaponAmmoCost(WeaponData upgradedWeapon)
    {
        if (upgradedWeapon == null) return 0;

        WeaponData baseVersion = GetBaseVersion(upgradedWeapon);
        if (baseVersion != null)
        {
            return baseVersion.ammoCost;
        }
        return upgradedWeapon.ammoCost;
    }
}