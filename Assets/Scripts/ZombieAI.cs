using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using GameAudio;

public class ZombieAI : MonoBehaviour
{
    Animator myAnim;
    public NavMeshAgent agent;
    public GameObject player;
    public GameObject roundManager;

    // Reference to the round data
    private RoundData roundData;
    private RoundManager roundManagerComponent;



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

    [Header("Sound Effect Data")]
    [SerializeField] SoundEffectData damageSound;
    [SerializeField] SoundEffectData attackSound;
    [SerializeField] SoundEffectData chaseSound;
    [SerializeField] SoundEffectData deathSound;

    [Header("Respawn Criteria")]
    [SerializeField] float maxDistanceFromPlayer = 50f;
    [SerializeField] float maxStuckTime = 10f; // How long zombie can be stuck before respawn
    [SerializeField] float minVelocityThreshold = 0.1f; // Velocity below this = stuck
    [SerializeField] float respawnCheckInterval = 3f;
    [SerializeField] LayerMask playerLayerMask = 1 << 6; // Only check player layer for line of sight

    private float nextRespawnCheck = 0f;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    private bool wasStuckLastFrame = false;


    public float zombieHP;
    public bool dead;
    List<Rigidbody> ragdollRigids;

    // Zombie type enum
    public enum ZombieType { Walker, Runner, Sprinter }

    [Header("Zombie Type Settings")]
    [Tooltip("The type of zombie (affects speed)")]
    private ZombieType zombieType;

    private Vector3 lastHitPoint;
    private Vector3 lastHitForce;

    // Base speeds for zombie types - Moderately increased
    [Header("Movement Settings")]
    [Tooltip("Base speed for walker zombies")]
    public float walkerBaseSpeed = 1.8f; // Slightly increased from 1.2f
    [Tooltip("Base speed for runner zombies")]
    public float runnerBaseSpeed = 3.0f; // Moderately increased from 2.0f
    [Tooltip("Base speed for sprinter zombies")]
    public float sprinterBaseSpeed = 4.8f; // Slightly increased from 4.5f
    [Tooltip("Player movement speed (for reference)")]
    public float playerSpeed = 6.0f;
    [Tooltip("Speed variation percentage (0.4 = ±40%)")]
    [Range(0f, 1f)]
    public float speedVariation = 0.4f;
    private float individualSpeedMultiplier;

    // Preferred position relative to player
    [Header("Positioning Settings")]
    [Tooltip("Should zombies take direct path when alone")]
    public bool useDirectPathWhenAlone = true;
    [Tooltip("Distance from player when attacking")]
    public float attackRange = 1.6f;
    [Tooltip("Distance at which zombie will initiate an attack")]
    public float attackDistance = 1.8f;
    [Tooltip("Buffer multiplier for attack distance when standing still (1.7 = 70% more range)")]
    [Range(1.0f, 2.5f)]
    public float stationaryAttackRangeMultiplier = 1.7f;
    [Tooltip("Buffer multiplier for when to face the player (1.5 = 50% more range)")]
    [Range(1.0f, 2.5f)]
    public float facingRangeMultiplier = 1.5f;
    private Vector3 preferredOffset;

    // Avoidance preference
    [Header("Avoidance Settings")]
    [Tooltip("Angle for avoiding obstacles")]
    public float avoidanceAngle = 10f;
    private bool preferRightAvoidance;

    // Spacing parameters
    [Tooltip("Distance for spacing between zombies")]
    public float spacingRadius = 3f;
    [Tooltip("Force applied for spacing")]
    public float spacingForce = 3f;
    private LayerMask zombieLayer;

