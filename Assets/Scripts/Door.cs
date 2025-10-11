using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Power System")]
    [SerializeField] PowerSystemData powerSystemData;
    [SerializeField] bool requiresPower = false;
    [SerializeField] bool autoOpenOnPower = true;
    [SerializeField] string noPowerPrompt = "Requires power";

    [Header("Door Settings")]
    public int cost;
    public bool isPaidFor;
    public string displayName = "Door";
    public string promptText;

    [Header("Linked Doors")]
    [SerializeField] private List<Door> linkedDoors = new List<Door>();
    [SerializeField] private bool openLinkedDoorsOnPurchase = true;

    [Header("Auto Setup Children")]
    [SerializeField] bool setupChildrenAsInteractable = true;
    [SerializeField] LayerMask interactableLayer = 1 << 6; // Default to layer 6

    [Header("References")]
    public GameObject triggerObject; // Reference to separate trigger object
    public LayerMask beings;
    public LayerMask players;
    public AudioClip open;
    public AudioClip close;

    private List<GameObject> objectsInTrigger = new List<GameObject>();
    private Animator anim;
    private AudioSource doorAudio;
    private bool isOpen = false;
    private PointManager pointManager;
    private PlayerPointsManager playerPointsManager;
    private DoorTrigger doorTrigger;

    // Start is called before the first frame update
    private void Start()
    {
        UpdatePromptText();

        // Find the PointManager in the scene
        pointManager = FindObjectOfType<PointManager>();
        if (pointManager == null)
        {
            //Debug.LogError("PointManager not found in scene!");
        }

        // Find the PlayerPointsManager in the scene
        playerPointsManager = FindObjectOfType<PlayerPointsManager>();
        if (playerPointsManager == null)
        {
            //Debug.Log("PlayerPointsManager not found. Falling back to direct PointManager usage.");
        }

        // Get references
        anim = GetComponent<Animator>();
        doorAudio = GetComponent<AudioSource>();

        // Setup children as interactable
        if (setupChildrenAsInteractable)
        {
            SetupChildrenInteractable();
        }

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
            //Debug.LogError("Trigger object not assigned to door: " + gameObject.name);
        }

        // Subscribe to power events if door requires power
        if (requiresPower && powerSystemData != null)
        {
            powerSystemData.OnPowerStateChanged.AddListener(OnPowerStateChanged);
            // Check initial power state
            OnPowerStateChanged(powerSystemData.IsPowerOn);
        }
    }

    private void SetupChildrenInteractable()
    {
        // Get the layer number from the LayerMask
        int layerNumber = GetLayerFromMask(interactableLayer);

        // Get all child transforms (excluding this transform)
        Transform[] allChildren = GetComponentsInChildren<Transform>();

        foreach (Transform child in allChildren)
        {
            // Skip self and skip trigger object
            if (child == transform || child.gameObject == triggerObject)
                continue;

            // Set the child to interactable layer
            child.gameObject.layer = layerNumber;

            //Debug.Log($"Setup {child.name} as interactable on layer {layerNumber}");
        }
    }

    private int GetLayerFromMask(LayerMask layerMask)
    {
        int layerNumber = 0;
        int layer = layerMask.value;
        while (layer > 1)
        {
            layer = layer >> 1;
            layerNumber++;
        }
        return layerNumber;
    }

    private void OnDestroy()
    {
        // Unsubscribe from power events
        if (powerSystemData != null)
        {
            powerSystemData.OnPowerStateChanged.RemoveListener(OnPowerStateChanged);
        }
    }

    private void UpdatePromptText()
    {
        if (requiresPower && powerSystemData != null && !powerSystemData.IsPowerOn)
        {
            promptText = noPowerPrompt;
        }
        else
        {
            promptText = $"Buy {displayName} for {cost}";
        }
    }

    private void OnPowerStateChanged(bool isPowerOn)
    {
        UpdatePromptText();

        // Auto-open door if configured and power just turned on
        if (isPowerOn && requiresPower && autoOpenOnPower && !isPaidFor)
        {
            isPaidFor = true;
            //Debug.Log($"{displayName} automatically opened due to power activation");

            // Open if there are beings in trigger area
            if (objectsInTrigger.Count > 0)
            {
                DoorOpen();
            }
        }
    }

    public void BuyItem(GameObject player)
    {
        if (isPaidFor == true) return;

        // Check if power is required but not available
        if (requiresPower && powerSystemData != null && !powerSystemData.IsPowerOn)
        {
            //Debug.Log($"Cannot open {displayName} - power is required!");
            return;
        }

        bool hasEnoughPoints = false;

        if (playerPointsManager != null)
        {
            int playerPoints = pointManager.GetPoints(player);
            hasEnoughPoints = (playerPoints >= cost);
        }
        else if (pointManager != null)
        {
            hasEnoughPoints = pointManager.GetPoints(player) >= cost;
        }

        if (hasEnoughPoints)
        {
            if (pointManager != null)
            {
                pointManager.TakePoints(player, cost);
            }

            isPaidFor = true;
            //Debug.Log($"Player {player.name} bought {displayName} for {cost} points");

            // Handle linked doors
            UnlockLinkedDoors();

            DoorOpen();
        }
        else
        {
            //Debug.Log($"Not enough points to open {displayName}!");
        }
    }

    // New method to unlock linked doors
    private void UnlockLinkedDoors()
    {
        if (linkedDoors.Count > 0)
        {
            foreach (Door linkedDoor in linkedDoors)
            {
                if (linkedDoor != null && !linkedDoor.isPaidFor)
                {
                    linkedDoor.MarkAsPaidFor(openLinkedDoorsOnPurchase);
                    //Debug.Log($"Linked door {linkedDoor.displayName} was automatically unlocked");
                }
            }
        }
    }

    // New public method to mark a door as paid for from other scripts
    public void MarkAsPaidFor(bool shouldOpen = false)
    {
        if (!isPaidFor)
        {
            isPaidFor = true;
            //Debug.Log($"{displayName} was automatically marked as paid for");

            // Recursively unlock any doors linked to this one
            UnlockLinkedDoors();

            if (shouldOpen)
            {
                DoorOpen();
            }
        }
    }

    public string GetItemName()
    {
        return promptText;
    }

    public int GetCost()
    {
        return cost;
    }

    public void ObjectEntered(GameObject obj)
    {
        if (isPaidFor)
        {
            objectsInTrigger.Add(obj);
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
            objectsInTrigger.Remove(obj);
            if (objectsInTrigger.Count == 0 && isOpen)
            {
                DoorClose();
            }
        }
    }

    // Made public for access from other scripts
    public void DoorOpen()
    {
        //Debug.Log("DoorOpen called");
        if (doorAudio != null && open != null)
        {
            doorAudio.clip = open;
            doorAudio.Play();
        }

        if (anim != null)
        {
            anim.SetTrigger("openDoor");
            isOpen = true;
            //Debug.Log("openDoor trigger set");
        }
    }

    // Made public for access from other scripts
    public void DoorClose()
    {
        //Debug.Log("DoorClose called");
        if (doorAudio != null && close != null)
        {
            doorAudio.clip = close;
            doorAudio.Play();
        }

        if (anim != null)
        {
            anim.SetTrigger("closeDoor");
            isOpen = false;
            //Debug.Log("closeDoor trigger set");
        }
    }

    public bool IsPaidFor()
    {
        return isPaidFor;
    }
}