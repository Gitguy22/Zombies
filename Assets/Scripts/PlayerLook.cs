using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class PlayerLook : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivityX = 100f;
    [SerializeField] private float mouseSensitivityY = 100f;
    [SerializeField] private float gamepadSensitivityX = 300f;
    [SerializeField] private float gamepadSensitivityY = 300f;
    [SerializeField] private float lookSmoothTime = 0.03f;
    [SerializeField] private bool invertY = false;

    [Header("Camera Constraints")]
    [SerializeField] private float upperLookLimit = 80f;
    [SerializeField] private float lowerLookLimit = -80f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform orientation;

    [Header("Recoil Settings")]
    [SerializeField] private bool enableRecoil = true;
    [SerializeField] private float maxRecoilDistance = 5f;
    [SerializeField] private float recoilSharpness = 20f;

    [Header("Aiming Settings")]
    [SerializeField] private float aimSensitivityMultiplier = 0.5f;
    private bool isAiming = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    // Input system
    private PlayerInputs playerInputs;
    private InputAction lookAction;
    private InputAction pauseAction;
    private InputAction aimAction;

    // Camera variables
    private float cameraPitch = 0f;
    private float cameraYaw = 0f;
    private Vector2 lookInputDelta;
    private Vector2 currentLookingVelocity;
    private Vector2 currentLookingPosition;
    private Vector2 targetLookingPosition;
    private bool isPaused;

    // Recoil variables
    private Vector2 currentRecoil;
    private Vector2 targetRecoil;
    private float recoilRecoverySpeed;

    //IK lookpoint
    [SerializeField] private Transform lookPoint;
    [SerializeField] private float lookDistance = 100f;

    private void Awake()
    {
        // Auto-find camera if not assigned
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Create orientation object if not assigned
        if (orientation == null)
        {
            GameObject orientationObj = new GameObject("Orientation");
            orientationObj.transform.SetParent(transform);
            orientationObj.transform.localPosition = Vector3.zero;
            orientationObj.transform.localRotation = Quaternion.identity;
            orientation = orientationObj.transform;
        }

        // Initialize inputs
        playerInputs = new PlayerInputs();
        lookAction = playerInputs.OnFoot.Look;
        pauseAction = playerInputs.OnFoot.Pause;
        aimAction = playerInputs.OnFoot.Aim; // Moved to proper location

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (showDebugInfo)
            Debug.Log("FirstPersonController initialized");
    }

    private void OnEnable()
    {
        playerInputs.Enable();
        pauseAction.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        if (playerInputs != null)
        {
            pauseAction.performed -= OnPausePerformed;
            playerInputs.Disable();
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext obj)
    {
        isPaused = !isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }

    private void Update()
    {
        if (isPaused) return;

        HandleLookInput();
        UpdateRecoil();
        UpdateCameraRotation();
        UpdateLookPoint();
    }

    private void UpdateLookPoint()
    {
        if (lookPoint == null || cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, lookDistance))
        {
            lookPoint.position = hit.point; // Move lookPoint to hit location
        }
        else
        {
            lookPoint.position = ray.origin + ray.direction * lookDistance; // Default to max distance
        }
    }

    private void HandleLookInput()
    {
        lookInputDelta = lookAction.ReadValue<Vector2>();

        // Determine input device explicitly
        bool usingMouse = lookAction.activeControl?.device is Mouse;

        float sensitivityX = usingMouse ? mouseSensitivityX : gamepadSensitivityX;
        float sensitivityY = usingMouse ? mouseSensitivityY : gamepadSensitivityY;

        if (isAiming)
        {
            sensitivityX *= aimSensitivityMultiplier;
            sensitivityY *= aimSensitivityMultiplier;
        }

        targetLookingPosition = new Vector2(
            lookInputDelta.x * sensitivityX * Time.deltaTime,
            lookInputDelta.y * sensitivityY * Time.deltaTime
        );

        if (invertY)
            targetLookingPosition.y = -targetLookingPosition.y;

        currentLookingPosition = Vector2.SmoothDamp(
            currentLookingPosition,
            targetLookingPosition,
            ref currentLookingVelocity,
            lookSmoothTime
        );

        if (showDebugInfo)
        {
            Debug.Log($"Look input: X={currentLookingPosition.x:F2}, Y={currentLookingPosition.y:F2}, Device: {(usingMouse ? "Mouse" : "Gamepad")}");
        }
    }


    private void UpdateRecoil()
    {
        if (!enableRecoil) return;

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

        // Debug recoil info
        if (showDebugInfo && (Mathf.Abs(currentRecoil.x) > 0.1f || Mathf.Abs(currentRecoil.y) > 0.1f))
        {
            Debug.Log($"Recoil: X={currentRecoil.x:F2}, Y={currentRecoil.y:F2}");
        }
    }

    private void UpdateCameraRotation()
    {
        if (cameraTransform == null)
        {
            Debug.LogError("Camera transform not assigned!");
            return;
        }

        // Update camera pitch (looking up/down) with added recoil
        cameraPitch -= currentLookingPosition.y;
        cameraPitch += currentRecoil.y; // Apply vertical recoil
        cameraPitch = Mathf.Clamp(cameraPitch, lowerLookLimit, upperLookLimit);

        // Update camera yaw (looking left/right) with added recoil
        cameraYaw += currentLookingPosition.x;
        cameraYaw += currentRecoil.x; // Apply horizontal recoil

        // Apply rotations
        transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f); // Rotate entire player on Y axis
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f); // Rotate camera on X axis
        orientation.rotation = Quaternion.Euler(0f, cameraYaw, 0f); // Update orientation for movement direction
    }

    // Public method to be called from weapon when firing
    public void AddRecoil(Vector2 recoilAmount, float recoverySpeed)
    {
        if (!enableRecoil) return;

        targetRecoil += recoilAmount;

        // Clamp recoil to max distance to prevent extreme values
        targetRecoil = Vector2.ClampMagnitude(targetRecoil, maxRecoilDistance);

        recoilRecoverySpeed = recoverySpeed;
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    public bool IsAiming()
    {
        return isAiming;
    }

    private bool IsUsingMouse()
    {
        //determine if the player is using a mouse or gamepad
        return Mathf.Abs(lookInputDelta.x) > 0.1f || Mathf.Abs(lookInputDelta.y) > 0.1f;
    }
}