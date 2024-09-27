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

    void Start()
    {
        RoundStart();
        activeSpawnPoints = new List<GameObject>();
        spawnPointLastSpawnTime = new Dictionary<GameObject, float>();

        audioSource = GetComponent<AudioSource>(); // Get the AudioSource component attached to this GameObject

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
        foreach (var obstacleSet in currentArea.obstacleDependentSpawnPoints)
        {
            if (obstacleSet.obstacle == null) // If the obstacle is destroyed
            {
                foreach (var spawnPoint in obstacleSet.spawnPoints)
                {
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
        }
    }

    public void SpawnZombie()
    {
        if (activeSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No active spawn points available.");
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
            Debug.LogWarning("No available spawn points (waiting for cooldown).");
            return;
        }

        // Get a random available spawn point
        int index = Random.Range(0, availableSpawnPoints.Count);
        GameObject randomSpawnPoint = availableSpawnPoints[index];

        // Instantiate the zombie at the random spawn point
        Instantiate(zombiePrefab, randomSpawnPoint.transform.position, Quaternion.identity);
        zombiesOnMap++;
        zombiesLeft--;

        // Update the last spawn time for the chosen spawn point
        spawnPointLastSpawnTime[randomSpawnPoint] = currentTime;
    }

    public void RoundEnd()
    {
        // Play round end music
        PlayRoundAudio(roundEndClip);

        StartCoroutine(StartNextRoundAfterDelay(16f)); // Start next round after a 16-second delay
    }

    private IEnumerator StartNextRoundAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        currentRound++;
        Debug.Log("Round " + currentRound + " started.");
        zombiesOnMap = 0;
        UpdateRoundText();
        PlayRoundAudio(roundStartClip);
        RoundStart();
    }

    private void RoundStart()
    {
        int maxZombiesOnMap = GetMaxZombiesOnMap();
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
        else // For rounds 10 and above
        {
            zombiesLeft = currentRound * 0.15f;
        }
    }

    private int GetMaxZombiesOnMap()
    {
        int baseZombies = 24;
        int additionalZombies = playersInGame * 6;
        return baseZombies + additionalZombies;
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