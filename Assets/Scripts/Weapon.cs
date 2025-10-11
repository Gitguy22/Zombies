using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine;
using GameAudio;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Configuration")]
    public WeaponData weaponData;

    [Header("Combat Settings")]
    [SerializeField] int weaponDamage;
    [SerializeField] float bulletVelocity = 100f;
    [SerializeField] float maxRange = 100f;
    [SerializeField] bool isAutomatic = false;
    [SerializeField] float fireRate = 10f;

    [Header("Bullet Penetration")]
    [SerializeField] bool enablePenetration = true;
    [SerializeField] float penetrationDamageMultiplier = 0.7f;
    [SerializeField] int maxPenetrationCount = 2;
    [SerializeField] LayerMask zombieLayer;

    [Header("Aiming")]
    [SerializeField] Transform aimSightTransform;
    [SerializeField] float aimFOV = 40f;
    [SerializeField] float regularFOV = 60f;
    [SerializeField] float aimTransitionSpeed = 10f;
    private Vector3 defaultWeaponPosition;
    private Quaternion defaultWeaponRotation;
    private Vector3 aimPosition;
    private bool isAiming = false;
    private Camera mainCamera;
    private InputAction aimAction;

    [Header("Idle Weapon Sway")]
    [SerializeField] float swayAmount = 0.02f;
    [SerializeField] float swaySpeed = 1.0f;
    [SerializeField] float aimSwayAmount = 0.01f; // Less sway when aiming
    [SerializeField] AnimationCurve swayCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Movement Sway")]
    [SerializeField] float walkSwayMultiplier = 1.5f;
    [SerializeField] float runSwayMultiplier = 3.0f;
    [SerializeField] float jumpSwayMultiplier = 2.5f;

    [Header("Bullet Spread")]
    [SerializeField] bool addBulletSpread = true;
    [SerializeField] Vector3 bulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);
    [SerializeField] float aimSpreadMultiplier = 0.5f; // Reduces spread by half when aiming
    [SerializeField] float walkingSpreadMultiplier = 1.5f;
    [SerializeField] float runningSpreadMultiplier = 3.0f;
    [SerializeField] float jumpingSpreadMultiplier = 2.5f;

    [Header("Shotgun Settings")]
    [Tooltip("Number of pellets per shot (for shotguns)")]
    [SerializeField] int pelletsPerShot = 8;
    [Range(0f, 90f)]
    [Tooltip("Maximum spread angle in degrees (for shotguns)")]
    [SerializeField] float spreadAngle = 15f;
    [SerializeField] float aimSpreadAngleMultiplier = 0.5f; // Reduces shotgun spread by half when aiming
    [SerializeField] float chamberTimeDelay = 0.45f; // Time to chamber a new round
    [SerializeField] AudioClip chamberSoundEffect; // Pump/cock sound

    [Header("Recoil Settings")]
    [SerializeField] float recoilVerticalStrength = 1f;
    [SerializeField] float recoilHorizontalStrength = 0.3f;
    [SerializeField] float recoilVerticalRandomness = 0.1f;
    [SerializeField] float recoilHorizontalRandomness = 0.2f;
    [SerializeField] float recoilRecoverySpeed = 10f;
    [SerializeField] bool applyRecoil = true;

    [Header("Weapon Shake")]
    [SerializeField] float weaponShakeIntensity = 0.05f;
    [SerializeField] float weaponShakeDuration = 0.1f;
    [SerializeField] AnimationCurve weaponShakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] float runningShakeIntensity = 0.10f;
    [SerializeField] float runningShakeSpeed = 4.0f;
    private Vector3 weaponOriginPosition;
    private Coroutine shakeCoroutine;
    private Coroutine runningShakeCoroutine;

    [Header("Visual Effects")]
    [SerializeField] ParticleSystem shootingSystem; // Muzzle flash
    [SerializeField] ParticleSystem impactParticleSystem;
    [SerializeField] TrailRenderer bulletTracer;
    [SerializeField] float minTrailDuration = 0.1f;
    [SerializeField] Transform barrelExit;

    [Header("Shotgun Effects")]
    [SerializeField] ParticleSystem shellEjectionSystem;
    [SerializeField] GameObject shotgunImpactPrefab;

    [Header("Sound Effect Data")]
    [SerializeField] SoundEffectData shootSoundData;
    [SerializeField] SoundEffectData emptySoundData;
    [SerializeField] SoundEffectData reloadSoundData;
    [SerializeField] SoundEffectData unloadSoundData;
    [SerializeField] SoundEffectData loadSoundData;
    [SerializeField] SoundEffectData pickupSoundData;
    [SerializeField] SoundEffectData chamberSoundData; // Shotgun pump/cock
    [SerializeField] SoundEffectData shellInsertSoundData; // Shotgun shell insert
    [SerializeField] SoundEffectData zombieHitSoundData;

    [Header("Audio")]
    [SerializeField] AudioClip shootSound;
    [SerializeField] AudioClip clickSound;
    [SerializeField] AudioClip reloadSound;
    [SerializeField] AudioClip unloadSound; // New sound when removing magazine/shells
    [SerializeField] AudioClip loadSound;   // New sound when inserting magazine/shells
    [SerializeField] AudioClip pickupSound; // Sound when swapping weapons
    [SerializeField] bool useSingleReloadSound = false; // Toggle for using just one reload sound
    [SerializeField] AudioClip shellInsertSound; // For shotgun shell-by-shell reload

    [Header("Hit Effects")]
    [SerializeField] AudioClip zombieHitSound;
    [SerializeField] string bloodEffectsDataName = "BloodEffectsData"; // Default ScriptableObject name
    private BloodEffectsData bloodEffectsData; // Reference to the loaded ScriptableObject
    [SerializeField] float bloodSplashScale = 1f;
    [SerializeField] bool enableHitEffects = true;
    [SerializeField] string defaultHitSoundName = "hit-flesh-01-266311";

    [Header("Upgraded Sounds")]
    [SerializeField] bool useUpgradedSounds = false; // Toggle for laser-like sounds
    [SerializeField] float minUpgradedPitch = 1.6f; // Minimum pitch for laser effect
    [SerializeField] float maxUpgradedPitch = 2.2f; // Maximum pitch for laser effect

    [Header("Reload Settings")]
    [SerializeField] float reloadSpeed = 2.0f; // Time in seconds to complete reload
    [SerializeField] Vector3 reloadRotation = new Vector3(45f, -30f, 0f); // Rotation during reload
    [SerializeField] Vector3 reloadPosition = new Vector3(0.2f, -0.3f, 0.1f); // Position offset during reload

    [Header("Running Settings")]
    [SerializeField] Vector3 runningRotation = new Vector3(-40f, 0f, 0f); // Point gun up while running
    [SerializeField] Vector3 runningPosition = new Vector3(0.1f, -0.1f, 0.1f); // Position offset while runningn
    [SerializeField] float runningTransitionSpeed = 15.0f; // Faster transition when state changes
    [SerializeField] AnimationCurve runningShakeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Smooth curve for running motion

    [Header("Layer Settings")]
    [SerializeField] LayerMask mask;

    [Header("Layer Management")]
    [SerializeField] private string weaponLayerName = "Weapon";
    [SerializeField] private string interactableLayerName = "Interactable";
    private int weaponLayer;
    private int interactableLayer;

    private bool isAttachedToPlayer = false;
    private WeaponPickup pickupComponent;

    // Events for weapons actions
    public System.Action<string> OnWeaponAction; // For general weapon events like reload, fire, etc.

    // Reference to player's ammo data
    private PlayerAmmoReference playerAmmoRef;
    private PlayerAmmoData playerAmmoData;

    private PlayerInputs playerInput;
    private Animator animator;
    private AudioSource audioSource;
    private PointManager points;
    private PlayerPointsManager playerPointsManager;
    private GameObject owner;
    private PlayerLook playerLook;
    private PlayerMovement playerMovement;
    private WeaponManager weaponManager;
    [SerializeField] private GameObject playerObject; // Reference to the owning player
    private CrosshairManager crosshairManager;

    private InputAction shootAction;
    private InputAction reloadAction;

    private float lastShootTime;
    private bool isReloading;
    private bool isFiring;

    // Track combat results for scoring and feedback
    private bool gotHit;
    private bool gotKill;
    private string bodyPart;

    // Shotgun hit tracking
    private int totalHitsThisShot = 0;
    private int totalKillsThisShot = 0;
    private Dictionary<ZombieAI, bool> zombiesHitThisShot = new Dictionary<ZombieAI, bool>();

    // Aim stuff
    private Vector3 targetPosition; // Used to track where the weapon should be moving to
    private bool positionOverridden = false; // Flag to track when position is temporarily changed

    // Flag to skip ammo initialization during pickup
    private bool skipAmmoInit = false;

    // Idle weapon sway variables
    private float swayTime = 0f;
    private Vector3 swayOffset = Vector3.zero;

    // Penetration tracking
    private List<GameObject> hitObjects = new List<GameObject>();

    // Helper method to find PlayerAmmoReference in the hierarchy
    private PlayerAmmoReference FindPlayerAmmoReference()
    {
        // First try the owner GameObject
        PlayerAmmoReference reference = owner.GetComponent<PlayerAmmoReference>();
        if (reference != null)
            return reference;

        // If not found on owner, try to find it in any parent
        reference = owner.GetComponentInParent<PlayerAmmoReference>();
        if (reference != null)
            return reference;

        // If still not found, look for it in children of owner
        reference = owner.GetComponentInChildren<PlayerAmmoReference>();
        if (reference != null)
            return reference;

        // Finally, check if it's in the scene on a different GameObject
        PlayerAmmoReference[] allReferences = GameObject.FindObjectsOfType<PlayerAmmoReference>();
        if (allReferences.Length > 0)
        {
            //Debug.LogWarning("Found PlayerAmmoReference on a different GameObject: " + allReferences[0].gameObject.name);
            return allReferences[0];
        }

        // Not found anywhere
        return null;
    }


    private void Awake()
    {
        // Cache the layer indices
        weaponLayer = LayerMask.NameToLayer(weaponLayerName);
        interactableLayer = LayerMask.NameToLayer(interactableLayerName);

        if (weaponLayer == -1)
            //Debug.LogWarning($"Layer '{weaponLayerName}' not found in project settings!");
        if (interactableLayer == -1)
            //Debug.LogWarning($"Layer '{interactableLayerName}' not found in project settings!");

        pickupComponent = GetComponent<WeaponPickup>();
        InitializeComponents();
        LoadDefaultHitSound();

        // Check attachment status immediately in Awake
        CheckAttachmentStatus();
    }

    private void OnEnable()
    {
        // Check attachment status and update component state
        CheckAttachmentStatus();

        // Only proceed with normal OnEnable logic if attached to player
        if (!isAttachedToPlayer) return;

        // Owner reference is used for points and stuff
        owner = transform.root.gameObject;

        // Try to get the player's ammo reference if not already set
        if (playerAmmoRef == null && owner != null)
        {
            playerAmmoRef = FindPlayerAmmoReference();
            if (playerAmmoRef != null)
            {
                playerAmmoData = playerAmmoRef.AmmoData;
            }
        }

        // Don't initialize ammo during weapon switching to prevent conflicts
        if (skipAmmoInit)
        {
            //Debug.Log($"Skipping OnEnable ammo initialization for {weaponData?.weaponName} (switch/pickup in progress)");
            return;
        }

        // Only initialize ammo if we have valid data and we're not switching weapons
        WeaponManager weaponManager = owner?.GetComponent<WeaponManager>();
        if (weaponManager != null && (weaponManager.IsPickingUpWeapon() || weaponManager.IsSwitchingWeapons()))
        {
            //Debug.Log($"Skipping OnEnable ammo initialization for {weaponData?.weaponName} (manager busy)");
            return;
        }
    }

    private void Start()
    {
        // Check attachment status again in Start
        CheckAttachmentStatus();

        // Only proceed if attached to player
        if (!isAttachedToPlayer) return;

        InitializeInputSystem();

        // Try to find the PlayerAmmoReference component on the player or any parent
        playerAmmoRef = FindPlayerAmmoReference();
        if (playerAmmoRef != null)
        {
            playerAmmoData = playerAmmoRef.AmmoData;
        }
        else
        {
            //Debug.LogWarning("PlayerAmmoReference not found on player or any parent. Weapon will use default ammo values.");
        }

        InitializeFromWeaponData();

        // Only initialize ammo if not skipping (prevents conflicts during switching/pickup)
        if (!skipAmmoInit)
        {
            InitializeAmmo();
        }
        else
        {
            //Debug.Log($"Skipping Start() ammo initialization for {weaponData?.weaponName} (skip flag set)");
        }

        // Find the PlayerMovement component
        playerMovement = owner.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            //Debug.LogWarning("PlayerMovement component not found on the owner. Movement-related features won't work properly.");
        }

        // Find the WeaponManager component
        weaponManager = owner.GetComponent<WeaponManager>();
        if (weaponManager == null)
        {
            //Debug.LogWarning("WeaponManager component not found on the owner. Weapon swapping functionality may be limited.");
        }

        // Find the PointManager in the scene instead of requiring it to be assigned
        points = FindPointManager();

        playerLook = owner.GetComponent<PlayerLook>();
        weaponOriginPosition = transform.localPosition;

        if (playerLook == null)
        {
            //Debug.LogWarning("PlayerLook component not found on the owner. Recoil will not work.");
        }

        mainCamera = Camera.main;
        defaultWeaponPosition = transform.localPosition;
        defaultWeaponRotation = transform.localRotation;
        if (aimSightTransform == null)
        {
            //Debug.LogWarning("Aim sight transform not assigned. Creating a default one.");
            GameObject aimSight = new GameObject("AimSight");
            aimSight.transform.SetParent(transform);
            aimSight.transform.localPosition = new Vector3(0, 0.02f, 0.2f);
            aimSightTransform = aimSight.transform;
        }
        aimPosition = CalculateAimPosition();
    }
    private void LoadDefaultHitSound()
    {
        // Existing hit sound loading code...

        // Load blood effects data
        if (bloodEffectsData == null && !string.IsNullOrEmpty(bloodEffectsDataName))
        {
            bloodEffectsData = Resources.Load<BloodEffectsData>($"Data/{bloodEffectsDataName}");

            if (bloodEffectsData == null)
            {
                // Try loading from root folder
                bloodEffectsData = Resources.Load<BloodEffectsData>(bloodEffectsDataName);
            }

            if (bloodEffectsData != null)
            {
                //Debug.Log($"Loaded blood effects data: {bloodEffectsDataName}");
            }
            else
            {
                //Debug.LogWarning($"Could not load blood effects data: {bloodEffectsDataName}. Make sure it's in Resources/ folder.");
            }
        }
    }

    public void PlayPickupSound()
    {
        if (pickupSoundData != null)
        {
            SoundEffectsManager.Instance.PlaySound(pickupSoundData, transform.position);
        }
        else if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }
    }

    // Check if this weapon is attached to a player and update state accordingly
    private void CheckAttachmentStatus()
    {
        // Find the root parent
        Transform rootParent = transform.root;

        // Check if root has player components - DON'T use GetComponent<PlayerInputs>()
        // Instead check for actual MonoBehaviour components that players would have
        bool isPlayer = rootParent.GetComponent<WeaponManager>() != null ||
                        rootParent.GetComponent<PlayerMovement>() != null ||
                        rootParent.GetComponent<PlayerLook>() != null;

        // Update attachment status
        if (isAttachedToPlayer != isPlayer)
        {
            isAttachedToPlayer = isPlayer;
            UpdateComponentState();
        }
    }

    // Update component state based on attachment status
    private void UpdateComponentState()
    {
        // Set the layer appropriately
        SetLayerRecursively(gameObject, isAttachedToPlayer ? weaponLayer : interactableLayer);

        // Enable/disable components based on attachment
        if (pickupComponent != null)
        {
            pickupComponent.enabled = !isAttachedToPlayer;
        }

        // If weapon becomes detached from player, disable input bindings
        if (!isAttachedToPlayer && playerInput != null)
        {
            UnregisterInputCallbacks();
        }
    }

    // Helper method to set layer recursively on all children
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // Helper method to unregister input callbacks
    private void UnregisterInputCallbacks()
    {
        if (shootAction != null)
        {
            shootAction.started -= ctx => OnShootStarted();
            shootAction.canceled -= ctx => OnShootCanceled();
        }

        if (reloadAction != null)
        {
            reloadAction.performed -= ctx => BeginReload();
        }

        if (aimAction != null)
        {
            aimAction.started -= ctx => StartAiming();
            aimAction.canceled -= ctx => StopAiming();
        }
    }

    // Find the PointManager in the scene
    private PointManager FindPointManager()
    {
        // First search for the PlayerPointsManager in the scene
        playerPointsManager = GameObject.FindObjectOfType<PlayerPointsManager>();
        if (playerPointsManager != null)
        {
            //Debug.Log("Found PlayerPointsManager for scoring system");
        }

        // Also get direct reference to PointManager for fallback
        PointManager pointManager = GameObject.FindObjectOfType<PointManager>();
        if (pointManager == null)
        {
            //Debug.LogWarning("PointManager not found in the scene. Points will not be tracked.");
        }

        return pointManager;
    }

    public void SetPlayerReference(GameObject player)
    {
        playerObject = player;

        // Try to find the crosshair manager for this player
        if (playerObject != null)
        {
            crosshairManager = playerObject.GetComponentInChildren<CrosshairManager>();
        }
    }

    private void Update()
    {
        // Check attachment status each frame
        CheckAttachmentStatus();

        // Skip update if not attached to player
        if (!isAttachedToPlayer) return;

        HandleInput();
        HandleAutomaticFire();
        HandleAiming();
        UpdateWeaponPositioning();
        UpdateIdleSway();
    }

    private void InitializeComponents()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    private void InitializeFromWeaponData()
    {
        owner = transform.root.gameObject;
        if (weaponData != null)
        {
            // Set serialized fields based on weapon data
            weaponDamage = Mathf.CeilToInt(weaponData.weaponType == WeaponType.Shotgun ?
                            weaponDamage / pelletsPerShot : weaponDamage);

            // Configure spread based on weapon type
            if (weaponData.weaponType == WeaponType.Shotgun)
            {
                // Shotguns use their own spread system
                addBulletSpread = false;

                // Increase recoil for shotguns
                recoilVerticalStrength *= 1.8f;
                recoilHorizontalStrength *= 1.5f;
                weaponShakeIntensity *= 1.5f;
                weaponShakeDuration *= 1.5f;
            }

            // Other weapon-specific configurations can be added here
        }
    }

    private void InitializeInputSystem()
    {
        playerInput = new PlayerInputs();
        playerInput.Enable();

        shootAction = playerInput.OnFoot.Shoot;
        reloadAction = playerInput.OnFoot.Reload;

        // Callbacks for tracking when fire button is pressed and released
        shootAction.started += ctx => OnShootStarted();
        shootAction.canceled += ctx => OnShootCanceled();

        // Reloading action
        reloadAction.performed += ctx => BeginReload();

        aimAction = playerInput.OnFoot.Aim;
        aimAction.started += ctx => StartAiming();
        aimAction.canceled += ctx => StopAiming();
    }



    private void OnShootStarted()
    {
        isFiring = true;

        // Cancel running, reloading, and swapping when shooting
        if (playerMovement != null && playerMovement.IsRunning())
        {
            playerMovement.StopRunning();

            // Immediately start transitioning to non-running position
            if (runningShakeCoroutine != null)
            {
                StopCoroutine(runningShakeCoroutine);
                runningShakeCoroutine = null;
            }

            // Quick transition to default position
            StartCoroutine(QuickTransitionToDefault());
        }

        if (isReloading)
        {
            CancelReload();
        }
    }

    private void OnShootCanceled()
    {
        isFiring = false;
    }

    private void InitializeAmmo()
    {
        // Skip ammo initialization during weapon pickup to prevent conflicts
        if (skipAmmoInit)
        {
            //Debug.Log($"Skipping ammo initialization for {weaponData?.weaponName} (pickup in progress)");
            return;
        }

        if (playerAmmoData != null && weaponData != null)
        {
            // Initialize the scriptable object values from weapon data
            playerAmmoData.SetAmmoInMag(weaponData.defaultAmmoInMag);
            playerAmmoData.SetAmmoInReserve(weaponData.defaultReserveAmmo);
            playerAmmoData.SetMaxAmmoInMag(weaponData.maxAmmoInMag);
            playerAmmoData.SetMaxAmmoInReserve(weaponData.maxReserveAmmo);
        }
    }

    private void HandleInput()
    {
        if (!isAutomatic && shootAction.triggered && Time.time >= lastShootTime)
        {
            OnShoot();
        }

        if (reloadAction.triggered && CanReload())
        {
            BeginReload();
        }
    }

    private void HandleAutomaticFire()
    {
        // Shotguns can never be automatic
        bool isShotgun = weaponData != null && weaponData.weaponType == WeaponType.Shotgun;

        if (isAutomatic && !isShotgun && isFiring && Time.time >= lastShootTime + (1f / fireRate))
        {
            OnShoot();
        }
    }

    private bool CanReload()
    {
        return playerAmmoData != null && playerAmmoData.CanReload() && !isReloading;
    }

    private void OnShoot()
    {
        ResetCombatTracking();

        if (isReloading)
        {
            CancelReload();
            return;
        }

        if (playerMovement != null)
        {
            playerMovement.StopRunning();
        }

        if (playerAmmoData == null || !playerAmmoData.CanShoot())
        {
            // Use sound data if available, fallback to old system
            if (emptySoundData != null)
            {
                SoundEffectsManager.Instance.PlaySound(emptySoundData, transform.position);
            }
            else if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }

            OnWeaponAction?.Invoke("empty");
            return;
        }

        if (weaponData != null && weaponData.weaponType == WeaponType.Shotgun)
        {
            StartCoroutine(ShotgunFireSequence());
        }
        else
        {
            ExecuteStandardShot();

            // Reduce ammo using the scriptable object
            playerAmmoData.UseAmmo();
            lastShootTime = Time.time;

            OnWeaponAction?.Invoke("fire");
        }
    }

    private IEnumerator ShotgunFireSequence()
    {
        ExecuteShotgunShot();
        playerAmmoData.UseAmmo();
        lastShootTime = Time.time;
        OnWeaponAction?.Invoke("fire_shotgun");

        if (chamberTimeDelay > 0)
        {
            yield return new WaitForSeconds(chamberTimeDelay * 0.3f);

            // Play chamber sound
            if (chamberSoundData != null)
            {
                SoundEffectsManager.Instance.PlaySound(chamberSoundData, transform.position);
            }
            else if (audioSource != null && chamberSoundEffect != null)
            {
                audioSource.PlayOneShot(chamberSoundEffect);
            }

            OnWeaponAction?.Invoke("chamber");
            yield return new WaitForSeconds(chamberTimeDelay * 0.7f);
        }
    }

    private IEnumerator ShotgunReloadSequence()
    {
        isReloading = true;
        OnWeaponAction?.Invoke("reload_start");

        int shellsToLoad = playerAmmoData.GetShellsNeeded();

        for (int i = 0; i < shellsToLoad; i++)
        {
            // Play shell insert sound
            if (shellInsertSoundData != null)
            {
                SoundEffectsManager.Instance.PlaySound(shellInsertSoundData, transform.position);
            }
            else if (audioSource != null && shellInsertSound != null)
            {
                audioSource.PlayOneShot(shellInsertSound);
            }

            yield return new WaitForSeconds(0.3f); // Time per shell

            // Add one shell to ammo
            playerAmmoData.AddOneShell();

            // Check if reload was cancelled
            if (!isReloading) yield break;
        }

        isReloading = false;
        OnWeaponAction?.Invoke("reload_complete");
    }

    private void ExecuteStandardShot()
    {
        PlayShootingEffects();

        // Make sure any previous shake is stopped
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        // Start new shake - weapon position will be managed by the coroutine
        shakeCoroutine = StartCoroutine(ShakeWeapon());

        ApplyRecoil();
        Vector3 direction = CalculateBulletDirection();

        // Reset hit objects list for penetration
        hitObjects.Clear();

        // Perform the raycast for hit detection with potential penetration
        RaycastPenetration(barrelExit.position, direction, maxRange, mask, 0);
    }

    private void RaycastPenetration(Vector3 startPos, Vector3 direction, float remainingRange, LayerMask hitMask, int penetrationCount)
    {
        if (Physics.Raycast(startPos, direction, out RaycastHit hit, remainingRange, hitMask))
        {
            // Skip if we've already hit this object
            if (hitObjects.Contains(hit.collider.gameObject))
            {
                return;
            }

            // Add to hit objects list
            hitObjects.Add(hit.collider.gameObject);

            // Handle the hit
            HandleHit(hit, direction, Mathf.Pow(penetrationDamageMultiplier, penetrationCount));

            // Check if we can penetrate further
            bool canPenetrate = enablePenetration &&
                               penetrationCount < maxPenetrationCount &&
                               ((1 << hit.transform.gameObject.layer) & zombieLayer) != 0;

            if (canPenetrate)
            {
                // Calculate remaining range
                float distanceTraveled = hit.distance;
                float newRemainingRange = remainingRange - distanceTraveled;

                if (newRemainingRange > 0)
                {
                    // Calculate new starting position slightly past the hit point
                    Vector3 newStartPos = hit.point + direction * 0.1f;

                    // Continue penetration
                    RaycastPenetration(newStartPos, direction, newRemainingRange, hitMask, penetrationCount + 1);
                }
            }
        }
        else
        {
            // If this is the first penetration (or there's no penetration), create a miss trail
            if (penetrationCount == 0)
            {
                CreateMissTrail(direction);
            }
        }
    }

    private void ExecuteShotgunShot()
    {
        PlayShootingEffects();

        // Only play shell ejection if it's separate from the muzzle flash
        if (shellEjectionSystem != null)
            shellEjectionSystem.Play();

        // Make sure any previous shake is stopped
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        // Start new shake - weapon position will be managed by the coroutine
        shakeCoroutine = StartCoroutine(ShakeWeapon());

        ApplyRecoil(1.5f); // Apply stronger recoil for shotgun

        zombiesHitThisShot.Clear();
        totalHitsThisShot = 0;
        totalKillsThisShot = 0;

        // Get effective spread angle (reduced when aiming)
        float effectiveSpreadAngle = isAiming ?
            spreadAngle * aimSpreadAngleMultiplier : spreadAngle;

        // Apply movement spread modifiers
        if (playerMovement != null)
        {
            if (playerMovement.IsRunning())
            {
                effectiveSpreadAngle *= runningSpreadMultiplier;
            }
            else if (playerMovement.IsWalking())
            {
                effectiveSpreadAngle *= walkingSpreadMultiplier;
            }

            if (!playerMovement.IsGrounded())
            {
                effectiveSpreadAngle *= jumpingSpreadMultiplier;
            }
        }

        // Fire the pellets
        for (int i = 0; i < pelletsPerShot; i++)
        {
            // Calculate a random direction within the spread cone
            Vector3 spreadDirection = CalculateShotgunSpread(effectiveSpreadAngle);

            // Reset hit objects list for each pellet
            hitObjects.Clear();

            // Fire the pellet with potential penetration
            if (enablePenetration)
            {
                RaycastPenetration(barrelExit.position, spreadDirection, maxRange, mask, 0);
            }
            else
            {
                // Original non-penetrating shot
                if (Physics.Raycast(barrelExit.position, spreadDirection, out RaycastHit hit, maxRange, mask))
                {
                    HandleShotgunPelletHit(hit, spreadDirection);
                }
                else
                {
                    CreateMissTrail(spreadDirection);
                }
            }
        }
        // Award points based on combined shotgun hits after all pellets are fired
        AwardShotgunPoints();
    }

    private Vector3 CalculateShotgunSpread(float maxAngle)
    {
        Vector3 forwardDirection = barrelExit.forward;

        // Generate a random direction within a cone
        float horizontalAngle = Random.Range(-maxAngle, maxAngle);
        float verticalAngle = Random.Range(-maxAngle, maxAngle);

        // Create a rotation for this spread
        Quaternion spreadRotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);

        // Apply the spread rotation to the forward direction of the barrel
        return spreadRotation * forwardDirection;
    }

    private void HandleShotgunPelletHit(RaycastHit hit, Vector3 direction, float damageMultiplier = 1.0f)
    {
        // Draw a debug ray to visualize the pellet path
        //Debug.DrawLine(barrelExit.position, hit.point, Color.red, 2f);

        // Create the trail using the same logic as standard bullets
        TrailRenderer trail = Instantiate(bulletTracer, barrelExit.position, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, hit.point, hit));

        // Calculate hit force direction
        Vector3 hitDirection = CalculateHitDirection(hit.point);

        // Check if we hit a zombie and play effects
        bool hitZombie = hit.transform.GetComponent<Limb>() != null ||
                         hit.transform.GetComponent<NonAmputatableLimb>() != null;

        if (hitZombie)
        {
            PlayHitEffects(hit.point, hit.normal);
        }

        // Process damage and track it per zombie
        ProcessShotgunPelletDamage(hit, hitDirection, damageMultiplier);
    }

    private void ProcessShotgunPelletDamage(RaycastHit hit, Vector3 hitDirection, float damageMultiplier = 1.0f)
    {
        // Try to get the limb or non-amputatable limb component
        Limb limb = hit.transform.GetComponent<Limb>();
        NonAmputatableLimb nLimb = hit.transform.GetComponent<NonAmputatableLimb>();

        ZombieAI hitZombie = null;
        string hitBodyPart = "";

        // Debug logs to help troubleshoot
        //Debug.Log($"Shotgun pellet hit: {hit.transform.name}, has Limb: {limb != null}, has NonAmputatableLimb: {nLimb != null}");

        if (limb != null)
        {
            hitZombie = FindZombieComponent(limb.transform);
            hitBodyPart = limb.name;

            if (hitZombie != null)
            {
                // Track whether this zombie was alive before the hit
                bool wasAlive = !hitZombie.dead;
                float previousHP = hitZombie.zombieHP;

                //Debug.Log($"Applying shotgun damage to zombie via limb. Before: HP={previousHP}, Dead={hitZombie.dead}");

                // Apply damage with explicit damage amount and damage multiplier
                int modifiedDamage = Mathf.RoundToInt(weaponDamage * damageMultiplier);
                limb.GetHit(modifiedDamage, hit.point, hitDirection);

                //Debug.Log($"After damage applied: HP={hitZombie.zombieHP}, Dead={hitZombie.dead}");

                // Record hit and check for kill
                if (wasAlive)
                {
                    totalHitsThisShot++;

                    if (!zombiesHitThisShot.ContainsKey(hitZombie))
                    {
                        zombiesHitThisShot[hitZombie] = false;
                    }

                    // Check if this pellet caused a kill
                    if (previousHP > 0 && hitZombie.dead && !zombiesHitThisShot[hitZombie])
                    {
                        totalKillsThisShot++;
                        zombiesHitThisShot[hitZombie] = true;
                    }
                }
            }
            else
            {
                //Debug.LogWarning($"Shotgun hit a Limb but couldn't find associated ZombieAI");
            }
        }
        else if (nLimb != null)
        {
            hitZombie = FindZombieComponent(nLimb.transform);
            hitBodyPart = nLimb.name;

            if (hitZombie != null)
            {
                // Track whether this zombie was alive before the hit
                bool wasAlive = !hitZombie.dead;
                float previousHP = hitZombie.zombieHP;

                //Debug.Log($"Applying shotgun damage to zombie via non-amputatable limb. Before: HP={previousHP}, Dead={hitZombie.dead}");

                // Apply damage with damage multiplier
                int modifiedDamage = Mathf.RoundToInt(weaponDamage * damageMultiplier);
                nLimb.GetHit(modifiedDamage, hit.point, hitDirection);

                //Debug.Log($"After damage applied: HP={hitZombie.zombieHP}, Dead={hitZombie.dead}");

                // Record hit and check for kill
                if (wasAlive)
                {
                    totalHitsThisShot++;

                    if (!zombiesHitThisShot.ContainsKey(hitZombie))
                    {
                        zombiesHitThisShot[hitZombie] = false;
                    }

                    // Check if this pellet caused a kill
                    if (previousHP > 0 && hitZombie.dead && !zombiesHitThisShot[hitZombie])
                    {
                        totalKillsThisShot++;
                        zombiesHitThisShot[hitZombie] = true;
                    }
                }
            }
            else
            {
                //Debug.LogWarning($"Shotgun hit a NonAmputatableLimb but couldn't find associated ZombieAI");
            }
        }

        // Create visual impact effect for shotgun
        if (shotgunImpactPrefab != null)
        {
            Instantiate(shotgunImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }
        else if (impactParticleSystem != null)
        {
            Instantiate(impactParticleSystem, hit.point, Quaternion.LookRotation(hit.normal));
        }
    }

    private void AwardShotgunPoints()
    {
        // If we've hit and killed zombies with this shotgun blast
        if (totalHitsThisShot > 0 || totalKillsThisShot > 0)
        {
            gotHit = totalHitsThisShot > 0;
            gotKill = totalKillsThisShot > 0;

            // Award points for the hits and kills
            if (points != null)
            {
                points.AddPoints(owner, gotHit, gotKill, "shotgun");

                // For multiple kills with one shot, we'll make additional AddPoints calls
                // with hit=false but kill=true to give bonus points for each additional kill
                if (totalKillsThisShot > 1)
                {
                    // Add one kill worth of points for each additional kill beyond the first
                    for (int i = 1; i < totalKillsThisShot; i++)
                    {
                        points.AddPoints(owner, false, true, "shotgun_multikill");
                    }

                    //Debug.Log($"Shotgun multikill bonus awarded for {totalKillsThisShot} kills in one shot!");
                }
            }
        }
    }

    public void ToggleUpgradedSounds(bool enable)
    {
        useUpgradedSounds = enable;
        // Optionally notify the player
        if (enable)
        {
            OnWeaponAction?.Invoke("upgraded_sounds_on");
        }
        else
        {
            OnWeaponAction?.Invoke("upgraded_sounds_off");
        }
    }

    // Simple toggle method that flips the current state
    public void ToggleUpgradedSounds()
    {
        ToggleUpgradedSounds(!useUpgradedSounds);
    }

    // Getter for current state
    public bool IsUsingUpgradedSounds()
    {
        return useUpgradedSounds;
    }

    private void ApplyRecoil(float multiplier = 1.0f)
    {
        if (!applyRecoil || playerLook == null) return;

        // Calculate recoil with randomness
        float verticalRecoil = -recoilVerticalStrength * multiplier;
        float horizontalRecoil = Random.Range(-recoilHorizontalStrength, recoilHorizontalStrength) * multiplier;

        // Apply randomness
        verticalRecoil += Random.Range(-recoilVerticalRandomness, recoilVerticalRandomness) * multiplier;
        horizontalRecoil += Random.Range(-recoilHorizontalRandomness, recoilHorizontalRandomness) * multiplier;

        // Apply recoil through the PlayerLook component
        playerLook.AddRecoil(new Vector2(horizontalRecoil, verticalRecoil), recoilRecoverySpeed);
    }

    private IEnumerator ShakeWeapon()
    {
        float elapsed = 0f;

        // Store the position we should return to after shaking
        Vector3 returnPosition = GetTargetWeaponPosition();
        positionOverridden = true;

        // Reduce shake intensity when aiming
        float shakeIntensityMultiplier = isAiming ? 0.33f : 1.0f;

        while (elapsed < weaponShakeDuration)
        {
            float strength = weaponShakeCurve.Evaluate(elapsed / weaponShakeDuration);
            Vector3 shakeAmount = new Vector3(
                Random.Range(-1f, 1f) * weaponShakeIntensity * shakeIntensityMultiplier,
                Random.Range(0.5f, 1f) * weaponShakeIntensity * shakeIntensityMultiplier, // Biased upward for recoil effect
                0
            ) * strength;

            // Use the correct base position based on current state
            transform.localPosition = returnPosition + shakeAmount;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Return to correct position
        transform.localPosition = returnPosition;
        positionOverridden = false;
        shakeCoroutine = null;
    }

    private void PlayHitEffects(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!enableHitEffects) return;

        // Play hit sound using new system
        if (zombieHitSoundData != null)
        {
            SoundEffectsManager.Instance.PlaySound(zombieHitSoundData, hitPoint);
        }
        else if (zombieHitSound != null && audioSource != null)
        {
            // Fallback to old system
            GameObject tempAudio = new GameObject("HitSound");
            tempAudio.transform.position = hitPoint;
            AudioSource hitAudioSource = tempAudio.AddComponent<AudioSource>();
            hitAudioSource.clip = zombieHitSound;
            hitAudioSource.volume = audioSource.volume * 0.7f;
            hitAudioSource.spatialBlend = 1f;
            hitAudioSource.maxDistance = 15f;
            hitAudioSource.Play();
            Destroy(tempAudio, zombieHitSound.length + 0.1f);
        }

        // Spawn blood splash effect
        if (bloodEffectsData != null && bloodEffectsData.bloodSplashPrefabs != null && bloodEffectsData.bloodSplashPrefabs.Length > 0)
        {
            // Choose a random blood splash effect from the ScriptableObject
            GameObject randomBloodSplash = bloodEffectsData.bloodSplashPrefabs[Random.Range(0, bloodEffectsData.bloodSplashPrefabs.Length)];

            if (randomBloodSplash != null)
            {
                GameObject bloodSplash = Instantiate(randomBloodSplash, hitPoint, Quaternion.LookRotation(hitNormal));
                bloodSplash.transform.localScale = Vector3.one * bloodSplashScale;

                // Destroy after a set time since it's just a GameObject
                Destroy(bloodSplash, 3f);
            }
        }
    }

    private IEnumerator RunningShake()
    {
        float shakeTime = 0f;

        while (playerMovement != null && playerMovement.IsRunning())
        {
            shakeTime += Time.deltaTime * runningShakeSpeed;

            // Create a bobbing motion while running
            float xShake = Mathf.Sin(shakeTime * 1.1f) * runningShakeIntensity * 0.5f;
            float yShake = Mathf.Abs(Mathf.Sin(shakeTime * 2.2f)) * runningShakeIntensity;

            Vector3 runShakeOffset = new Vector3(xShake, yShake, 0);

            // Only apply if not being overridden by other effects
            if (!positionOverridden)
            {
                transform.localPosition = GetTargetWeaponPosition() + runShakeOffset;
            }

            yield return null;
        }

        // Reset position if no longer running
        if (!positionOverridden)
        {
            transform.localPosition = GetTargetWeaponPosition();
        }

        runningShakeCoroutine = null;
    }

    private void UpdateIdleSway()
    {
        if (positionOverridden)
            return;

        // Increase time
        swayTime += Time.deltaTime * swaySpeed;

        // Calculate base sway amount based on aiming state
        float currentSwayAmount = isAiming ? aimSwayAmount : swayAmount;

        // Apply movement modifiers to sway
        if (playerMovement != null)
        {
            if (playerMovement.IsRunning())
            {
                currentSwayAmount *= runSwayMultiplier;
            }
            else if (playerMovement.IsWalking())
            {
                currentSwayAmount *= walkSwayMultiplier;
            }

            if (!playerMovement.IsGrounded())
            {
                currentSwayAmount *= jumpSwayMultiplier;
            }
        }

        // Calculate a smooth sway pattern using sine waves
        float swayX = Mathf.Sin(swayTime * 1.1f) * currentSwayAmount;
        float swayY = Mathf.Sin(swayTime * 1.3f + 1.0f) * currentSwayAmount * 0.7f;

        // Apply sway curve for more natural movement
        swayX *= swayCurve.Evaluate(Mathf.PingPong(swayTime * 0.5f, 1.0f));
        swayY *= swayCurve.Evaluate(Mathf.PingPong(swayTime * 0.5f + 0.5f, 1.0f));

        // Set the sway offset
        swayOffset = new Vector3(swayX, swayY, 0);
    }

    private IEnumerator QuickTransitionToDefault()
    {
        float elapsed = 0f;
        float duration = 0.15f; // Quick transition
        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = GetTargetWeaponPosition();
        Quaternion startRot = transform.localRotation;
        Quaternion targetRot = GetTargetWeaponRotation();

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = targetPos;
        transform.localRotation = targetRot;

        // Recalculate aim position after transition
        if (isAiming)
        {
            aimPosition = CalculateAimPosition();
        }
    }

    private void HandleAiming()
    {
        // Recalculate aim position if aiming and weapon state might have changed
        if (isAiming)
        {
            // Recalculate aim position to ensure it's accurate for current weapon state
            aimPosition = CalculateAimPosition();
        }

        // Determine target position based on aiming state
        targetPosition = isAiming ? aimPosition : defaultWeaponPosition;
    }

    private void UpdateWeaponPositioning()
    {
        // Check if running state has just changed
        bool shouldBeRunning = playerMovement != null && playerMovement.IsRunning() && !isReloading && !isAiming;

        // Only update position if not being overridden by shake or other effects
        if (!positionOverridden)
        {
            Vector3 targetPos = GetTargetWeaponPosition();
            Quaternion targetRot = GetTargetWeaponRotation();

            // Use faster transition when state changes, normal speed otherwise
            float transitionSpeed = shouldBeRunning ? aimTransitionSpeed : runningTransitionSpeed;

            // Apply the position and rotation
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos + swayOffset, Time.deltaTime * transitionSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * transitionSpeed);
        }

        // Handle FOV changes for aiming
        if (mainCamera != null)
        {
            float targetFOV = isAiming ? aimFOV : regularFOV;
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.deltaTime * aimTransitionSpeed);
        }

        // Start or maintain running shake effect
        if (shouldBeRunning && runningShakeCoroutine == null)
        {
            runningShakeCoroutine = StartCoroutine(RunningShake());
        }
    }

    private Vector3 GetTargetWeaponPosition()
    {
        if (isReloading)
        {
            return defaultWeaponPosition + (weaponData != null ? weaponData.reloadPositionOffset : Vector3.zero);
        }
        else if (playerMovement != null && playerMovement.IsRunning() && !isAiming)
        {
            return defaultWeaponPosition + (weaponData != null ? weaponData.runningPositionOffset : Vector3.zero);
        }
        else if (isAiming)
        {
            return aimPosition;
        }
        else
        {
            return defaultWeaponPosition;
        }
    }

    private Quaternion GetTargetWeaponRotation()
    {
        if (isReloading)
        {
            return defaultWeaponRotation * Quaternion.Euler(reloadRotation);
        }
        else if (playerMovement != null && playerMovement.IsRunning() && !isAiming)
        {
            return defaultWeaponRotation * Quaternion.Euler(runningRotation);
        }
        else
        {
            return defaultWeaponRotation;
        }
    }

    private Vector3 CalculateAimPosition()
    {
        if (mainCamera == null || aimSightTransform == null)
        {
            //Debug.LogError("Cannot calculate aim position - missing camera or sight transform");
            return defaultWeaponPosition;
        }

        // Log debug info
        //Debug.Log("Calculating aim position...");
        //Debug.Log("Sight transform: " + aimSightTransform.position);
        //Debug.Log("Camera position: " + mainCamera.transform.position);

        // We need to find the position that would place the gun's sights
        // directly in the center of the camera's view

        // Get the world position of the sight
        Vector3 sightWorldPos = aimSightTransform.position;

        // Get the position of the camera
        Vector3 cameraPos = mainCamera.transform.position;

        // Get the forward direction of the camera
        Vector3 cameraForward = mainCamera.transform.forward;

        // Calculate the vector from the sight to the camera
        Vector3 sightToCamera = cameraPos - sightWorldPos;

        // Project this vector onto the camera's forward direction to get the distance
        float distanceAlongForward = Vector3.Dot(sightToCamera, cameraForward);

        // Calculate the point along the camera's forward vector at this distance
        Vector3 targetSightPos = cameraPos - cameraForward * distanceAlongForward;

        // Calculate how much the weapon needs to move to get the sight to this position
        Vector3 weaponOffset = targetSightPos - sightWorldPos;

        // Apply this offset to the current weapon position
        Vector3 targetWeaponPos = transform.localPosition + transform.parent.InverseTransformDirection(weaponOffset);

        // Draw debug lines
        //Debug.DrawLine(cameraPos, targetSightPos, Color.red, 2f);
        //Debug.DrawLine(sightWorldPos, targetSightPos, Color.blue, 2f);

        //Debug.Log("Calculated aim position: " + targetWeaponPos);
        return targetWeaponPos;
    }

    private void StartAiming()
    {
        // Can't aim while reloading
        if (isReloading)
            return;

        isAiming = true;
        // Recalculate aim position each time we start aiming to ensure accuracy
        aimPosition = CalculateAimPosition();

        if (playerLook != null)
            playerLook.SetAiming(true);

        OnWeaponAction?.Invoke("aim_start");
    }

    private void StopAiming()
    {
        isAiming = false;
        if (playerLook != null)
            playerLook.SetAiming(false);

        OnWeaponAction?.Invoke("aim_stop");
    }

    // Public method to recalculate aim position when weapon state changes
    public void RecalculateAimPosition()
    {
        if (isAiming)
        {
            aimPosition = CalculateAimPosition();
        }
    }

    private void BeginReload()
    {
        if (!CanReload())
            return;

        // Cancel running when reloading
        if (playerMovement != null)
        {
            playerMovement.StopRunning();
        }

        // Start reload process
        StartCoroutine(ReloadSequence());
    }

    private IEnumerator ReloadSequence()
    {
        isReloading = true;
        OnWeaponAction?.Invoke("reload_start");

        // Handle reload sounds with new system
        if (useSingleReloadSound)
        {
            if (reloadSoundData != null)
            {
                SoundEffectsManager.Instance.PlaySound(reloadSoundData, transform.position);
            }
            else if (audioSource != null && reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
        }
        else
        {
            // Play unload sound
            if (unloadSoundData != null)
            {
                SoundEffectsManager.Instance.PlaySound(unloadSoundData, transform.position);
            }
            else if (audioSource != null && unloadSound != null)
            {
                audioSource.PlayOneShot(unloadSound);
            }
        }

        float loadSoundDelay = reloadSpeed;
        if (!useSingleReloadSound && loadSound != null && audioSource.clip != null)
        {
            loadSoundDelay = reloadSpeed - audioSource.clip.length;
            if (loadSoundDelay < 0) loadSoundDelay = reloadSpeed * 0.7f;
        }

        yield return new WaitForSeconds(loadSoundDelay);

        if (!isReloading) yield break;

        // Play load sound
        if (!useSingleReloadSound)
        {
            if (loadSoundData != null)
            {
                SoundEffectsManager.Instance.PlaySound(loadSoundData, transform.position);
            }
            else if (audioSource != null && loadSound != null)
            {
                audioSource.PlayOneShot(loadSound);
            }
        }

        yield return new WaitForSeconds(reloadSpeed - loadSoundDelay);

        if (!isReloading) yield break;

        if (playerAmmoData != null)
        {
            playerAmmoData.Reload();
        }

        isReloading = false;

        if (isAiming)
        {
            aimPosition = CalculateAimPosition();
        }

        OnWeaponAction?.Invoke("reload_complete");
    }

    private void CancelReload()
    {
        isReloading = false;
        OnWeaponAction?.Invoke("reload_canceled");
    }

    // Method to refill ammo
    public void RefillAmmo()
    {
        if (playerAmmoData != null)
        {
            playerAmmoData.ResetAmmo();
        }

        // Play appropriate reload sound
        if (useSingleReloadSound)
        {
            if (reloadSoundData != null)
            {
                SoundEffectsManager.Instance.PlaySound(reloadSoundData, transform.position);
            }
            else if (audioSource != null && reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
        }
        else
        {
            if (loadSoundData != null)
            {
                SoundEffectsManager.Instance.PlaySound(loadSoundData, transform.position);
            }
            else if (audioSource != null && loadSound != null)
            {
                audioSource.PlayOneShot(loadSound);
            }
            else if (audioSource != null && reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
        }

        OnWeaponAction?.Invoke("ammo_refill");
    }

    private void HandleHit(RaycastHit hit, Vector3 direction, float damageMultiplier = 1.0f)
    {
        TrailRenderer trail = Instantiate(bulletTracer, barrelExit.transform.position, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, hit.point, hit));

        //Debug.DrawLine(barrelExit.transform.position, hit.point, Color.red, 100f);

        Vector3 hitDirection = CalculateHitDirection(hit.point);

        // Check if we hit a zombie before processing damage
        bool hitZombie = hit.transform.GetComponent<Limb>() != null ||
                         hit.transform.GetComponent<NonAmputatableLimb>() != null;

        if (hitZombie)
        {
            PlayHitEffects(hit.point, hit.normal);
        }

        ProcessTargetDamage(hit, hitDirection, damageMultiplier);

        if (crosshairManager != null)
        {
            crosshairManager.SetCrosshairColor(Color.red, 0.1f);
        }
    }

    // Calculates the force applied to hit objects based on damage and distance
    private Vector3 CalculateHitDirection(Vector3 hitPoint)
    {
        return (hitPoint - barrelExit.position).normalized * weaponDamage * 10f;
    }

    // Routes damage to the appropriate component based on what was hit
    private void ProcessTargetDamage(RaycastHit hit, Vector3 hitDirection, float damageMultiplier = 1.0f)
    {
        if (hit.transform.TryGetComponent(out Limb limb))
        {
            HandleLimbDamage(limb, hit.point, hitDirection, damageMultiplier);
            AwardPoints();
        }
        else if (hit.transform.TryGetComponent(out NonAmputatableLimb nLimb))
        {
            HandleNonAmputatableLimbDamage(nLimb, hit.point, hitDirection, damageMultiplier);
            AwardPoints();
        }
    }

    // Handles damage to removable limbs and tracks which part was hit
    private void HandleLimbDamage(Limb limb, Vector3 hitPoint, Vector3 hitDirection, float damageMultiplier = 1.0f)
    {
        if (limb == null)
        {
            //Debug.LogWarning("Attempted to handle damage for a null limb");
            return;
        }

        ZombieAI zombie = FindZombieComponent(limb.transform);

        // Check if we found a valid zombie
        if (zombie == null)
        {
            //Debug.LogWarning("Could not find a ZombieAI component on the limb's root");
            return;
        }

        bodyPart = limb.name;
        int modifiedDamage = Mathf.RoundToInt(weaponDamage * damageMultiplier);
        ApplyDamageAndTrackResults(zombie, () => limb.GetHit(modifiedDamage, hitPoint, hitDirection));
    }

    private void HandleNonAmputatableLimbDamage(NonAmputatableLimb nLimb, Vector3 hitPoint, Vector3 hitDirection, float damageMultiplier = 1.0f)
    {
        if (nLimb == null)
        {
            //Debug.LogWarning("Attempted to handle damage for a null non-amputatable limb");
            return;
        }

        // Get the zombie component with null check
        ZombieAI zombie = FindZombieComponent(nLimb.transform);

        // Check if we found a valid zombie
        if (zombie == null)
        {
            //Debug.LogWarning("Could not find a ZombieAI component on the non-amputatable limb's root");
            return;
        }

        bodyPart = nLimb.name;
        int modifiedDamage = Mathf.RoundToInt(weaponDamage * damageMultiplier);
        ApplyDamageAndTrackResults(zombie, () => nLimb.GetHit(modifiedDamage, hitPoint, hitDirection));
    }

    // Tracks zombie state before and after damage to determine if hit was lethal
    private void ApplyDamageAndTrackResults(ZombieAI zombie, System.Action applyDamage)
    {
        // Check if zombie is null before proceeding
        if (zombie == null)
        {
            //Debug.LogWarning("Attempted to apply damage to a null zombie reference");
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

    private void AwardPoints()
    {
        if (gotHit || gotKill)
        {
            if (points != null)
            {
                points.AddPoints(owner, gotHit, gotKill, bodyPart);
            }
        }
    }

    private void PlayShootingEffects()
    {
        if (audioSource != null && shootSound != null)
        {
            // Create a temporary AudioSource for pitched sounds
            if (useUpgradedSounds)
            {
                // Create a temporary GameObject and AudioSource for this sound
                GameObject tempAudio = new GameObject("TempAudio");
                tempAudio.transform.position = transform.position;
                AudioSource tempSource = tempAudio.AddComponent<AudioSource>();

                // Copy settings from our main AudioSource
                tempSource.volume = audioSource.volume;
                tempSource.spatialBlend = audioSource.spatialBlend;
                tempSource.minDistance = audioSource.minDistance;
                tempSource.maxDistance = audioSource.maxDistance;

                // Set a random pitch in our defined range
                tempSource.pitch = Random.Range(minUpgradedPitch, maxUpgradedPitch);

                // Play the sound and destroy the temporary object after it's done
                tempSource.clip = shootSound;
                tempSource.Play();
                Destroy(tempAudio, shootSound.length + 0.1f);
            }
            else
            {
                // Play with normal pitch
                audioSource.PlayOneShot(shootSound);
            }
        }

        if (shootingSystem != null)
        {
            shootingSystem.Play();
        }
    }

    // Adds randomization to shot direction based on weapon spread settings
    private Vector3 CalculateBulletDirection()
    {
        if (!addBulletSpread) return transform.forward;

        // Apply spread reduction when aiming
        float spreadFactor = isAiming ? aimSpreadMultiplier : 1.0f;

        // Apply movement spread modifiers
        if (playerMovement != null)
        {
            if (playerMovement.IsRunning())
            {
                spreadFactor *= runningSpreadMultiplier;
            }
            else if (playerMovement.IsWalking())
            {
                spreadFactor *= walkingSpreadMultiplier;
            }

            if (!playerMovement.IsGrounded())
            {
                spreadFactor *= jumpingSpreadMultiplier;
            }
        }

        Vector3 spread = new Vector3(
            Random.Range(-bulletSpreadVariance.x, bulletSpreadVariance.x) * spreadFactor,
            Random.Range(-bulletSpreadVariance.y, bulletSpreadVariance.y) * spreadFactor,
            Random.Range(-bulletSpreadVariance.z, bulletSpreadVariance.z) * spreadFactor
        );

        return (transform.forward + spread).normalized;
    }

    private void CreateMissTrail(Vector3 direction)
    {
        TrailRenderer trail = Instantiate(bulletTracer, barrelExit.position, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, barrelExit.position + direction * maxRange, null));
    }

    // Animates bullet trail from barrel to impact point
    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, RaycastHit? hit, float durationMultiplier = 1.0f)
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

        yield return new WaitForSeconds(minTrailDuration * durationMultiplier);
        Destroy(trail.gameObject, trail.time);
    }

    private void ResetCombatTracking()
    {
        gotHit = false;
        gotKill = false;
        bodyPart = "";
    }

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

        //Debug.LogWarning($"Could not find ZombieAI component. Hierarchy: {GetHierarchyPath(limbTransform)}");
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

    // Public methods for other scripts to check weapon state
    public bool IsReloading() => isReloading;
    public bool IsFiring() => isFiring;
    public bool IsAiming() => isAiming;

    // Public method for weapon manager to control ammo initialization during pickup
    public void SetFlag_SkipAmmoInit(bool skip)
    {
        skipAmmoInit = skip;
        //Debug.Log($"Weapon {weaponData?.weaponName} - Skip ammo init set to: {skip}");
    }
}