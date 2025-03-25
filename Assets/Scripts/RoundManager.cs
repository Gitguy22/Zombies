using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ObstacleDependentSpawnPoints
{
    public GameObject obstacle; // The obstacle that needs to be destroyed
    public List<GameObject> spawnPoints; // The spawn points that will be activated if the obstacle is destroyed
}

[System.Serializable]
public class MapArea
{
    public string areaName;
    public Collider areaCollider; // Collider for the area
    public List<GameObject> spawnPoints; // List of regular spawn points in the area
    public List<ObstacleDependentSpawnPoints> obstacleDependentSpawnPoints; // List of obstacle-dependent spawn points
}

public class RoundManager : MonoBehaviour
{
    public int currentRound = 1;
    public float zombiesLeft;
    public int zombiesOnMap;
    public int playersInGame;

    public List<MapArea> mapAreas;
    private List<GameObject> activeSpawnPoints;

    public GameObject zombiePrefab;
    public GameObject player;

    public TextMeshProUGUI roundText;

    public AudioClip roundStartClip;
    public AudioClip roundEndClip;

    private AudioSource audioSource;

    private MapArea currentArea;

    // Dictionary to track the last spawn time for each spawn point
    private Dictionary<GameObject, float> spawnPointLastSpawnTime;

    // Reference to the ZombiePool
    private ZombiePool zombiePool;

    // Flag to track if the round is ending
    private bool isRoundEnding = false;

    void Start()
    {
        // Get the ZombiePool reference
        zombiePool = ZombiePool.Instance;

        // If ZombiePool doesn't exist, create one
        if (zombiePool == null)
        {
            GameObject poolObject = new GameObject("ZombiePool");
            zombiePool = poolObject.AddComponent<ZombiePool>();
        }

        // Initialize the zombie pool with our zombie prefab
        zombiePool.Initialize(zombiePrefab, 30);

        RoundStart();
        activeSpawnPoints = new List<GameObject>();
        spawnPointLastSpawnTime = new Dictionary<GameObject, float>();

        audioSource = GetComponent<AudioSource>();

        // Assuming the player starts in the first defined area
        if (mapAreas.Count > 0)
        {
            SetCurrentArea(mapAreas[0]);
        }

        // Update the round text at the start
        UpdateRoundText();

        // Play round start music
        PlayRoundAudio(roundStartClip);

        playersInGame = GameObject.FindGameObjectsWithTag("Player").Length;
    }

    void Update()
    {
        // Skip updates if the round is ending
        if (isRoundEnding)
        {
            return;
        }

        // Check what area they're in.
        CheckPlayerArea();

        int maxZombiesOnMap = GetMaxZombiesOnMap();

        if (zombiesOnMap < maxZombiesOnMap) // Stop spawning zombies if there are already max on the map
        {
            if (zombiesLeft > 0)
            {
                SpawnZombie();
            }
        }

        // Check for destroyed obstacles and update spawn points
        CheckDestroyedObstacles();

        // Update zombiesOnMap based on the active zombies count
        if (zombiePool != null)
        {
            zombiesOnMap = zombiePool.ActiveZombieCount();

            // Check if we need to end the round
            if (zombiesLeft <= 0 && zombiesOnMap <= 0 && !isRoundEnding)
            {
                RoundEnd();
            }
        }
    }

    void CheckPlayerArea()
    {
        foreach (var area in mapAreas)
        {
            // Check if the player is within the bounds of the current area's collider
            if (area.areaCollider.bounds.Contains(player.transform.position))
            {
                // If the player is in a new area, update the current area
                if (currentArea != area)
                {
                    SetCurrentArea(area);
                }
                break; // Exit the loop once the relevant area is found
            }
        }
    }

    void SetCurrentArea(MapArea area)
    {
        currentArea = area;
        activeSpawnPoints.Clear();
        activeSpawnPoints.AddRange(area.spawnPoints);

        Debug.Log("Player entered area: " + area.areaName);

        // Initialize last spawn times for new active spawn points
        foreach (var spawnPoint in activeSpawnPoints)
        {
            if (!spawnPointLastSpawnTime.ContainsKey(spawnPoint))
            {
                spawnPointLastSpawnTime[spawnPoint] = -Mathf.Infinity; // Set to negative infinity to allow immediate spawning
            }
        }
    }

