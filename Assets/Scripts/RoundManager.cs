using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObstacleDependentSpawnPoints
{
    public GameObject obstacle;
    public List<GameObject> spawnPoints;
}

[System.Serializable]
public class MapArea
{
    public string areaName;
    public Collider areaCollider;
    public List<GameObject> spawnPoints;
    public List<ObstacleDependentSpawnPoints> obstacleDependentSpawnPoints;
}

public class RoundManager : MonoBehaviour
{
    [Header("Round Data")]
    [Tooltip("Assign the RoundData scriptable object here")]
    public RoundData roundData;

    [Header("Zombie Settings")]
    public float zombiesLeft;
    public int zombiesOnMap;
    public int zombiesAlive;
    public int playersInGame;

    [Header("Zombie Respawn System")]
    [Tooltip("Maximum distance before zombie gets respawned")]
    [SerializeField] float maxZombieDistance = 50f;
    [Tooltip("How often to check zombie distances (seconds)")]
    [SerializeField] float distanceCheckInterval = 3f;
    [Tooltip("Minimum distance from spawn point to avoid instant respawn")]
    [SerializeField] float minSpawnDistance = 5f;
    private float nextDistanceCheck = 0f;

    [Header("Spawning Settings")]
    [SerializeField] float spawnInterval = 1f; // Time between spawns
    [SerializeField] float spawnPointCooldown = 3f; // Cooldown per spawn point
    private float nextSpawnTime = 0f;

    [Header("Mutant Settings")]
    public MutantSpawner mutantSpawner;

    [Header("Map Settings")]
    public List<MapArea> mapAreas;
    private List<GameObject> activeSpawnPoints;

    [Header("Prefabs")]
    public GameObject zombiePrefab;
    public GameObject player;

    [Header("Audio")]
    public AudioClip roundStartClip;
    public AudioClip roundEndClip;

    [Header("Debug")]
    [SerializeField] bool enableDebugLogs = true;

    private AudioSource audioSource;
    public MapArea currentArea;
    private Dictionary<GameObject, float> spawnPointLastSpawnTime;
    private ZombiePool zombiePool;
    private bool isRoundEnding = false;
    private List<GameObject> allPlayers = new List<GameObject>();

    private void Start()
    {
        // Reset round data at game start just to saffe
        if (roundData != null)
        {
            roundData.ResetRoundData();
            //Debug.Log("Round data reset at game start");
        }

        StartCoroutine(InitializeRoundManager());
    }

    private IEnumerator InitializeRoundManager()
    {
        //Debug.Log("RoundManager initializing...");

        // Wait a frame to ensure all objects are initialized
        yield return null;

        // Validate essential components
        if (!ValidateEssentialComponents())
        {
            //Debug.LogError("RoundManager initialization failed - missing essential components!");
            yield break;
        }

        // Initialize collections
        activeSpawnPoints = new List<GameObject>();
        spawnPointLastSpawnTime = new Dictionary<GameObject, float>();
        audioSource = GetComponent<AudioSource>();

        // Find mutant spawner
        if (mutantSpawner == null)
        {
            mutantSpawner = FindObjectOfType<MutantSpawner>();
        }

        // Initialize zombie counters
        zombiesLeft = 0;
        zombiesOnMap = 0;
        zombiesAlive = 0;

        // Find and validate players
        if (!InitializePlayers())
        {
            //Debug.LogError("No valid players found!");
            yield break;
        }

        // Initialize map areas
        if (!InitializeMapAreas())
        {
            //Debug.LogError("No valid map areas found!");
            yield break;
        }

        // Initialize zombie pool
        if (!InitializeZombiePool())
        {
            //Debug.LogError("Failed to initialize zombie pool!");
            yield break;
        }

        // Initialize round data
        roundData.SetCurrentRound(0);
        roundData.SetRoundActive(false);

        playersInGame = allPlayers.Count;
        //Debug.Log($"RoundManager initialized successfully with {playersInGame} players");

        // Start the first round after a short delay
        yield return new WaitForSeconds(1f);
        StartFirstRound();
    }


