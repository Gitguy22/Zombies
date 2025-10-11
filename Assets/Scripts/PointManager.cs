using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class PointManager : MonoBehaviour
{
    private Dictionary<GameObject, int> playerPoints = new Dictionary<GameObject, int>();

    [Header("Point Values")]
    [SerializeField] private int hitPoints = 10;
    [SerializeField] private int killPoints = 60;
    [SerializeField] private int headShotPoints = 100;
    [SerializeField] private int shotgunMultikillBonus = 25; // Additional bonus for shotgun multikills

    [Header("Point Indicators")]
    [SerializeField] private GameObject pointPopupPrefab; // Floating point text prefab
    [SerializeField] private float pointPopupDuration = 1.5f; // How long the popup stays visible

    // Callback for any systems that need to know about point updates
    private Action<GameObject, int> pointsUpdateCallback;

    // Method to set a callback for points updates
    public void SetPointsUpdateCallback(Action<GameObject, int> callback)
    {
        pointsUpdateCallback = callback;
    }

    // Register a player with the point system
    public void RegisterPlayer(GameObject player, int initialPoints = 0)
    {
        if (player == null) return;

        if (!playerPoints.ContainsKey(player))
        {
            playerPoints[player] = initialPoints;
            UpdatePointsDisplay(player);
        }
    }

    private void Start()
    {
        // Initialize any existing player points
        foreach (var player in playerPoints.Keys)
        {
            UpdatePointsDisplay(player);
        }
    }

    public void AddPoints(GameObject owner, bool hit, bool kill, string bodyPart)
    {
        if (owner == null) return;

        // Debug logging
        //Debug.Log($"AddPoints called - Owner: {owner.name}, Hit: {hit}, Kill: {kill}, BodyPart: {bodyPart}");

        if (!playerPoints.ContainsKey(owner))
        {
            playerPoints[owner] = 0;
        }

        int pointsAdded = 0;

        if (hit)
        {
            playerPoints[owner] += hitPoints;
            pointsAdded += hitPoints;
            //Debug.Log($"Added {hitPoints} points for hit. Total: {playerPoints[owner]}");
        }

        if (kill)
        {
            // Special handling for different kill types
            int killReward = killPoints;

            if (bodyPart == "mixamorig:Head")
            {
                killReward = headShotPoints;
            }
            else if (bodyPart == "shotgun_multikill")
            {
                // Additional bonus for multikills with shotgun
                killReward += shotgunMultikillBonus;
            }

            playerPoints[owner] += killReward;
            pointsAdded += killReward;
            //Debug.Log($"Added {killReward} points for kill. Type: {bodyPart}. Total: {playerPoints[owner]}");
        }

        UpdatePointsDisplay(owner);

        // Show point popup if configured
        if (pointPopupPrefab != null && pointsAdded > 0)
        {
            ShowPointsPopup(owner.transform.position, pointsAdded);
        }
    }

    // Overload to directly add a specific amount of points
    public void AddPoints(GameObject owner, int pointsToAdd)
    {
        if (owner == null || pointsToAdd <= 0) return;

        if (!playerPoints.ContainsKey(owner))
        {
            playerPoints[owner] = 0;
        }

        playerPoints[owner] += pointsToAdd;
        //Debug.Log($"Added {pointsToAdd} points directly. Total: {playerPoints[owner]}");

        UpdatePointsDisplay(owner);

        // Show point popup if configured
        if (pointPopupPrefab != null)
        {
            ShowPointsPopup(owner.transform.position, pointsToAdd);
        }
    }

    public void TakePoints(GameObject owner, int pointsToTake)
    {
        if (owner == null || pointsToTake <= 0) return;

        if (!playerPoints.ContainsKey(owner))
        {
            playerPoints[owner] = 0;
        }

        playerPoints[owner] -= pointsToTake;
        //Debug.Log($"Deducted {pointsToTake} points. Total: {playerPoints[owner]}");

        UpdatePointsDisplay(owner);
    }

    public int GetPoints(GameObject owner)
    {
        return playerPoints.TryGetValue(owner, out int points) ? points : 0;
    }

    private void UpdatePointsDisplay(GameObject owner)
    {
        // Use callback if available for any system that needs point updates
        if (pointsUpdateCallback != null)
        {
            pointsUpdateCallback(owner, playerPoints[owner]);
        }
    }

    private void ShowPointsPopup(Vector3 position, int pointsAmount)
    {
        GameObject popup = Instantiate(pointPopupPrefab, position + Vector3.up, Quaternion.identity);
        TextMeshPro textMesh = popup.GetComponent<TextMeshPro>();

        if (textMesh != null)
        {
            // Format based on points amount
            string pointsText = pointsAmount.ToString("+0;-0");
            textMesh.text = pointsText;

            // Scale color based on amount
            if (pointsAmount >= 100)
            {
                textMesh.color = Color.yellow; // Large points in yellow
            }
            else if (pointsAmount >= 50)
            {
                textMesh.color = Color.green; // Medium points in green
            }

            // Destroy after duration
            Destroy(popup, pointPopupDuration);
        }
    }
}