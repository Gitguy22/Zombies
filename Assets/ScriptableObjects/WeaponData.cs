using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Identity")]
    public string weaponName = "Default Weapon";
    public string weaponID = "weapon_default"; // Unique identifier for the weapon
    public GameObject weaponPrefab;
    public Sprite weaponIcon;

    [Header("Weapon Type")]
    public WeaponType weaponType = WeaponType.StandardGun;

    [Header("Transform Settings")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
    public Vector3 scale = Vector3.one;

    [Header("State Transforms")]
    public Vector3 reloadPositionOffset = new Vector3(0.2f, -0.3f, 0.1f);
    public Vector3 reloadRotationOffset = new Vector3(45f, -30f, 0f);
    public Vector3 runningPositionOffset = new Vector3(0.1f, -0.1f, 0.1f);
    public Vector3 runningRotationOffset = new Vector3(-40f, 0f, 0f);

    [Header("Purchase Information")]
    public int purchaseCost = 1000;
    public int ammoCost = 500; // Cost to repurchase ammo for a weapon already owned

    [Header("Ammo Settings")]
    public int maxAmmoInMag = 30;
    public int maxReserveAmmo = 270;
    public int defaultAmmoInMag = 30;
    public int defaultReserveAmmo = 90;

    [Header("Crosshair Settings")]
    public Sprite crosshairSprite; // The center dot
    public Sprite crosshairLineSprite; // The surrounding lines/segments
    public float crosshairSize = 15f; // Base size of crosshair
    public float crosshairLineLength = 20f;
    public float crosshairLineWidth = 5f;// Length of crosshair lines
    public float crosshairLineDistance = 35f; // Distance from center to lines
    public Color crosshairColor = Color.white;
    public bool dynamicCrosshair = true; // Whether crosshair expands with spread
    public float crosshairMaxSpread = 300f; // Maximum spread size
    public bool hideCrosshairWhenAiming = false; // Option to hide crosshair when aiming

    [Header("Rarity Settings")]
    [Range(0, 100)]
    public float mysteryBoxProbability = 10f; // Chance of getting this weapon from the mystery box

    [Header("Sound Effects")]
    public AudioClip purchaseSound;
    public AudioClip pickupSound;

    public bool Equals(WeaponData other)
    {
        if (other == null) return false;
        return weaponID == other.weaponID;
    }
}

public enum WeaponType
{
    StandardGun,
    Shotgun,
    Explosive,
    Melee,
    Special
}