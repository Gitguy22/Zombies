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

    void Start()
    {
        myAnim = GetComponent<Animator>();
        ragdollRigids = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());
        ragdollRigids.Remove(GetComponent<Rigidbody>());
        DeactivateRagdoll();
        zombieLayer = LayerMask.GetMask("Zombie"); // Make sure zombies are on this layer
    }

    void Awake()
    {
        int currentRound = roundManager.GetComponent<RoundManager>().currentRound;
        zombieHP = currentRound * 150;
        SetZombieType(currentRound);
        dead = false;
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
        if (!dead)
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
        float speed = agent.velocity.magnitude;

        myAnim.SetBool("isIdle", speed < 0.1f);
        myAnim.SetBool("isWalking", zombieType == ZombieType.Walker && speed > 0.1f);
        myAnim.SetBool("isRunning", zombieType == ZombieType.Runner && speed > 0.1f);
        myAnim.SetBool("isSprinting", zombieType == ZombieType.Sprinter && speed > 0.1f);
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
                Death(hitPoint, hitDirection);
                return true; // Return true to indicate a kill
            }
        }
        return false; // Return false if the zombie is still alive
    }

    public void Death(Vector3 hitPoint, Vector3 hitDirection)
    {
        ActivateRagdoll();
        agent.ResetPath();

        foreach (var rb in ragdollRigids)
        {
            float distance = Vector3.Distance(rb.position, hitPoint);
            if (distance < 0.5f)
            {
                rb.AddForce(hitDirection * 0.05f, ForceMode.Impulse);
            }
        }

        StartCoroutine(ResetTimer());
    }

    private IEnumerator ResetTimer()
    {
        yield return new WaitForSeconds(15.0f);
        Reset();
    }

    public void Reset()
    {
        DeactivateRagdoll();
        dead = false;
    }

    void ActivateRagdoll()
    {
        myAnim.enabled = false;

        Rigidbody closestRigidbody = null;
        float closestDistance = float.MaxValue;

        foreach (var rb in ragdollRigids)
        {
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
        myAnim.enabled = true;
        foreach (var rb in ragdollRigids) { rb.useGravity = false; rb.isKinematic = true; }
    }
}