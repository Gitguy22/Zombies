using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPointsManager : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private GameObject player1Object;
    [SerializeField] private GameObject player2Object;
    [SerializeField] private GameObject player3Object;
    [SerializeField] private GameObject player4Object;

    [Header("Data")]
    [SerializeField] private PlayerPointsData pointsData;

    private PointManager pointManager;
    private Dictionary<GameObject, int> playerIndexMap = new Dictionary<GameObject, int>();

    private void Awake()
    {
        pointsData.ResetAllPoints();
        //Debug.Log("PlayerPointsManager Awake - Finding PointManager and setting up mappings");

        // Get the PointManager
        pointManager = GetComponent<PointManager>();
        if (pointManager == null)
        {
            pointManager = FindObjectOfType<PointManager>();
            if (pointManager == null)
            {
                //Debug.LogError("PointManager not found!");
                return;
            }
            //Debug.Log("Found PointManager via FindObjectOfType");
        }
        else
        {
            //Debug.Log("Found PointManager on same GameObject");
        }

        // Map player GameObjects to indices
        if (player1Object != null)
        {
            playerIndexMap[player1Object] = 1;
            //Debug.Log($"Mapped Player 1: {player1Object.name}");
        }

        if (player2Object != null)
        {
            playerIndexMap[player2Object] = 2;
            //Debug.Log($"Mapped Player 2: {player2Object.name}");
        }

        if (player3Object != null)
        {
            playerIndexMap[player3Object] = 3;
            //Debug.Log($"Mapped Player 3: {player3Object.name}");
        }

        if (player4Object != null)
        {
            playerIndexMap[player4Object] = 4;
            //Debug.Log($"Mapped Player 4: {player4Object.name}");
        }

        // Initialize players with their current points from data
        InitializePlayerPoints();

        // Subscribe to point updates
        pointManager.SetPointsUpdateCallback(OnPointsUpdated);
        //Debug.Log("Subscribed to PointManager updates");
    }

    private void InitializePlayerPoints()
    {
        if (pointsData == null)
        {
            //Debug.LogError("PlayerPointsData is null! Points will not be saved.");
            return;
        }

        //Debug.Log("Initializing player points from PlayerPointsData");

        // Register all players and set their initial points
        if (player1Object != null)
        {
            pointManager.RegisterPlayer(player1Object, pointsData.Player1Points);
            //Debug.Log($"Registered Player 1 with {pointsData.Player1Points} points");
        }

        if (player2Object != null)
        {
            pointManager.RegisterPlayer(player2Object, pointsData.Player2Points);
            //Debug.Log($"Registered Player 2 with {pointsData.Player2Points} points");
        }

        if (player3Object != null)
        {
            pointManager.RegisterPlayer(player3Object, pointsData.Player3Points);
            //Debug.Log($"Registered Player 3 with {pointsData.Player3Points} points");
        }

        if (player4Object != null)
        {
            pointManager.RegisterPlayer(player4Object, pointsData.Player4Points);
            //Debug.Log($"Registered Player 4 with {pointsData.Player4Points} points");
        }
    }

    // Callback that receives point updates from the PointManager
    private void OnPointsUpdated(GameObject player, int points)
    {
        if (pointsData == null)
        {
            //Debug.LogError("PlayerPointsData is null! Points update cannot be saved.");
            return;
        }

        if (!playerIndexMap.TryGetValue(player, out int playerIndex))
        {
            //Debug.LogWarning($"Received points update for unregistered player: {player.name}");
            return;
        }

        //Debug.Log($"Points Updated: Player {playerIndex} now has {points} points");

        // Update the appropriate player's points in the scriptable object
        switch (playerIndex)
        {
            case 1:
                pointsData.SetPlayer1Points(points);
                //Debug.Log($"Updated Player 1 data to {points} points");
                break;
            case 2:
                pointsData.SetPlayer2Points(points);
                //Debug.Log($"Updated Player 2 data to {points} points");
                break;
            case 3:
                pointsData.SetPlayer3Points(points);
                //Debug.Log($"Updated Player 3 data to {points} points");
                break;
            case 4:
                pointsData.SetPlayer4Points(points);
                //Debug.Log($"Updated Player 4 data to {points} points");
                break;
        }
    }

    private void Update()
    {

    }

    // Helper method to get player object from index (1-4)
    public GameObject GetPlayerObject(int playerIndex)
    {
        switch (playerIndex)
        {
            case 1: return player1Object;
            case 2: return player2Object;
            case 3: return player3Object;
            case 4: return player4Object;
            default: return null;
        }
    }

    // Helper method to add points to a specific player by index
    public void AddPointsByPlayerIndex(int playerIndex, int points)
    {
        GameObject playerObj = GetPlayerObject(playerIndex);
        if (playerObj != null)
        {
            //Debug.Log($"Adding {points} points to Player {playerIndex}");
            pointManager.AddPoints(playerObj, points);
        }
        else
        {
            //Debug.LogWarning($"Tried to add points to Player {playerIndex}, but could not find player object");
        }
    }

    // Helper method to get current points for a player by index
    public int GetPointsByPlayerIndex(int playerIndex)
    {
        GameObject playerObj = GetPlayerObject(playerIndex);
        return playerObj != null ? pointManager.GetPoints(playerObj) : 0;
    }
}