using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombiePool : MonoBehaviour
{
    public static ZombiePool Instance { get; private set; }

    [Header("Pool Configuration")]
    [SerializeField] GameObject zombiePrefab;
    [SerializeField] int initialPoolSize = 30;
    [SerializeField] bool expandable = true;
    [SerializeField] Transform poolParent;

    [Header("Debug")]
    [SerializeField] bool enableDebugLogs = false;

    private Queue<GameObject> zombiePool;
    private List<GameObject> activeZombies;
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            ////Debug.LogWarning("Multiple ZombiePool instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // REMOVED: DontDestroyOnLoad(gameObject);

        zombiePool = new Queue<GameObject>();
        activeZombies = new List<GameObject>();

        if (poolParent == null)
        {
            GameObject parentObj = new GameObject("ZombiePoolParent");
            poolParent = parentObj.transform;
            poolParent.SetParent(transform);
        }

        ////Debug.Log("ZombiePool Awake completed");
    }

    private void Start()
    {
        if (zombiePrefab != null && !isInitialized)
        {
            Initialize(zombiePrefab, initialPoolSize);
        }
    }

    public void Initialize(GameObject prefab, int poolSize = 30)
    {
        if (isInitialized)
        {
            ////Debug.LogWarning("ZombiePool already initialized!");
            return;
        }

        if (prefab == null)
        {
            ////Debug.LogError("Cannot initialize ZombiePool: prefab is null!");
            return;
        }

        zombiePrefab = prefab;
        initialPoolSize = poolSize;

        ////Debug.Log($"Initializing ZombiePool with {poolSize} zombies using prefab: {prefab.name}");

        zombiePool.Clear();
        activeZombies.Clear();

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject zombie = CreateNewZombie();
            if (zombie != null)
            {
                zombie.name = $"PooledZombie_{i}";
                zombie.SetActive(false);
                zombiePool.Enqueue(zombie);
            }
            else
            {
                ////Debug.LogError($"Failed to create zombie {i} during pool initialization!");
                break;
            }
        }

        isInitialized = true;
        ////Debug.Log($"ZombiePool initialized successfully with {zombiePool.Count} zombies");
    }

    private GameObject CreateNewZombie()
    {
        if (zombiePrefab == null)
        {
            ////Debug.LogError("Cannot create zombie: prefab is null!");
            return null;
        }

        try
        {
            GameObject zombie = Instantiate(zombiePrefab, poolParent);

            ZombieAI zombieAI = zombie.GetComponent<ZombieAI>();
            if (zombieAI == null)
            {
                ////Debug.LogError($"Zombie prefab '{zombiePrefab.name}' is missing ZombieAI component!");
                Destroy(zombie);
                return null;
            }

            if (enableDebugLogs)
            {
                ////Debug.Log($"Created new zombie: {zombie.name}");
            }

            return zombie;
        }
        catch (System.Exception e)
        {
            ////Debug.LogError($"Exception while creating zombie: {e.Message}");
            return null;
        }
    }

    public GameObject GetZombie(Vector3 position, Quaternion rotation)
    {
        if (!isInitialized)
        {
            ////Debug.LogError("ZombiePool not initialized! Call Initialize() first.");
            return null;
        }

        GameObject zombie = null;

        if (zombiePool.Count > 0)
        {
            zombie = zombiePool.Dequeue();
            if (enableDebugLogs)
            {
                ////Debug.Log($"Got zombie from pool. Pool count: {zombiePool.Count}");
            }
        }
        else if (expandable)
        {
            zombie = CreateNewZombie();
            if (zombie != null)
            {
                zombie.name = $"ExpandedZombie_{activeZombies.Count}";
                ////Debug.LogWarning($"Pool exhausted! Created new zombie: {zombie.name}");
            }
        }
        else
        {
            ////Debug.LogWarning("Zombie pool is empty and not expandable!");
            return null;
        }

        if (zombie == null)
        {
            ////Debug.LogError("Failed to get zombie from pool!");
            return null;
        }

        zombie.transform.position = position;
        zombie.transform.rotation = rotation;

        ZombieAI zombieAI = zombie.GetComponent<ZombieAI>();
        if (zombieAI != null)
        {
            zombieAI.ResetZombie();
        }
        else
        {
            ////Debug.LogError($"Spawned zombie '{zombie.name}' has no ZombieAI component!");
        }

        zombie.SetActive(true);
        activeZombies.Add(zombie);

        if (enableDebugLogs)
        {
            ////Debug.Log($"Spawned zombie at {position}. Active: {activeZombies.Count}, Pooled: {zombiePool.Count}");
        }

        return zombie;
    }

    public void ReturnZombie(GameObject zombie)
    {
        if (zombie == null)
        {
            ////Debug.LogWarning("Trying to return null zombie to pool");
            return;
        }

        if (activeZombies.Remove(zombie))
        {
            ResetZombieForPool(zombie);
            zombie.SetActive(false);
            zombiePool.Enqueue(zombie);

            if (enableDebugLogs)
            {
                ////Debug.Log($"Returned zombie to pool. Active: {activeZombies.Count}, Pooled: {zombiePool.Count}");
            }
        }
        else
        {
            ////Debug.LogWarning($"Zombie '{zombie.name}' was not in active list when returning to pool");
        }
    }

    public void ReturnAllZombies()
    {
        ////Debug.Log($"Returning all {activeZombies.Count} zombies to pool");

        List<GameObject> zombiesToReturn = new List<GameObject>(activeZombies);

        foreach (GameObject zombie in zombiesToReturn)
        {
            if (zombie != null)
            {
                ReturnZombie(zombie);
            }
        }

        ////Debug.Log("All zombies returned to pool");
    }

    private void ResetZombieForPool(GameObject zombie)
    {
        if (zombie == null) return;

        zombie.transform.position = Vector3.zero;
        zombie.transform.rotation = Quaternion.identity;

        ZombieAI zombieAI = zombie.GetComponent<ZombieAI>();
        if (zombieAI != null)
        {
            zombieAI.ResetZombie();
        }

        Rigidbody rb = zombie.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }

        Animator animator = zombie.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public int ActiveZombieCount()
    {
        activeZombies.RemoveAll(zombie => zombie == null);
        return activeZombies.Count;
    }

    public int PooledZombieCount()
    {
        return zombiePool.Count;
    }

    public List<GameObject> GetActiveZombies()
    {
        activeZombies.RemoveAll(zombie => zombie == null);
        return new List<GameObject>(activeZombies);
    }

    public bool IsInitialized()
    {
        return isInitialized;
    }

    public string GetPoolStatus()
    {
        return $"Initialized: {isInitialized}, Active: {ActiveZombieCount()}, Pooled: {PooledZombieCount()}, Prefab: {(zombiePrefab ? zombiePrefab.name : "NULL")}";
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}