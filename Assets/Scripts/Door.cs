using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public int cost;
    public bool isPaidFor;
    public string displayName = "Door";

    [Header("References")]
    public GameObject triggerObject; // Reference to separate trigger object
    public LayerMask beings;
    public AudioClip open;
    public AudioClip close;

    private List<GameObject> objectsInTrigger = new List<GameObject>();
    private Animator anim;
    private AudioSource audio;
    private bool isOpen = false;
    private PointManager pointManager;
    private DoorTrigger doorTrigger;

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

        // Setup trigger
        if (triggerObject != null)
        {
            // Add the DoorTrigger component if it doesn't exist
            doorTrigger = triggerObject.GetComponent<DoorTrigger>();
            if (doorTrigger == null)
            {
                doorTrigger = triggerObject.AddComponent<DoorTrigger>();
            }

            // Setup the trigger to reference this door
            doorTrigger.SetupTrigger(this, beings);
        }
        else
        {
            Debug.LogError("Trigger object not assigned to door: " + gameObject.name);
        }
    }

    public void BuyItem(GameObject player)
    {
        // Fixed the assignment bug (= vs ==)
        if (isPaidFor == true) return; // Already unlocked

        // Check if player has enough points
        if (pointManager != null && pointManager.GetPoints(player) >= cost)
        {
            // Deduct points
            pointManager.TakePoints(player, cost);
            // Unlock the door
            isPaidFor = true;
            Debug.Log($"Player {player.name} bought {displayName} for {cost} points");
        }
        else
        {
            Debug.Log($"Not enough points to open {displayName}!");
            // Maybe play a sound for not enough points
        }

        DoorOpen();
    }

    public string GetItemName()
    {
        return displayName;
    }

    public int GetCost()
    {
        return cost;
    }

    // Methods to be called by the DoorTrigger
    public void ObjectEntered(GameObject obj)
    {
        if (isPaidFor)
        {
            // Add the object to our tracking list
            objectsInTrigger.Add(obj);
            // Only open the door if it's not already open
            if (!isOpen)
            {
                DoorOpen();
            }
        }
    }

    public void ObjectExited(GameObject obj)
    {
        if (isPaidFor)
        {
            // Remove the object from our tracking list
            objectsInTrigger.Remove(obj);
            // Only close the door if no objects remain in the trigger
            if (objectsInTrigger.Count == 0 && isOpen)
            {
                DoorClose();
            }
        }
    }

    private void DoorOpen()
    {
        Debug.Log("DoorOpen called");
        if (audio != null && open != null)
        {
            audio.clip = open;
            audio.Play();
        }

        if (anim != null)
        {
            anim.SetTrigger("openDoor");
            isOpen = true;
            Debug.Log("openDoor trigger set");
        }
    }

    private void DoorClose()
    {
        Debug.Log("DoorClose called");
        if (audio != null && close != null)
        {
            audio.clip = close;
            audio.Play();
        }

        if (anim != null)
        {
            anim.SetTrigger("closeDoor");
            isOpen = false;
            Debug.Log("closeDoor trigger set");
        }
    }

    public bool IsPaidFor()
    {
        return isPaidFor;
    }
}