    // Attack parameters
    [Header("Attack Settings")]
    [Tooltip("Minimum time between attacks")]
    public float minAttackCooldown = 1.0f;
    [Tooltip("Animation speed multiplier for attacks")]
    [Range(0.5f, 3.0f)]
    public float attackSpeedMultiplier = 1.4f;
    [Tooltip("How much to scale cooldown reduction when standing still")]
    [Range(1.0f, 5.0f)]
    public float standingCooldownReduction = 2.0f;
    [Tooltip("Extra buffer time for attack animations (seconds)")]
    [Range(0f, 1.0f)]
    public float attackAnimationBuffer = 0.2f;
    [Tooltip("Round-based cooldown reduction (percent per round)")]
    [Range(0f, 0.1f)]
    public float roundCooldownReductionPerRound = 0.02f;
    [Tooltip("Maximum cooldown reduction from rounds (percent)")]
    [Range(0f, 0.9f)]
    public float maxRoundCooldownReduction = 0.5f;
    [Tooltip("Round-based attack speed increase (percent per round)")]
    [Range(0f, 0.1f)]
    public float attackSpeedIncreasePerRound = 0.05f;
    [Tooltip("Maximum attack speed multiplier from rounds")]
    [Range(1.0f, 5.0f)]
    public float maxAttackSpeedMultiplier = 3.0f;
    private float attackCooldown = 0f;
    private bool isAttacking = false;
    private float currentAttackDuration = 0f;
    private float attackStartTime = 0f;
    private bool wasStandingStillWhenAttackStarted = false;

    // Multiplayer Support
    [Header("Multiplayer")]
    [SerializeField] float targetSwitchCooldown = 2f;
    [SerializeField] float targetSwitchDistanceThreshold = 0.7f; // 70% closer to switch
    private float lastTargetSwitchTime;
    private List<GameObject> potentialTargets = new List<GameObject>();

    // Layer Management
    [Header("Layer Settings")]
    [SerializeField] string deadZombieLayerName = "DeadZombie";
    [SerializeField] string aliveZombieLayerName = "Zombie";
    private int deadZombieLayer;
    private int aliveZombieLayer;

    // Coroutine reference for death timer
    private Coroutine deathCoroutine;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        // Find all players for multiplayer support
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        potentialTargets.Clear();
        potentialTargets.AddRange(players);

        // Select initial target
        if (potentialTargets.Count > 0)
        {
            SelectNearestTarget();
        }

        if (roundManager == null)
        {
            roundManager = GameObject.FindGameObjectWithTag("RoundManager");
        }

        lastPosition = transform.position;
        stuckTimer = 0f;
        wasStuckLastFrame = false;

