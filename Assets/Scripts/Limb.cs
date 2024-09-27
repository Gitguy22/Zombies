using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Limb : MonoBehaviour
{
    [SerializeField] Limb[] childLimbs;
    [SerializeField] GameObject limbPrefab;
    [SerializeField] GameObject wound;

    private bool hasBeenHit = false; // Track if this limb has already been hit

    void Start()
    {
        if (wound != null)
        {
            wound.SetActive(false);
        }
    }

    public void GetHit()
    {
        Debug.Log(hasBeenHit);
        if (hasBeenHit)
        {
            return; // Avoid multiple hits
        } else{
            Debug.Log("Limb Hit");

            // Mark this limb as hit
            hasBeenHit = true;
        }
        
        

        // Propagate the hit effect to child limbs, but do not trigger prefab spawning
        if (childLimbs.Length > 0)
        {
            foreach (Limb limb in childLimbs)
            {
                if (limb != null)
                {
                    limb.GetHit(); // Call GetHit on child limbs
                }
            }
        }

        if (wound != null)
        {
            wound.SetActive(true);
        }

        if (limbPrefab != null)
        {
            Instantiate(limbPrefab, transform.position, transform.rotation);
        }

        // Set the scale to zero to simulate amputation
        transform.localScale = Vector3.zero;

        // Optionally destroy this limb after hit
        Destroy(this);
    }
}