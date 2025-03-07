using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombiePool : MonoBehaviour
{
    public static ZombiePool Instance;

    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private int initialPoolSize = 30;
    [SerializeField] private bool expandable = true;

    private Queue<GameObject> zombiePool;
    private List<GameObject> activeZombies;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        zombiePool = new Queue<GameObject>();
        activeZombies = new List<GameObject>();
    }

    // Initialize the pool with a specific zombie prefab
    public void Initialize(GameObject prefab, int poolSize = 30)
    {
        zombiePrefab = prefab;
        initialPoolSize = poolSize;

        // Initialize the pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewZombie();
        }
    }

    private GameObject CreateNewZombie()
    {
        if (zombiePrefab == null)
        {
            Debug.LogError("Zombie prefab is not set in the ZombiePool!");
            return null;
        }

        GameObject zombie = Instantiate(zombiePrefab, transform);
        zombie.SetActive(false);
        zombiePool.Enqueue(zombie);
        return zombie;
    }

    public GameObject GetZombie(Vector3 position, Quaternion rotation)
    {
        if (zombiePool.Count == 0 && !expandable)
        {
            Debug.LogWarning("Zombie pool is empty and not expandable!");
            return null;
        }

        // Create a new zombie if the pool is empty but expandable
        if (zombiePool.Count == 0)
        {
            CreateNewZombie();
        }

        // Get a zombie from the pool
        GameObject zombie = zombiePool.Dequeue();

        // Reset the zombie's position and rotation
        zombie.transform.position = position;
        zombie.transform.rotation = rotation;

        // Reset the zombie's state
        ZombieAI zombieAI = zombie.GetComponent<ZombieAI>();
        if (zombieAI != null)
        {
            zombieAI.ResetZombie();
        }

        // Activate the zombie
        zombie.SetActive(true);
        activeZombies.Add(zombie);

        return zombie;
    }

    public void ReturnZombie(GameObject zombie)
    {
        // Deactivate the zombie
        zombie.SetActive(false);

        // Return the zombie to the pool
        zombiePool.Enqueue(zombie);
        activeZombies.Remove(zombie);
    }

    public void ReturnAllZombies()
    {
        // Create a temporary list to avoid collection modification issues
        List<GameObject> zombiesToReturn = new List<GameObject>(activeZombies);

        foreach (GameObject zombie in zombiesToReturn)
        {
            ReturnZombie(zombie);
        }
    }

    public int ActiveZombieCount()
    {
        return activeZombies.Count;
    }

    public int PooledZombieCount()
    {
        return zombiePool.Count;
    }
}