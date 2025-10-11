using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerAmmoData", menuName = "Game Data/Player Ammo Data")]
public class PlayerAmmoData : ScriptableObject
{
    [SerializeField] int ammoInMag = 0;
    [SerializeField] int ammoInReserve = 0;
    [SerializeField] int maxAmmoInMag = 30;
    [SerializeField] int maxAmmoInReserve = 90;

    // Getters
    public int AmmoInMag => ammoInMag;
    public int AmmoInReserve => ammoInReserve;
    public int MaxAmmoInMag => maxAmmoInMag;
    public int MaxAmmoInReserve => maxAmmoInReserve;

    // Setters
    public void SetAmmoInMag(int ammo)
    {
        ammoInMag = Mathf.Clamp(ammo, 0, maxAmmoInMag);
    }

    public void SetAmmoInReserve(int ammo)
    {
        ammoInReserve = Mathf.Clamp(ammo, 0, maxAmmoInReserve);
    }

    public void SetMaxAmmoInMag(int maxAmmo)
    {
        maxAmmoInMag = maxAmmo;
        ammoInMag = Mathf.Clamp(ammoInMag, 0, maxAmmoInMag);
    }

    public void SetMaxAmmoInReserve(int maxAmmo)
    {
        maxAmmoInReserve = maxAmmo;
        ammoInReserve = Mathf.Clamp(ammoInReserve, 0, maxAmmoInReserve);
    }

    // Gameplay methods
    public bool CanShoot()
    {
        return ammoInMag > 0;
    }

    public void UseAmmo(int amount = 1)
    {
        ammoInMag = Mathf.Max(0, ammoInMag - amount);
    }

    public bool CanReload()
    {
        return ammoInMag < maxAmmoInMag && ammoInReserve > 0;
    }

    public void Reload()
    {
        if (!CanReload()) return;

        int ammoNeeded = maxAmmoInMag - ammoInMag;
        int ammoToAdd = Mathf.Min(ammoNeeded, ammoInReserve);

        ammoInMag += ammoToAdd;
        ammoInReserve -= ammoToAdd;
    }

    public int GetShellsNeeded()
    {
        return Mathf.Min(maxAmmoInMag - ammoInMag, ammoInReserve);
    }

    public void AddOneShell()
    {
        if (ammoInReserve > 0 && ammoInMag < maxAmmoInMag)
        {
            ammoInMag++;
            ammoInReserve--;
        }
    }

    // Reset ammo to default values
    public void ResetAmmo(bool fillMag = true, bool fillReserve = true)
    {
        ammoInMag = fillMag ? maxAmmoInMag : 0;
        ammoInReserve = fillReserve ? maxAmmoInReserve : 0;
    }
}