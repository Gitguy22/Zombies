using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorSetup : MonoBehaviour
{
    // Run this to set up any doors that don't have anchors yet
    public void SetupDoors()
    {
        // Find all doors in the scene
        Door[] allDoors = FindObjectsOfType<Door>();
        int newAnchorsCreated = 0;

        foreach (Door door in allDoors)
        {
            // Check if this door already has an anchor parent
            if (door.transform.parent != null && door.transform.parent.name.Contains("_Anchor"))
            {
                // This door already has an anchor, skip it
                //Debug.Log($"Door {door.gameObject.name} already has an anchor parent. Skipping.");
                continue;
            }

            // Store original parent to maintain hierarchy
            Transform originalParent = door.transform.parent;

            // Create an empty GameObject at the door's position
            GameObject anchor = new GameObject(door.gameObject.name + "_Anchor");
            anchor.transform.position = door.transform.position;
            anchor.transform.rotation = door.transform.rotation;

            // Make the door a child of the anchor
            door.transform.SetParent(anchor.transform, true);

            // Put the anchor in the same place in hierarchy
            if (originalParent != null)
            {
                anchor.transform.SetParent(originalParent, true);
            }

            newAnchorsCreated++;
        }

        //Debug.Log($"Door setup complete. Created {newAnchorsCreated} new anchors.");
    }

    // Optional: Run automatically when the game starts
    private void Start()
    {
        SetupDoors();
    }
}