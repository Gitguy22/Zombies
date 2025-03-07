using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBuy : MonoBehaviour , IInteractable
{
    [Header("WallBuy Settings")]
    public int cost;
    public bool isPaidFor;
    public string displayName = " ";

    [Header("References")]
    public AudioClip purchase;

    private Animator anim;
    private AudioSource audio;
    private PointManager pointManager;
    // Start is called before the first frame update
    private void Start()
    {
        // Find the PointManager in the scene
        pointManager = FindObjectOfType<PointManager>();
        if (pointManager == null)
        {
            Debug.LogError("PointManager not found in scene!");
        }

        // Get references
        anim = GetComponent<Animator>();
        audio = GetComponent<AudioSource>();
    }

    public void BuyItem(GameObject player)
    {
        if (isPaidFor == true) return; // Already unlocked

        // Check if player has enough points
        if (pointManager != null && pointManager.GetPoints(player) >= cost)
        {
            // Deduct points
            pointManager.TakePoints(player, cost);
            isPaidFor = true;
            Debug.Log($"Player {player.name} bought {displayName} for {cost} points");
        }
        else
        {
            Debug.Log($"Not enough points to open {displayName}!");
            // Maybe play a sound for not enough points
        }

        GiveItem();
    }

    public string GetItemName()
    {
        return displayName;
    }

    public int GetCost()
    {
        return cost;
    }

    public void GiveItem()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool IsPaidFor()
    {
        return isPaidFor;
    }
}
