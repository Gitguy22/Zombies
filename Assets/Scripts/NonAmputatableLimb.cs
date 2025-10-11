using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NonAmputatableLimb : MonoBehaviour
{
    private ZombieAI zombie;

    void Awake()
    {
        zombie = FindZombieComponent(this.transform);
    }

    public void GetHit(float damage, Vector3 hitPoint, Vector3 hitForce)
    {
        if (zombie != null && zombie.zombieHP > 0)
        {
            if (zombie != null && zombie.zombieHP > 0)
            {
                // Clamp the hit force magnitude to a reasonable range
                Vector3 clampedHitForce = Vector3.ClampMagnitude(hitForce, 400f);

                if (gameObject.name == "mixamorig:Head")
                {
                    zombie.GetHit(damage * 2, hitPoint, clampedHitForce);
                }
                else
                {
                    //Debug.Log("zombie got hit");
                    zombie.GetHit(damage, hitPoint, clampedHitForce);
                }
            }
        }
    }

    private ZombieAI FindZombieComponent(Transform limbTransform)
    {

        //try traversing up the hierarchy
        Transform current = limbTransform;
        while (current != null)
        {
            zombie = current.GetComponent<ZombieAI>();
            if (zombie != null) return zombie;
            current = current.parent;
        }

        // Log the hierarchy to help debugging
        Debug.LogWarning($"Could not find ZombieAI component. Hierarchy: {GetHierarchyPath(limbTransform)}");
        return null;
    }



    private string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}