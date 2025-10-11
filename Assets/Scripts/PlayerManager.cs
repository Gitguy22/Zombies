using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] PlayerCountData playerCountData;
    [SerializeField] int maxPlayers = 4;

    [Header("Cursor Settings")]
    [SerializeField] GameObject playerCursorPrefab;
    [SerializeField] Canvas cursorCanvas;
    [SerializeField]
    Color[] playerColors = new Color[] {
        new Color(1, 0.3f, 0.3f),  // Red
        new Color(0.3f, 0.5f, 1),  // Blue  
        new Color(0.3f, 1, 0.5f),  // Green
        new Color(1, 0.9f, 0.2f)   // Yellow
    };

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip playerJoinSound;
    [SerializeField] AudioClip playerLeaveSound;

    [Header("UI References")]
    [SerializeField] Text playerCountText; // Optional: to display current player count

    // Internal state
    private List<InputDevice> assignedDevices = new List<InputDevice>();
    private List<GameObject> playerCursors = new List<GameObject>();
    private bool isAcceptingPlayers = false;

    // Input tracking
    private HashSet<InputDevice> devicesPressed = new HashSet<InputDevice>();

    private void Awake()
    {
        if (playerCountData != null)
        {
            playerCountData.MaxPlayers = maxPlayers;
            playerCountData.Reset();
        }

        // Clear any existing device assignments
        ClearPlayerPrefs();
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Update()
    {
        if (!isAcceptingPlayers) return;

        CheckForJoinInputs();
        CheckForLeaveInputs();
    }

    private void CheckForJoinInputs()
    {
        // Check all input devices for join input
        foreach (var device in InputSystem.devices)
        {
            if (device is Keyboard keyboard)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    TryAddPlayer(device);
                }
            }
            else if (device is Gamepad gamepad)
            {
                if (gamepad.buttonSouth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame)
                {
                    TryAddPlayer(device);
                }
            }
        }
    }

    private void CheckForLeaveInputs()
    {
        // Check assigned devices for leave input
        for (int i = assignedDevices.Count - 1; i >= 0; i--)
        {
            var device = assignedDevices[i];

            if (device is Keyboard keyboard)
            {
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)
                {
                    RemovePlayer(i);
                }
            }
            else if (device is Gamepad gamepad)
            {
                if (gamepad.buttonEast.wasPressedThisFrame || gamepad.selectButton.wasPressedThisFrame)
                {
                    RemovePlayer(i);
                }
            }
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed)
        {
            int deviceIndex = assignedDevices.IndexOf(device);
            if (deviceIndex >= 0)
            {
                RemovePlayer(deviceIndex);
            }
        }
    }

    public void EnablePlayerJoining()
    {
        isAcceptingPlayers = true;
        ClearAllPlayers();
        Cursor.visible = true;
        Debug.Log("Player joining enabled");
    }

    public void DisablePlayerJoining()
    {
        isAcceptingPlayers = false;
        Debug.Log("Player joining disabled");
    }

    private void TryAddPlayer(InputDevice device)
    {
        // Check if device already assigned
        if (assignedDevices.Contains(device)) return;

        // Check if max players reached
        if (assignedDevices.Count >= maxPlayers) return;

        AddPlayer(device);
    }

    private void AddPlayer(InputDevice device)
    {
        // Add device
        assignedDevices.Add(device);
        int playerIndex = assignedDevices.Count - 1;

        // Create cursor
        CreatePlayerCursor(playerIndex, device);

        // Update player count data
        if (playerCountData != null)
        {
            playerCountData.SetAmountOfPlayers(assignedDevices.Count);
        }

        // Update UI
        UpdatePlayerCountDisplay();

        // Play sound
        if (audioSource != null && playerJoinSound != null)
        {
            audioSource.PlayOneShot(playerJoinSound);
        }

        // Save device assignment
        SaveDeviceAssignment(playerIndex, device);

        Debug.Log($"Player {playerIndex + 1} joined with device: {device.displayName}");
    }

    private void RemovePlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= assignedDevices.Count) return;

        // Remove cursor
        if (playerIndex < playerCursors.Count && playerCursors[playerIndex] != null)
        {
            Destroy(playerCursors[playerIndex]);
        }

        // Remove device
        assignedDevices.RemoveAt(playerIndex);

        // Update player count data
        if (playerCountData != null)
        {
            playerCountData.SetAmountOfPlayers(assignedDevices.Count);
        }

        // Update UI
        UpdatePlayerCountDisplay();

        // Play sound
        if (audioSource != null && playerLeaveSound != null)
        {
            audioSource.PlayOneShot(playerLeaveSound);
        }

        // Reassign remaining players
        ReassignPlayerCursors();

        // Clear and re-save all device assignments
        ClearPlayerPrefs();
        SaveAllDeviceAssignments();

        Debug.Log($"Player {playerIndex + 1} left");
    }

    private void ClearAllPlayers()
    {
        assignedDevices.Clear();

        foreach (var cursor in playerCursors)
        {
            if (cursor != null) Destroy(cursor);
        }
        playerCursors.Clear();

        if (playerCountData != null)
        {
            playerCountData.SetAmountOfPlayers(0);
        }

        UpdatePlayerCountDisplay();
        ClearPlayerPrefs();
    }

    private void CreatePlayerCursor(int playerIndex, InputDevice device)
    {
        if (playerCursorPrefab == null || cursorCanvas == null) return;

        GameObject cursorObject = Instantiate(playerCursorPrefab, cursorCanvas.transform);
        PlayerCursor cursor = cursorObject.GetComponent<PlayerCursor>();

        if (cursor != null)
        {
            Color playerColor = playerIndex < playerColors.Length ?
                               playerColors[playerIndex] : Color.white;

            cursor.Initialize(playerIndex, playerColor, device);

            while (playerCursors.Count <= playerIndex)
            {
                playerCursors.Add(null);
            }
            playerCursors[playerIndex] = cursorObject;
        }
    }

    private void ReassignPlayerCursors()
    {
        foreach (var cursor in playerCursors)
        {
            if (cursor != null) Destroy(cursor);
        }
        playerCursors.Clear();

        for (int i = 0; i < assignedDevices.Count; i++)
        {
            CreatePlayerCursor(i, assignedDevices[i]);
        }
    }

    private void UpdatePlayerCountDisplay()
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Players: {assignedDevices.Count}/{maxPlayers}";
        }
    }

    private void SaveDeviceAssignment(int playerIndex, InputDevice device)
    {
        PlayerPrefs.SetString($"Player{playerIndex}Device", device.deviceId.ToString());
        PlayerPrefs.SetString($"Player{playerIndex}DeviceName", device.displayName);
        PlayerPrefs.SetInt("PlayerCount", assignedDevices.Count);
        PlayerPrefs.Save();
    }

    private void SaveAllDeviceAssignments()
    {
        for (int i = 0; i < assignedDevices.Count; i++)
        {
            SaveDeviceAssignment(i, assignedDevices[i]);
        }
        PlayerPrefs.SetInt("PlayerCount", assignedDevices.Count);
        PlayerPrefs.Save();
    }

    private void ClearPlayerPrefs()
    {
        for (int i = 0; i < 4; i++)
        {
            PlayerPrefs.DeleteKey($"Player{i}Device");
            PlayerPrefs.DeleteKey($"Player{i}DeviceName");
        }
        PlayerPrefs.SetInt("PlayerCount", 0);
        PlayerPrefs.Save();
    }

    public int GetPlayerCount()
    {
        return assignedDevices.Count;
    }

    public void StartGame()
    {
        SaveAllDeviceAssignments();
        DisablePlayerJoining();
    }
}