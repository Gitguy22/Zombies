using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Limb : MonoBehaviour
{
    [SerializeField] Limb[] childLimbs;
    [SerializeField] GameObject limbPrefab;
    [SerializeField] GameObject wound;
    [SerializeField] GameObject objectPool;
    [SerializeField] GameObject player;
    private GameObject roundManager;
    public GameObject bloodGushPS;

    private bool hasBeenRemoved = false;

    public float limbHP = 1;

    private ZombieAI zombie;

    private ParticleSystem ps; // Declare the ParticleSystem as a class variable

    void Start()
    {
        if (bloodGushPS == null)
        {
            return;
        }

        // Get the ParticleSystem component
        ps = bloodGushPS.GetComponent<ParticleSystem>();

        if (wound != null)
        {
            wound.SetActive(false);
        }
    }

    void Awake()
    {
        roundManager = GameObject.Find("Round Manager");
        zombie = transform.root.GetComponent<ZombieAI>();
        limbHP = (roundManager.GetComponent<RoundManager>().currentRound * 150) * 0.45f;
    }

    public void GetHit(float damage, Vector3 hitPoint, Vector3 hitForce)
    {
        // Debug logs
        Debug.Log($"Limb Hit. Current HP: {limbHP}");
        Debug.Log($"Damage taken: {damage}");

        // Early return if limb is already removed
        if (hasBeenRemoved)
        {
            Debug.Log("Limb already removed, no further processing.");
            return;
        }

        // Call zombie's GetHit
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

        // Reduce limb HP if it is still alive
        if (limbHP > 0)
        {
            limbHP -= damage;
           // Debug.Log($"Limb still intact. Remaining HP: {limbHP}");
        }

        // If limb HP is 0 or less, mark it as removed
        if (limbHP <= 0)
        {
            transform.localScale = Vector3.zero;  // Destroy the limb visually
            if (ps != null)
            {
                ps.Play();
            }
            //Debug.Log("Limb destroyed.");
            hasBeenRemoved = true;
        }

        // Show the wound effect if available
        if (wound != null)
        {
            wound.SetActive(true);
        }

        // Spawn the limb prefab
        if (limbPrefab != null && limbHP <= 0)
        {
            Instantiate(limbPrefab, transform.position, transform.rotation);
        }

        // Propagate damage to child limbs
        if (childLimbs.Length > 0)
        {
            foreach (Limb limb in childLimbs)
            {
                if (limb != null)
                {
                    limb.GetHit(damage, hitPoint, hitForce);
                }
            }
        }
    }
}