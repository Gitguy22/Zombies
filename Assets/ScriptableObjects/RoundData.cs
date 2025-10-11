using UnityEngine;

[CreateAssetMenu(fileName = "NewRoundData", menuName = "Game Data/Round Data")]
public class RoundData : ScriptableObject
{
    [SerializeField] private int currentRound = 0;
    [SerializeField] private bool isRoundActive = false;

    // Getters
    public int CurrentRound => currentRound;
    public bool IsRoundActive => isRoundActive;

    // Setters
    public void SetCurrentRound(int round)
    {
        currentRound = round;
    }

    public void SetRoundActive(bool active)
    {
        isRoundActive = active;
    }

    // Reset data to default values
    public void ResetRoundData()
    {
        currentRound = 0;
        isRoundActive = false;
    }
}