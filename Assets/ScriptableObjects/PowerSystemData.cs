using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "PowerSystemData", menuName = "Game/Power System Data")]
public class PowerSystemData : ScriptableObject
{
    [Header("Power State")]
    [SerializeField] bool isPowerOn = false;
    [SerializeField] bool startWithPowerOn = false; // Add this line

    [Header("Events")]
    public UnityEvent<bool> OnPowerStateChanged;

    // Property to get power state
    public bool IsPowerOn => isPowerOn;

    // Initialize state when the game starts
    void OnEnable()
    {
        // Set initial state based on startWithPowerOn
        isPowerOn = startWithPowerOn;
    }

    // Method to toggle power
    public void SetPowerState(bool state)
    {
        if (isPowerOn != state)
        {
            isPowerOn = state;
            OnPowerStateChanged?.Invoke(isPowerOn);
        }
    }

    // Reset power state (useful for new games)
    public void ResetPowerState()
    {
        isPowerOn = startWithPowerOn; // Modified to use startWithPowerOn
        OnPowerStateChanged?.Invoke(isPowerOn);
    }
}