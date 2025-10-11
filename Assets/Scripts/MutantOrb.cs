using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MutantOrb : MonoBehaviour
{
    [Header("Orb Settings")]
    [SerializeField] float moveSpeed = 15f;
    [SerializeField] float floatHeight = 3f;
    [SerializeField] float arrivalDistance = 2f;
    [SerializeField] ParticleSystem orbEffect;
    [SerializeField] AudioClip orbTravelSound;
    [SerializeField] AudioClip orbArrivalSound;

    [Header("Debug")]
    [SerializeField] bool showDebugLogs = true;

    private MutantAI originalMutant;
    private Vector3 targetPosition;
    private AudioSource audioSource;
    private bool hasArrived = false;
    private float storedHealth;
    private float storedChargeTime;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
        }
    }

    void Start()
    {
        // Play travel effects
        if (orbEffect != null)
            orbEffect.Play();

        if (orbTravelSound != null && audioSource != null)
            audioSource.PlayOneShot(orbTravelSound);

        // Find target position
        FindTargetArea();

        // Start movement
        if (targetPosition != Vector3.zero)
        {
            StartCoroutine(MoveToTarget());
        }
        else
        {
            Debug.LogError("Failed to find target position for orb");
            Destroy(gameObject);
        }
    }

    public void InitializeFromMutant(MutantAI mutant)
    {
        originalMutant = mutant;
        storedHealth = mutant.GetCurrentHealth();
        storedChargeTime = mutant.GetLastChargeTime();

        if (showDebugLogs)
            Debug.Log($"Orb initialized with health: {storedHealth}, charge time: {storedChargeTime}");
    }

    void FindTargetArea()
    {
        // Find the closest player area
        RoundManager roundManager = FindObjectOfType<RoundManager>();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0)
        {
            Debug.LogWarning("No players found for orb teleportation");
            Destroy(gameObject);
            return;
        }

        if (roundManager?.mapAreas == null)
        {
            Debug.LogWarning("No map areas found for orb teleportation");
            // Fallback to closest player position
            GameObject closestPlayer = FindClosestPlayer(players);
            if (closestPlayer != null)
            {
                targetPosition = closestPlayer.transform.position + Vector3.up * floatHeight;
                if (showDebugLogs)
                    Debug.Log("Using fallback target position near closest player");
            }
            return;
        }

        // Find closest player
        GameObject nearestPlayer = FindClosestPlayer(players);

        if (nearestPlayer != null)
        {
            // Find which area the closest player is in
            foreach (var area in roundManager.mapAreas)
            {
                if (area.areaCollider != null && area.areaCollider.bounds.Contains(nearestPlayer.transform.position))
                {
                    // Pick a random spawn point in this area
                    if (area.spawnPoints.Count > 0)
                    {
                        GameObject targetSpawn = area.spawnPoints[Random.Range(0, area.spawnPoints.Count)];
                        targetPosition = targetSpawn.transform.position + Vector3.up * floatHeight;

                        if (showDebugLogs)
                            Debug.Log($"Orb targeting area: {area.areaName}");
                        return;
                    }
                }
            }

            // Fallback: move to closest player's position
            targetPosition = nearestPlayer.transform.position + Vector3.up * floatHeight;

            if (showDebugLogs)
                Debug.Log("Using fallback target position near closest player");
        }
    }

    GameObject FindClosestPlayer(GameObject[] players)
    {
        GameObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject player in players)
        {
            if (player == null) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = player;
            }
        }

        return closest;
    }

    IEnumerator MoveToTarget()
    {
        if (showDebugLogs)
            Debug.Log($"Orb moving from {transform.position} to {targetPosition}");

        while (!hasArrived)
        {
            // Move towards target
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Check if arrived
            if (Vector3.Distance(transform.position, targetPosition) <= arrivalDistance)
            {
                hasArrived = true;
                StartCoroutine(TransformBackToMutant());
            }

            yield return null;
        }
    }

    IEnumerator TransformBackToMutant()
    {
        if (showDebugLogs)
            Debug.Log("Orb arrived at destination, transforming back to mutant");

        // Play arrival effects
        if (orbArrivalSound != null && audioSource != null)
            audioSource.PlayOneShot(orbArrivalSound);

        // Stop orb effects
        if (orbEffect != null)
            orbEffect.Stop();

        yield return new WaitForSeconds(0.5f);

        // Transform back to mutant
        if (originalMutant != null)
        {
            // Find ground position
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f))
            {
                Vector3 groundPosition = hit.point + Vector3.up * 0.1f;
                originalMutant.SetHealthAndCooldown(storedHealth, storedChargeTime);
                originalMutant.TransformFromOrb(groundPosition);

                if (showDebugLogs)
                    Debug.Log($"Mutant restored at {groundPosition} with health: {storedHealth}");
            }
            else
            {
                // Fallback if raycast fails
                Vector3 fallbackPosition = targetPosition;
                fallbackPosition.y = transform.position.y - floatHeight;
                originalMutant.SetHealthAndCooldown(storedHealth, storedChargeTime);
                originalMutant.TransformFromOrb(fallbackPosition);

                if (showDebugLogs)
                    Debug.Log($"Mutant restored at fallback position {fallbackPosition}");
            }
        }
        else
        {
            Debug.LogError("Original mutant reference is null!");
        }

        // Destroy orb
        Destroy(gameObject);
    }
}