    void CheckDestroyedObstacles()
    {
        // Check if currentArea is null to avoid null reference
        if (currentArea == null)
        {
            Debug.LogWarning("currentArea is null. Ensure it is properly initialized.");
            return;
        }

        // Check if obstacleDependentSpawnPoints is null
        if (currentArea.obstacleDependentSpawnPoints == null)
        {
            Debug.LogWarning("obstacleDependentSpawnPoints is null in currentArea.");
            return;
        }

        foreach (var obstacleSet in currentArea.obstacleDependentSpawnPoints)
        {
            // Check if obstacleSet is null
            if (obstacleSet == null)
            {
                Debug.LogWarning("obstacleSet is null. Skipping.");
                continue; // Skip this iteration
            }

            // Check if the obstacle is null
            if (obstacleSet.obstacle == null)
            {
                // If the obstacle is destroyed, handle the spawn points
                if (obstacleSet.spawnPoints != null)
                {
                    foreach (var spawnPoint in obstacleSet.spawnPoints)
                    {
                        // Check if spawnPoint is null
                        if (spawnPoint == null)
                        {
                            Debug.LogWarning("spawnPoint is null. Skipping.");
                            continue; // Skip this spawn point if it's null
                        }

                        if (!activeSpawnPoints.Contains(spawnPoint))
                        {
                            activeSpawnPoints.Add(spawnPoint);

                            // Initialize last spawn time for the new spawn point
                            if (!spawnPointLastSpawnTime.ContainsKey(spawnPoint))
                            {
                                spawnPointLastSpawnTime[spawnPoint] = -Mathf.Infinity;
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("spawnPoints is null for an obstacleSet.");
                }
            }
        }
    }

    public void SpawnZombie()
    {
        if (activeSpawnPoints.Count == 0)
        {
            //Debug.LogWarning("No active spawn points available.");
            return;
        }

        // Find an available spawn point that hasn't been used in the last 3 seconds
        float currentTime = Time.time;
        List<GameObject> availableSpawnPoints = new List<GameObject>();

        foreach (var spawnPoint in activeSpawnPoints)
        {
            if (currentTime - spawnPointLastSpawnTime[spawnPoint] >= 3.0f)
            {
                availableSpawnPoints.Add(spawnPoint);
            }
        }

        if (availableSpawnPoints.Count == 0)
        {
            //Debug.LogWarning("No available spawn points (waiting for cooldown).");
            return;
        }

        // Get a random available spawn point
        int index = Random.Range(0, availableSpawnPoints.Count);
        GameObject randomSpawnPoint = availableSpawnPoints[index];

        // Use zombie pool to get a zombie
        if (zombiePool != null)
        {
            GameObject zombie = zombiePool.GetZombie(randomSpawnPoint.transform.position, Quaternion.identity);

            if (zombie != null)
            {
                // Configure the zombie
                ZombieAI zombieAI = zombie.GetComponent<ZombieAI>();
                if (zombieAI != null)
                {
                    zombieAI.player = player;
                    zombieAI.roundManager = gameObject;
                }

                zombiesLeft--;

                // Update the last spawn time for the chosen spawn point
                spawnPointLastSpawnTime[randomSpawnPoint] = currentTime;
            }
        }
        else
        {
            Debug.LogError("ZombiePool is not available!");
        }
    }

    public void RoundEnd()
    {
        if (isRoundEnding)
        {
            return; // Prevent multiple calls
        }

        isRoundEnding = true;
        Debug.Log("Round " + currentRound + " ended.");

        // Play round end music
        PlayRoundAudio(roundEndClip);

        StartCoroutine(StartNextRoundAfterDelay(10f)); // Start next round after a 16-second delay
    }

    private IEnumerator StartNextRoundAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        currentRound++;
        Debug.Log("Round " + currentRound + " started.");

        // Clear any remaining zombies
        if (zombiePool != null)
        {
            zombiePool.ReturnAllZombies();
        }

        zombiesOnMap = 0;
        UpdateRoundText();
        PlayRoundAudio(roundStartClip);
        RoundStart();

        isRoundEnding = false;
    }

    private void RoundStart()
    {
        // Setting zombies for each round using if statements like in original code
        if (currentRound == 1)
        {
            zombiesLeft = 6;
        }
        else if (currentRound == 2)
        {
            zombiesLeft = 8;
        }
        else if (currentRound == 3)
        {
            zombiesLeft = 13;
        }
        else if (currentRound == 4)
        {
            zombiesLeft = 18;
        }
        else if (currentRound == 5)
        {
            zombiesLeft = 24;
        }
        else if (currentRound == 6)
        {
            zombiesLeft = 27;
        }
        else if (currentRound == 7)
        {
            zombiesLeft = 28;
        }
        else if (currentRound == 8)
        {
            zombiesLeft = 28;
        }
        else if (currentRound == 9)
        {
            zombiesLeft = 29;
        }
        else // For rounds 10 and above - fixed formula to properly scale
        {
            // Better formula that increases with round number rather than decreasing
            zombiesLeft = 30 + (currentRound - 9) * 3;
        }

        Debug.Log($"Round {currentRound} starting with {zombiesLeft} zombies to spawn");
    }

    private int GetMaxZombiesOnMap()
    {
        int baseZombies = 24;
        int additionalZombies = playersInGame * 6;
        int roundScaling = Mathf.FloorToInt(currentRound * 0.5f); // Small increase per round

        return baseZombies + additionalZombies + roundScaling;
    }

    private void UpdateRoundText()
    {
        if (roundText != null)
        {
            roundText.text = "" + currentRound;
        }
    }

    private void PlayRoundAudio(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
