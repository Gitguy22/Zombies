using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    public string playerName = "Player";
    public int playerIndex = 0;

    [Header("Input Settings")]
    public InputDevice assignedDevice;

    [Header("Camera Settings")]
    public Camera playerCamera;
    public CinemachineVirtualCamera virtualCamera;

    [Header("Spawn Settings")]
    public Transform spawnPoint;

    private AudioListener audioListener;
    private PlayerInputs playerInputs;
    private CinemachineBrain cinemachineBrain;

    void Awake()
    {
        // Find camera components
        playerCamera = GetComponentInChildren<Camera>();
        virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();

        // Each camera should have its own brain
        if (playerCamera != null)
        {
            cinemachineBrain = playerCamera.GetComponent<CinemachineBrain>();
        }

        audioListener = GetComponentInChildren<AudioListener>();

        // Set player name based on index
        playerName = $"Player {playerIndex + 1}";

        // Create player inputs
        playerInputs = new PlayerInputs();
    }

    void Start()
    {
        RegisterWithManagers();
        ConfigureCinemachine();
    }

    void OnEnable()
    {
        if (playerInputs != null)
            playerInputs.Enable();
    }

    void OnDisable()
    {
        if (playerInputs != null)
            playerInputs.Disable();
    }

    void ConfigureCinemachine()
    {
        // Configure this player's virtual camera
        if (virtualCamera != null)
        {
            virtualCamera.Priority = 10 + playerIndex;
        }

        // Configure this player's camera brain
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
            cinemachineBrain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.LateUpdate;
        }
    }

    void RegisterWithManagers()
    {
        // Register with PointManager (your existing system)
        PointManager pointManager = FindObjectOfType<PointManager>();
        if (pointManager != null)
        {
            pointManager.RegisterPlayer(gameObject);
            //Debug.Log($"Registered {playerName} with PointManager");
        }

        // Connect weapon manager if exists
        WeaponManager weaponManager = GetComponent<WeaponManager>();
        if (weaponManager != null)
        {
            //Debug.Log($"Connected {playerName} to WeaponManager");
        }

        // Connect health system if exists
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            //Debug.Log($"Connected {playerName} to PlayerHealth");
        }
    }

    void OnDestroy()
    {
        //Debug.Log($"Player {playerName} destroyed");
    }

    public void RespawnPlayer()
    {
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        // Reset health through existing health system
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            PlayerHealthReference healthRef = GetComponent<PlayerHealthReference>();
            if (healthRef != null && healthRef.HealthData != null)
            {
                healthRef.HealthData.ResetHealth();
            }
        }
    }

    // Method for GameInitializer to set up this player's camera rect
    public void SetCameraRect(Rect cameraRect)
    {
        if (playerCamera != null)
        {
            playerCamera.rect = cameraRect;
            //Debug.Log($"Player {playerIndex + 1} camera rect set to: {cameraRect}");
        }
    }

    // Method for GameInitializer to set up audio listener
    public void SetAudioListener(bool enabled)
    {
        if (audioListener != null)
        {
            audioListener.enabled = enabled;
        }
    }

    // Public getters
    public PlayerInputs GetPlayerInputs()
    {
        return playerInputs;
    }

    public bool IsKeyboardPlayer()
    {
        return playerIndex == 0 || assignedDevice == null || assignedDevice is Keyboard;
    }

    public Camera GetPlayerCamera()
    {
        return playerCamera;
    }

    public CinemachineVirtualCamera GetVirtualCamera()
    {
        return virtualCamera;
    }
}