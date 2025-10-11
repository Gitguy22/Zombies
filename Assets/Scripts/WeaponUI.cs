using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI weaponNameText;
    public List<Image> weaponIcons;

    [Header("Optional Elements")]
    public TextMeshProUGUI ammoCountText;
    public TextMeshProUGUI reserveAmmoText;

    public void UpdateWeaponIcons(List<GameObject> weapons, int activeIndex)
    {
        // Make sure we don't try to access more icons than we have
        int maxIconsToUpdate = Mathf.Min(weaponIcons.Count, weapons.Count);

        for (int i = 0; i < weaponIcons.Count; i++)
        {
            if (i < maxIconsToUpdate)
            {
                // Get the weapon data
                WeaponPickup pickup = weapons[i].GetComponent<WeaponPickup>();
                if (pickup != null && pickup.weaponData != null && pickup.weaponData.weaponIcon != null)
                {
                    // Set the icon from weapon data
                    weaponIcons[i].sprite = pickup.weaponData.weaponIcon;
                    // Highlight the active weapon, dim the others
                    weaponIcons[i].color = (i == activeIndex) ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.8f);
                    weaponIcons[i].gameObject.SetActive(true);
                }
                else
                {
                    // No weapon data or icon, but still a weapon slot
                    weaponIcons[i].sprite = null;
                    weaponIcons[i].color = (i == activeIndex) ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.8f);
                    weaponIcons[i].gameObject.SetActive(true);
                }
            }
            else
            {
                // Empty weapon slot or more icons than weapons
                weaponIcons[i].sprite = null;
                weaponIcons[i].color = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Dimmed for empty slots
                weaponIcons[i].gameObject.SetActive(true);
            }
        }
    }

    public void UpdateWeaponName(string weaponName)
    {
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponName;
        }
    }

    // Called by the Weapon component to update ammo display
    public void UpdateAmmoCount(int currentAmmo, int reserveAmmo)
    {
        if (ammoCountText != null)
        {
            ammoCountText.text = currentAmmo.ToString();
        }

        if (reserveAmmoText != null)
        {
            reserveAmmoText.text = reserveAmmo.ToString();
        }
    }
}