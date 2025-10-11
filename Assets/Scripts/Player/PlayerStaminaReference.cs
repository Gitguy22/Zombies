using UnityEngine;

public class PlayerStaminaReference : MonoBehaviour
{
    [Header("Stamina Data Reference")]
    [SerializeField] PlayerStaminaData staminaData;

    [Header("Auto-Create Settings")]
    [SerializeField] bool createIfMissing = true;
    [SerializeField] string defaultDataName = "PlayerStaminaData";

    // Property to access the stamina data
    public PlayerStaminaData StaminaData
    {
        get
        {
            if (staminaData == null && createIfMissing)
            {
                CreateDefaultStaminaData();
            }
            return staminaData;
        }
        set
        {
            staminaData = value;
        }
    }

    private void Awake()
    {
        // Ensure we have stamina data
        if (staminaData == null && createIfMissing)
        {
            CreateDefaultStaminaData();
        }
    }

    private void CreateDefaultStaminaData()
    {
        // Try to load from Resources first
        staminaData = Resources.Load<PlayerStaminaData>($"Data/{defaultDataName}");

        if (staminaData == null)
        {
            // Create a new instance if not found
            staminaData = ScriptableObject.CreateInstance<PlayerStaminaData>();
            staminaData.name = defaultDataName;

            Debug.LogWarning($"Created temporary PlayerStaminaData. Consider creating one in your project.");
        }
    }
}