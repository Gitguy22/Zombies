using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Import TextMeshPro namespace

public class PointManager : MonoBehaviour
{
    private Dictionary<GameObject, int> playerPoints = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, TextMeshProUGUI> playerTextUI = new Dictionary<GameObject, TextMeshProUGUI>();

    public TextMeshProUGUI[] pointsTexts; // Array for each player's UI text

    public void AddPoints(GameObject owner, bool hit, bool kill, string bodyPart)
    {
        if (owner == null) return;

        // Debug logging
        Debug.Log($"AddPoints called - Owner: {owner.name}, Hit: {hit}, Kill: {kill}, BodyPart: {bodyPart}");

        if (!playerPoints.ContainsKey(owner))
        {
            playerPoints[owner] = 0;
            SetOwner(owner); // Ensure UI is set up
        }

        if (hit)
        {
            playerPoints[owner] += 10;
            Debug.Log($"Added 10 points for hit. Total: {playerPoints[owner]}");
        }

        if (kill)
        {
            int killPoints = bodyPart == "mixamorig:Head" ? 100 : 60;
            playerPoints[owner] += killPoints;
            Debug.Log($"Added {killPoints} points for kill. Total: {playerPoints[owner]}");
        }

        UpdatePointsText(owner);
    }

    public void TakePoints(GameObject owner, int pointsToTake)
    {
        playerPoints[owner] -= pointsToTake;
        UpdatePointsText(owner);
    }

    public void SetOwner(GameObject owner)
    {
        if (owner == null) return;

        if (!playerPoints.ContainsKey(owner))
        {
            playerPoints.Add(owner, 0);
        }

        AssignTextToOwner(owner);
        UpdatePointsText(owner);
    }

    public int GetPoints(GameObject owner)
    {
        return playerPoints.TryGetValue(owner, out int points) ? points : 0;
    }

    private void AssignTextToOwner(GameObject owner)
    {
        if (playerTextUI.ContainsKey(owner)) return;

        foreach (TextMeshProUGUI textUI in pointsTexts)
        {
            if (!playerTextUI.ContainsValue(textUI))
            {
                playerTextUI[owner] = textUI;
                textUI.gameObject.SetActive(true);
                Debug.Log($"Assigned text UI to {owner.name}");
                return;
            }
        }
        Debug.LogWarning($"No available text UI for {owner.name}");
    }

    private void UpdatePointsText(GameObject owner)
    {
        if (playerTextUI.TryGetValue(owner, out TextMeshProUGUI textUI))
        {
            textUI.text = $"{playerPoints[owner]}";
            Debug.Log($"Updated points text for {owner.name} to {playerPoints[owner]}");
        }
        else
        {
            Debug.LogWarning($"No text UI found for {owner.name}");
            AssignTextToOwner(owner);
        }
    }

}