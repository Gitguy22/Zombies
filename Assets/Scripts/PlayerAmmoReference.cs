using UnityEngine;

public class PlayerAmmoReference : MonoBehaviour
{
    // Reference to the player's ammo data scriptable object
    [SerializeField] private PlayerAmmoData ammoData;

    // Getter and setter for other scripts to access the ammo data
    public PlayerAmmoData AmmoData
    {
        get => ammoData;
        set => ammoData = value;
    }

    void Awake()
    {
        // Create a runtime instance if not set in the inspector
        if (ammoData == null)
        {
            ammoData = ScriptableObject.CreateInstance<PlayerAmmoData>();

            // Set default values
            ammoData.SetMaxAmmoInMag(30);
            ammoData.SetMaxAmmoInReserve(90);
            ammoData.SetAmmoInMag(30);
            ammoData.SetAmmoInReserve(90);

            Debug.LogWarning("Created runtime PlayerAmmoData instance on " + gameObject.name);
        }
    }
}