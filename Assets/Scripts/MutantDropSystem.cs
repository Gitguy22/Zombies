using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MutantDropSystem : MonoBehaviour
{
    [Header("Drop Configuration")]
    [SerializeField] bool alwaysDropPowerUp = true;
    [SerializeField] float dropRadius = 5f;
    [SerializeField] float dropForce = 300f;

    [Header("Possible Drops")]
    [SerializeField] DropItem[] possibleDrops;

    [System.Serializable]
    public class DropItem
    {
        public GameObject dropPrefab;
        [Range(0f, 100f)]
        public float dropChance = 10f;
        public string dropName = "Power Up";
    }

    void Start()
    {
        ValidateDropChances();
    }

    void ValidateDropChances()
    {
        if (possibleDrops == null || possibleDrops.Length == 0)
        {
            Debug.LogWarning("MutantDropSystem: No possible drops configured!");
            return;
        }

        float totalChance = 0f;
        foreach (var drop in possibleDrops)
        {
            totalChance += drop.dropChance;
        }

        if (totalChance > 100f)
        {
            Debug.LogWarning($"MutantDropSystem: Total drop chances exceed 100% ({totalChance}%)");
        }
    }

    public void HandleDrop()
    {
        if (possibleDrops == null || possibleDrops.Length == 0)
        {
            Debug.LogWarning("No drops configured for mutant");
            return;
        }

        if (alwaysDropPowerUp)
        {
            // Always drop something
            DropItem selectedDrop = SelectRandomDrop();
            if (selectedDrop != null)
            {
                SpawnDrop(selectedDrop);
            }
        }
        else
        {
            // Roll for each possible drop
            foreach (var drop in possibleDrops)
            {
                float roll = Random.Range(0f, 100f);
                if (roll <= drop.dropChance)
                {
                    SpawnDrop(drop);
                    break; // Only drop one item
                }
            }
        }
    }

    DropItem SelectRandomDrop()
    {
        if (possibleDrops.Length == 0) return null;

        // Weighted random selection
        float totalWeight = 0f;
        foreach (var drop in possibleDrops)
        {
            totalWeight += drop.dropChance;
        }

        if (totalWeight <= 0f)
        {
            // If no weights, pick randomly
            return possibleDrops[Random.Range(0, possibleDrops.Length)];
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var drop in possibleDrops)
        {
            currentWeight += drop.dropChance;
            if (randomValue <= currentWeight)
            {
                return drop;
            }
        }

        // Fallback
        return possibleDrops[possibleDrops.Length - 1];
    }

    void SpawnDrop(DropItem drop)
    {
        if (drop?.dropPrefab == null)
        {
            Debug.LogWarning("Attempted to spawn null drop prefab");
            return;
        }

        // Calculate spawn position (slight offset from mutant)
        Vector3 spawnPosition = transform.position + Vector3.up * 1f;
        Vector3 randomOffset = Random.insideUnitSphere * dropRadius;
        randomOffset.y = Mathf.Abs(randomOffset.y); // Keep above ground
        spawnPosition += randomOffset;

        // Spawn the drop
        GameObject droppedItem = Instantiate(drop.dropPrefab, spawnPosition, Quaternion.identity);

        // Add some physics force for dramatic effect
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 force = Random.insideUnitSphere * dropForce;
            force.y = Mathf.Abs(force.y); // Always push upward
            rb.AddForce(force);
        }

        Debug.Log($"Mutant dropped: {drop.dropName}");
    }
}