using UnityEngine;
using PlayerStats;

[CreateAssetMenu(fileName = "NewStatModifier", menuName = "Game Data/Stat Modifier")]
public class StatModifierData : ScriptableObject
{
    [Header("Modifier Settings")]
    [SerializeField] string modifierName = "New Modifier";
    [SerializeField] string modifierID = "";
    [SerializeField] StatType statType = StatType.Health;
    [SerializeField] ModifierType modifierType = ModifierType.Flat;
    [SerializeField] float value = 0f;

    [Header("Advanced Settings")]
    [SerializeField] int priority = 0;
    [SerializeField] bool isStackable = true;
    [SerializeField] float duration = 0f;
    [SerializeField] string source = "Unknown";

    [Header("Visual/Audio")]
    [SerializeField] Sprite icon;
    [SerializeField] Color color = Color.white;
    [SerializeField] AudioClip applySound;
    [SerializeField] AudioClip removeSound;

    // Properties
    public string ModifierName => modifierName;
    public string ModifierID => string.IsNullOrEmpty(modifierID) ? name : modifierID;
    public Sprite Icon => icon;
    public Color Color => color;
    public AudioClip ApplySound => applySound;
    public AudioClip RemoveSound => removeSound;

    // Create a StatModifier instance from this data
    public StatModifier CreateModifier(string customSource = null)
    {
        string finalSource = string.IsNullOrEmpty(customSource) ? source : customSource;

        return new StatModifier(
            ModifierID,
            statType,
            modifierType,
            value,
            priority,
            isStackable,
            duration,
            finalSource
        );
    }

    // Validate the data
    private void OnValidate()
    {
        // Auto-generate ID if empty
        if (string.IsNullOrEmpty(modifierID))
        {
            modifierID = name.Replace(" ", "_").ToLower();
        }

        // Ensure priority is reasonable
        priority = Mathf.Clamp(priority, -100, 100);

        // Ensure duration is non-negative
        duration = Mathf.Max(0, duration);
    }
}