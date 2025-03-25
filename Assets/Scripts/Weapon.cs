using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;


public class Weapon : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private int weaponDamage;
    [SerializeField] private float bulletVelocity = 100f;
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private bool isAutomatic = false;  // Controls if weapon is automatic
    [SerializeField] private float fireRate = 10f;      // Rounds per second (when automatic)

    [Header("Aiming")]
    [SerializeField] private Transform aimSightTransform;
    [SerializeField] private float aimFOV = 40f;
    [SerializeField] private float regularFOV = 60f;
    [SerializeField] private float aimTransitionSpeed = 10f;
    private Vector3 defaultWeaponPosition;
    private Quaternion defaultWeaponRotation;
    private Vector3 aimPosition;
    private bool isAiming = false;
    private Camera mainCamera;
    private InputAction aimAction;

    // higher variance means less accurate
    [Header("Bullet Spread")]
    [SerializeField] private bool addBulletSpread = true;
    [SerializeField] private Vector3 bulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);

    [Header("Recoil Settings")]
    [SerializeField] private float recoilVerticalStrength = 1f;   
    [SerializeField] private float recoilHorizontalStrength = 0.3f; 
    [SerializeField] private float recoilVerticalRandomness = 0.1f; 
    [SerializeField] private float recoilHorizontalRandomness = 0.2f;
    [SerializeField] private float recoilRecoverySpeed = 10f;
    [SerializeField] private bool applyRecoil = true;

    [Header("Weapon Shake")]
    [SerializeField] private float weaponShakeIntensity = 0.05f;
    [SerializeField] private float weaponShakeDuration = 0.1f;
    [SerializeField] private AnimationCurve weaponShakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    private Vector3 weaponOriginPosition;
    private Coroutine shakeCoroutine;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem shootingSystem;
    [SerializeField] private ParticleSystem impactParticleSystem;
    [SerializeField] private TrailRenderer bulletTracer;
    [SerializeField] private float minTrailDuration = 0.1f;
    [SerializeField] private Transform barrelExit;

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
    private PlayerLook playerLook;
    private ZombieAI zombie;

    private InputAction shootAction;
    private InputAction reloadAction;

    private int ammoInMag;
    private int ammoInReserves;
    private float lastShootTime;
    private bool isReloading;
    private bool isFiring; 

    // Track combat results for scoring and feedback
    private bool gotHit;
    private bool gotKill;
    private string bodyPart;

    private void OnEnable()
    {
        // Owner reference is used for points and stuff
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
        playerLook = owner.GetComponent<PlayerLook>();
        weaponOriginPosition = transform.localPosition;

        if (playerLook == null)
        {
            Debug.LogWarning("PlayerLook component not found on the owner. Recoil will not work.");
        }

        mainCamera = Camera.main;
        defaultWeaponPosition = transform.localPosition;
        defaultWeaponRotation = transform.localRotation;
        if (aimSightTransform == null)
        {
            Debug.LogWarning("Aim sight transform not assigned. Creating a default one.");
            GameObject aimSight = new GameObject("AimSight");
            aimSight.transform.SetParent(transform);
            aimSight.transform.localPosition = new Vector3(0, 0.02f, 0.2f);
            aimSightTransform = aimSight.transform;
        }
        aimPosition = CalculateAimPosition();
    }

    private void Update()
    {
        HandleInput();
        HandleAutomaticFire();
        UpdateAmmoUI();
        HandleAiming();
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

        // Callbacks for tracking when fire button is pressed and released
        shootAction.started += ctx => isFiring = true;
        shootAction.canceled += ctx => isFiring = false;
        aimAction = playerInput.OnFoot.Aim;
        aimAction.started += ctx => StartAiming();
        aimAction.canceled += ctx => StopAiming();
    }

    private void InitializeAmmo()
    {
        ammoInMag = initialAmmoInMag;
        ammoInReserves = initialReserves;
    }

    private void HandleInput()
    {
        if (!isAutomatic && shootAction.triggered && Time.time >= lastShootTime)
        {
            OnShoot();
        }

        if (reloadAction.triggered && CanReload())
        {
            StartCoroutine(OnReload());
        }
    }

    private void HandleAutomaticFire()
    {
        if (isAutomatic && isFiring && Time.time >= lastShootTime + (1f / fireRate))
        {
            OnShoot();
        }
    }

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

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeWeapon());

        ApplyRecoil();
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

    private void ApplyRecoil()
    {
        if (!applyRecoil || playerLook == null) return;

        // Calculate recoil with randomness
        float verticalRecoil = -recoilVerticalStrength;
        float horizontalRecoil = Random.Range(-recoilHorizontalStrength, recoilHorizontalStrength);

        // Apply randomness
        verticalRecoil += Random.Range(-recoilVerticalRandomness, recoilVerticalRandomness);
        horizontalRecoil += Random.Range(-recoilHorizontalRandomness, recoilHorizontalRandomness);

        // Apply recoil through the PlayerLook component
        playerLook.AddRecoil(new Vector2(horizontalRecoil, verticalRecoil), recoilRecoverySpeed);
    }

    private IEnumerator ShakeWeapon()
    {
        float elapsed = 0f;

        while (elapsed < weaponShakeDuration)
        {
            float strength = weaponShakeCurve.Evaluate(elapsed / weaponShakeDuration);
            Vector3 shakeAmount = new Vector3(
                Random.Range(-1f, 1f) * weaponShakeIntensity,
                Random.Range(0.5f, 1f) * weaponShakeIntensity, // Biased upward for recoil effect
                0
            ) * strength;

            transform.localPosition = weaponOriginPosition + shakeAmount;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = weaponOriginPosition;
    }

    private void HandleAiming()
    {
        if (isAiming)
        {
            // move weapon to aim position
            //transform.localPosition = Vector3.Lerp(transform.localPosition, aimPosition, Time.deltaTime * aimTransitionSpeed);

            //adjust FOV
            if (mainCamera != null)
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, aimFOV, Time.deltaTime * aimTransitionSpeed);
        }
        else
        {
            // Return to default position (unless currently being shaken)
            if (shakeCoroutine == null)
                transform.localPosition = Vector3.Lerp(transform.localPosition, defaultWeaponPosition, Time.deltaTime * aimTransitionSpeed);

            // Return to regular FOV
            if (mainCamera != null)
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, regularFOV, Time.deltaTime * aimTransitionSpeed);
        }
    }

    private Vector3 CalculateAimPosition()
    {
        //the position that would place the aimSightTransform at screen center
        if (mainCamera == null) return defaultWeaponPosition;

        // Convert the sight's position to world space
        Vector3 sightWorldPos = aimSightTransform.position;

        // Find the offset vector from the weapon to the sight
        Vector3 sightOffset = sightWorldPos - transform.position;

        // Calculate how much to move the weapon so the sight is at screen center
        Vector3 targetPosition = defaultWeaponPosition - sightOffset;

        return targetPosition;
    }

    private void StartAiming()
    {
        isAiming = true;
        if (playerLook != null)
            playerLook.SetAiming(true);
    }

    private void StopAiming()
    {
        isAiming = false;
        if (playerLook != null)
            playerLook.SetAiming(false);
    }

    private void HandleHit(RaycastHit hit, Vector3 direction)
    {
        TrailRenderer trail = Instantiate(bulletTracer, barrelExit.transform.position, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, hit.point, hit));
 
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
        if (limb == null)
        {
            Debug.LogWarning("Attempted to handle damage for a null limb");
            return;
        }

        ZombieAI zombie = FindZombieComponent(limb.transform);

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
        if (nLimb == null)
        {
            Debug.LogWarning("Attempted to handle damage for a null non-amputatable limb");
            return;
        }

        // Get the zombie component with null check
        ZombieAI zombie = FindZombieComponent(nLimb.transform);

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

    // Was having issues with being able to get the zombie component so I just made two ways to find it
    private ZombieAI FindZombieComponent(Transform limbTransform)
    {
        //try traversing up the hierarchy
        Transform current = limbTransform;
        ZombieAI foundZombie = null; // Use local variable instead of class field

        while (current != null)
        {
            foundZombie = current.GetComponent<ZombieAI>();
            if (foundZombie != null) return foundZombie;
            current = current.parent;
        }

        Debug.LogWarning($"Could not find ZombieAI component. Hierarchy: {GetHierarchyPath(limbTransform)}");
        return null;
    }

    private string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}