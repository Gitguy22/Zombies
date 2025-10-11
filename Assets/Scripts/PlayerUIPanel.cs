using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Manages individual player settings UI panel with slider acceleration.
/// Attach to the PauseMenu GameObject under the player's Canvas.
/// </summary>
public class PlayerUIPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TextMeshProUGUI playerLabel;
    [SerializeField] Button firstSelectedButton;

    [Header("Audio Settings (Shared)")]
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;

    [Header("Player-Specific Settings")]
    [SerializeField] Slider mouseSensitivityXSlider;
    [SerializeField] Slider mouseSensitivityYSlider;
    [SerializeField] Slider gamepadSensitivityXSlider;
    [SerializeField] Slider gamepadSensitivityYSlider;
    [SerializeField] Slider fovSlider;
    [SerializeField] Toggle invertYToggle;

    [Header("Value Displays")]
    [SerializeField] TextMeshProUGUI mouseSensXText;
    [SerializeField] TextMeshProUGUI mouseSensYText;
    [SerializeField] TextMeshProUGUI gamepadSensXText;
    [SerializeField] TextMeshProUGUI gamepadSensYText;
    [SerializeField] TextMeshProUGUI fovText;

    [Header("Buttons")]
    [SerializeField] Button resumeButton;
    [SerializeField] Button quitButton;
    [SerializeField] Button resetButton;

    [Header("Slider Ranges")]
    [SerializeField] Vector2 sensitivityRange = new Vector2(0f, 2000f);
    [SerializeField] Vector2 fovRange = new Vector2(40f, 120f);

    [Header("Slider Acceleration")]
    [Range(0f, 1f)]
    [SerializeField] float sliderSpeed = 0.8f; // Higher = faster acceleration

    private int playerIndex;
    private PlayerInput playerInput;
    private PlayerLook targetPlayerLook;
    private PauseManager pauseManager;
    private MultiplayerEventSystem eventSystem;
    private PlayerUINavigator uiNavigator;

    // Slider acceleration tracking
    private float sliderTimeOfChanged;
    private float sliderOldValue;
    private int sliderSetCount = 0;
    private int sliderCurrentSpeed = 1;
    private Slider lastChangedSlider;

    void Awake()
    {
        SetupSliderRanges();
        SetDefaultSliderValues();
    }

    void Start()
    {
        SetDefaultSliderValues();
    }

    void SetupSliderRanges()
    {
        // Audio sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
        }

        // Sensitivity sliders
        if (mouseSensitivityXSlider != null)
        {
            mouseSensitivityXSlider.minValue = sensitivityRange.x;
            mouseSensitivityXSlider.maxValue = sensitivityRange.y;
            mouseSensitivityXSlider.wholeNumbers = true;
        }
        if (mouseSensitivityYSlider != null)
        {
            mouseSensitivityYSlider.minValue = sensitivityRange.x;
            mouseSensitivityYSlider.maxValue = sensitivityRange.y;
            mouseSensitivityYSlider.wholeNumbers = true;
        }
        if (gamepadSensitivityXSlider != null)
        {
            gamepadSensitivityXSlider.minValue = sensitivityRange.x;
            gamepadSensitivityXSlider.maxValue = sensitivityRange.y;
            gamepadSensitivityXSlider.wholeNumbers = true;
        }
        if (gamepadSensitivityYSlider != null)
        {
            gamepadSensitivityYSlider.minValue = sensitivityRange.x;
            gamepadSensitivityYSlider.maxValue = sensitivityRange.y;
            gamepadSensitivityYSlider.wholeNumbers = true;
        }

        // FOV slider
        if (fovSlider != null)
        {
            fovSlider.minValue = fovRange.x;
            fovSlider.maxValue = fovRange.y;
            fovSlider.wholeNumbers = true;
        }
    }

    void SetDefaultSliderValues()
    {
        // Mouse sensitivity default: 750
        if (mouseSensitivityXSlider != null)
        {
            mouseSensitivityXSlider.value = 750f;
            if (mouseSensXText != null) mouseSensXText.text = "750";
        }
        if (mouseSensitivityYSlider != null)
        {
            mouseSensitivityYSlider.value = 750f;
            if (mouseSensYText != null) mouseSensYText.text = "750";
        }

        // Gamepad sensitivity default: 1200
        if (gamepadSensitivityXSlider != null)
        {
            gamepadSensitivityXSlider.value = 1200f;
            if (gamepadSensXText != null) gamepadSensXText.text = "1200";
        }
        if (gamepadSensitivityYSlider != null)
        {
            gamepadSensitivityYSlider.value = 1200f;
            if (gamepadSensYText != null) gamepadSensYText.text = "1200";
        }

        // Set default FOV
        if (fovSlider != null)
        {
            fovSlider.value = 90f;
            if (fovText != null) fovText.text = "90";
        }

        // Set default audio values
        if (masterVolumeSlider != null) masterVolumeSlider.value = 0.75f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0.75f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 0.75f;

        // Set default invert Y
        if (invertYToggle != null) invertYToggle.isOn = false;
    }

    /// <summary>
    /// Handles slider acceleration for rapid input changes
    /// </summary>
    void UpdateSliderStepSize(Slider slider)
    {
        if (lastChangedSlider == slider && Time.unscaledTime - sliderTimeOfChanged < 0.15f)
        {
            sliderSetCount++;
            float countForSpeedup = (1.1f - sliderSpeed) * 10;

            if (sliderSetCount > countForSpeedup)
            {
                float stepSize = slider.wholeNumbers ? 1 : (slider.maxValue - slider.minValue) * 0.1f;

                if (sliderOldValue < slider.value)
                    slider.SetValueWithoutNotify(slider.value + stepSize * sliderCurrentSpeed);
                else
                    slider.SetValueWithoutNotify(slider.value - stepSize * sliderCurrentSpeed);

                if (sliderSetCount > countForSpeedup + countForSpeedup * sliderCurrentSpeed * (1.1f - sliderSpeed))
                {
                    sliderCurrentSpeed++;
                }
            }
            else
            {
                sliderOldValue = slider.value;
            }
        }
        else
        {
            lastChangedSlider = slider;
            sliderSetCount = 0;
            sliderCurrentSpeed = 1;
        }

        sliderTimeOfChanged = Time.unscaledTime;
    }

    public void Initialize(PlayerInput input, int index)
    {
        playerInput = input;
        playerIndex = index;

        // Get components from player hierarchy
        pauseManager = playerInput.GetComponent<PauseManager>();
        targetPlayerLook = playerInput.GetComponentInChildren<PlayerLook>();

        // Get event system from canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            eventSystem = canvas.GetComponent<MultiplayerEventSystem>();
        }

        if (playerLabel != null)
            playerLabel.text = $"Player {playerIndex + 1} Settings";

        // Setup UI Navigator
        uiNavigator = GetComponent<PlayerUINavigator>();
        if (uiNavigator == null)
            uiNavigator = gameObject.AddComponent<PlayerUINavigator>();

        uiNavigator.Initialize(playerInput, eventSystem, firstSelectedButton?.gameObject);

        SetupButtonCallbacks();
        SetupSliderListeners();

        // Load saved settings AFTER setting defaults
        LoadPlayerSettings();

        //Debug.Log($"PlayerUIPanel initialized for Player {playerIndex + 1}");
    }

    public void OnMenuShown()
    {
        if (eventSystem != null && firstSelectedButton != null)
        {
            eventSystem.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
    }

    public void OnMenuHidden()
    {
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    void SetupButtonCallbacks()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => pauseManager?.ForceResume());
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetToDefaults);
    }

    void OnQuitClicked()
    {
        pauseManager?.ForceResume();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    void SetupSliderListeners()
    {
        // Audio sliders (shared) - with acceleration
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            masterVolumeSlider.onValueChanged.AddListener(_ => UpdateSliderStepSize(masterVolumeSlider));
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            sfxVolumeSlider.onValueChanged.AddListener(_ => UpdateSliderStepSize(sfxVolumeSlider));
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            musicVolumeSlider.onValueChanged.AddListener(_ => UpdateSliderStepSize(musicVolumeSlider));
        }

        // Player-specific sliders - with acceleration
        if (mouseSensitivityXSlider != null)
        {
            mouseSensitivityXSlider.onValueChanged.AddListener(SetMouseSensitivityX);
            mouseSensitivityXSlider.onValueChanged.AddListener(_ => UpdateSliderStepSize(mouseSensitivityXSlider));
        }
        if (mouseSensitivityYSlider != null)
        {
            mouseSensitivityYSlider.onValueChanged.AddListener(SetMouseSensitivityY);
            mouseSensitivityYSlider.onValueChanged.AddListener(_ => UpdateSliderStepSize(mouseSensitivityYSlider));
        }
        if (gamepadSensitivityXSlider != null)
        {
            gamepadSensitivityXSlider.onValueChanged.AddListener(SetGamepadSensitivityX);
            gamepadSensitivityXSlider.onValueChanged.AddListener(_ => UpdateSliderStepSize(gamepadSensitivityXSlider));
        }
        if (gamepadSensitivityYSlider != null)
        {
            gamepadSensitivityYSlider.onValueChanged.AddListener(SetGamepadSensitivityY);
            gamepadSensitivityYSlider.onValueChanged.AddListener(_ => UpdateSliderStepSize(gamepadSensitivityYSlider));
        }
        if (fovSlider != null)
        {
            fovSlider.onValueChanged.AddListener(SetFOV);
            fovSlider.onValueChanged.AddListener(_ => UpdateSliderStepSize(fovSlider));
        }
        if (invertYToggle != null)
            invertYToggle.onValueChanged.AddListener(SetInvertY);
    }

    void LoadPlayerSettings()
    {
        // Load shared audio settings (only player 0 loads to prevent conflicts)
        if (playerIndex == 0)
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        }

        // Load player-specific settings
        string playerPrefix = $"Player{playerIndex}_";

        if (mouseSensitivityXSlider != null)
        {
            float value = PlayerPrefs.GetFloat(playerPrefix + "MouseSensX", 750f);
            mouseSensitivityXSlider.value = value;
            if (mouseSensXText != null) mouseSensXText.text = value.ToString("F0");
        }

        if (mouseSensitivityYSlider != null)
        {
            float value = PlayerPrefs.GetFloat(playerPrefix + "MouseSensY", 750f);
            mouseSensitivityYSlider.value = value;
            if (mouseSensYText != null) mouseSensYText.text = value.ToString("F0");
        }

        if (gamepadSensitivityXSlider != null)
        {
            float value = PlayerPrefs.GetFloat(playerPrefix + "GamepadSensX", 1200f);
            gamepadSensitivityXSlider.value = value;
            if (gamepadSensXText != null) gamepadSensXText.text = value.ToString("F0");
        }

        if (gamepadSensitivityYSlider != null)
        {
            float value = PlayerPrefs.GetFloat(playerPrefix + "GamepadSensY", 1200f);
            gamepadSensitivityYSlider.value = value;
            if (gamepadSensYText != null) gamepadSensYText.text = value.ToString("F0");
        }

        if (fovSlider != null)
        {
            float value = PlayerPrefs.GetFloat(playerPrefix + "FOV", 90f);
            fovSlider.value = value;
            if (fovText != null) fovText.text = value.ToString("F0");
        }

        if (invertYToggle != null)
            invertYToggle.isOn = PlayerPrefs.GetInt(playerPrefix + "InvertY", 0) == 1;

        ApplySettings();
    }

    // Audio Methods (Shared Settings)
    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("MasterVolume", volume);
        SyncAudioToOtherPlayers("master", volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("SFXVolume", volume);
        SyncAudioToOtherPlayers("sfx", volume);
    }

    public void SetMusicVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("MusicVolume", volume);
        SyncAudioToOtherPlayers("music", volume);
    }

    void SyncAudioToOtherPlayers(string audioType, float value)
    {
        PlayerUIPanel[] allPanels = FindObjectsOfType<PlayerUIPanel>();
        foreach (PlayerUIPanel panel in allPanels)
        {
            if (panel != this && panel.playerIndex != playerIndex)
            {
                switch (audioType)
                {
                    case "master":
                        if (panel.masterVolumeSlider != null)
                            panel.masterVolumeSlider.SetValueWithoutNotify(value);
                        break;
                    case "sfx":
                        if (panel.sfxVolumeSlider != null)
                            panel.sfxVolumeSlider.SetValueWithoutNotify(value);
                        break;
                    case "music":
                        if (panel.musicVolumeSlider != null)
                            panel.musicVolumeSlider.SetValueWithoutNotify(value);
                        break;
                }
            }
        }
    }

    // Player-Specific Settings Methods
    public void SetMouseSensitivityX(float sensitivity)
    {
        string key = $"Player{playerIndex}_MouseSensX";
        PlayerPrefs.SetFloat(key, sensitivity);
        if (mouseSensXText != null) mouseSensXText.text = sensitivity.ToString("F0");
        ApplySettings();
    }

    public void SetMouseSensitivityY(float sensitivity)
    {
        string key = $"Player{playerIndex}_MouseSensY";
        PlayerPrefs.SetFloat(key, sensitivity);
        if (mouseSensYText != null) mouseSensYText.text = sensitivity.ToString("F0");
        ApplySettings();
    }

    public void SetGamepadSensitivityX(float sensitivity)
    {
        string key = $"Player{playerIndex}_GamepadSensX";
        PlayerPrefs.SetFloat(key, sensitivity);
        if (gamepadSensXText != null) gamepadSensXText.text = sensitivity.ToString("F0");
        ApplySettings();
    }

    public void SetGamepadSensitivityY(float sensitivity)
    {
        string key = $"Player{playerIndex}_GamepadSensY";
        PlayerPrefs.SetFloat(key, sensitivity);
        if (gamepadSensYText != null) gamepadSensYText.text = sensitivity.ToString("F0");
        ApplySettings();
    }

    public void SetFOV(float fov)
    {
        string key = $"Player{playerIndex}_FOV";
        PlayerPrefs.SetFloat(key, fov);
        if (fovText != null) fovText.text = fov.ToString("F0");
        ApplySettings();
    }

    public void SetInvertY(bool invert)
    {
        string key = $"Player{playerIndex}_InvertY";
        PlayerPrefs.SetInt(key, invert ? 1 : 0);
        ApplySettings();
    }

    void ApplySettings()
    {
        if (targetPlayerLook == null) return;

        string playerPrefix = $"Player{playerIndex}_";

        // Apply settings with correct defaults
        float mouseSensX = PlayerPrefs.GetFloat(playerPrefix + "MouseSensX", 750f);
        float mouseSensY = PlayerPrefs.GetFloat(playerPrefix + "MouseSensY", 750f);
        float gamepadSensX = PlayerPrefs.GetFloat(playerPrefix + "GamepadSensX", 1200f);
        float gamepadSensY = PlayerPrefs.GetFloat(playerPrefix + "GamepadSensY", 1200f);
        float fov = PlayerPrefs.GetFloat(playerPrefix + "FOV", 90f);
        bool invertY = PlayerPrefs.GetInt(playerPrefix + "InvertY", 0) == 1;

        targetPlayerLook.UpdateSettings(mouseSensX, mouseSensY, gamepadSensX, gamepadSensY, fov, invertY);
    }

    public void ResetToDefaults()
    {
        // Reset mouse sensitivity to 750
        if (mouseSensitivityXSlider != null)
        {
            mouseSensitivityXSlider.value = 750f;
            if (mouseSensXText != null) mouseSensXText.text = "750";
        }
        if (mouseSensitivityYSlider != null)
        {
            mouseSensitivityYSlider.value = 750f;
            if (mouseSensYText != null) mouseSensYText.text = "750";
        }

        // Reset gamepad sensitivity to 1200
        if (gamepadSensitivityXSlider != null)
        {
            gamepadSensitivityXSlider.value = 1200f;
            if (gamepadSensXText != null) gamepadSensXText.text = "1200";
        }
        if (gamepadSensitivityYSlider != null)
        {
            gamepadSensitivityYSlider.value = 1200f;
            if (gamepadSensYText != null) gamepadSensYText.text = "1200";
        }

        if (fovSlider != null)
        {
            fovSlider.value = 90f;
            if (fovText != null) fovText.text = "90";
        }
        if (invertYToggle != null) invertYToggle.isOn = false;

        // Reset shared audio settings (only player 0)
        if (playerIndex == 0)
        {
            if (masterVolumeSlider != null) masterVolumeSlider.value = 0.75f;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0.75f;
            if (musicVolumeSlider != null) musicVolumeSlider.value = 0.75f;
        }
    }
}