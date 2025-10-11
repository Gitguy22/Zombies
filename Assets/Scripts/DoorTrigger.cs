using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    private Door doorReference;
    private LayerMask targetLayers;
    private LayerMask players;

    public void SetupTrigger(Door door, LayerMask layers)
    {
        doorReference = door;
        targetLayers = layers;

        // Make sure this object has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError("Trigger object has no collider: " + gameObject.name);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (doorReference != null && ((1 << other.gameObject.layer) & targetLayers.value) != 0)
        {
            doorReference.ObjectEntered(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (doorReference != null && ((1 << other.gameObject.layer) & targetLayers.value) != 0)
        {
            doorReference.ObjectExited(other.gameObject);
        }
    }
}
