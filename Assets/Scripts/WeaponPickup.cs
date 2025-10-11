using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour, IInteractable
{
    [Header("Weapon Configuration")]
    public WeaponData weaponData;

    [Header("Interaction Settings")]
    public string promptText;
    private bool isPaidFor = false;
    private float pickupTimeout = 10f;
    private float pickupTimer = 0f;
    private bool timerActive = false;

    [Header("Visual Effects")]
    public float rotationSpeed = 30f;
    public float floatAmplitude = 0.1f;
    public float floatFrequency = 1f;
    private Vector3 startPosition;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Layer Management")]
    [SerializeField] private string weaponLayerName = "Weapon";
    [SerializeField] private string interactableLayerName = "Interactable";
    private int weaponLayer;
    private int interactableLayer;

    private bool isDetached = true; // Pickup should only function when not attached to player
    private Weapon weaponComponent;

    private void Awake()
    {
        // Cache layer indices
        weaponLayer = LayerMask.NameToLayer(weaponLayerName);
        interactableLayer = LayerMask.NameToLayer(interactableLayerName);

        if (weaponLayer == -1)
            Debug.LogWarning($"Layer '{weaponLayerName}' not found in project settings!");
        if (interactableLayer == -1)
            Debug.LogWarning($"Layer '{interactableLayerName}' not found in project settings!");

        weaponComponent = GetComponent<Weapon>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (weaponData == null)
        {
            Debug.LogError("WeaponPickup is missing WeaponData!", this);
        }

        // Check attachment status immediately in Awake
        CheckAttachmentStatus();

        // Force layer to interactable when first created
        if (transform.root == transform) // If this is a root object (not parented to anything)
        {
            isDetached = true;
            SetLayerRecursively(gameObject, interactableLayer);
            if (weaponComponent != null)
            {
                weaponComponent.enabled = false;
            }
        }
    }

    void OnEnable()
    {
        // Check attachment status and update component state
        CheckAttachmentStatus();

        // Force layer to interactable when enabled if not attached to player
        if (transform.root == transform)
        {
            isDetached = true;
            SetLayerRecursively(gameObject, interactableLayer);
            if (weaponComponent != null)
            {
                weaponComponent.enabled = false;
            }
        }

        // Only proceed if detached from player
        if (!isDetached) return;

        if (weaponData != null)
        {
            promptText = $"Pickup {weaponData.weaponName}";
        }
        else
        {
            promptText = "Pickup Weapon";
        }

        startPosition = transform.position;
    }

    void Update()
    {
        // Check attachment status each frame
        CheckAttachmentStatus();

        // Skip update if attached to player
        if (!isDetached) return;

        // Apply visual effects for display
        ApplyVisualEffects();

        // Handle timeout for mystery box weapons
        if (timerActive)
        {
            pickupTimer += Time.deltaTime;
            if (pickupTimer >= pickupTimeout)
            {
                StartCoroutine(DisappearEffect());
            }
        }
    }

    // Check if this pickup is NOT attached to a player
    private void CheckAttachmentStatus()
    {
        // Find the root parent
        Transform rootParent = transform.root;

        // Check if root has player components
        bool isAttachedToPlayer = rootParent.GetComponent<WeaponManager>() != null ||
                                  rootParent.GetComponent<PlayerMovement>() != null ||
                                  rootParent.GetComponent<PlayerLook>() != null;

        // Update detached status (inverse of attached)
        if (isDetached == isAttachedToPlayer)
        {
            isDetached = !isAttachedToPlayer;
            UpdateComponentState();
        }
    }

    // Update component state based on attachment status
    private void UpdateComponentState()
    {
        // Set the layer appropriately
        SetLayerRecursively(gameObject, isDetached ? interactableLayer : weaponLayer);

        // Enable/disable the weapon component based on attachment
        if (weaponComponent != null)
        {
            weaponComponent.enabled = !isDetached;
        }
    }

    // Helper method to set layer recursively on all children
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void ApplyVisualEffects()
    {
        // Rotate the weapon
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Make it float up and down
        float newY = startPosition.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private IEnumerator DisappearEffect()
    {
        // Fade out effect
        float duration = 1.0f;
        float elapsed = 0f;

        // Get all renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        List<Material> materials = new List<Material>();

        // Collect all materials and store original colors
        Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();
        foreach (Renderer r in renderers)
        {
            foreach (Material m in r.materials)
            {
                if (!originalColors.ContainsKey(m))
                {
                    originalColors.Add(m, m.color);
                    materials.Add(m);
                }
            }
        }

        // Fade out
        while (elapsed < duration)
        {
            float t = elapsed / duration;

            foreach (Material m in materials)
            {
                Color originalColor = originalColors[m];
                Color newColor = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(originalColor.a, 0f, t));
                m.color = newColor;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Destroy the object
        Destroy(gameObject);
    }

    public void StartPickupTimer()
    {
        timerActive = true;
        pickupTimer = 0f;
    }

    public void BuyItem(GameObject player)
    {
        // Only allow pickup if detached from player
        if (!isDetached) return;

        if (weaponData == null) return;

        WeaponManager weaponManager = player.GetComponent<WeaponManager>();
        if (weaponManager != null)
        {
            bool success = weaponManager.PickupWeapon(weaponData);

            if (success)
            {
                // Play sound
                if (weaponData.pickupSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(weaponData.pickupSound);
                }

                isPaidFor = true;

                // Destroy this pickup
                Destroy(gameObject);
            }
        }
    }

    public string GetItemName()
    {
        return promptText;
    }

    public int GetCost()
    {
        return 0; // No cost for picking up, cost is handled by wall buy or mystery box
    }

    public bool IsPaidFor()
    {
        return isPaidFor;
    }
}