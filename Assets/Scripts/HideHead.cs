using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideHead : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera; // The specific camera to hide the head from
    public GameObject headObject; // The head GameObject with its own MeshRenderer

    [Header("Layer Settings")]
    public string hiddenLayerName = "HiddenFromPlayer"; // Name for the custom layer

    private int originalLayer;

    private void Start()
    {
        if (playerCamera == null || headObject == null)
        {
            Debug.LogError("Player camera or head object reference is missing!");
            return;
        }

        // Store the original layer to restore it if needed
        originalLayer = headObject.layer;

        // Create the hidden layer if it doesn't exist
        int hiddenLayer = LayerMask.NameToLayer(hiddenLayerName);
        if (hiddenLayer == -1)
        {
            Debug.LogError("Layer '" + hiddenLayerName + "' does not exist. Please create it in the Tags & Layers settings.");
            return;
        }

        // Assign the head to the hidden layer
        headObject.layer = hiddenLayer;

        // Exclude the hidden layer from the player camera's culling mask
        playerCamera.cullingMask &= ~(1 << hiddenLayer);
    }

    // Optional: Restore the original layer if needed
    private void OnDisable()
    {
        if (headObject != null)
        {
            headObject.layer = originalLayer;
        }
    }
}
