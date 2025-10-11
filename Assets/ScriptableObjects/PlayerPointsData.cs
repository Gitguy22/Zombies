using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerPointsData", menuName = "Game Data/Player Points Data")]
public class PlayerPointsData : ScriptableObject
{
    [SerializeField] int player1Points = 0;
    [SerializeField] int player2Points = 0;
    [SerializeField] int player3Points = 0;
    [SerializeField] int player4Points = 0;

    // Getters
    public int Player1Points => player1Points;
    public int Player2Points => player2Points;
    public int Player3Points => player3Points;
    public int Player4Points => player4Points;

    // Setters
    public void SetPlayer1Points(int points)
    {
        player1Points = points;
    }

    public void SetPlayer2Points(int points)
    {
        player2Points = points;
    }

    public void SetPlayer3Points(int points)
    {
        player3Points = points;
    }

    public void SetPlayer4Points(int points)
    {
        player4Points = points;
    }

    // Add points
    public void AddPlayer1Points(int pointsToAdd)
    {
        player1Points += pointsToAdd;
    }

    public void AddPlayer2Points(int pointsToAdd)
    {
        player2Points += pointsToAdd;
    }

    public void AddPlayer3Points(int pointsToAdd)
    {
        player3Points += pointsToAdd;
    }

    public void AddPlayer4Points(int pointsToAdd)
    {
        player4Points += pointsToAdd;
    }

    // Reset all player points
    public void ResetAllPoints()
    {
        player1Points = 0;
        player2Points = 0;
        player3Points = 0;
        player4Points = 0;
    }
}