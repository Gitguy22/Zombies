using System.Collections;
using UnityEngine;

public class PipeController : MonoBehaviour
{
    [Header("Animation References")]
    [SerializeField] private Animator pipeAnimator;

    [Header("Rattle Configuration")]
    [SerializeField] private float minRattleInterval = 4f;
    [SerializeField] private float maxRattleInterval = 9f;
    [SerializeField] private float minRattleDuration = 5f;
    [SerializeField] private float maxRattleDuration = 11f;

    [Header("Explosion Detection")]
    [SerializeField] private float explosionDetectionRadius = 5f;
    [SerializeField] private LayerMask explosionLayer;

    // State tracking
    private bool isRattling = false;
    private bool hasFallen = false;

    // Coroutine references
    private Coroutine rattleCoroutine;
    private Coroutine rattleIntervalCoroutine;

    private void Start()
    {
        // Check for missing references
        if (pipeAnimator == null)
        {
            pipeAnimator = GetComponent<Animator>();
            if (pipeAnimator == null)
            {
                Debug.LogError("PipeController: No Animator component found!");
                return;
            }
        }

        // Start the rattle interval system
        StartRattleSystem();
    }

    private void Update()
    {
        // Check for nearby explosions only if pipes haven't fallen yet
        if (!hasFallen)
        {
            CheckForExplosions();
        }
    }

    private void StartRattleSystem()
    {
        // Start the coroutine that handles the random rattle intervals
        rattleIntervalCoroutine = StartCoroutine(RattleIntervalRoutine());
    }

    private IEnumerator RattleIntervalRoutine()
    {
        while (!hasFallen)
        {
            // Wait for a random interval between min and max
            float waitTime = Random.Range(minRattleInterval, maxRattleInterval);
            yield return new WaitForSeconds(waitTime);

            // Start the rattle animation loop if not already rattling and not fallen
            if (!isRattling && !hasFallen)
            {
                rattleCoroutine = StartCoroutine(RattleRoutine());
            }
        }
    }

    private IEnumerator RattleRoutine()
    {
        isRattling = true;

        // Set the Rattling parameter to true to start the rattling animation
        pipeAnimator.SetBool("Rattling", true);

        // Determine how long to rattle
        float rattleDuration = Random.Range(minRattleDuration, maxRattleDuration);
        yield return new WaitForSeconds(rattleDuration);

        // Stop the rattle animation if we haven't fallen
        if (!hasFallen)
        {
            pipeAnimator.SetBool("Rattling", false);
            isRattling = false;
        }
    }

    private void CheckForExplosions()
    {
        // Check for explosions in the detection radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionDetectionRadius, explosionLayer);

        if (hitColliders.Length > 0)
        {
            // Explosion detected, stop rattling and start falling
            TriggerPipeFalling();
        }
    }

    public void TriggerPipeFalling()
    {
        // Only trigger falling if not already fallen
        if (hasFallen)
            return;

        hasFallen = true;

        // Stop any ongoing rattle coroutines
        if (rattleCoroutine != null)
            StopCoroutine(rattleCoroutine);

        if (rattleIntervalCoroutine != null)
            StopCoroutine(rattleIntervalCoroutine);

        // Set rattling to false and trigger the fall animation
        pipeAnimator.SetBool("Rattling", false);
        pipeAnimator.SetTrigger("Fall");
    }

    // Public method to trigger falling from external explosion events
    public void OnExplosionNearby()
    {
        TriggerPipeFalling();
    }

    // Visualize the explosion detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionDetectionRadius);
    }
}