    private bool ValidateEssentialComponents()
    {
        bool isValid = true;

        if (roundData == null)
        {
            //Debug.LogError("RoundData not assigned to RoundManager!");
            isValid = false;
        }

        if (zombiePrefab == null)
        {
            //Debug.LogError("Zombie Prefab not assigned to RoundManager!");
            isValid = false;
        }
        else
        {
            // Check if zombie prefab has ZombieAI
            ZombieAI zombieAI = zombiePrefab.GetComponent<ZombieAI>();
            if (zombieAI == null)
            {
                //Debug.LogError(" Zombie Prefab is missing ZombieAI component!");
                isValid = false;
            }
        }

        if (mapAreas == null || mapAreas.Count == 0)
        {
            //Debug.LogError(" No Map Areas defined in RoundManager!");
            isValid = false;
        }

        return isValid;
    }

    private bool InitializePlayers()
    {
        RefreshPlayerList();

        if (allPlayers.Count == 0)
        {
            //Debug.LogError(" No players found! Make sure player objects have 'Player' tag.");
            return false;
        }

        // Set main player reference if not set
        if (player == null && allPlayers.Count > 0)
        {
            player = allPlayers[0];
        }

        //Debug.Log($" Found {allPlayers.Count} players");
        return true;
    }

    private bool InitializeMapAreas()
    {
        if (mapAreas.Count == 0)
        {
            return false;
        }

        // Validate first area and set as current
        MapArea firstArea = mapAreas[0];
        if (firstArea == null)
        {
            //Debug.LogError("First map area is null!");
            return false;
        }

        if (firstArea.spawnPoints == null || firstArea.spawnPoints.Count == 0)
        {
            //Debug.LogError($"First map area '{firstArea.areaName}' has no spawn points!");
            return false;
        }

        SetCurrentArea(firstArea);
        //Debug.Log($" Initialized with area: {firstArea.areaName} ({firstArea.spawnPoints.Count} spawn points)");
        return true;
    }


    private bool InitializeZombiePool()
    {
        // Get or create zombie pool
        zombiePool = ZombiePool.Instance;
        if (zombiePool == null)
        {
            //Debug.LogWarning(" ZombiePool.Instance is null, looking for ZombiePool in scene...");
            zombiePool = FindObjectOfType<ZombiePool>();

            if (zombiePool == null)
            {
                //Debug.LogError(" No ZombiePool found in scene! Add ZombiePool script to a GameObject.");
                return false;
            }
        }

        // Initialize the pool
        if (!zombiePool.IsInitialized())
        {
            //Debug.Log(" Initializing ZombiePool...");
            zombiePool.Initialize(zombiePrefab, 30);

            // Verify initialization
            if (!zombiePool.IsInitialized())
            {
                //Debug.LogError(" Failed to initialize ZombiePool!");
                return false;
            }
        }

        //Debug.Log($" ZombiePool ready: {zombiePool.GetPoolStatus()}");
        return true;
    }

