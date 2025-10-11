using UnityEngine;
using TMPro;

public class AmmoInMagDisplay : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Assign the PlayerAmmoData scriptable object here")]
    [SerializeField] private PlayerAmmoData ammoData;

    [Header("Display Settings")]
    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Formatting Options")]
    [SerializeField] private bool useColorCoding = true;
    [SerializeField] private float lowAmmoPercentage = 0.15f; // 15% of max magazine
    [SerializeField] private string lowAmmoColor = "yellow";
    [SerializeField] private string emptyAmmoColor = "red";

    private int lastAmmoCount = -1;
    private int lowAmmoThreshold = 3; // Will be calculated in Update

    private void Start()
    {
        if (ammoText == null)
        {
            ammoText = GetComponent<TextMeshProUGUI>();
            if (ammoText == null)
            {
                Debug.LogError("No TextMeshProUGUI component found on " + gameObject.name);
                enabled = false;
                return;
            }
        }

        if (ammoData == null)
        {
            Debug.LogError("No PlayerAmmoData assigned to " + gameObject.name);
            enabled = false;
            return;
        }

        // Initial update
        UpdateAmmoText();
    }

    private void Update()
    {
        // Calculate low ammo threshold as percentage of max capacity
        lowAmmoThreshold = Mathf.RoundToInt(ammoData.MaxAmmoInMag * lowAmmoPercentage);
        UpdateAmmoText();
    }

    private void UpdateAmmoText()
    {
        int currentAmmo = ammoData.AmmoInMag;

        // Only update text if ammo has changed
        if (currentAmmo != lastAmmoCount)
        {
            lastAmmoCount = currentAmmo;

            // Format with or without color coding
            if (useColorCoding)
            {
                if (currentAmmo == 0)
                {
                    ammoText.text = $"<color={emptyAmmoColor}>{currentAmmo}</color>";
                }
                else if (currentAmmo <= lowAmmoThreshold)
                {
                    ammoText.text = $"<color={lowAmmoColor}>{currentAmmo}</color>";
                }
                else
                {
                    ammoText.text = currentAmmo.ToString();
                }
            }
            else
            {
                ammoText.text = currentAmmo.ToString();
            }
        }
    }

    // Public method that can be called to assign the ammo data at runtime if needed
    public void SetAmmoData(PlayerAmmoData newAmmoData)
    {
        ammoData = newAmmoData;
        UpdateAmmoText();
    }
}