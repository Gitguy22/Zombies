using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    Animator myAnim;
    public NavMeshAgent agent;
    public GameObject player;
    public GameObject roundManager;

    public float zombieHP;
    public bool dead;
    List<Rigidbody> ragdollRigids;

    private enum ZombieType { Walker, Runner, Sprinter }
    private ZombieType zombieType;

    private Vector3 lastHitPoint;
    private Vector3 lastHitForce;

    private float walkerSpeed = 1.0f;
    private float runnerSpeed = 2.5f;
    private float sprinterSpeed = 5.0f;
    private float playerSpeed = 6.0f;

    // Spacing parameters
    private float spacingRadius = 3f;
    private float spacingForce = 3f;
    private LayerMask zombieLayer;

    // Coroutine reference for death timer
    private Coroutine deathCoroutine;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (roundManager == null)
        {
            roundManager = GameObject.FindGameObjectWithTag("RoundManager");
        }

        myAnim = GetComponent<Animator>();

        // Always reinitialize the ragdollRigids list
        ragdollRigids = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());

        // Remove the main rigidbody if there is one
        Rigidbody mainRigidbody = GetComponent<Rigidbody>();
        if (mainRigidbody != null && ragdollRigids.Contains(mainRigidbody))
        {
            ragdollRigids.Remove(mainRigidbody);
        }

        DeactivateRagdoll();

        zombieLayer = LayerMask.GetMask("Zombie");

        InitializeZombieProperties();
    }

    void InitializeZombieProperties()
    {
        if (roundManager != null)
        {
            int currentRound = roundManager.GetComponent<RoundManager>().currentRound;
            zombieHP = currentRound * 150;
            SetZombieType(currentRound);
        }
        else
        {
            zombieHP = 150;  // Default value if roundManager is not found
            SetZombieType(1);
        }

        dead = false;
    }

    // This method is called when the zombie is retrieved from the pool
    public void ResetZombie()
    {
        // Stop any active death coroutines
        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
            deathCoroutine = null;
        }

        // Reset properties
        DeactivateRagdoll();
        dead = false;

        // Re-initialize the zombie
        Initialize();

        // Make sure the NavMeshAgent is enabled
        if (agent != null)
        {
            agent.enabled = true;
        }
    }

    void SetZombieType(int round)
    {
        float runnerChance = Mathf.Clamp01((round - 5) / 35f);
        float sprinterChance = Mathf.Clamp01((round - 30) / 10f);

        float roll = Random.value;
        if (roll < sprinterChance * 0.75f)
        {
            zombieType = ZombieType.Sprinter;
            agent.speed = Mathf.Min(sprinterSpeed, playerSpeed - 0.5f);
        }
        else if (roll < runnerChance * 0.25f + sprinterChance * 0.75f)
        {
            zombieType = ZombieType.Runner;
            agent.speed = Mathf.Min(runnerSpeed, playerSpeed - 0.5f);
        }
        else
        {
            zombieType = ZombieType.Walker;
            agent.speed = walkerSpeed;
        }
    }

    void Update()
    {
        if (!dead && player != null && agent != null && agent.enabled)
        {
            Vector3 targetPosition = player.transform.position;
            Vector3 spacingOffset = CalculateSpacingOffset();

            // Apply spacing to the target position
            targetPosition += spacingOffset;

            agent.SetDestination(targetPosition);
            UpdateAnimations();
        }
    }

    Vector3 CalculateSpacingOffset()
    {
        Vector3 offset = Vector3.zero;
        Collider[] nearbyZombies = Physics.OverlapSphere(transform.position, spacingRadius, zombieLayer);

        foreach (Collider zombie in nearbyZombies)
        {
            if (zombie.gameObject != gameObject)
            {
                Vector3 directionToNeighbor = transform.position - zombie.transform.position;
                float distance = directionToNeighbor.magnitude;
                float normalizedForce = 1f - (distance / spacingRadius);
                offset += directionToNeighbor.normalized * normalizedForce * spacingForce;
            }
        }

        return offset.normalized;
    }

    void UpdateAnimations()
    {
        if (myAnim != null)
        {
            float speed = agent.velocity.magnitude;

            myAnim.SetBool("isIdle", speed < 0.1f);
            myAnim.SetBool("isWalking", zombieType == ZombieType.Walker && speed > 0.1f);
            myAnim.SetBool("isRunning", zombieType == ZombieType.Runner && speed > 0.1f);
            myAnim.SetBool("isSprinting", zombieType == ZombieType.Sprinter && speed > 0.1f);
        }
    }

    public bool GetHit(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        Debug.Log($"ZombieAI GetHit - Current HP: {zombieHP}, Damage: {damage}");

        if (zombieHP > 0)
        {
            zombieHP -= damage;
            Debug.Log($"After damage - HP: {zombieHP}");

            if (zombieHP <= 0)
            {
                Debug.Log("Zombie died from this hit");
                dead = true;
                lastHitPoint = hitPoint;
                lastHitForce = hitDirection;
                Death(hitPoint, hitDirection);
                return true; // Return true to indicate a kill
            }
        }
        return false; // Return false if the zombie is still alive
    }

    public void Death(Vector3 hitPoint, Vector3 hitDirection)
    {
        // Disable NavMeshAgent to prevent movement
        if (agent != null)
        {
            agent.enabled = false;
        }

        ActivateRagdoll();

        // Apply force to ragdoll parts
        foreach (var rb in ragdollRigids)
        {
            float distance = Vector3.Distance(rb.position, hitPoint);
            if (distance < 0.5f)
            {
                rb.AddForce(hitDirection * 0.05f, ForceMode.Impulse);
            }
        }

        // Start the death timer coroutine
        deathCoroutine = StartCoroutine(DeathTimer());
    }

    private IEnumerator DeathTimer()
    {
        yield return new WaitForSeconds(15.0f);

        // Return this zombie to the pool
        if (ZombiePool.Instance != null)
        {
            ZombiePool.Instance.ReturnZombie(gameObject);
        }
        else
        {
            // If there's no pool, just destroy the zombie
            Destroy(gameObject);
        }
    }

    void ActivateRagdoll()
    {
        if (myAnim != null)
        {
            myAnim.enabled = false;
        }

        // Check if the list exists and has elements
        if (ragdollRigids == null || ragdollRigids.Count == 0)
        {
            ragdollRigids = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());
            ragdollRigids.Remove(GetComponent<Rigidbody>());
        }

        Rigidbody closestRigidbody = null;
        float closestDistance = float.MaxValue;

        foreach (var rb in ragdollRigids)
        {
            if (rb == null) continue; // Skip if rigidbody is null

            rb.useGravity = true;
            rb.isKinematic = false;

            float distance = Vector3.Distance(rb.position, lastHitPoint);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestRigidbody = rb;
            }
        }

        if (closestRigidbody != null)
        {
            closestRigidbody.AddForce(lastHitForce, ForceMode.Impulse);
        }
    }

    void DeactivateRagdoll()
    {
        if (myAnim != null)
        {
            myAnim.enabled = true;
        }

        // Check if the list exists and has elements before trying to use it
        if (ragdollRigids != null && ragdollRigids.Count > 0)
        {
            foreach (var rb in ragdollRigids)
            {
                if (rb != null) // Additional null check for each rigidbody
                {
                    rb.useGravity = false;
                    rb.isKinematic = true;
                }
            }
        }
        else
        {
            // Reinitialize the list if it's null or empty
            ragdollRigids = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());
            ragdollRigids.Remove(GetComponent<Rigidbody>());

            // Then deactivate each rigidbody
            foreach (var rb in ragdollRigids)
            {
                if (rb != null)
                {
                    rb.useGravity = false;
                    rb.isKinematic = true;
                }
            }
        }
    }
}