    void Update()
    {
        if (isRoundEnding || !roundData.IsRoundActive)
        {
            return;
        }

        CheckPlayerArea();
        HandleZombieSpawning();
        CheckDestroyedObstacles();  
        UpdateZombieCounts();  
    }
    private void HandleZombieSpawning()
    {
        // Check if we can spawn zombies
        if (zombiesLeft <= 0 || Time.time < nextSpawnTime)
        {
            return;
        }

        int maxZombiesOnMap = GetMaxZombiesOnMap();
        if (zombiesOnMap >= maxZombiesOnMap)
        {
            if (enableDebugLogs)
            {
                //Debug.Log($"Max zombies on map reached: {zombiesOnMap}/{maxZombiesOnMap}");
            }
            return;
        }

        // Try to spawn a zombie
        if (SpawnZombie())
        {
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void UpdateZombieCounts()
    {
        if (zombiePool != null)
        {
            zombiesOnMap = zombiePool.ActiveZombieCount();
        }
    }


    public bool SpawnZombie()
    {
        if (activeSpawnPoints.Count == 0)
        {
            //Debug.LogError("No active spawn points available!");
            return false;
        }

        if (zombiePool == null)
        {
            //Debug.LogError("ZombiePool is null!");
            return false;
        }

        if (!zombiePool.IsInitialized())
        {
            //Debug.LogError("ZombiePool is not initialized!");
            return false;
        }

        // Find available spawn points
        List<GameObject> availableSpawnPoints = GetAvailableSpawnPoints();
        if (availableSpawnPoints.Count == 0)
        {
            if (enableDebugLogs)
            {
                //Debug.Log("No available spawn points (all on cooldown)");
            }
            return false;
        }

        // Select random spawn point
        GameObject spawnPoint = availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];

        // Spawn zombie
        GameObject zombie = zombiePool.GetZombie(spawnPoint.transform.position, spawnPoint.transform.rotation);

        if (zombie == null)
        {
            //Debug.LogError("ZombiePool.GetZombie() returned null!");
            return false;
        }

        // Configure zombie
        ZombieAI zombieAI = zombie.GetComponent<ZombieAI>();
        if (zombieAI != null)
        {
            zombieAI.player = GetNearestPlayer(spawnPoint.transform.position);
            zombieAI.roundManager = gameObject;
        }
        else
        {
            //Debug.LogError($"Spawned zombie '{zombie.name}' has no ZombieAI component!");
        }

        // Update counters
        zombiesLeft--;
        zombiesAlive++;
        spawnPointLastSpawnTime[spawnPoint] = Time.time;

        if (enableDebugLogs)
        {
            //Debug.Log($"Spawned zombie at {spawnPoint.name}: {zombiesAlive} alive, {zombiesLeft} left to spawn");
        }

        return true;
    }

    private List<GameObject> GetAvailableSpawnPoints()
    {
        List<GameObject> availableSpawnPoints = new List<GameObject>();
        float currentTime = Time.time;

        foreach (GameObject spawnPoint in activeSpawnPoints)
        {
            if (spawnPoint == null) continue;

            if (!spawnPointLastSpawnTime.ContainsKey(spawnPoint) ||
                currentTime - spawnPointLastSpawnTime[spawnPoint] >= spawnPointCooldown)
            {
                availableSpawnPoints.Add(spawnPoint);
            }
        }

        return availableSpawnPoints;
    }

    public void NotifyZombieDeath()
    {
        if (isRoundEnding)
        {
            return;
        }

        zombiesAlive--;

        if (enableDebugLogs)
        {
            //Debug.Log($"Zombie died: {zombiesAlive} alive, {zombiesLeft} left to spawn");
        }

        // Check if round should end
        if (zombiesLeft <= 0 && zombiesAlive <= 0)
        {
            //Debug.Log("Last zombie killed - ending round");
            RoundEnd();
        }
    }

    void RefreshPlayerList()
    {
        allPlayers.Clear();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        allPlayers.AddRange(players);

        if (player == null && allPlayers.Count > 0)
        {
            player = allPlayers[0];
        }
    }


    public void HandleDistantZombieReturn()
    {
        if (zombiesLeft >= 0)
        {
            zombiesLeft++;
            zombiesOnMap--;
            zombiesAlive--; 

            if (enableDebugLogs)
            {
                //Debug.Log($"Distant zombie returned to pool. Alive: {zombiesAlive}, Left to spawn: {zombiesLeft}, On map: {zombiesOnMap}");
            }
        }
    }

    /// <summary>
    /// Get the nearest player to a specific position
    /// </summary>
    GameObject GetNearestPlayer(Vector3 position)
    {
        GameObject nearestPlayer = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject player in allPlayers)
        {
            if (player == null) continue;

            float distance = Vector3.Distance(position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestPlayer = player;
            }
        }

        return nearestPlayer ?? (allPlayers.Count > 0 ? allPlayers[0] : null);
    }

    void CheckPlayerArea()
    {
        foreach (GameObject currentPlayer in allPlayers)
        {
            if (currentPlayer == null) continue;

            foreach (var area in mapAreas)
            {
                if (area?.areaCollider != null && area.areaCollider.bounds.Contains(currentPlayer.transform.position))
                {
                    if (currentArea != area)
                    {
                        SetCurrentArea(area);
                    }
                    return;
                }
            }
        }
    }

    void SetCurrentArea(MapArea area)
    {
        if (area == null)
        {
            //Debug.LogError("Trying to set null area!");
            return;
        }

        currentArea = area;
        activeSpawnPoints.Clear();

        if (area.spawnPoints != null)
        {
            // Filter out null spawn points
            foreach (GameObject spawnPoint in area.spawnPoints)
            {
                if (spawnPoint != null)
                {
                    activeSpawnPoints.Add(spawnPoint);
                }
            }
        }

        // Initialize spawn point cooldowns
        foreach (var spawnPoint in activeSpawnPoints)
        {
            if (!spawnPointLastSpawnTime.ContainsKey(spawnPoint))
            {
                spawnPointLastSpawnTime[spawnPoint] = -Mathf.Infinity;
            }
        }

        //Debug.Log($"🗺️ Entered area: {area.areaName} with {activeSpawnPoints.Count} spawn points");
    }

