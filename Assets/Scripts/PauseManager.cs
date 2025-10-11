using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Manages pause state for individual players in a multiplayer game.
/// Attach to the Player GameObject (same level as PlayerInput).
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] Canvas playerCanvas;
    [SerializeField] GameObject pauseMenuUI;
    [SerializeField] PlayerUIPanel playerUIPanel;

    [Header("Debug")]
    [SerializeField] bool showDebugLogs = true;

    private bool wantsToPause = false;
    private PlayerInput playerInput;
    private InputAction pauseAction;

    // Static tracking for global pause state
    private static bool isGloballyPaused = false;
    private static int pausedPlayerCount = 0;

    void Awake()
    {
        // Simple instance assignment (not singleton since each player has one)
        Instance = this;

        // Get PlayerInput from same GameObject
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            //Debug.LogError($"PauseManager: No PlayerInput found on {gameObject.name}!");
            enabled = false;
            return;
        }

        // Auto-find Canvas in children
        if (playerCanvas == null)
            playerCanvas = GetComponentInChildren<Canvas>();

        // Auto-find PauseMenu in Canvas
        if (pauseMenuUI == null && playerCanvas != null)
        {
            Transform pauseMenuTransform = playerCanvas.transform.Find("PauseMenu");
            if (pauseMenuTransform != null)
                pauseMenuUI = pauseMenuTransform.gameObject;
        }

        // Auto-find PlayerUIPanel in PauseMenu
        if (playerUIPanel == null && pauseMenuUI != null)
            playerUIPanel = pauseMenuUI.GetComponent<PlayerUIPanel>();

        // Setup pause input
        pauseAction = playerInput.actions["Pause"];
        if (pauseAction != null)
        {
            pauseAction.performed += OnPausePressed;
        }
        else
        {
            Debug.LogError($"PauseManager: No 'Pause' action found for Player {playerInput.playerIndex + 1}!");
        }
    }

    void Start()
    {
        // Initialize UI panel
        if (playerUIPanel != null)
        {
            playerUIPanel.Initialize(playerInput, playerInput.playerIndex);
        }
        else
        {
            Debug.LogError($"PauseManager: No PlayerUIPanel found for Player {playerInput.playerIndex + 1}!");
        }

        // Hide pause menu initially
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        // Verify setup
        VerifySetup();
    }

    void VerifySetup()
    {
        if (playerCanvas != null)
        {
            var eventSystem = playerCanvas.GetComponent<MultiplayerEventSystem>();
            if (eventSystem != null)
            {
                // Ensure playerRoot is set to canvas
                if (eventSystem.playerRoot == null)
                {
                    eventSystem.playerRoot = playerCanvas.gameObject;
                    if (showDebugLogs)
                        Debug.Log($"Player {playerInput.playerIndex + 1}: Set playerRoot to Canvas");
                }
            }
            else
            {
                Debug.LogError($"Player {playerInput.playerIndex + 1}: Missing MultiplayerEventSystem on Canvas!");
            }

            var inputModule = playerCanvas.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                Debug.LogError($"Player {playerInput.playerIndex + 1}: Missing InputSystemUIInputModule on Canvas!");
            }
        }
        else
        {
            Debug.LogError($"Player {playerInput.playerIndex + 1}: No Canvas found!");
        }
    }

    void OnPausePressed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        wantsToPause = !wantsToPause;

        if (wantsToPause)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }

        UpdateGlobalPauseState();
    }

    public void PauseGame()
    {
        // Switch to UI action map (PlayerInput auto-configures InputSystemUIInputModule)
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("UI");
        }

        // Show pause menu
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }

        // Notify UI panel
        if (playerUIPanel != null)
        {
            playerUIPanel.OnMenuShown();
        }

        pausedPlayerCount++;

        if (showDebugLogs)
            Debug.Log($"Player {playerInput.playerIndex + 1} paused (Total paused: {pausedPlayerCount})");
    }

    public void ResumeGame()
    {
        // Switch back to OnFoot action map
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("OnFoot");
        }

        // Hide pause menu
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        // Notify UI panel
        if (playerUIPanel != null)
        {
            playerUIPanel.OnMenuHidden();
        }

        pausedPlayerCount = Mathf.Max(0, pausedPlayerCount - 1);

        if (showDebugLogs)
            Debug.Log($"Player {playerInput.playerIndex + 1} resumed (Total paused: {pausedPlayerCount})");
    }

    void UpdateGlobalPauseState()
    {
        bool shouldBePaused = pausedPlayerCount > 0;

        if (shouldBePaused != isGloballyPaused)
        {
            isGloballyPaused = shouldBePaused;
            Time.timeScale = isGloballyPaused ? 0f : 1f;

            // Handle cursor (primarily for mouse/keyboard players)
            if (isGloballyPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (showDebugLogs)
                Debug.Log($"Global pause state changed: {isGloballyPaused}");
        }
    }

    // Public methods for UI callbacks
    public void ForceResume()
    {
        if (wantsToPause)
        {
            wantsToPause = false;
            ResumeGame();
            UpdateGlobalPauseState();
        }
    }

    // Getters
    public bool IsPaused() => wantsToPause;
    public static bool IsGloballyPaused() => isGloballyPaused;
    public int GetPlayerIndex() => playerInput?.playerIndex ?? -1;

    void OnDestroy()
    {
        // Clean up pause state
        if (wantsToPause)
        {
            pausedPlayerCount = Mathf.Max(0, pausedPlayerCount - 1);
            UpdateGlobalPauseState();
        }

        // Unsubscribe from input
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePressed;
        }
    }
}