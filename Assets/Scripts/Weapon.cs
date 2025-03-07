using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;


public class Weapon : MonoBehaviour
{
    // Combat properties determine the weapon's effectiveness and feel
    [Header("Combat Settings")]
    [SerializeField] private int weaponDamage;
    [SerializeField] private float bulletVelocity = 100f;
    [SerializeField] private float maxRange = 100f;

    // Spread settings affect accuracy - higher variance means less accurate
    [Header("Bullet Spread")]
    [SerializeField] private bool addBulletSpread = true;
    [SerializeField] private Vector3 bulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);

    // VFX components for visual feedback
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem shootingSystem;
    [SerializeField] private ParticleSystem impactParticleSystem;
    [SerializeField] private TrailRenderer bulletTracer;
    [SerializeField] private float minTrailDuration = 0.1f; // Ensures trails are visible even for close-range shots
    [SerializeField] private Transform barrelExit;

    // Ammo system configuration
    [Header("Ammunition")]
    [SerializeField] private int magCapacity;
    [SerializeField] private int maxReserves;
    [SerializeField] private int initialAmmoInMag;
    [SerializeField] private int initialReserves;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI ammoInMagText;
    [SerializeField] private TextMeshProUGUI ammoInReservesText;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip reloadSound;

    // Determines what objects can be hit by bullets
    [Header("Layer Settings")]
    [SerializeField] private LayerMask mask;

    [Header("Points System")]
    [SerializeField] private GameObject pointManager;

    private PlayerInputs playerInput;
    private Animator animator;
    private AudioSource audioSource;
    private PointManager points;
    private GameObject owner;

    private InputAction shootAction;
    private InputAction reloadAction;

    private int ammoInMag;
    private int ammoInReserves;
    private float lastShootTime;
    private bool isReloading;

    // Track combat results for scoring and feedback
    private bool gotHit;
    private bool gotKill;
    private string bodyPart;

    private void OnEnable()
    {
        // Owner reference is used for score attribution
        owner = transform.root.gameObject;
    }

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        InitializeInputSystem();
        InitializeAmmo();
        points = pointManager.GetComponent<PointManager>();
    }

    private void Update()
    {
        HandleInput();
        UpdateAmmoUI();
    }

    private void InitializeComponents()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    private void InitializeInputSystem()
    {
        playerInput = new PlayerInputs();
        playerInput.Enable();

        shootAction = playerInput.OnFoot.Shoot;
        reloadAction = playerInput.OnFoot.Reload;
    }

    private void InitializeAmmo()
    {
        ammoInMag = initialAmmoInMag;
        ammoInReserves = initialReserves;
    }

    private void HandleInput()
    {
        if (shootAction.triggered && Time.time >= lastShootTime)
        {
            OnShoot();
        }

        if (reloadAction.triggered && CanReload())
        {
            StartCoroutine(OnReload());
        }
    }

    // Checks all conditions that might prevent reloading
    private bool CanReload()
    {
        return ammoInReserves > 0 && ammoInMag < magCapacity && !isReloading;
    }

    private void OnShoot()
    {
        ResetCombatTracking();

        // Allow shooting to interrupt reload animation(doesn't work yet)
        if (isReloading)
        {
            CancelReload();
            return;
        }

        if (ammoInMag <= 0)
        {
            audioSource.PlayOneShot(clickSound);
            return;
        }

        ExecuteShot();
    }

    private void ExecuteShot()
    {
        PlayShootingEffects();
        Vector3 direction = CalculateBulletDirection();

        // Perform the actual raycast for hit detection
        if (Physics.Raycast(barrelExit.position, direction, out RaycastHit hit, maxRange, mask))
        {
            HandleHit(hit, direction);
        }
        else
        {
            CreateMissTrail(direction);
        }

        ammoInMag--;
        lastShootTime = Time.time;
    }

    private void HandleHit(RaycastHit hit, Vector3 direction)
    {
        TrailRenderer trail = Instantiate(bulletTracer, barrelExit.transform.position, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, hit.point, hit));

        // Visualize bullet path in debug view for 100s 
        Debug.DrawLine(barrelExit.transform.position, hit.point, Color.red, 100f);

        Vector3 hitDirection = CalculateHitDirection(hit.point);
        ProcessTargetDamage(hit, hitDirection);
        Debug.Log("Barrel Exit Location: " + barrelExit.transform.position);
    }

    // Calculates the force applied to hit objects based on damage and distance
    private Vector3 CalculateHitDirection(Vector3 hitPoint)
    {
        return (hitPoint - barrelExit.position).normalized * weaponDamage * 10f;
    }

    // Routes damage to the appropriate component based on what was hit
    private void ProcessTargetDamage(RaycastHit hit, Vector3 hitDirection)
    {
        if (hit.transform.TryGetComponent(out Limb limb))
        {
            HandleLimbDamage(limb, hit.point, hitDirection);
            points.AddPoints(owner, gotHit, gotKill, bodyPart);
        }
        else if (hit.transform.TryGetComponent(out NonAmputatableLimb nLimb))
        {
            HandleNonAmputatableLimbDamage(nLimb, hit.point, hitDirection);
            points.AddPoints(owner, gotHit, gotKill, bodyPart);
        }
    }

    // Handles damage to removable limbs and tracks which part was hit
    private void HandleLimbDamage(Limb limb, Vector3 hitPoint, Vector3 hitDirection)
    {
        // Check if limb is null
        if (limb == null)
        {
            Debug.LogWarning("Attempted to handle damage for a null limb");
            return;
        }

        // Get the zombie component with null check
        ZombieAI zombie = limb.transform.root.GetComponent<ZombieAI>();

        // Check if we found a valid zombie
        if (zombie == null)
        {
            Debug.LogWarning("Could not find a ZombieAI component on the limb's root");
            return;
        }

        bodyPart = limb.name;
        ApplyDamageAndTrackResults(zombie, () => limb.GetHit(weaponDamage, hitPoint, hitDirection));
    }

    private void HandleNonAmputatableLimbDamage(NonAmputatableLimb nLimb, Vector3 hitPoint, Vector3 hitDirection)
    {
        // Check if nLimb is null
        if (nLimb == null)
        {
            Debug.LogWarning("Attempted to handle damage for a null non-amputatable limb");
            return;
        }

        // Get the zombie component with null check
        ZombieAI zombie = nLimb.transform.root.GetComponent<ZombieAI>();

        // Check if we found a valid zombie
        if (zombie == null)
        {
            Debug.LogWarning("Could not find a ZombieAI component on the non-amputatable limb's root");
            return;
        }

        bodyPart = nLimb.name;
        ApplyDamageAndTrackResults(zombie, () => nLimb.GetHit(weaponDamage, hitPoint, hitDirection));
    }

    // Tracks zombie state before and after damage to determine if hit was lethal
    private void ApplyDamageAndTrackResults(ZombieAI zombie, System.Action applyDamage)
    {
        // Check if zombie is null before proceeding
        if (zombie == null)
        {
            Debug.LogWarning("Attempted to apply damage to a null zombie reference");
            return;
        }

        bool wasAlive = !zombie.dead;
        float previousHP = zombie.zombieHP;

        applyDamage();

        if (wasAlive)
        {
            gotHit = true;
            gotKill = (previousHP >= 0 && zombie.dead);
        }
    }

    private void PlayShootingEffects()
    {
        animator.SetTrigger("Fire");
        audioSource.PlayOneShot(shootSound);
        shootingSystem.Play();
    }

    // Adds randomization to shot direction based on weapon spread settings
    private Vector3 CalculateBulletDirection()
    {
        if (!addBulletSpread) return transform.forward;

        Vector3 spread = new Vector3(
            Random.Range(-bulletSpreadVariance.x, bulletSpreadVariance.x),
            Random.Range(-bulletSpreadVariance.y, bulletSpreadVariance.y),
            Random.Range(-bulletSpreadVariance.z, bulletSpreadVariance.z)
        );

        return (transform.forward + spread).normalized;
    }

    private void CreateMissTrail(Vector3 direction)
    {
        TrailRenderer trail = Instantiate(bulletTracer, barrelExit.position, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, barrelExit.position + direction * maxRange, null));
    }

    // Animates bullet trail from barrel to impact point
    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, RaycastHit? hit)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;
        float startingDistance = Vector3.Distance(trail.transform.position, hitPoint);
        float distance = startingDistance;

        // Move trail along bullet path at specified velocity
        while (distance > 0)
        {
            time += Time.deltaTime * bulletVelocity / startingDistance;
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, time);
            distance -= Time.deltaTime * bulletVelocity;
            yield return null;
        }

        trail.transform.position = hitPoint;

        if (hit.HasValue)
        {
            Instantiate(impactParticleSystem, hit.Value.point, Quaternion.LookRotation(hit.Value.normal));
        }

        yield return new WaitForSeconds(minTrailDuration);
        Destroy(trail.gameObject, trail.time);
    }

    private IEnumerator OnReload()
    {
        StartReloadAnimation();

        int ammoToLoad = Mathf.Min(magCapacity - ammoInMag, ammoInReserves);
        audioSource.PlayOneShot(reloadSound);

        // Wait for reload animation to complete
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);

        CompleteReload(ammoToLoad);
    }

    private void StartReloadAnimation()
    {
        isReloading = true;
        animator.SetTrigger("Reload");
        animator.SetBool("CanReload", true);
    }

    private void CompleteReload(int ammoToLoad)
    {
        ammoInMag += ammoToLoad;
        ammoInReserves -= ammoToLoad;
        isReloading = false;
        animator.SetBool("CanReload", false);
    }

    // Handles interruption of reload animation when shooting
    private void CancelReload()
    {
        var currentClip = animator.GetCurrentAnimatorClipInfo(0);
        Debug.Log($"Cancelling reload animation: {currentClip[0].clip.name}");
        animator.StopPlayback();
    }

    private void UpdateAmmoUI()
    {
        ammoInMagText.text = ammoInMag.ToString();
        ammoInReservesText.text = ammoInReserves.ToString();
    }

    private void ResetCombatTracking()
    {
        gotHit = false;
        gotKill = false;
        bodyPart = "";
    }

    //Draw gizmo where the barrel exit is.
    private void OnDrawGizmos()
    {
        if (barrelExit == null)
        {
            return;
        }
    }
}