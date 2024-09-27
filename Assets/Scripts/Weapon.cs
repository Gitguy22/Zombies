using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] bool addBulletSpread = true;
    [SerializeField] Vector3 bulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);
    [SerializeField] ParticleSystem shootingSystem;
    [SerializeField] Transform barrelExit;
    [SerializeField] ParticleSystem impactParticleSystem;
    [SerializeField] TrailRenderer bulletTracer;
    [SerializeField] float shootDelay = 0.5f;
    [SerializeField] LayerMask mask;
    [SerializeField] float bulletVelocity = 100;
    [SerializeField] float maxRange = 100f; // Maximum range for the bullet
    [SerializeField] float minTrailDuration = 0.1f; // Minimum time for trail visibility

    private PlayerInputs playerInput;
    private Animator animator;
    private float lastShootTime;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        playerInput = new PlayerInputs();
    }

    private void Update()
    {
        // Draw the raycast for debugging
        Vector3 direction = GetDirection();
    }

    private void OnShoot()
    {
        
        Debug.Log("Shot");

        shootingSystem.Play();
        Vector3 direction = GetDirection();
        Debug.DrawLine(barrelExit.position, direction, Color.red, 100f);

        if (lastShootTime + shootDelay < Time.time)
        {
            if (Physics.Raycast(barrelExit.position, direction, out RaycastHit hit, maxRange, mask))
            {


                TrailRenderer trail = Instantiate(bulletTracer, barrelExit.position, Quaternion.identity);

                StartCoroutine(SpawnTrail(trail, hit.point, hit)); // Pass the hit point and RaycastHit here

                Debug.Log("Hit: " + hit.transform.name); // Log the hit object's name

                // Check if the hit object is a Limb and handle the hit
                if(hit.transform.gameObject.GetComponent<Limb>())
                {
                    Limb limb = hit.transform.gameObject.GetComponent<Limb>();
                    limb.GetHit();
                }

                lastShootTime = Time.time;
            }
            else
            {
                TrailRenderer trail = Instantiate(bulletTracer, barrelExit.position, Quaternion.identity);

                // Use maxRange to calculate the endpoint when there's no hit
                StartCoroutine(SpawnTrail(trail, barrelExit.position + direction * maxRange, null)); // Pass null for RaycastHit when there's no hit
            }
        }
    }

    private Vector3 GetDirection()
    {
        Vector3 direction = transform.forward;

        if (addBulletSpread == true)
        {
            direction += new Vector3
            (
                Random.Range(-bulletSpreadVariance.x, bulletSpreadVariance.x),
                Random.Range(-bulletSpreadVariance.y, bulletSpreadVariance.y),
                Random.Range(-bulletSpreadVariance.z, bulletSpreadVariance.z)
            );

            direction.Normalize();
        }

        return direction;
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, RaycastHit? hit)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;
        float startingDistance = Vector3.Distance(trail.transform.position, hitPoint);
        float distance = startingDistance;

        while (distance > 0)
        {
            time += Time.deltaTime * bulletVelocity / startingDistance; // Adjust time increment for faster interpolation
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, time);
            distance -= Time.deltaTime * bulletVelocity;

            yield return null;
        }

        // Ensure the trail is visible for at least minTrailDuration
        yield return new WaitForSeconds(minTrailDuration);

        trail.transform.position = hitPoint;

        if (hit.HasValue)
        {
            Instantiate(impactParticleSystem, hit.Value.point, Quaternion.LookRotation(hit.Value.normal));
        }

        Destroy(trail.gameObject, trail.time);
    }

    private IEnumerator DestroyBulletAfterTime(GameObject projectile, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(projectile);
    }
}
