using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NonAmputatableLimb : MonoBehaviour
{
    private ZombieAI zombie;

    void Awake()
    {
        zombie = transform.root.GetComponent<ZombieAI>();
    }

    public void GetHit(float damage, Vector3 hitPoint, Vector3 hitForce)
    {
        if (zombie != null && zombie.zombieHP > 0)
        {
            if (gameObject.name == "mixamorig:Head")
            {
                zombie.GetHit(damage * 2, hitPoint, hitForce);
            }
            else
            {
                zombie.GetHit(damage, hitPoint, hitForce);
            }
        }
    }
}