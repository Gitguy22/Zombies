using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class EventManager : MonoBehaviour
{
    [Header("Menu References")]
    [SerializeField] GameObject mainMenu;
    [SerializeField] GameObject lobbyMenu;
    [SerializeField] GameObject levelSelectMenu;
    [SerializeField] GameObject settingsMenu;
    [SerializeField] GameObject leaderboardMenu;
    [SerializeField] GameObject creditsMenu;

    [Header("Lobby UI")]
    [SerializeField] GameObject[] playerSlots; // UI panels for each player
    [SerializeField] TextMeshProUGUI[] playerStatusTexts; // "Press any button to join" or "Player X Ready"
    [SerializeField] Image currentLevelImage;
    [SerializeField] TextMeshProUGUI currentLevelName;
    [SerializeField] Button startGameButton;
    [SerializeField] TextMeshProUGUI countdownText;
    [SerializeField] AudioSource countdownAudioSource;
    [SerializeField] AudioClip countdownSound;
    [SerializeField] AudioClip countdownFinalSound;

    [Header("Level Data")]
    [SerializeField] LevelData[] availableLevels;

    private string selectedLevel = "MainMap";
    private int selectedLevelIndex = 0;
    private bool isCountingDown = false;

    // Device tracking
    private List<InputDevice> assignedDevices = new List<InputDevice>();
    private List<InputDevice> availableDevices = new List<InputDevice>();
    private int joinedPlayerCount = 1; // Always have at least 1 player (keyboard/mouse)

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Initialize with default level
            UpdateSelectedLevel(0);

            // Make sure main menu is visible at start
            DisableAllMenus();
            mainMenu.SetActive(true);

            // Subscribe to device changes
            InputSystem.onDeviceChange += OnDeviceChange;

            // Initialize device tracking
            UpdateAvailableDevices();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from device changes
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed ||
            change == InputDeviceChange.Reconnected || change == InputDeviceChange.Disconnected)
        {
            UpdateAvailableDevices();
            if (lobbyMenu.activeSelf)
            {
                UpdatePlayerSlots();
            }
        }
    }

    void UpdateAvailableDevices()
    {
        availableDevices.Clear();

        // Add keyboard/mouse as one device option
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard != null || mouse != null)
        {
            // Use keyboard as the representative for keyboard+mouse
            if (keyboard != null)
                availableDevices.Add(keyboard);
        }

        // Add all connected gamepads
        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad.enabled)
                availableDevices.Add(gamepad);
        }

        Debug.Log($"Available devices: {availableDevices.Count}");
    }

    void Update()
    {
        if (lobbyMenu.activeSelf && !isCountingDown)
        {
            CheckForNewPlayers();
        }
    }

    void CheckForNewPlayers()
    {
        // Check each available device
        for (int i = 0; i < availableDevices.Count && i < 4; i++)
        {
            var device = availableDevices[i];

            // Skip if device is already assigned
            if (assignedDevices.Contains(device))
                continue;

            bool buttonPressed = false;

            // Check for button press based on device type
            if (device is Keyboard keyboard)
            {
                buttonPressed = keyboard.anyKey.wasPressedThisFrame;
            }
            else if (device is Gamepad gamepad)
            {
                buttonPressed = gamepad.buttonSouth.wasPressedThisFrame ||
                               gamepad.buttonNorth.wasPressedThisFrame ||
                               gamepad.buttonEast.wasPressedThisFrame ||
                               gamepad.buttonWest.wasPressedThisFrame ||
                               gamepad.startButton.wasPressedThisFrame;
            }

            if (buttonPressed)
            {
                // Assign this device to a player
                assignedDevices.Add(device);
                joinedPlayerCount = assignedDevices.Count;
                Debug.Log($"Player joined with device: {device.displayName}. Total players: {joinedPlayerCount}");
                UpdatePlayerSlots();
            }
        }
    }

    void DisableAllMenus()
    {
        mainMenu.SetActive(false);
        lobbyMenu.SetActive(false);
        levelSelectMenu.SetActive(false);
        settingsMenu.SetActive(false);
        leaderboardMenu.SetActive(false);
        creditsMenu.SetActive(false);
    }

    // Main Menu Methods
    public void EnableMainMenu()
    {
        DisableAllMenus();
        mainMenu.SetActive(true);
    }

    public void PlayGame()
    {
        DisableMainMenu();
        EnableLobby();
    }

    public void OpenSettings()
    {
        DisableMainMenu();
        settingsMenu.SetActive(true);
    }

    public void OpenLeaderboard()
    {
        DisableMainMenu();
        leaderboardMenu.SetActive(true);
    }

    public void OpenCredits()
    {
        DisableMainMenu();
        creditsMenu.SetActive(true);
    }

    // Lobby Methods
    public void EnableLobby()
    {
        DisableAllMenus();
        lobbyMenu.SetActive(true);

        // Reset player assignments
        assignedDevices.Clear();

        // Always assign the first device (keyboard or first gamepad)
        if (availableDevices.Count > 0)
        {
            assignedDevices.Add(availableDevices[0]);
            joinedPlayerCount = 1;
        }

        UpdatePlayerSlots();
        UpdateLevelDisplay();
    }

    public void DisableLobby()
    {
        lobbyMenu.SetActive(false);
    }

    void UpdatePlayerSlots()
    {
        // Update all 4 player slots
        for (int i = 0; i < 4; i++)
        {
            if (i < joinedPlayerCount)
            {
                // Player has joined
                playerSlots[i].SetActive(true);
                playerStatusTexts[i].text = $"Player {i + 1} Ready";
                playerStatusTexts[i].color = Color.green;
            }
            else if (i < availableDevices.Count)
            {
                // Device available but not joined
                playerSlots[i].SetActive(true);
                playerStatusTexts[i].text = "Press any button to join";
                playerStatusTexts[i].color = Color.yellow;
            }
            else
            {
                // No device available
                playerSlots[i].SetActive(true);
                playerStatusTexts[i].text = "No controller detected";
                playerStatusTexts[i].color = Color.gray;
            }
        }

        // Update start button state
        startGameButton.interactable = joinedPlayerCount > 0 && !isCountingDown;
    }

    public void StartGame()
    {
        if (isCountingDown) return;

        // Save player count and selected level
        PlayerPrefs.SetInt("PlayerCount", joinedPlayerCount);
        PlayerPrefs.SetString("SelectedLevel", selectedLevel);
        PlayerPrefs.Save();

        StartCoroutine(CountdownAndStart());
    }

    IEnumerator CountdownAndStart()
    {
        isCountingDown = true;
        startGameButton.interactable = false;
        countdownText.gameObject.SetActive(true);

        for (int i = 5; i > 0; i--)
        {
            countdownText.text = i.ToString();

            // Play countdown sound
            if (countdownAudioSource != null && countdownSound != null)
            {
                countdownAudioSource.PlayOneShot(i == 1 ? countdownFinalSound : countdownSound);
            }

            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        // Load the selected level
        SceneManager.LoadScene(selectedLevel);
    }

    public void ChangeLevel()
    {
        DisableLobby();
        EnableLevelSelect();
    }

    // Level Select Methods
    public void EnableLevelSelect()
    {
        DisableAllMenus();
        levelSelectMenu.SetActive(true);
    }

    public void SelectLevel(int levelIndex)
    {
        UpdateSelectedLevel(levelIndex);
        DisableLevelSelect();
        EnableLobby();
    }

    void UpdateSelectedLevel(int index)
    {
        selectedLevelIndex = Mathf.Clamp(index, 0, availableLevels.Length - 1);

        if (availableLevels.Length > 0 && selectedLevelIndex < availableLevels.Length)
        {
            var levelData = availableLevels[selectedLevelIndex];
            selectedLevel = levelData.sceneName;
            UpdateLevelDisplay();
        }
    }

    void UpdateLevelDisplay()
    {
        if (availableLevels.Length > 0 && selectedLevelIndex < availableLevels.Length)
        {
            var levelData = availableLevels[selectedLevelIndex];

            if (currentLevelImage != null)
                currentLevelImage.sprite = levelData.levelImage;

            if (currentLevelName != null)
                currentLevelName.text = levelData.levelName;
        }
    }

    // Navigation Methods
    public void BackToMainMenu()
    {
        DisableAllMenus();
        EnableMainMenu();

        // Reset player count
        assignedDevices.Clear();
        joinedPlayerCount = 1;
    }

    public void BackToLobby()
    {
        DisableLevelSelect();
        EnableLobby();
    }

    private void DisableMainMenu()
    {
        mainMenu.SetActive(false);
    }

    private void DisableLevelSelect()
    {
        levelSelectMenu.SetActive(false);
    }
}

// Data structure for level information
[System.Serializable]
public class LevelData
{
    public string levelName = "Level Name";
    public string sceneName = "SceneName";
    public Sprite levelImage;
    public string levelDescription = "Level description";
}