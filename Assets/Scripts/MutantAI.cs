using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MutantAI : MonoBehaviour
{
    [Header("Mutant Configuration")]
    [SerializeField] float mutantHealth = 1500f;
    [SerializeField] float searchSpeed = 2f;
    [SerializeField] float chargeSpeed = 8f;
    [SerializeField] float chargeDistance = 15f;
    [SerializeField] float chargeCooldown = 10f;
    [SerializeField] float meleeRange = 2f;
    [SerializeField] float detectionRange = 12f;
    [SerializeField] float teleportDistance = 20f;
    [SerializeField] int meleeDamage = 60;
    [SerializeField] int chargeDamage = 80;
    [SerializeField] float stunDuration = 1f;

    [Header("Audio")]
    [SerializeField] AudioClip[] screamSounds;
    [SerializeField] AudioClip[] chargeSounds;
    [SerializeField] AudioClip[] meleeSounds;
    [SerializeField] AudioClip[] stunSounds;
    [SerializeField] AudioClip deathSound;
    [SerializeField] float soundVolume = 0.8f;

    [Header("Effects")]
    [SerializeField] GameObject summonCirclePrefab;
    [SerializeField] GameObject orbPrefab;
    [SerializeField] ParticleSystem chargeEffect;
    [SerializeField] ParticleSystem deathEffect;

    [Header("Ragdoll")]
    [SerializeField] bool useRagdoll = true;

    public enum MutantState { Spawning, Searching, Screaming, Charging, Melee, Stunned, Transforming, Dead }

    private MutantState currentState;
    private NavMeshAgent agent;
    private Animator animator;
    private AudioSource audioSource;
    private GameObject targetPlayer;
    private Vector3 chargeDirection;
    private float lastChargeTime = -999f;
    private float currentHealth;
    private bool isDead = false;
    private List<GameObject> potentialTargets = new List<GameObject>();
    private RoundManager roundManager;
    private MutantDropSystem dropSystem;
    private List<Rigidbody> ragdollRigids;
    private Vector3 lastHitPoint;
    private Vector3 lastHitForce;

    // Coroutine references
    private Coroutine stunCoroutine;
    private Coroutine searchCoroutine;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        dropSystem = GetComponent<MutantDropSystem>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.volume = soundVolume;
        }

        // Initialize ragdoll
        InitializeRagdoll();

        roundManager = FindObjectOfType<RoundManager>();
        InitializeMutant();
    }

    void InitializeRagdoll()
    {
        ragdollRigids = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());

        // Remove the main rigidbody if it exists
        Rigidbody mainRigidbody = GetComponent<Rigidbody>();
        if (mainRigidbody != null && ragdollRigids.Contains(mainRigidbody))
        {
            ragdollRigids.Remove(mainRigidbody);
        }

        DeactivateRagdoll();
    }

    void ActivateRagdoll()
    {
        if (!useRagdoll) return;

        if (animator != null)
        {
            animator.enabled = false;
        }

        foreach (var rb in ragdollRigids)
        {
            if (rb == null) continue;

            rb.useGravity = true;
            rb.isKinematic = false;
        }

        // Apply force to closest rigidbody to hit point
        if (ragdollRigids.Count > 0 && lastHitPoint != Vector3.zero)
        {
            Rigidbody closestRb = ragdollRigids[0];
            float closestDistance = Vector3.Distance(closestRb.position, lastHitPoint);

            foreach (var rb in ragdollRigids)
            {
                if (rb == null) continue;

                float distance = Vector3.Distance(rb.position, lastHitPoint);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestRb = rb;
                }
            }

            if (closestRb != null)
            {
                closestRb.AddForce(lastHitForce * 0.3f, ForceMode.Impulse);
            }
        }
    }

    void DeactivateRagdoll()
    {
        if (animator != null)
        {
            animator.enabled = true;
        }

        foreach (var rb in ragdollRigids)
        {
            if (rb == null) continue;

            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    void InitializeMutant()
    {
        currentHealth = mutantHealth;
        currentState = MutantState.Spawning;
        agent.speed = searchSpeed;

        // Find all players
        RefreshPlayerTargets();

        // Spawn with summoning circle
        StartCoroutine(SpawnSequence());
    }

    void RefreshPlayerTargets()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        potentialTargets.Clear();
        potentialTargets.AddRange(players);
    }

    IEnumerator SpawnSequence()
    {
        // Disable agent during spawn
        agent.enabled = false;

        // Spawn summoning circle
        if (summonCirclePrefab != null)
        {
            GameObject circle = Instantiate(summonCirclePrefab, transform.position, Quaternion.identity);
            Destroy(circle, 3f);
        }

        // Play spawn animation
        if (animator != null)
            animator.SetTrigger("Spawn");

        yield return new WaitForSeconds(2f);

        // Enable agent and start searching
        agent.enabled = true;
        SetState(MutantState.Searching);
    }

    void Update()
    {
        if (isDead) return;

        RefreshPlayerTargets();

        switch (currentState)
        {
            case MutantState.Searching:
                HandleSearching();
                break;
            case MutantState.Charging:
                HandleCharging();
                break;
            case MutantState.Melee:
                HandleMelee();
                break;
        }

        UpdateAnimations();
    }

    void HandleSearching()
    {
        GameObject nearestPlayer = FindNearestPlayer();

        if (nearestPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, nearestPlayer.transform.position);

            // Check if player is in detection range
            if (distance <= detectionRange)
            {
                targetPlayer = nearestPlayer;
                SetState(MutantState.Screaming);
                return;
            }

            // Check if should teleport (no players nearby)
            if (distance > teleportDistance)
            {
                StartCoroutine(TransformToOrb());
                return;
            }

            // Move towards player
            agent.SetDestination(nearestPlayer.transform.position);
        }
        else
        {
            // No players found, wander or teleport
            if (searchCoroutine == null)
                searchCoroutine = StartCoroutine(WanderSearch());
        }
    }

    void HandleCharging()
    {
        // Check if hit a wall (stopped moving but still charging)
        if (agent.velocity.magnitude < 0.5f && Time.time > lastChargeTime + 0.5f)
        {
            SetState(MutantState.Stunned);
            return;
        }

        // Check if charge duration expired
        if (Time.time > lastChargeTime + 3f)
        {
            SetState(MutantState.Searching);
        }
    }

    void HandleMelee()
    {
        if (targetPlayer == null)
        {
            SetState(MutantState.Searching);
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);

        // If player moved away, go back to searching
        if (distance > meleeRange * 2f)
        {
            SetState(MutantState.Searching);
            return;
        }

        // Face the player
        FaceTarget(targetPlayer.transform.position);

        // Check if can charge again
        if (Time.time > lastChargeTime + chargeCooldown && distance > meleeRange && distance <= chargeDistance)
        {
            StartCharge();
        }
    }

    GameObject FindNearestPlayer()
    {
        GameObject nearest = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject player in potentialTargets)
        {
            if (player == null) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearest = player;
            }
        }

        return nearest;
    }

    void SetState(MutantState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case MutantState.Screaming:
                StartCoroutine(ScreamSequence());
                break;

            case MutantState.Stunned:
                StartCoroutine(StunSequence());
                break;

            case MutantState.Melee:
                agent.speed = searchSpeed;
                break;

            case MutantState.Searching:
                agent.speed = searchSpeed;
                if (searchCoroutine != null)
                {
                    StopCoroutine(searchCoroutine);
                    searchCoroutine = null;
                }
                break;
        }
    }

    IEnumerator ScreamSequence()
    {
        // Stop and face player
        agent.velocity = Vector3.zero;
        agent.isStopped = true;

        if (targetPlayer != null)
            FaceTarget(targetPlayer.transform.position);

        // Play scream
        PlayRandomSound(screamSounds);
        if (animator != null)
            animator.SetTrigger("Scream");

        yield return new WaitForSeconds(1.5f);

        agent.isStopped = false;

        // Decide next action based on distance and cooldown
        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);

            if (Time.time > lastChargeTime + chargeCooldown && distance > meleeRange && distance <= chargeDistance)
            {
                StartCharge();
            }
            else
            {
                SetState(MutantState.Melee);
            }
        }
        else
        {
            SetState(MutantState.Searching);
        }
    }

    void StartCharge()
    {
        if (targetPlayer == null) return;

        SetState(MutantState.Charging);
        lastChargeTime = Time.time;

        // Calculate charge direction
        chargeDirection = (targetPlayer.transform.position - transform.position).normalized;

        // Set charge speed and destination
        agent.speed = chargeSpeed;
        Vector3 chargeTarget = transform.position + chargeDirection * chargeDistance;

        // Make sure charge target is on navmesh - FIXED: Use -1 instead of NavMesh.AllAreas
        if (NavMesh.SamplePosition(chargeTarget, out NavMeshHit hit, 5f, -1))
        {
            agent.SetDestination(hit.position);
        }

        // Play effects and sounds
        PlayRandomSound(chargeSounds);
        if (animator != null)
            animator.SetTrigger("Charge");

        if (chargeEffect != null)
            chargeEffect.Play();
    }

    IEnumerator StunSequence()
    {
        agent.velocity = Vector3.zero;
        agent.isStopped = true;

        PlayRandomSound(stunSounds);
        if (animator != null)
            animator.SetTrigger("Stunned");

        yield return new WaitForSeconds(stunDuration);

        agent.isStopped = false;
        SetState(MutantState.Searching);
    }

    IEnumerator WanderSearch()
    {
        while (currentState == MutantState.Searching)
        {
            // Pick random point to wander to
            Vector3 randomDirection = Random.insideUnitSphere * 10f;
            randomDirection += transform.position;

            // FIXED: Use -1 instead of NavMesh.AllAreas
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 10f, -1))
            {
                agent.SetDestination(hit.position);
            }

            yield return new WaitForSeconds(Random.Range(3f, 6f));
        }

        searchCoroutine = null;
    }

    IEnumerator TransformToOrb()
    {
        SetState(MutantState.Transforming);

        // Instant transformation - no animation, just brief delay for effect
        yield return new WaitForSeconds(0.2f);

        // Create orb and pass mutant data
        if (orbPrefab != null)
        {
            GameObject orb = Instantiate(orbPrefab, transform.position, Quaternion.identity);
            MutantOrb orbScript = orb.GetComponent<MutantOrb>();
            if (orbScript != null)
            {
                orbScript.InitializeFromMutant(this);
            }
        }

        // Hide this mutant
        gameObject.SetActive(false);
    }

    public void TransformFromOrb(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
        StartCoroutine(SpawnFromOrb());
    }

    IEnumerator SpawnFromOrb()
    {
        // Instant transformation - no animation, just brief delay for effect
        yield return new WaitForSeconds(0.2f);
        SetState(MutantState.Searching);
    }

    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", agent.velocity.magnitude);
        animator.SetBool("IsCharging", currentState == MutantState.Charging);
        animator.SetBool("IsStunned", currentState == MutantState.Stunned);
    }

    public void TakeDamage(float damage, Vector3 hitPoint = default, Vector3 hitDirection = default)
    {
        if (isDead) return;

        // Store hit info for ragdoll
        lastHitPoint = hitPoint;
        lastHitForce = hitDirection;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        SetState(MutantState.Dead);

        // Stop all movement
        if (agent != null)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            agent.enabled = false; // Disable agent for ragdoll
        }

        // Activate ragdoll instead of playing death animation
        ActivateRagdoll();

        // Play death effects
        PlaySound(deathSound);

        if (deathEffect != null)
            deathEffect.Play();

        // Award points to all players
        AwardPointsToAllPlayers();

        // Handle drops
        if (dropSystem != null)
            dropSystem.HandleDrop();

        // Destroy after delay
        Destroy(gameObject, 8f); // Longer delay for ragdoll
    }

    void AwardPointsToAllPlayers()
    {
        PointManager pointManager = FindObjectOfType<PointManager>();
        if (pointManager != null)
        {
            foreach (GameObject player in potentialTargets)
            {
                if (player != null)
                {
                    pointManager.AddPoints(player, 2000);
                }
            }
        }
    }

    void PlayRandomSound(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0 && audioSource != null)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Animation events
    public void OnMeleeAttackHit()
    {
        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
            if (distance <= meleeRange)
            {
                // Deal damage to player
                PlayerHealthReference healthRef = targetPlayer.GetComponent<PlayerHealthReference>();
                if (healthRef?.HealthData != null)
                {
                    healthRef.HealthData.TakeDamage(meleeDamage);
                    PlayRandomSound(meleeSounds);
                }
            }
        }
    }

    public void OnChargeAttackHit()
    {
        // Check for player collision during charge
        Collider[] hits = Physics.OverlapSphere(transform.position, 2f);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealthReference healthRef = hit.GetComponent<PlayerHealthReference>();
                if (healthRef?.HealthData != null)
                {
                    healthRef.HealthData.TakeDamage(chargeDamage);
                    PlayRandomSound(meleeSounds);
                }
            }
        }
    }

    // Public getters for orb transformation
    public float GetCurrentHealth() => currentHealth;
    public float GetLastChargeTime() => lastChargeTime;
    public void SetHealthAndCooldown(float health, float chargeTime)
    {
        currentHealth = health;
        lastChargeTime = chargeTime;
    }
}