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
    public GameObject bloodGushPS;

    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip limbPopSound;
    [SerializeField] AudioClip bloodGushSound;


    // Reference to round data
    private RoundData roundData;
    private GameObject roundManager;

    // Cleanup settings
    [Header("Cleanup Settings")]
    [SerializeField] float limbCleanupTime = 20f; // Time before limb disappears
    [SerializeField] float fadeOutDuration = 3f; // Time to fade out before destruction
    [SerializeField] string detachedLimbLayerName = "DeadZombie"; // Same layer as dead zombies

    private bool hasBeenRemoved = false;
    public float limbHP = 1;
    private ZombieAI zombie;
    private ParticleSystem ps;

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

        // Get the RoundData from the RoundManager
        if (roundManager != null)
        {
            RoundManager rm = roundManager.GetComponent<RoundManager>();
            if (rm != null)
            {
                roundData = rm.roundData;
            }
        }

        zombie = FindZombieComponent(this.transform);

        // Calculate limbHP based on the current round from RoundData
        if (roundData != null)
        {
            limbHP = (roundData.CurrentRound * 150) * 0.45f;
        }
        else if (roundManager != null)
        {
            // Fallback to using RoundManager if available
            RoundManager rm = roundManager.GetComponent<RoundManager>();
            if (rm != null && rm.roundData != null)
            {
                limbHP = (rm.roundData.CurrentRound * 150) * 0.45f;
            }
            else
            {
                // Default fallback
                limbHP = 150 * 0.45f;
            }
        }
        else
        {
            // Default fallback
            limbHP = 150 * 0.45f;
        }
    }

    public void GetHit(float damage, Vector3 hitPoint, Vector3 hitForce)
    {
        // Debug logs
        //Debug.Log($"Limb Hit. Current HP: {limbHP}");
        //Debug.Log($"Damage taken: {damage}");

        // Early return if limb is already removed
        if (hasBeenRemoved)
        {
            //Debug.Log("Limb already removed, no further processing.");
            return;
        }

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
                zombie.GetHit(damage, hitPoint, clampedHitForce);
            }
        }

        // Reduce limb HP if it is still alive
        if (limbHP > 0)
        {
            limbHP -= damage;
        }

        // If limb HP is 0 or less, mark it as removed
        if (limbHP <= 0)
        {
            transform.localScale = Vector3.zero;  // Destroy the limb visually
            if (ps != null)
            {
                ps.Play();
            }

            // Play blood gush sound
            if (audioSource != null && bloodGushSound != null)
            {
                audioSource.PlayOneShot(bloodGushSound);
            }

            // Play limb pop sound
            if (audioSource != null && limbPopSound != null)
            {
                audioSource.PlayOneShot(limbPopSound);
            }

            hasBeenRemoved = true;
        }

        if (wound != null)
        {
            wound.SetActive(true);
        }

        // Spawn the limb prefab
        if (limbPrefab != null && limbHP <= 0)
        {
            GameObject detachedLimb = Instantiate(limbPrefab, transform.position, transform.rotation);

            // Set layer to same as dead zombies so player doesn't collide
            int detachedLayer = LayerMask.NameToLayer(detachedLimbLayerName);
            if (detachedLayer != -1)
            {
                SetLayerRecursively(detachedLimb, detachedLayer);
            }

            // Start cleanup coroutine
            LimbCleanup cleanup = detachedLimb.AddComponent<LimbCleanup>();
            cleanup.StartCleanup(limbCleanupTime, fadeOutDuration);
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

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
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

        //Debug.LogWarning($"Could not find ZombieAI component. Hierarchy: {GetHierarchyPath(limbTransform)}");
        return null;
    }

    public void ResetLimb()
    {
        // Reset the limb's scale to its original size
        transform.localScale = Vector3.one;

        // Reset the HP based on the current round
        if (roundData != null)
        {
            limbHP = (roundData.CurrentRound * 150) * 0.45f;
        }
        else if (roundManager != null)
        {
            // Try to get the round data from the round manager
            RoundManager rm = roundManager.GetComponent<RoundManager>();
            if (rm != null && rm.roundData != null)
            {
                limbHP = (rm.roundData.CurrentRound * 150) * 0.45f;
            }
            else
            {
                // Fallback default
                limbHP = 150 * 0.45f;
            }
        }
        else
        {
            // Fallback default
            limbHP = 150 * 0.45f;
        }

        // Hide wound if present
        if (wound != null)
        {
            wound.SetActive(false);
        }

        // Reset the removed flag
        hasBeenRemoved = false;
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

// Separate component for cleaning up detached limbs
public class LimbCleanup : MonoBehaviour
{
    private Coroutine cleanupCoroutine;

    public void StartCleanup(float cleanupTime, float fadeOutDuration)
    {
        cleanupCoroutine = StartCoroutine(CleanupCoroutine(cleanupTime, fadeOutDuration));
    }

    private IEnumerator CleanupCoroutine(float cleanupTime, float fadeOutDuration)
    {
        // Wait for cleanup time
        yield return new WaitForSeconds(cleanupTime);

        // Get all renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        // Fade out materials
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - (elapsedTime / fadeOutDuration);

            // Update alpha for all materials
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;

                    // Handle transparent materials
                    if (mat.HasProperty("_Color"))
                    {
                        Color matColor = mat.GetColor("_Color");
                        matColor.a = alpha;
                        mat.SetColor("_Color", matColor);
                    }
                }
            }

            yield return null;
        }

        // Destroy the limb
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
        }
    }
}