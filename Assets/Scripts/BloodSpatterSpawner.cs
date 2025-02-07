using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BloodSpatterSpawner : MonoBehaviour
{
    public GameObject[] bloodDecals; // Assign blood decal prefabs in Inspector
    public int maxDecals = 50; // Max number of active decals
    public float decalLifetime = 10f; // How long before a decal disappears
    public float spawnChance = 0.3f; // 30% chance to spawn a decal

    private Queue<GameObject> decalPool = new Queue<GameObject>();

    void Start()
    {
        // Pre-instantiate decal pool
        for (int i = 0; i < maxDecals; i++)
        {
            GameObject decal = Instantiate(bloodDecals[Random.Range(0, bloodDecals.Length)]);
            decal.SetActive(false);
            decalPool.Enqueue(decal);
        }
    }

    void OnParticleCollision(GameObject other)
    {
        if (Random.value > spawnChance) return; // 30% chance to spawn a decal

        List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
        int numCollisionEvents = GetComponent<ParticleSystem>().GetCollisionEvents(other, collisionEvents);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            SpawnBloodDecal(collisionEvents[i].intersection, collisionEvents[i].normal);
        }
    }

    void SpawnBloodDecal(Vector3 position, Vector3 normal)
    {
        if (decalPool.Count == 0) return; // Safety check

        GameObject decalInstance = decalPool.Dequeue();
        decalInstance.transform.position = position;

        // Ensure the decal faces outward correctly
        Quaternion rotation = Quaternion.LookRotation(-normal, Vector3.up);

        // If it hits the floor, rotate it flat with a random Y rotation
        if (Vector3.Angle(normal, Vector3.up) < 45f)
        {
            rotation = Quaternion.Euler(-90, Random.Range(0, 360), 0);
        }

        decalInstance.transform.rotation = rotation;
        decalInstance.SetActive(true);

        // Re-add to pool after some time
        StartCoroutine(DeactivateDecal(decalInstance, decalLifetime));
    }

    private IEnumerator DeactivateDecal(GameObject decal, float delay)
    {
        yield return new WaitForSeconds(delay);
        decal.SetActive(false);
        decalPool.Enqueue(decal);
    }
}