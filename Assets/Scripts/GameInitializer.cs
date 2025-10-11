using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Cinemachine;

public class GameInitializer : MonoBehaviour
{
    [Header("Player Objects")]
    [SerializeField] GameObject[] playerObjects; // Size 4 - player GameObjects

    [Header("Virtual Cameras")]
    [SerializeField] CinemachineVirtualCamera[] virtualCameras; // Size 4 - virtual cameras

    [Header("HUD Objects")]
    [SerializeField] GameObject[] hudObjects; // Size 4 - HUD GameObjects

    [Header("Pause Menus")]
    [SerializeField] GameObject[] pauseMenus; // Size 4 - pause menu GameObjects

    [Header("Player Count Data")]
    [SerializeField] PlayerCountData playerCountData;

    private Dictionary<int, InputDevice> playerDevices = new Dictionary<int, InputDevice>();
    private int activePlayerCount = 0;

    private void Awake()
    {
        // Get player count from ScriptableObject first, then PlayerPrefs as fallback
        // Force minimum of 1 player to prevent disabling all players
        activePlayerCount = Mathf.Max(1, playerCountData != null ?
                           playerCountData.AmountOfPlayers :
                           PlayerPrefs.GetInt("PlayerCount", 1));

        //Debug.Log($"Initializing game for {activePlayerCount} players");

        LoadPlayerDevices();
        SetupPlayers();
        SetupCameras();
    }

    private void LoadPlayerDevices()
    {
        playerDevices.Clear();

        for (int i = 0; i < activePlayerCount; i++)
        {
            string deviceIdStr = PlayerPrefs.GetString($"Player{i}Device", "");
            string deviceName = PlayerPrefs.GetString($"Player{i}DeviceName", "");

            if (!string.IsNullOrEmpty(deviceIdStr) && int.TryParse(deviceIdStr, out int deviceId))
            {
                InputDevice device = FindInputDeviceById(deviceId);

                if (device != null)
                {
                    playerDevices[i] = device;
                    //Debug.Log($"Loaded device for Player {i + 1}: {device.displayName}");
                }
                else
                {
                    //Debug.LogWarning($"Could not find device with ID {deviceId} for Player {i + 1}");
                    AssignFallbackDevice(i);
                }
            }
            else
            {
                AssignFallbackDevice(i);
            }
        }
    }

    private InputDevice FindInputDeviceById(int deviceId)
    {
        foreach (var device in InputSystem.devices)
        {
            if (device.deviceId == deviceId)
                return device;
        }
        return null;
    }

    private void AssignFallbackDevice(int playerIndex)
    {
        if (playerIndex == 0)
        {
            // Player 1 gets keyboard by default
            if (Keyboard.current != null)
            {
                playerDevices[playerIndex] = Keyboard.current;
                //Debug.Log("Assigned keyboard to Player 1 (fallback)");
                return;
            }
        }

        // Try to find an available gamepad
        var availableGamepads = new List<Gamepad>(Gamepad.all);

        // Remove already assigned gamepads
        foreach (var kvp in playerDevices)
        {
            if (kvp.Value is Gamepad gamepad)
            {
                availableGamepads.Remove(gamepad);
            }
        }

        if (availableGamepads.Count > 0)
        {
            playerDevices[playerIndex] = availableGamepads[0];
            //Debug.Log($"Assigned gamepad to Player {playerIndex + 1} (fallback)");
        }
        else
        {
            //Debug.LogWarning($"No available device for Player {playerIndex + 1}");
        }
    }

