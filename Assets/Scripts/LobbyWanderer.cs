using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class LobbyWanderer : MonoBehaviour
{
    public NavMeshAgent agent;
    public float minIdleTime = 3f;
    public float maxIdleTime = 15f;
    public float wanderRadius = 10f; // Radius to search for a random point
    public float updateFrequency = 0.5f; // How often to check remaining distance

    private Vector3 randomPoint;

    void Start()
    {
        StartCoroutine(Wander());
    }

    IEnumerator Wander()
    {
        while (true) // Loop indefinitely
        {
            randomPoint = GetRandomPointOnNavMesh();
            agent.SetDestination(randomPoint);

            // Wait until the agent reaches the destination or a certain amount of time has passed
            float elapsedTime = 0f;
            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime >= updateFrequency)
                {
                    elapsedTime = 0f; // Reset the elapsed time
                    // Optional: You can add logic here to break if needed
                }
                yield return null; // Wait for the next frame
            }

            // Wait for a random amount of time before picking a new point
            float idleTime = Random.Range(minIdleTime, maxIdleTime);
            yield return new WaitForSeconds(idleTime);
        }
    }

    Vector3 GetRandomPointOnNavMesh()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius; // Get a random direction
        randomDirection += transform.position; // Offset by the current position

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas)) // Sample the nearest point on the NavMesh
        {
            return hit.position; // Return the position of the random point
        }

        return transform.position; // Fallback in case no valid point is found
    }
}