using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class PlayerLook : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] float mouseSensitivityX = 750f;
    [SerializeField] float mouseSensitivityY = 750f;
    [SerializeField] float gamepadSensitivityX = 1200f;
    [SerializeField] float gamepadSensitivityY = 1200f;
    [SerializeField] float lookSmoothTime = 0.03f;
    [SerializeField] bool invertY = false;

    [Header("Camera Constraints")]
    [SerializeField] float upperLookLimit = 80f;
    [SerializeField] float lowerLookLimit = -80f;

    [Header("References")]
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform orientation;
    [SerializeField] CinemachineVirtualCamera virtualCamera;

    [Header("Recoil Settings")]
    [SerializeField] bool enableRecoil = true;
    [SerializeField] float maxRecoilDistance = 5f;
    [SerializeField] float recoilSharpness = 20f;

    [Header("Aiming Settings")]
    [SerializeField] float aimSensitivityMultiplier = 0.5f;
    [SerializeField] float regularFOV = 90f;
    [SerializeField] float aimFOV = 40f;
    [SerializeField] float fovTransitionSpeed = 10f;
    bool isAiming = false;

    [Header("Debug")]
    [SerializeField] bool showDebugInfo = true;

    [Header("IK lookpoint")]
    [SerializeField] Transform lookPoint;
    [SerializeField] float lookDistance = 100f;

    // Player reference
    private PlayerController playerController;

    // Input system
    PlayerInputs playerInputs;
    InputAction lookAction;
    InputAction pauseAction;
    InputAction aimAction;

    // Camera variables
    float cameraPitch = 0f;
    float cameraYaw = 0f;
    Vector2 lookInputDelta;

    // Recoil variables
    Vector2 currentRecoil;
    Vector2 targetRecoil;
    float recoilRecoverySpeed;

    // Cinemachine components
    CinemachinePOV cinemachinePOV;
    CinemachineBasicMultiChannelPerlin cinemachineNoise;
    float targetFOV;
    float currentFOV;

    // Track pause state for cursor management
    private bool wasPaused = false;
    private bool hasInitializedCursor = false;
    private bool inputsEnabled = false;

    private void Awake()
    {
        // Get player controller reference
        playerController = GetComponentInParent<PlayerController>();

        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            Camera playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
                cameraTransform = playerCamera.transform;
        }

        // Setup Cinemachine
        SetupCinemachine();

        // Create orientation object if not assigned
        if (orientation == null)
        {
            GameObject orientationObj = new GameObject("Orientation");
            orientationObj.transform.SetParent(transform);
            orientationObj.transform.localPosition = Vector3.zero;
            orientationObj.transform.localRotation = Quaternion.identity;
            orientation = orientationObj.transform;
        }

        // Initialize FOV
        currentFOV = regularFOV;
        targetFOV = regularFOV;
        UpdateCinemachineFOV(regularFOV);

        // Create the lookpoint if not assigned
        if (lookPoint == null)
        {
            GameObject lookPointObj = new GameObject("LookPoint");
            lookPoint = lookPointObj.transform;
            lookPoint.SetParent(transform);
        }

        // Initialize camera angles from transform rotation
        cameraYaw = transform.eulerAngles.y;
        if (cinemachinePOV != null)
        {
            cinemachinePOV.m_HorizontalAxis.Value = cameraYaw;
        }
    }

    private void Start()
    {
        // Initialize inputs properly
        InitializeInputs();

        // Force cursor to be locked for player 0 at game start
        if (playerController != null && playerController.playerIndex == 0)
        {
            StartCoroutine(InitializeCursorAfterDelay());
        }
    }

    private void InitializeInputs()
    {
        // Get player inputs from controller OR create new ones
        if (playerController != null)
        {
            playerInputs = playerController.GetPlayerInputs();
        }

        if (playerInputs == null)
        {
            playerInputs = new PlayerInputs();
        }

        // Enable inputs
        if (!inputsEnabled)
        {
            playerInputs.Enable();
            inputsEnabled = true;
        }

        // Initialize input actions
        lookAction = playerInputs.OnFoot.Look;
        pauseAction = playerInputs.OnFoot.Pause;
        aimAction = playerInputs.OnFoot.Aim;

        // Bind aim actions
        if (aimAction != null)
        {
            aimAction.started += ctx => StartAiming();
            aimAction.canceled += ctx => StopAiming();
        }

        if (showDebugInfo)
        {
            //Debug.Log($"Player {(playerController?.playerIndex ?? -1) + 1} inputs initialized - Look Action: {(lookAction != null ? "OK" : "NULL")}");
        }
    }

    private IEnumerator InitializeCursorAfterDelay()
    {
        // Wait for everything to initialize
        yield return new WaitForSeconds(0.5f);

        // Force lock cursor for mouse player
        ForceLockCursor();
        hasInitializedCursor = true;

        if (showDebugInfo)
        {
            //Debug.Log($"Player {playerController.playerIndex + 1} cursor initialized and locked");
        }
    }

    private void ForceLockCursor()
    {
        if (playerController != null && playerController.playerIndex == 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (showDebugInfo)
            {
                //Debug.Log($"Force locked cursor for Player {playerController.playerIndex + 1}");
            }
        }
    }

    private void SetupCinemachine()
    {
        // Setup Cinemachine if not already assigned
        if (virtualCamera == null)
        {
            virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
            if (virtualCamera == null)
            {
                //Debug.LogWarning("No CinemachineVirtualCamera found for this player");
                return;
            }
        }

        // Set up Cinemachine POV component
        cinemachinePOV = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
        if (cinemachinePOV == null)
        {
            cinemachinePOV = virtualCamera.AddCinemachineComponent<CinemachinePOV>();
        }

        // Configure POV component for our needs
        ConfigureCinemachinePOV();

        // Get noise component for camera shake
        cinemachineNoise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (cinemachineNoise == null)
        {
            cinemachineNoise = virtualCamera.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }
    }

    private void ConfigureCinemachinePOV()
    {
        if (cinemachinePOV == null) return;

        // Clear input axis names to prevent Cinemachine from using Input Manager
        cinemachinePOV.m_HorizontalAxis.m_InputAxisName = "";
        cinemachinePOV.m_VerticalAxis.m_InputAxisName = "";

        // Configure the POV component with our settings
        cinemachinePOV.m_VerticalAxis.m_MaxSpeed = mouseSensitivityY;
        cinemachinePOV.m_VerticalAxis.m_MinValue = lowerLookLimit;
        cinemachinePOV.m_VerticalAxis.m_MaxValue = upperLookLimit;
        cinemachinePOV.m_VerticalAxis.m_InvertInput = invertY;
        cinemachinePOV.m_HorizontalAxis.m_MaxSpeed = mouseSensitivityX;
        cinemachinePOV.m_HorizontalAxis.m_Wrap = true;

        // Set up smoothing with matching accel/decel times
        cinemachinePOV.m_VerticalAxis.m_AccelTime = lookSmoothTime;
        cinemachinePOV.m_HorizontalAxis.m_AccelTime = lookSmoothTime;
        cinemachinePOV.m_VerticalAxis.m_DecelTime = lookSmoothTime;
        cinemachinePOV.m_HorizontalAxis.m_DecelTime = lookSmoothTime;
    }

    private void OnEnable()
    {
        // Reinitialize inputs if needed
        if (!inputsEnabled && playerInputs != null)
        {
            playerInputs.Enable();
            inputsEnabled = true;
        }
    }

    private void OnDisable()
    {
        if (playerInputs != null && inputsEnabled)
        {
            if (aimAction != null)
            {
                aimAction.started -= ctx => StartAiming();
                aimAction.canceled -= ctx => StopAiming();
            }
            playerInputs.Disable();
            inputsEnabled = false;
        }
    }

    // Centralized cursor state management
    private void SetCursorState(bool showCursor)
    {
        // Only manage cursor for player 0 (mouse user)
        if (playerController == null || playerController.playerIndex != 0)
            return;

        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (showDebugInfo)
        {
            //Debug.Log($"Player {playerController.playerIndex + 1} cursor state: {(showCursor ? "Visible" : "Locked")}");
        }
    }

    public void UpdateSettings(float mouseX, float mouseY, float gamepadX, float gamepadY, float fov, bool invert)
    {
        mouseSensitivityX = mouseX;
        mouseSensitivityY = mouseY;
        gamepadSensitivityX = gamepadX;
        gamepadSensitivityY = gamepadY;
        regularFOV = fov;
        targetFOV = isAiming ? aimFOV : regularFOV;
        invertY = invert;

        // Update Cinemachine POV if it exists
        ConfigureCinemachinePOV();

        // Update current FOV if not aiming
        if (!isAiming)
        {
            UpdateCinemachineFOV(regularFOV);
        }

        if (showDebugInfo)
        {
            //Debug.Log($"Player {(playerController?.playerIndex ?? -1) + 1} settings updated: " +
                     // $"MouseSens({mouseX},{mouseY}) GamepadSens({gamepadX},{gamepadY}) FOV({fov}) InvertY({invert})");
        }
    }

    private void Update()
    {
        // Handle cursor state for pause transitions
        HandlePauseStateTransitions();

        // Check pause state from PauseManager only
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused())
            return;

        // Ensure inputs are available
        if (lookAction == null)
        {
            if (showDebugInfo)
                //Debug.LogWarning($"Player {(playerController?.playerIndex ?? -1) + 1} - lookAction is null!");
            return;
        }

        // Handle look input
        lookInputDelta = lookAction.ReadValue<Vector2>();

        // Debug info to help troubleshoot
        if (showDebugInfo && playerController != null && lookInputDelta.magnitude > 0.1f)
        {
            ////Debug.Log($"Player {playerController.playerIndex}: Look Delta: {lookInputDelta}, Active Device: {lookAction.activeControl?.device?.name}");
        }

        UpdateLookRotation();

        // Update other systems
        UpdateRecoil();
        UpdateLookPoint();
        UpdateFOV();
        UpdateOrientation();
    }

    private void HandlePauseStateTransitions()
    {
        // Only handle cursor for player 0 (mouse user)
        if (playerController == null || playerController.playerIndex != 0)
            return;

        bool isPaused = PauseManager.Instance != null && PauseManager.Instance.IsPaused();

        // Check for pause state changes
        if (isPaused != wasPaused)
        {
            wasPaused = isPaused;
            SetCursorState(isPaused);

            if (showDebugInfo)
            {
                ////Debug.Log($"Pause state changed for Player {playerController.playerIndex + 1}: {(isPaused ? "Paused" : "Resumed")}");
            }

            // When resuming, ensure cursor is locked
            if (!isPaused && hasInitializedCursor)
            {
                StartCoroutine(EnsureCursorLockedAfterResume());
            }
        }
    }

    private IEnumerator EnsureCursorLockedAfterResume()
    {
        yield return new WaitForEndOfFrame();
        ForceLockCursor();
    }

    private void UpdateLookRotation()
    {
        if (cinemachinePOV == null) return;

        // Skip if no input
        if (lookInputDelta.magnitude < 0.001f) return;

        // Determine if using mouse or gamepad
        bool usingMouse = (lookAction.activeControl?.device is Mouse);

        // Set appropriate sensitivity
        float sensitivityX = usingMouse ? mouseSensitivityX : gamepadSensitivityX;
        float sensitivityY = usingMouse ? mouseSensitivityY : gamepadSensitivityY;

        // Apply aim sensitivity if aiming
        if (isAiming)
        {
            sensitivityX *= aimSensitivityMultiplier;
            sensitivityY *= aimSensitivityMultiplier;
        }

        // Update Cinemachine sensitivities
        cinemachinePOV.m_HorizontalAxis.m_MaxSpeed = sensitivityX;
        cinemachinePOV.m_VerticalAxis.m_MaxSpeed = sensitivityY;

        // Apply input with proper sensitivity scaling
        if (usingMouse)
        {
            // For mouse: use sensitivity with a base scale factor
            float mouseScale = 0.01f;
            cinemachinePOV.m_HorizontalAxis.Value += lookInputDelta.x * sensitivityX * mouseScale * Time.deltaTime;
            cinemachinePOV.m_VerticalAxis.Value += (invertY ? lookInputDelta.y : -lookInputDelta.y) * sensitivityY * mouseScale * Time.deltaTime;
        }
        else
        {
            // For gamepad: use sensitivity with time-based scaling
            float gamepadScale = 0.1f;
            cinemachinePOV.m_HorizontalAxis.Value += lookInputDelta.x * sensitivityX * gamepadScale * Time.deltaTime;
            cinemachinePOV.m_VerticalAxis.Value += (invertY ? lookInputDelta.y : -lookInputDelta.y) * sensitivityY * gamepadScale * Time.deltaTime;
        }

        // Update our tracking variables from Cinemachine
        cameraYaw = cinemachinePOV.m_HorizontalAxis.Value;
        cameraPitch = cinemachinePOV.m_VerticalAxis.Value;
    }

    private void UpdateOrientation()
    {
        if (orientation == null) return;

        // Get camera forward direction for this player
        Camera playerCamera = cameraTransform?.GetComponent<Camera>();
        if (playerCamera == null) return;

        Vector3 cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0;

        if (cameraForward.magnitude > 0.001f)
        {
            cameraForward.Normalize();
            orientation.rotation = Quaternion.LookRotation(cameraForward);
        }
    }

    private void UpdateLookPoint()
    {
        if (lookPoint == null) return;

        // Use this player's camera
        Camera playerCamera = cameraTransform?.GetComponent<Camera>();
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, lookDistance))
        {
            lookPoint.position = hit.point;
        }
        else
        {
            lookPoint.position = ray.origin + ray.direction * lookDistance;
        }
    }

    private void UpdateRecoil()
    {
        if (!enableRecoil || cinemachinePOV == null) return;

        // Smoothly interpolate current recoil towards target recoil
        currentRecoil = Vector2.Lerp(
            currentRecoil,
            targetRecoil,
            Time.deltaTime * recoilSharpness
        );

        // Gradually reduce recoil over time
        targetRecoil = Vector2.Lerp(
            targetRecoil,
            Vector2.zero,
            Time.deltaTime * recoilRecoverySpeed
        );

        // Apply recoil to Cinemachine
        cinemachinePOV.m_HorizontalAxis.Value += currentRecoil.x * Time.deltaTime;
        cinemachinePOV.m_VerticalAxis.Value += currentRecoil.y * Time.deltaTime;

        // Update our tracking variables
        cameraYaw = cinemachinePOV.m_HorizontalAxis.Value;
        cameraPitch = cinemachinePOV.m_VerticalAxis.Value;

        // Add camera shake using Cinemachine noise if available
        if (cinemachineNoise != null && currentRecoil.magnitude > 0.1f)
        {
            cinemachineNoise.m_AmplitudeGain = currentRecoil.magnitude * 0.5f;
            StartCoroutine(FadeOutShake(0.3f));
        }
    }

    private IEnumerator FadeOutShake(float duration)
    {
        if (cinemachineNoise == null) yield break;

        float initialAmplitude = cinemachineNoise.m_AmplitudeGain;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cinemachineNoise.m_AmplitudeGain = Mathf.Lerp(initialAmplitude, 0, elapsed / duration);
            yield return null;
        }

        cinemachineNoise.m_AmplitudeGain = 0f;
    }

    private void UpdateFOV()
    {
        // Smoothly transition FOV
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovTransitionSpeed);

        // Apply FOV based on camera type
        UpdateCinemachineFOV(currentFOV);
    }

    private void UpdateCinemachineFOV(float fov)
    {
        if (virtualCamera != null)
        {
            virtualCamera.m_Lens.FieldOfView = fov;
        }
        else
        {
            // Fallback to direct camera
            Camera playerCamera = cameraTransform?.GetComponent<Camera>();
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = fov;
            }
        }
    }

    private void StartAiming()
    {
        isAiming = true;
        targetFOV = aimFOV;
    }

    private void StopAiming()
    {
        isAiming = false;
        targetFOV = regularFOV;
    }

    // Public methods
    public void AddRecoil(Vector2 recoilAmount, float recoverySpeed)
    {
        if (!enableRecoil) return;

        targetRecoil += recoilAmount;
        targetRecoil = Vector2.ClampMagnitude(targetRecoil, maxRecoilDistance);
        recoilRecoverySpeed = recoverySpeed;
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
        targetFOV = isAiming ? aimFOV : regularFOV;
    }

    public bool IsAiming()
    {
        return isAiming;
    }

    public Transform GetOrientation()
    {
        return orientation;
    }

    public Transform GetLookPoint()
    {
        return lookPoint;
    }

    public float GetCameraYaw()
    {
        return cameraYaw;
    }
}