        // Get RoundData and RoundManager component
        if (roundManager != null)
        {
            roundManagerComponent = roundManager.GetComponent<RoundManager>();
            if (roundManagerComponent != null)
            {
                roundData = roundManagerComponent.roundData;
            }
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

        // Get layer indices
        deadZombieLayer = LayerMask.NameToLayer(deadZombieLayerName);
        aliveZombieLayer = LayerMask.NameToLayer(aliveZombieLayerName);

        // Set initial layer
        SetLayerRecursively(gameObject, aliveZombieLayer);

        InitializeZombieProperties();
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        if (layer == -1) return; // Invalid layer

        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    void SelectNearestTarget()
    {
        if (potentialTargets.Count == 0) return;

        float closestDistance = float.MaxValue;
        GameObject nearestPlayer = null;

        foreach (GameObject potentialPlayer in potentialTargets)
        {
            if (potentialPlayer == null) continue;

            float distance = Vector3.Distance(transform.position, potentialPlayer.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestPlayer = potentialPlayer;
            }
        }

        if (nearestPlayer != null)
        {
            player = nearestPlayer;
            lastTargetSwitchTime = Time.time;
        }
    }

    void ConsiderTargetSwitch()
    {
        // Refresh player list
        GameObject[] currentPlayers = GameObject.FindGameObjectsWithTag("Player");
        potentialTargets.Clear();
        potentialTargets.AddRange(currentPlayers);

        // Check if current target is still valid
        if (player == null || !potentialTargets.Contains(player))
        {
            SelectNearestTarget();
            return;
        }

        // Check if there's a significantly closer player
        float currentDistance = Vector3.Distance(transform.position, player.transform.position);

        foreach (GameObject potentialPlayer in potentialTargets)
        {
            if (potentialPlayer == null || potentialPlayer == player) continue;

            float distance = Vector3.Distance(transform.position, potentialPlayer.transform.position);

            // Switch if another player is significantly closer
            if (distance < currentDistance * targetSwitchDistanceThreshold)
            {
                player = potentialPlayer;
                lastTargetSwitchTime = Time.time;
                break;
            }
        }
    }

    void InitializeZombieProperties()
    {
        int currentRound = 1;

        // Get current round from RoundData if available
        if (roundData != null)
        {
            currentRound = roundData.CurrentRound;
            zombieHP = currentRound * 150;
            SetZombieType(currentRound);
        }
        else if (roundManager != null)
        {
            // Fallback to RoundManager if needed
            RoundManager rm = roundManager.GetComponent<RoundManager>();
            if (rm != null)
            {
                currentRound = rm.roundData.CurrentRound;
                zombieHP = currentRound * 150;
                SetZombieType(currentRound);
            }
            else
            {
                zombieHP = 150;
                SetZombieType(1);
            }
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

        // Calculate attack speed multiplier based on round
        attackSpeedMultiplier = Mathf.Min(1.0f + ((currentRound - 1) * attackSpeedIncreasePerRound), maxAttackSpeedMultiplier);

        // Reduce cooldown as rounds progress
        float cooldownReduction = Mathf.Min((currentRound - 1) * roundCooldownReductionPerRound, maxRoundCooldownReduction);
        attackCooldown = minAttackCooldown * (1.0f - cooldownReduction);

        dead = false;
        isAttacking = false;
        currentAttackDuration = 0f;
        attackStartTime = 0f;
        wasStandingStillWhenAttackStarted = false;
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

        // Reset layer to alive
        SetLayerRecursively(gameObject, aliveZombieLayer);

        // Reset properties
        DeactivateRagdoll();
        dead = false;
        isAttacking = false;
        currentAttackDuration = 0f;
        attackStartTime = 0f;
        wasStandingStillWhenAttackStarted = false;

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
        // Balanced difficulty progression
        // Runners start appearing at round 3, become common by round 7
        float runnerChance = Mathf.Clamp01((round - 2) / 5f); // Start at round 3, max at round 7

        // Sprinters start appearing at round 8, become common by round 15
        float sprinterChance = Mathf.Clamp01((round - 7) / 8f); // Start at round 8, max at round 15

        // Moderate multipliers for balanced progression
        float runnerMultiplier = 0.8f; // Slightly less aggressive
        float sprinterMultiplier = 0.7f; // More gradual introduction

        float roll = Random.value;
        float baseSpeed;

        if (roll < sprinterChance * sprinterMultiplier)
        {
            zombieType = ZombieType.Sprinter;
            baseSpeed = sprinterBaseSpeed;
        }
        else if (roll < runnerChance * runnerMultiplier + sprinterChance * sprinterMultiplier)
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
        agent.speed = Mathf.Min(baseSpeed * individualSpeedMultiplier, playerSpeed - 0.4f); // Slightly more breathing room
    }

    void FacePlayer()
    {
        if (player != null)
        {
            Vector3 targetPosition = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
            Vector3 direction = targetPosition - transform.position;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
            }
        }
    }

    void Update()
    {
        if (!dead && agent != null && agent.enabled)
        {
            // Check respawn criteria periodically
            if (Time.time >= nextRespawnCheck)
            {
                CheckRespawnCriteria();
                nextRespawnCheck = Time.time + respawnCheckInterval;
            }

        

        // Periodically check for better targets in multiplayer
        if (Time.time - lastTargetSwitchTime > targetSwitchCooldown)
            {
                ConsiderTargetSwitch();
            }

            if (player == null)
            {
                SelectNearestTarget();
                return;
            }

            // Calculate distance to player
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            // Always face the player when close enough, even if not attacking
            if (distanceToPlayer <= attackDistance * facingRangeMultiplier)
            {
                FacePlayer();
            }

            // Check if we're still in an attack animation
            if (isAttacking)
            {
                FacePlayer();

                if (Time.time >= attackStartTime + currentAttackDuration + attackAnimationBuffer)
                {
                    if (wasStandingStillWhenAttackStarted)
                    {
                        agent.velocity = Vector3.zero;
                    }

                    isAttacking = false;
                    wasStandingStillWhenAttackStarted = false;

                    if (distanceToPlayer <= attackDistance)
                    {
                        attackCooldown = 0f;
                    }
                    else
                    {
                        attackCooldown = minAttackCooldown;
                    }
                }
                else
                {
                    if (wasStandingStillWhenAttackStarted)
                    {
                        agent.velocity = Vector3.zero;
                    }
                }

                return;
            }

            bool isStandingStill = agent.velocity.magnitude < 0.2f;

            bool shouldAttack = (distanceToPlayer <= attackDistance) ||
                               (isStandingStill && distanceToPlayer <= attackDistance * stationaryAttackRangeMultiplier);

            if (shouldAttack && !isAttacking && attackCooldown <= 0)
            {
                FacePlayer();
                StartAttack(isStandingStill);
            }
            else if (!isAttacking)
            {
                if (attackCooldown > 0)
                {
                    attackCooldown -= Time.deltaTime;

                    if (isStandingStill)
                    {
                        attackCooldown -= Time.deltaTime * standingCooldownReduction;
                    }
                }

                Vector3 targetPosition;

                bool otherZombiesNearby = Physics.OverlapSphere(transform.position, spacingRadius, zombieLayer).Length > 1;

                if (distanceToPlayer > attackDistance * 1.1f || (useDirectPathWhenAlone && !otherZombiesNearby))
                {
                    targetPosition = player.transform.position;
                }
                else
                {
                    targetPosition = player.transform.position + preferredOffset;

                    Vector3 spacingOffset = CalculateSpacingOffset();
                    targetPosition += spacingOffset;

                    if (isStandingStill && distanceToPlayer <= attackDistance * stationaryAttackRangeMultiplier)
                    {
                        attackCooldown = 0f;
                    }
                }

                agent.SetDestination(targetPosition);

                if (otherZombiesNearby)
                {
                    ApplyAvoidance();
                }

                UpdateChaseAudio(distanceToPlayer);
            }

            UpdateAnimations();
        }
    }
    void CheckRespawnCriteria()
    {
        if (potentialTargets.Count == 0) return;

        bool shouldRespawn = false;
        string respawnReason = "";

        // 1. Distance check
        float closestDistance = float.MaxValue;
        GameObject closestPlayer = null;

        foreach (GameObject potentialPlayer in potentialTargets)
        {
            if (potentialPlayer == null) continue;

            float distance = Vector3.Distance(transform.position, potentialPlayer.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = potentialPlayer;
            }
        }

        if (closestDistance > maxDistanceFromPlayer)
        {
            shouldRespawn = true;
            respawnReason = $"too far from players ({closestDistance:F1}m)";
        }

        // 2. Line of sight check (if not already too far)
        if (!shouldRespawn && closestPlayer != null)
        {
            bool hasLineOfSight = HasLineOfSightToAnyPlayer();
            if (!hasLineOfSight && closestDistance > 20f) // Only respawn if also reasonably far
            {
                shouldRespawn = true;
                respawnReason = "no line of sight and distant";
            }
        }

        // 3. Stuck check
        Vector3 currentPosition = transform.position;
        float distanceMoved = Vector3.Distance(currentPosition, lastPosition);

        if (agent != null && agent.velocity.magnitude < minVelocityThreshold && distanceMoved < 0.5f)
        {
            if (!wasStuckLastFrame)
            {
                stuckTimer = 0f; // Reset timer when first becoming stuck
            }

            stuckTimer += respawnCheckInterval;
            wasStuckLastFrame = true;

            if (stuckTimer >= maxStuckTime)
            {
                shouldRespawn = true;
                respawnReason = $"stuck for {stuckTimer:F1}s";
            }
        }
        else
        {
            stuckTimer = 0f;
            wasStuckLastFrame = false;
        }

        lastPosition = currentPosition;

        // 4. Invalid position check (fell through world, etc.)
        if (transform.position.y < -10f)
        {
            shouldRespawn = true;
            respawnReason = "fell through world";
        }

        // Execute respawn if any criteria met
        if (shouldRespawn)
        {
            //Debug.Log($"Zombie respawn triggered: {respawnReason}");

            if (roundManagerComponent != null)
            {
                roundManagerComponent.HandleDistantZombieReturn();
            }

            if (ZombiePool.Instance != null)
            {
                ZombiePool.Instance.ReturnZombie(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    bool HasLineOfSightToAnyPlayer()
    {
        foreach (GameObject potentialPlayer in potentialTargets)
        {
            if (potentialPlayer == null) continue;

            Vector3 directionToPlayer = potentialPlayer.transform.position - transform.position;
            float distanceToPlayer = directionToPlayer.magnitude;

            // Cast ray only on player layer
            if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer.normalized, distanceToPlayer, playerLayerMask))
            {
                return true; // Can see this player
            }
        }
        return false; // Can't see any player
    }


    void StartAttack(bool isStandingStill)
    {
        // Set attack state
        isAttacking = true;
        wasStandingStillWhenAttackStarted = isStandingStill;
        attackStartTime = Time.time;

        // Store velocity to prevent movement during standing still attacks
        if (isStandingStill)
        {
            agent.velocity = Vector3.zero;
        }

        // Play attack sound
        PlayRandomSound(attackSounds);

        // Choose random attack type
        int attackType = Random.Range(1, 4);

        // Set attack parameters and trigger animation
        myAnim.SetFloat("attackSpeed", attackSpeedMultiplier);
        myAnim.SetInteger("attackType", attackType);
        myAnim.SetTrigger("attack");

        // Get animation duration
        AnimatorStateInfo stateInfo = myAnim.GetCurrentAnimatorStateInfo(0);
        currentAttackDuration = stateInfo.length / attackSpeedMultiplier;
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

        if (avoidanceScale < 0.1f) return;

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
        if (myAnim != null && !isAttacking)
        {
            float speed = agent.velocity.magnitude;

            // Reset all animation booleans to false first
            myAnim.SetBool("isIdle", false);
            myAnim.SetBool("isWalking", false);
            myAnim.SetBool("isRunning", false);
            myAnim.SetBool("isSprinting", false);

            // Set only the appropriate animation based on zombie type and speed
            if (speed < 0.1f)
            {
                myAnim.SetBool("isIdle", true);
            }
            else
            {
                switch (zombieType)
                {
                    case ZombieType.Walker:
                        myAnim.SetBool("isWalking", true);
                        break;
                    case ZombieType.Runner:
                        myAnim.SetBool("isRunning", true);
                        break;
                    case ZombieType.Sprinter:
                        myAnim.SetBool("isSprinting", true);
                        break;
                }
            }
        }
    }

    public bool GetHit(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (zombieHP > 0)
        {
            zombieHP -= damage;

            // Don't interrupt attack animations with hit animation
            if (!isAttacking)
            {
                myAnim.SetTrigger("getHit");
            }

            // Play damage sound
            PlayRandomSound(damageSounds);

            if (zombieHP <= 0)
            {
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
        SetLayerRecursively(gameObject, deadZombieLayer);

        if (agent != null)
        {
            agent.enabled = false;
        }

        ActivateRagdoll();

        if (roundManagerComponent != null)
        {
            roundManagerComponent.NotifyZombieDeath();
        }

        deathCoroutine = StartCoroutine(DeathTimer());
    }

    private IEnumerator DeathTimer()
    {
        yield return new WaitForSeconds(15.0f);

        if (ZombiePool.Instance != null)
        {
            ZombiePool.Instance.ReturnZombie(gameObject);
        }
        else
        {
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
            if (rb == null) continue;

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
            closestRigidbody.AddForce(lastHitForce * 0.25f, ForceMode.Impulse);
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
                if (rb != null)
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