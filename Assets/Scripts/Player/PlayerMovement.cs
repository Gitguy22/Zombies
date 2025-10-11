using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using PlayerStats;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float baseWalkSpeed = 5f;
    [SerializeField] float baseSprintSpeed = 10f;
    [SerializeField] float baseJumpHeight = 1.2f;
    [SerializeField] float baseJumpHorizontalPower = 1f;
    [SerializeField] float baseMaxJumpDistance = 8f;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float groundedGravity = -2f;
    [SerializeField] float turnSpeed = 15f;

    [Header("Base Momentum Settings")]
    [SerializeField] float baseMomentumPreservation = 0.8f;
    [SerializeField] float baseAirControl = 0.3f;
    [SerializeField] float baseMomentumDecay = 2f;
    [SerializeField] float baseMaxAirSpeed = 12f;

    [Header("Base Acceleration Settings")]
    [SerializeField] float baseMovementAcceleration = 10f;
    [SerializeField] float baseSprintAcceleration = 8f;
    [SerializeField] float baseGroundDrag = 5f;

    [Header("Movement State")]
    [SerializeField] bool allowSprinting = true;
    [SerializeField] bool allowJumping = true;
    [SerializeField] bool requireStaminaToSprint = true;
    [SerializeField] bool requireStaminaToJump = true;

    [Header("References")]
    [SerializeField] GameObject pauseMenu;
    [SerializeField] Animator animator;
    [SerializeField] Transform orientation;

    [Header("Debug")]
    [SerializeField] bool showDebugInfo = false;

    // Component references
    private CharacterController controller;
    private PlayerLook playerLook;
    private Camera mainCamera;
    private WeaponManager weaponManager;
    private PlayerStamina playerStamina;
    private PlayerStatsManager statsManager;

    // Movement state
    private Vector3 playerVelocity;
    private Vector3 horizontalVelocity;
    private Vector3 lastGroundedVelocity;
    private Vector3 targetVelocity;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool isRunning;
    private bool wantsToRun;
    private int jumpCount = 0;

    // Stat-based values (cached)
    public float walkSpeed { get; private set; }
    public float sprintSpeed { get; private set; }
    private float jumpHeight;
    private float jumpHorizontalPower;
    private float maxJumpDistance;
    private float currentSpeed;
    private float gravityMultiplier;
    private float airControl;
    private float momentumPreservation;
    private float momentumDecay;
    private float maxAirSpeed;
    private float movementAcceleration;
    private float sprintAcceleration;
    private float groundDrag;
    private float adsMovementMultiplier;
    private int maxJumpCount;

    // Input System
    private PlayerInput playerInput;
    private InputAction movementAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private bool inputsInitialized = false;

    // Public getter for currentSpeed
    public float GetCurrentSpeed() => currentSpeed;

    private void Awake()
    {
        InitializeComponents();
        InitializeInputSystem();
    }

    private void Start()
    {
        ValidateComponents();
        InitializeStats();

        if (statsManager != null)
        {
            statsManager.OnStatChanged.AddListener(OnStatChanged);
        }
    }

    private void OnEnable()
    {
        if (playerInput != null)
        {
            RegisterInputCallbacks(true);
        }
    }

    private void OnDisable()
    {
        if (playerInput != null)
        {
            RegisterInputCallbacks(false);
        }
    }

    private void OnDestroy()
    {
        if (statsManager != null)
        {
            statsManager.OnStatChanged.RemoveListener(OnStatChanged);
        }
    }

    private void Update()
    {
        // Check pause state
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused()) return;

        if (controller == null) return;

        // Store previous grounded state
        wasGroundedLastFrame = isGrounded;

        // Update movement
        HandleMovement();

        // Update rotation
        UpdatePlayerRotation();

        // Check weapon state for sprint cancellation
        CheckWeaponState();

        // Handle sprint resumption after landing
        HandleSprintResumption();
    }

    private void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = GetComponentInChildren<Camera>();
        if (mainCamera == null) mainCamera = Camera.main;

        playerInput = GetComponent<PlayerInput>();
        playerLook = GetComponentInChildren<PlayerLook>();
        weaponManager = GetComponent<WeaponManager>();
        playerStamina = GetComponent<PlayerStamina>();
        statsManager = GetComponent<PlayerStatsManager>();

        if (playerLook != null && orientation == null)
        {
            orientation = playerLook.GetOrientation();
        }
    }

    private void InitializeInputSystem()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError($"{gameObject.name}: PlayerInput or actions are null!");
            return;
        }

        try
        {
            movementAction = playerInput.actions["Movement"];
            jumpAction = playerInput.actions["Jump"];
            sprintAction = playerInput.actions["Sprint"];

            inputsInitialized = movementAction != null && jumpAction != null && sprintAction != null;

            if (inputsInitialized)
            {
                currentSpeed = baseWalkSpeed;
                isRunning = false;
                wantsToRun = false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"{gameObject.name}: Error initializing inputs: {e.Message}");
            inputsInitialized = false;
        }
    }

    private void ValidateComponents()
    {
        if (controller == null)
        {
            Debug.LogError($"{gameObject.name}: No CharacterController found!");
            enabled = false;
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError($"{gameObject.name}: No Camera found!");
        }

        if (!inputsInitialized)
        {
            Debug.LogError($"{gameObject.name}: Inputs not initialized!");
        }
    }

    private void InitializeStats()
    {
        UpdateAllCachedStats();
        currentSpeed = walkSpeed;
    }

    private void UpdateAllCachedStats()
    {
        if (statsManager != null)
        {
            // Movement speeds
            walkSpeed = statsManager.GetStatValue(StatType.MovementSpeed);
            sprintSpeed = statsManager.GetStatValue(StatType.SprintSpeed);
            jumpHeight = statsManager.GetStatValue(StatType.JumpHeight);

            // Fixed jump power calculation
            // Always maintain at least base power, scale up for higher jumps
            float jumpRatio = jumpHeight / baseJumpHeight;
            if (jumpRatio >= 1f)
            {
                // Higher jumps get more horizontal power
                jumpHorizontalPower = baseJumpHorizontalPower * (1f + (jumpRatio - 1f) * 0.5f);
                maxJumpDistance = baseMaxJumpDistance * jumpRatio;
            }
            else
            {
                // Lower jumps maintain base horizontal power
                jumpHorizontalPower = baseJumpHorizontalPower;
                maxJumpDistance = baseMaxJumpDistance * 0.8f; // Slightly reduce max distance
            }

            // Movement physics
            gravityMultiplier = statsManager.GetStatValue(StatType.GravityMultiplier);
            airControl = statsManager.GetStatValue(StatType.AirControl);
            momentumPreservation = statsManager.GetStatValue(StatType.MomentumPreservation);
            momentumDecay = statsManager.GetStatValue(StatType.MomentumDecay);
            maxAirSpeed = statsManager.GetStatValue(StatType.MaxAirSpeed);

            // Acceleration
            movementAcceleration = statsManager.GetStatValue(StatType.MovementAcceleration);
            sprintAcceleration = statsManager.GetStatValue(StatType.SprintAcceleration);
            groundDrag = statsManager.GetStatValue(StatType.GroundDrag);

            // Combat
            adsMovementMultiplier = statsManager.GetStatValue(StatType.ADSMovementMultiplier);

            // Jump count
            maxJumpCount = Mathf.RoundToInt(statsManager.GetStatValue(StatType.JumpCount));
        }
        else
        {
            // Use base values
            walkSpeed = baseWalkSpeed;
            sprintSpeed = baseSprintSpeed;
            jumpHeight = baseJumpHeight;
            jumpHorizontalPower = baseJumpHorizontalPower;
            maxJumpDistance = baseMaxJumpDistance;
            gravityMultiplier = 1f;
            airControl = baseAirControl;
            momentumPreservation = baseMomentumPreservation;
            momentumDecay = baseMomentumDecay;
            maxAirSpeed = baseMaxAirSpeed;
            movementAcceleration = baseMovementAcceleration;
            sprintAcceleration = baseSprintAcceleration;
            groundDrag = baseGroundDrag;
            adsMovementMultiplier = 1f;
            maxJumpCount = 1;
        }
    }

    private void OnStatChanged(StatType statType, float oldValue, float newValue)
    {
        switch (statType)
        {
            case StatType.MovementSpeed:
            case StatType.SprintSpeed:
            case StatType.JumpHeight:
            case StatType.JumpCount:
            case StatType.GravityMultiplier:
            case StatType.AirControl:
            case StatType.MomentumPreservation:
            case StatType.MomentumDecay:
            case StatType.MaxAirSpeed:
            case StatType.MovementAcceleration:
            case StatType.SprintAcceleration:
            case StatType.GroundDrag:
            case StatType.ADSMovementMultiplier:
                UpdateAllCachedStats();

                // Update current speed if needed
                if (!isRunning && statType == StatType.MovementSpeed)
                {
                    currentSpeed = walkSpeed;
                }
                else if (isRunning && statType == StatType.SprintSpeed)
                {
                    currentSpeed = sprintSpeed;
                }
                break;
        }
    }

    private void RegisterInputCallbacks(bool register)
    {
        if (playerInput == null || sprintAction == null) return;

        if (register)
        {
            sprintAction.started += OnSprintStarted;
            sprintAction.canceled += OnSprintCanceled;
        }
        else
        {
            sprintAction.started -= OnSprintStarted;
            sprintAction.canceled -= OnSprintCanceled;
        }
    }

    private void OnSprintStarted(InputAction.CallbackContext ctx)
    {
        wantsToRun = true;
        TryStartSprinting();
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        wantsToRun = false;
        StopRunning();
    }

    private void HandleMovement()
    {
        CheckGrounded();

        if (isGrounded)
        {
            HandleGroundedMovement();
        }
        else
        {
            HandleAirMovement();
        }

        HandleJump();
        ApplyGravity();
        ApplyMovement();
    }

    private void CheckGrounded()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = groundedGravity;
            if (animator != null) animator.SetBool("Jumping", false);

            // Reset jump count when landing
            if (!wasGroundedLastFrame)
            {
                jumpCount = 0;
                // Don't zero out horizontal velocity - preserve momentum
            }
        }
    }

    private void HandleGroundedMovement()
    {
        if (movementAction == null) return;

        Vector2 input = movementAction.ReadValue<Vector2>();

        // Stop running if moving backward/sideways only OR out of stamina
        if (isRunning)
        {
            if (input.y <= 0)
            {
                StopRunning();
            }
            else if (requireStaminaToSprint && playerStamina != null && !playerStamina.CanSprint())
            {
                // Force stop sprinting if out of stamina
                StopRunning();
                wantsToRun = false; // Also clear the sprint intent
            }
        }

        // Calculate movement direction
        Vector3 moveDir = CalculateMoveDirection(input);

        // Apply ADS movement multiplier if aiming
        float currentMaxSpeed = currentSpeed;
        Weapon activeWeapon = GetActiveWeapon();
        if (activeWeapon != null && activeWeapon.IsAiming())
        {
            currentMaxSpeed *= adsMovementMultiplier;
        }

        if (moveDir.magnitude > 0.1f)
        {
            moveDir.Normalize();
            targetVelocity = moveDir * currentMaxSpeed;
        }
        else
        {
            targetVelocity = Vector3.zero;
        }

        // Apply acceleration
        float acceleration = isRunning ? sprintAcceleration : movementAcceleration;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, Time.deltaTime * acceleration);

        // Always update lastGroundedVelocity with current horizontal velocity
        lastGroundedVelocity = horizontalVelocity;

        // Update animations
        if (animator != null) UpdateAnimations(input);
    }

    private void HandleAirMovement()
    {
        if (movementAction == null || mainCamera == null) return;

        Vector2 input = movementAction.ReadValue<Vector2>();

        if (input.magnitude > 0.1f)
        {
            Vector3 inputDir = CalculateMoveDirection(input).normalized;

            // Apply air control - modify current horizontal velocity slightly
            Vector3 targetVelocity = inputDir * currentSpeed * airControl;
            horizontalVelocity = Vector3.Lerp(horizontalVelocity,
                horizontalVelocity + targetVelocity, Time.deltaTime * airControl);

            // Clamp air speed
            if (horizontalVelocity.magnitude > maxAirSpeed)
            {
                horizontalVelocity = horizontalVelocity.normalized * maxAirSpeed;
            }
        }

        // Only apply momentum decay if velocity is very low
        if (horizontalVelocity.magnitude < 0.5f)
        {
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, Time.deltaTime * momentumDecay);
        }
    }

    private Vector3 CalculateMoveDirection(Vector2 input)
    {
        if (mainCamera == null) return Vector3.zero;

        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;

        forward.y = 0;
        right.y = 0;

        if (forward.magnitude > 0.001f) forward.Normalize();
        if (right.magnitude > 0.001f) right.Normalize();

        return right * input.x + forward * input.y;
    }

    private void HandleJump()
    {
        if (!allowJumping || jumpAction == null) return;

        bool canJump = (isGrounded && jumpCount < maxJumpCount) ||
                      (!isGrounded && jumpCount > 0 && jumpCount < maxJumpCount);

        if (canJump && jumpAction.triggered)
        {
            Jump();
        }
    }

    private void Jump()
    {
        // Check stamina requirement
        if (requireStaminaToJump && playerStamina != null)
        {
            float jumpCost = statsManager != null ?
                statsManager.GetStatValue(StatType.JumpStaminaCost) : 10f;

            if (!playerStamina.HasEnoughStamina(jumpCost))
            {
                return;
            }

            playerStamina.ModifyStamina(-jumpCost);
        }

        // Apply jump with stat-based height
        playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity * gravityMultiplier);

        // Preserve horizontal momentum
        if (jumpCount == 0) // First jump
        {
            // Apply momentum preservation and jump power
            horizontalVelocity = lastGroundedVelocity * momentumPreservation * jumpHorizontalPower;

            // Clamp to maximum jump distance
            if (horizontalVelocity.magnitude > maxJumpDistance)
            {
                horizontalVelocity = horizontalVelocity.normalized * maxJumpDistance;
            }
        }
        // For double/triple jumps, maintain current velocity

        jumpCount++;

        if (animator != null) animator.SetBool("Jumping", true);

        if (showDebugInfo)
        {
            Debug.Log($"Jump #{jumpCount}: Height={jumpHeight}, Power={jumpHorizontalPower:F2}, HVelocity={horizontalVelocity.magnitude:F2}");
        }
    }

    private void ApplyGravity()
    {
        playerVelocity.y += gravity * gravityMultiplier * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        Vector3 totalMovement = horizontalVelocity * Time.deltaTime + playerVelocity * Time.deltaTime;
        controller.Move(totalMovement);
    }

    private void UpdatePlayerRotation()
    {
        if (mainCamera == null) return;

        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0;

        if (cameraForward.magnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    private void CheckWeaponState()
    {
        if (!isRunning) return;

        Weapon activeWeapon = GetActiveWeapon();
        if (activeWeapon == null) return;

        if (activeWeapon.IsReloading() || activeWeapon.IsFiring() || activeWeapon.IsAiming())
        {
            StopRunning();
        }
    }

    private void HandleSprintResumption()
    {
        if (wantsToRun && !isRunning && isGrounded && !wasGroundedLastFrame)
        {
            TryStartSprinting();
        }
    }

    private void TryStartSprinting()
    {
        if (!allowSprinting || !isGrounded) return;

        // Check stamina requirement
        if (requireStaminaToSprint && playerStamina != null && !playerStamina.CanSprint())
        {
            return;
        }

        // Check movement direction
        if (movementAction == null) return;
        Vector2 input = movementAction.ReadValue<Vector2>();
        if (input.y <= 0) return;

        // Check weapon state
        Weapon activeWeapon = GetActiveWeapon();
        if (activeWeapon != null)
        {
            if (activeWeapon.IsReloading() || activeWeapon.IsFiring() || activeWeapon.IsAiming())
                return;
        }

        StartSprinting();
    }

    private void StartSprinting()
    {
        currentSpeed = sprintSpeed;
        isRunning = true;
        if (animator != null) animator.SetBool("ForwardRunning", true);
    }

    public void StopRunning()
    {
        currentSpeed = walkSpeed;
        isRunning = false;
        if (animator != null) animator.SetBool("ForwardRunning", false);
    }

    private Weapon GetActiveWeapon()
    {
        if (weaponManager != null)
        {
            GameObject currentWeaponObj = weaponManager.GetCurrentWeapon();
            if (currentWeaponObj != null)
            {
                return currentWeaponObj.GetComponent<Weapon>();
            }
        }

        return GetComponentInChildren<Weapon>();
    }

    private void UpdateAnimations(Vector2 input)
    {
        bool isMoving = input != Vector2.zero;

        if (animator == null) return;

        animator.SetBool("Idle", !isMoving);

        if (isMoving)
        {
            animator.SetBool("Forward", input.y > 0 && currentSpeed == walkSpeed);
            animator.SetBool("Backward", input.y < 0);
            animator.SetBool("Left", input.x < 0);
            animator.SetBool("Right", input.x > 0);
        }
        else
        {
            animator.SetBool("Forward", false);
            animator.SetBool("Backward", false);
            animator.SetBool("Left", false);
            animator.SetBool("Right", false);
        }
    }

    // Public getters
    public bool IsRunning() => isRunning;
    public bool IsWalking() => movementAction?.ReadValue<Vector2>().magnitude > 0.1f && !isRunning;
    public bool IsGrounded() => isGrounded;
    public Vector3 GetHorizontalVelocity() => horizontalVelocity;
    public float GetMomentumMagnitude() => horizontalVelocity.magnitude;
    public bool WantsToRun() => wantsToRun;
    public int GetJumpCount() => jumpCount;
    public int GetMaxJumpCount() => maxJumpCount;

    // Public setters for external control
    public void SetAllowSprinting(bool allow) => allowSprinting = allow;
    public void SetAllowJumping(bool allow) => allowJumping = allow;
    public void SetRequireStaminaToSprint(bool require) => requireStaminaToSprint = require;
    public void SetRequireStaminaToJump(bool require) => requireStaminaToJump = require;
}