    void CheckDestroyedObstacles()
    {
        if (currentArea?.obstacleDependentSpawnPoints == null) return;

        foreach (var obstacleSet in currentArea.obstacleDependentSpawnPoints)
        {
            if (obstacleSet?.obstacle == null) continue;

            IInteractable interactable = obstacleSet.obstacle.GetComponent<IInteractable>();
            if (interactable?.IsPaidFor() == true && obstacleSet.spawnPoints != null)
            {
                foreach (var spawnPoint in obstacleSet.spawnPoints)
                {
                    if (spawnPoint != null && !activeSpawnPoints.Contains(spawnPoint))
                    {
                        activeSpawnPoints.Add(spawnPoint);
                        if (!spawnPointLastSpawnTime.ContainsKey(spawnPoint))
                        {
                            spawnPointLastSpawnTime[spawnPoint] = -Mathf.Infinity;
                        }
                    }
                }
            }
        }
    }

    public void RoundEnd()
    {
        if (isRoundEnding) return;

        isRoundEnding = true;
        roundData.SetRoundActive(false);

        int currentRound = roundData.CurrentRound;
        //Debug.Log($"🏁 Round {currentRound} ended");

        PlayRoundAudio(roundEndClip);

        if (currentRound > 0)
        {
            StartCoroutine(StartNextRoundAfterDelay(10f));
        }
        else
        {
            StartFirstRound();
        }
    }

    private void StartFirstRound()
    {
        //Debug.Log("🎯 Starting first round...");
        StartNextRound();
    }

    private IEnumerator StartNextRoundAfterDelay(float delaySeconds)
    {
        //Debug.Log($"⏰ Next round in {delaySeconds} seconds");
        yield return new WaitForSeconds(delaySeconds);
        StartNextRound();
    }

    private void StartNextRound()
    {
        int nextRound = roundData.CurrentRound + 1;
        roundData.SetCurrentRound(nextRound);
        roundData.SetRoundActive(true);

        //Debug.Log($"🎮 Round {nextRound} started");

        // Clear remaining zombies
        if (zombiePool != null)
        {
            zombiePool.ReturnAllZombies();
        }

        // Reset counters
        zombiesOnMap = 0;
        zombiesAlive = 0;

        PlayRoundAudio(roundStartClip);
        RoundStart();

        isRoundEnding = false;
    }

    private void RoundStart()
    {
        int currentRound = roundData.CurrentRound;

        // Set zombie count based on round
        if (currentRound == 1) zombiesLeft = 6;
        else if (currentRound == 2) zombiesLeft = 8;
        else if (currentRound == 3) zombiesLeft = 13;
        else if (currentRound == 4) zombiesLeft = 18;
        else if (currentRound == 5) zombiesLeft = 24;
        else if (currentRound == 6) zombiesLeft = 27;
        else if (currentRound == 7) zombiesLeft = 28;
        else if (currentRound == 8) zombiesLeft = 28;
        else if (currentRound == 9) zombiesLeft = 29;
        else zombiesLeft = 30 + (currentRound - 9) * 3;

        //Debug.Log($" Round {currentRound}: {zombiesLeft} zombies to spawn");
    }

    private int GetMaxZombiesOnMap()
    {
        int baseZombies = 24;
        int additionalZombies = playersInGame * 6;
        int roundScaling = Mathf.FloorToInt(roundData.CurrentRound * 0.5f);
        return baseZombies + additionalZombies + roundScaling;
    }

    private void PlayRoundAudio(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    // Debug methods
    [ContextMenu("Force Spawn Zombie")]
    public void DebugForceSpawn()
    {
        SpawnZombie();
    }

    [ContextMenu("Debug Pool Status")]
    public void DebugPoolStatus()
    {
        if (zombiePool != null)
        {
            //Debug.Log($"Pool Status: {zombiePool.GetPoolStatus()}");
        }
    }
}