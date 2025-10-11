using UnityEngine;
using TMPro;

public class AmmoInReserveDisplay : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Assign the PlayerAmmoData scriptable object here")]
    [SerializeField] private PlayerAmmoData ammoData;

    [Header("Display Settings")]
    [SerializeField] private TextMeshProUGUI reserveText;

    [Header("Formatting Options")]
    [SerializeField] private bool useColorCoding = true;
    [SerializeField] private float lowReservePercentage = 0.15f; // 15% of max reserve
    [SerializeField] private string lowReserveColor = "yellow";
    [SerializeField] private string emptyReserveColor = "red";

    private int lastReserveCount = -1;
    private int lowReserveThreshold = 10; // Will be calculated in Update

    private void Start()
    {
        if (reserveText == null)
        {
            reserveText = GetComponent<TextMeshProUGUI>();
            if (reserveText == null)
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
        UpdateReserveText();
    }

    private void Update()
    {
        // Calculate low reserve threshold as percentage of max capacity
        lowReserveThreshold = Mathf.RoundToInt(ammoData.MaxAmmoInReserve * lowReservePercentage);
        UpdateReserveText();
    }

    private void UpdateReserveText()
    {
        int currentReserve = ammoData.AmmoInReserve;

        // Only update text if reserve ammo has changed
        if (currentReserve != lastReserveCount)
        {
            lastReserveCount = currentReserve;

            // Format with or without color coding
            if (useColorCoding)
            {
                if (currentReserve == 0)
                {
                    reserveText.text = $"<color={emptyReserveColor}>{currentReserve}</color>";
                }
                else if (currentReserve <= lowReserveThreshold)
                {
                    reserveText.text = $"<color={lowReserveColor}>{currentReserve}</color>";
                }
                else
                {
                    reserveText.text = currentReserve.ToString();
                }
            }
            else
            {
                reserveText.text = currentReserve.ToString();
            }
        }
    }

    // Public method that can be called to assign the ammo data at runtime if needed
    public void SetAmmoData(PlayerAmmoData newAmmoData)
    {
        ammoData = newAmmoData;
        UpdateReserveText();
    }
}