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

    // Audio Components
    private AudioSource audioSource;
    [Header("Zombie Sounds")]
    [Tooltip("Sounds played when zombie takes damage")]
    public AudioClip[] damageSounds;
    [Tooltip("Sounds played when zombie attacks")]
    public AudioClip[] attackSounds;
    [Tooltip("Ambient sounds played while chasing")]
    public AudioClip[] chaseSounds;
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    private float chaseAudioTimer = 0f;
    private float minChaseAudioDelay = 3f;
    private float maxChaseAudioDelay = 8f;
    private float nextChaseAudioTime = 0f;

    public float zombieHP;
    public bool dead;
    List<Rigidbody> ragdollRigids;

    private enum ZombieType { Walker, Runner, Sprinter }
    private ZombieType zombieType;

    private Vector3 lastHitPoint;
    private Vector3 lastHitForce;

    // Base speeds for zombie types
    private float walkerBaseSpeed = 0.8f;
    private float runnerBaseSpeed = 2.0f;
    private float sprinterBaseSpeed = 4.5f;
    private float playerSpeed = 6.0f;

    // Speed variation for each zombie
    private float individualSpeedMultiplier;
    private float speedVariation = 0.4f; // ±40% variation

    // Preferred position relative to player
    private Vector3 preferredOffset;
    private float attackRange = 1.6f;

    // Avoidance preference
    private bool preferRightAvoidance;
    private float avoidanceAngle = 10f;

    // Spacing parameters
    private float spacingRadius = 3f;
    private float spacingForce = 3f;
    private LayerMask zombieLayer;

    // Attack parameters
    private float attackDistance = 1.8f;
    private float minAttackCooldown = 1.0f;
    private float attackCooldown = 0f;
    private int preferredAttackType;
    private float attackSpeedMultiplier = 1.0f;
    private bool isAttacking = false;

    // Coroutine reference for death timer
    private Coroutine deathCoroutine;

    void Awake()
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

        // Initialize audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // Full 3D sound
            audioSource.minDistance = 2.0f;
            audioSource.maxDistance = 30.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.volume = soundVolume;
        }

        // Initialize chase audio timing
        nextChaseAudioTime = Random.Range(minChaseAudioDelay, maxChaseAudioDelay);

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
        int currentRound = 1;

        if (roundManager != null)
        {
            currentRound = roundManager.GetComponent<RoundManager>().currentRound;
            zombieHP = currentRound * 150;
            SetZombieType(currentRound);
        }
        else
        {
            zombieHP = 150;
            SetZombieType(1);
        }

        // Set individual speed variation
        individualSpeedMultiplier = 1.0f + Random.Range(-speedVariation, speedVariation);

        // Set preferred relative position to player
        float angle = Random.Range(0f, 360f);
        preferredOffset = new Vector3(
            Mathf.Sin(angle * Mathf.Deg2Rad) * attackRange,
            0f,
            Mathf.Cos(angle * Mathf.Deg2Rad) * attackRange
        );

        // Set avoidance preference
        preferRightAvoidance = Random.value > 0.5f;

        // Set preferred attack animation
        preferredAttackType = Random.Range(1, 4);

        // Calculate attack speed multiplier based on round
        // Starts at 1.0 and increases by 5% each round, capping at 3.0 (300% speed)
        attackSpeedMultiplier = Mathf.Min(1.0f + ((currentRound - 1) * 0.05f), 3.0f);

        // Reduce cooldown as rounds progress
        float cooldownReduction = Mathf.Min((currentRound - 1) * 0.02f, 0.5f); // Max 50% reduction
        attackCooldown = minAttackCooldown * (1.0f - cooldownReduction);

        dead = false;
        isAttacking = false;
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
        isAttacking = false;

        // Re-initialize the zombie
        Initialize();

        // Make sure the NavMeshAgent is enabled
        if (agent != null)
        {
            agent.enabled = true;
        }
        // Reset all limbs
        Limb[] limbs = GetComponentsInChildren<Limb>(true);
        foreach (Limb limb in limbs)
        {
            limb.ResetLimb();
        }
    }

    void SetZombieType(int round)
    {
        float runnerChance = Mathf.Clamp01((round - 5) / 35f);
        float sprinterChance = Mathf.Clamp01((round - 30) / 10f);

        float roll = Random.value;
        float baseSpeed;

        if (roll < sprinterChance * 0.75f)
        {
            zombieType = ZombieType.Sprinter;
            baseSpeed = sprinterBaseSpeed;
        }
        else if (roll < runnerChance * 0.25f + sprinterChance * 0.75f)
        {
            zombieType = ZombieType.Runner;
            baseSpeed = runnerBaseSpeed;
        }
        else
        {
            zombieType = ZombieType.Walker;
            baseSpeed = walkerBaseSpeed;
        }

        // Apply individual speed variation and cap to slightly less than player speed
        agent.speed = Mathf.Min(baseSpeed * individualSpeedMultiplier, playerSpeed - 0.5f);
    }

    void Update()
    {
        if (!dead && player != null && agent != null && agent.enabled)
        {
            // Calculate distance to player
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            // Attack logic
            if (distanceToPlayer <= attackDistance && !isAttacking)
            {
                if (attackCooldown <= 0)
                {
                    StartCoroutine(PerformAttack());
                }
                else
                {
                    attackCooldown -= Time.deltaTime;
                }
            }

            // Only navigate when not attacking
            if (!isAttacking)
            {
                // Base position is player plus preferred offset
                Vector3 targetPosition = player.transform.position + preferredOffset;

                // Apply spacing
                Vector3 spacingOffset = CalculateSpacingOffset();
                targetPosition += spacingOffset;

                agent.SetDestination(targetPosition);

                // Apply avoidance if there's an obstacle
                ApplyAvoidance();

                // Handle chase sounds
                UpdateChaseAudio(distanceToPlayer);
            }

            UpdateAnimations();
        }
    }

    void UpdateChaseAudio(float distanceToPlayer)
    {
        // Only play chase sounds if we have audio clips and we're not too close to attack
        if (chaseSounds != null && chaseSounds.Length > 0 && distanceToPlayer > attackDistance * 1.5f)
        {
            chaseAudioTimer += Time.deltaTime;

            // Play a random chase sound at random intervals
            if (chaseAudioTimer >= nextChaseAudioTime && !audioSource.isPlaying)
            {
                PlayRandomSound(chaseSounds);
                chaseAudioTimer = 0f;
                nextChaseAudioTime = Random.Range(minChaseAudioDelay, maxChaseAudioDelay);
            }
        }
    }

    void PlayRandomSound(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0 && audioSource != null)
        {
            AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];
            if (clipToPlay != null)
            {
                audioSource.clip = clipToPlay;
                audioSource.volume = soundVolume;
                audioSource.Play();
            }
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        // Stop moving
        Vector3 lastVelocity = agent.velocity;
        agent.velocity = Vector3.zero;

        // Face the player
        transform.LookAt(new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z));

        // Play attack sound
        PlayRandomSound(attackSounds);

        // Trigger attack animation based on preferred type
        myAnim.SetFloat("attackSpeed", attackSpeedMultiplier);
        myAnim.SetInteger("attackType", preferredAttackType);
        myAnim.SetTrigger("attack");

        // Get attack animation length and wait for it scaled by speed multiplier
        AnimatorStateInfo stateInfo = myAnim.GetCurrentAnimatorStateInfo(0);
        float attackDuration = stateInfo.length / attackSpeedMultiplier;

        // Wait for attack to complete
        yield return new WaitForSeconds(attackDuration);

        // Reset attack state
        isAttacking = false;
        attackCooldown = minAttackCooldown;

        // Resume movement
        if (agent != null && agent.enabled)
        {
            agent.velocity = lastVelocity;
            UpdateAnimations();
        }
    }

    Vector3 CalculateSpacingOffset()
    {
        Vector3 offset = Vector3.zero;
        Collider[] nearbyZombies = Physics.OverlapSphere(transform.position, spacingRadius, zombieLayer);
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // Scale down spacing as we get closer to the player
        float spacingScale = Mathf.Clamp01((distanceToPlayer - attackDistance) / spacingRadius);

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

        return offset.normalized * spacingScale;
    }

    void ApplyAvoidance()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // Scale down avoidance as we get closer to the player
        float avoidanceScale = Mathf.Clamp01((distanceToPlayer - attackDistance) / 2.0f);

        if (avoidanceScale < 0.1f) return; // Skip avoidance when very close to attack range

        // Cast a ray forward to check for obstacles
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1.5f, zombieLayer))
        {
            // There's a zombie in front, apply avoidance
            float rotationAmount = preferRightAvoidance ? avoidanceAngle : -avoidanceAngle;
            Vector3 avoidanceDirection = Quaternion.Euler(0, rotationAmount, 0) * transform.forward;

            // Temporarily modify the agent's velocity to steer around the obstacle
            if (agent.velocity.magnitude > 0.1f)
            {
                Vector3 newVelocity = Vector3.Lerp(agent.velocity,
                                                 avoidanceDirection * agent.speed,
                                                 Time.deltaTime * 5f * avoidanceScale);
                agent.velocity = newVelocity;
            }
        }
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
            myAnim.SetTrigger("getHit");

            // Play damage sound
            PlayRandomSound(damageSounds);

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
        Vector3 normalizedDirection = hitDirection.normalized;

        foreach (var rb in ragdollRigids)
        {
            float distance = Vector3.Distance(rb.position, hitPoint);
            if (distance < 0.5f)
            {
                rb.AddForce(normalizedDirection * 0.001f, ForceMode.Impulse);
            }

            // Cap the velocity to prevent excessive movement
            if (rb.velocity.magnitude > 2f)
            {
                rb.velocity = rb.velocity.normalized * 2f;
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