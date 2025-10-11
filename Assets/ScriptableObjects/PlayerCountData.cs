using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCountData", menuName = "Game Data/Player Count Data")]
public class PlayerCountData : ScriptableObject
{
    [SerializeField] int amountOfPlayers = 0;
    [SerializeField] int maxAmountOfPlayers = 4;

    // Getters
    public int AmountOfPlayers => amountOfPlayers;
    public int MaxAmountOfPlayers => maxAmountOfPlayers;

    // Setters
    public void SetAmountOfPlayers(int value)
    {
        amountOfPlayers = Mathf.Clamp(value, 0, maxAmountOfPlayers);

        // If you want to add an event system:
        // OnPlayerCountChanged?.Invoke(amountOfPlayers);
    }

    public void SetMaxAmountOfPlayers(int value)
    {
        maxAmountOfPlayers = Mathf.Max(1, value);

        // Ensure current player count doesn't exceed new maximum
        if (amountOfPlayers > maxAmountOfPlayers)
        {
            SetAmountOfPlayers(maxAmountOfPlayers);
        }
    }

    // Shorthand property to easily set max players
    public int MaxPlayers
    {
        get => maxAmountOfPlayers;
        set => SetMaxAmountOfPlayers(value);
    }

    // Reset all data
    public void Reset()
    {
        amountOfPlayers = 0;
    }
}