    private void SetupPlayers()
    {
        // Activate and setup active players
        for (int i = 0; i < activePlayerCount; i++)
        {
            if (i < playerObjects.Length && playerObjects[i] != null)
            {
                // Activate player object
                playerObjects[i].SetActive(true);

                // Setup PlayerController
                PlayerController controller = playerObjects[i].GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.playerIndex = i;
                    controller.playerName = $"Player {i + 1}";

                    if (playerDevices.TryGetValue(i, out InputDevice device))
                    {
                        controller.assignedDevice = device;
                    }
                }

                // Activate HUD
                if (i < hudObjects.Length && hudObjects[i] != null)
                {
                    hudObjects[i].SetActive(true);
                }

                // Keep pause menu inactive but ensure it exists
                if (i < pauseMenus.Length && pauseMenus[i] != null)
                {
                    pauseMenus[i].SetActive(false);
                }

                //Debug.Log($"Activated Player {i + 1}");
            }
        }

        // Deactivate unused players
        for (int i = activePlayerCount; i < playerObjects.Length; i++)
        {
            if (playerObjects[i] != null)
            {
                playerObjects[i].SetActive(false);
            }

            if (i < hudObjects.Length && hudObjects[i] != null)
            {
                hudObjects[i].SetActive(false);
            }

            if (i < pauseMenus.Length && pauseMenus[i] != null)
            {
                pauseMenus[i].SetActive(false);
            }
        }
    }

    private void SetupCameras()
    {
        // Setup virtual cameras and screen splitting
        for (int i = 0; i < activePlayerCount; i++)
        {
            if (i < virtualCameras.Length && virtualCameras[i] != null)
            {
                // Activate and setup camera
                virtualCameras[i].gameObject.SetActive(true);
                virtualCameras[i].Priority = 10 + i;
            }

            if (i < playerObjects.Length && playerObjects[i] != null)
            {
                PlayerController controller = playerObjects[i].GetComponent<PlayerController>();
                if (controller != null)
                {
                    // Calculate and set camera rect for this player
                    Rect cameraRect = CalculateCameraRect(i, activePlayerCount);
                    controller.SetCameraRect(cameraRect);

                    // Enable audio listener only for first player
                    controller.SetAudioListener(i == 0);
                }
            }
        }

        // Deactivate unused cameras
        for (int i = activePlayerCount; i < virtualCameras.Length; i++)
        {
            if (virtualCameras[i] != null)
            {
                virtualCameras[i].gameObject.SetActive(false);
            }
        }

        //Debug.Log("Camera setup completed");
    }

    private Rect CalculateCameraRect(int playerIndex, int totalPlayers)
    {
        switch (totalPlayers)
        {
            case 1:
                return new Rect(0, 0, 1, 1);

            case 2:
                if (playerIndex == 0)
                    return new Rect(0, 0.5f, 1, 0.5f); // Top half
                else
                    return new Rect(0, 0, 1, 0.5f); // Bottom half

            case 3:
                if (playerIndex == 0)
                    return new Rect(0, 0.5f, 0.5f, 0.5f); // Top left
                else if (playerIndex == 1)
                    return new Rect(0.5f, 0.5f, 0.5f, 0.5f); // Top right
                else
                    return new Rect(0, 0, 1, 0.5f); // Bottom full

            case 4:
                float x = (playerIndex % 2) * 0.5f;
                float y = (playerIndex < 2) ? 0.5f : 0;
                return new Rect(x, y, 0.5f, 0.5f);

            default:
                return new Rect(0, 0, 1, 1);
        }
    }

    public GameObject GetPauseMenu(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < pauseMenus.Length)
        {
            return pauseMenus[playerIndex];
        }
        return null;
    }

    public int GetActivePlayerCount()
    {
        return activePlayerCount;
    }

    // Helper method to get player by index
    public PlayerController GetPlayerController(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerObjects.Length && playerObjects[playerIndex] != null)
        {
            return playerObjects[playerIndex].GetComponent<PlayerController>();
        }
        return null;
    }

    // Helper method to get all active player controllers
    public List<PlayerController> GetActivePlayerControllers()
    {
        List<PlayerController> controllers = new List<PlayerController>();

        for (int i = 0; i < activePlayerCount; i++)
        {
            PlayerController controller = GetPlayerController(i);
            if (controller != null)
            {
                controllers.Add(controller);
            }
        }

        return controllers;
    }
}