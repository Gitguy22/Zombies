using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MutantSpawner : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField] GameObject mutantPrefab;
    [SerializeField] int firstSpawnRound = 9;
    [SerializeField] int spawnIntervalMin = 2;
    [SerializeField] int spawnIntervalMax = 4;
    [SerializeField] int doubleSpawnStartRound = 20;
    [SerializeField] float spawnHeightOffset = 0.5f;
    [SerializeField] float maxSpawnHeight = 5f; // Workaround for roof spawning

    [Header("References")]
    [SerializeField] RoundData roundData;
    [SerializeField] RoundManager roundManager;

    [Header("Debug")]
    [SerializeField] bool showDebugLogs = true;

    private int lastSpawnRound = 0;
    private int nextSpawnRound = 0;
    private bool hasSpawnedThisRound = false;

    void Start()
    {
        if (roundData == null)
            roundData = FindObjectOfType<RoundManager>()?.roundData;

        if (roundManager == null)
            roundManager = FindObjectOfType<RoundManager>();

        CalculateNextSpawnRound();

        if (showDebugLogs)
            Debug.Log($"MutantSpawner initialized. First spawn round: {firstSpawnRound}");
    }

    void Update()
    {
        if (roundData == null) return;

        int currentRound = roundData.CurrentRound;

        // Check if it's time to spawn and we haven't spawned this round yet
        if (currentRound >= firstSpawnRound &&
            currentRound >= nextSpawnRound &&
            !hasSpawnedThisRound &&
            roundData.IsRoundActive)
        {
            SpawnMutants();
            hasSpawnedThisRound = true;
            lastSpawnRound = currentRound;
            CalculateNextSpawnRound();
        }

        // Reset spawn flag when round changes
        if (currentRound != lastSpawnRound)
        {
            hasSpawnedThisRound = false;
        }
    }

    void CalculateNextSpawnRound()
    {
        int interval = Random.Range(spawnIntervalMin, spawnIntervalMax + 1);
        nextSpawnRound = lastSpawnRound + interval;

        if (showDebugLogs)
            Debug.Log($"Next mutant spawn scheduled for round {nextSpawnRound}");
    }

    void SpawnMutants()
    {
        if (mutantPrefab == null)
        {
            Debug.LogError("Mutant prefab is not assigned!");
            return;
        }

        int spawnCount = (roundData.CurrentRound >= doubleSpawnStartRound) ? 2 : 1;

        if (showDebugLogs)
            Debug.Log($"Spawning {spawnCount} mutant(s) at round {roundData.CurrentRound}");

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            if (spawnPosition != Vector3.zero)
            {
                SpawnMutant(spawnPosition);
                if (showDebugLogs)
                    Debug.Log($"Spawned mutant {i + 1}/{spawnCount} at position {spawnPosition}");
            }
            else
            {
                Debug.LogWarning($"Failed to find spawn position for mutant {i + 1}");
            }
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        // Get player's current area from RoundManager
        if (roundManager?.currentArea?.spawnPoints == null || roundManager.currentArea.spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points available in current area");
            return Vector3.zero;
        }

        // Try multiple times to find a good spawn position
        for (int attempts = 0; attempts < 10; attempts++)
        {
            // Pick a random spawn point as reference
            GameObject referenceSpawn = roundManager.currentArea.spawnPoints[Random.Range(0, roundManager.currentArea.spawnPoints.Count)];

            // Sample a random position on navmesh near the reference point
            Vector3 randomDirection = Random.insideUnitSphere * 15f;
            randomDirection.y = 0; // Keep on ground level initially
            Vector3 targetPosition = referenceSpawn.transform.position + randomDirection;

            // Sample position on navmesh - FIXED: Use -1 instead of NavMesh.AllAreas
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 15f, -1))
            {
                // Check if position is not too high (roof workaround)
                if (hit.position.y <= referenceSpawn.transform.position.y + maxSpawnHeight)
                {
                    return hit.position + Vector3.up * spawnHeightOffset;
                }
            }
        }

        // Fallback to a spawn point if random sampling fails
        GameObject fallbackSpawn = roundManager.currentArea.spawnPoints[Random.Range(0, roundManager.currentArea.spawnPoints.Count)];

        if (showDebugLogs)
            Debug.Log("Using fallback spawn point for mutant");

        return fallbackSpawn.transform.position + Vector3.up * spawnHeightOffset;
    }

    void SpawnMutant(Vector3 position)
    {
        GameObject mutant = Instantiate(mutantPrefab, position, Quaternion.identity);

        // Set up any additional configuration here if needed
        MutantAI mutantAI = mutant.GetComponent<MutantAI>();
        if (mutantAI != null)
        {
            // Any additional setup can go here
            if (showDebugLogs)
                Debug.Log($"Mutant AI component found and configured");
        }
        else
        {
            Debug.LogError("Spawned mutant does not have MutantAI component!");
        }
    }

    // Public method to manually trigger spawn (for testing)
    public void ForceSpawn()
    {
        SpawnMutants();
    }

    // Public method to check if mutants should spawn this round
    public bool ShouldSpawnThisRound()
    {
        if (roundData == null) return false;

        int currentRound = roundData.CurrentRound;
        return currentRound >= firstSpawnRound &&
               currentRound >= nextSpawnRound &&
               !hasSpawnedThisRound;
    }

    // Public getters for inspector/debugging
    public int GetNextSpawnRound() => nextSpawnRound;
    public bool HasSpawnedThisRound() => hasSpawnedThisRound;
}