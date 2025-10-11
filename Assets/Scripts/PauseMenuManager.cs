using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Menu References")]
    [SerializeField] GameObject mainPausePanel;
    [SerializeField] GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] Button resumeButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button mainMenuButton;
    [SerializeField] Button backButton;

    [Header("Sensitivity Settings")]
    [SerializeField] Slider mouseSensitivitySlider;
    [SerializeField] Slider gamepadSensitivitySlider;
    [SerializeField] TextMeshProUGUI mouseSensitivityText;
    [SerializeField] TextMeshProUGUI gamepadSensitivityText;

    [Header("Audio Settings")]
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] TextMeshProUGUI volumeText;

    [Header("Other Settings")]
    [SerializeField] Toggle invertYToggle;

    // References to player systems
    private PlayerLook[] playerLooks;

    private void Start()
    {

        // Find all player look components in the scene
        playerLooks = FindObjectsOfType<PlayerLook>();

        // Setup initial slider values
        LoadSettings();

        // Make sure we start with the main pause panel active
        if (mainPausePanel != null && settingsPanel != null)
        {
            mainPausePanel.SetActive(true);
            settingsPanel.SetActive(false);
        }
    }

    private void LoadSettings()
    {
        // Load saved sensitivity settings or use defaults
        float mouseSens = PlayerPrefs.GetFloat("MouseSensitivity", 300f);
        float gamepadSens = PlayerPrefs.GetFloat("GamepadSensitivity", 300f);
        float volume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        bool invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;

        // Update UI
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = mouseSens;
            UpdateMouseSensitivityText(mouseSens);
        }

        if (gamepadSensitivitySlider != null)
        {
            gamepadSensitivitySlider.value = gamepadSens;
            UpdateGamepadSensitivityText(gamepadSens);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = volume;
            UpdateVolumeText(volume);
        }

        if (invertYToggle != null)
        {
            invertYToggle.isOn = invertY;
        }
    }

    // UI Button listeners
    public void ResumeGame()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ResumeGame();
        }
    }

    public void OpenSettings()
    {
        if (mainPausePanel != null && settingsPanel != null)
        {
            mainPausePanel.SetActive(false);
            settingsPanel.SetActive(true);
        }
    }

    public void BackToPauseMenu()
    {
        if (mainPausePanel != null && settingsPanel != null)
        {
            mainPausePanel.SetActive(true);
            settingsPanel.SetActive(false);
        }

        // Save settings when returning from settings menu
        SaveSettings();
    }

    public void ReturnToMainMenu()
    {
        // Save settings before exiting
        SaveSettings();

        // Resume time scale before loading new scene
        Time.timeScale = 1f;

        // Load the main menu scene
        SceneManager.LoadScene("Menu");
    }

    // Settings sliders event listeners
    public void OnMouseSensitivityChanged(float value)
    {
        UpdateMouseSensitivityText(value);

        // Update all player look components
        foreach (PlayerLook playerLook in playerLooks)
        {
            if (playerLook != null)
            {
                // Use reflection to set private field values
                var mouseXField = typeof(PlayerLook).GetField("mouseSensitivityX",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var mouseYField = typeof(PlayerLook).GetField("mouseSensitivityY",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (mouseXField != null) mouseXField.SetValue(playerLook, value);
                if (mouseYField != null) mouseYField.SetValue(playerLook, value);
            }
        }
    }

    public void OnGamepadSensitivityChanged(float value)
    {
        UpdateGamepadSensitivityText(value);

        // Update all player look components
        foreach (PlayerLook playerLook in playerLooks)
        {
            if (playerLook != null)
            {
                // Use reflection to set private field values
                var gamepadXField = typeof(PlayerLook).GetField("gamepadSensitivityX",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var gamepadYField = typeof(PlayerLook).GetField("gamepadSensitivityY",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (gamepadXField != null) gamepadXField.SetValue(playerLook, value);
                if (gamepadYField != null) gamepadYField.SetValue(playerLook, value);
            }
        }
    }

    public void OnVolumeChanged(float value)
    {
        UpdateVolumeText(value);
        AudioListener.volume = value;
    }

    public void OnInvertYChanged(bool value)
    {
        // Update all player look components
        foreach (PlayerLook playerLook in playerLooks)
        {
            if (playerLook != null)
            {
                // Use reflection to set private field
                var invertYField = typeof(PlayerLook).GetField("invertY",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (invertYField != null) invertYField.SetValue(playerLook, value);

                // Also update Cinemachine POV component if needed
                var cinemachinePOVField = typeof(PlayerLook).GetField("cinemachinePOV",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (cinemachinePOVField != null)
                {
                    var cinemachinePOV = cinemachinePOVField.GetValue(playerLook) as Cinemachine.CinemachinePOV;
                    if (cinemachinePOV != null)
                    {
                        cinemachinePOV.m_VerticalAxis.m_InvertInput = value;
                    }
                }
            }
        }
    }

    // Update UI text helpers
    private void UpdateMouseSensitivityText(float value)
    {
        if (mouseSensitivityText != null)
        {
            mouseSensitivityText.text = $"Mouse Sensitivity: {value:F0}";
        }
    }

    private void UpdateGamepadSensitivityText(float value)
    {
        if (gamepadSensitivityText != null)
        {
            gamepadSensitivityText.text = $"Gamepad Sensitivity: {value:F0}";
        }
    }

    private void UpdateVolumeText(float value)
    {
        if (volumeText != null)
        {
            volumeText.text = $"Master Volume: {Mathf.RoundToInt(value * 100)}%";
        }
    }

    // Save all settings to PlayerPrefs
    private void SaveSettings()
    {
        if (mouseSensitivitySlider != null)
            PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivitySlider.value);

        if (gamepadSensitivitySlider != null)
            PlayerPrefs.SetFloat("GamepadSensitivity", gamepadSensitivitySlider.value);

        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);

        if (invertYToggle != null)
            PlayerPrefs.SetInt("InvertY", invertYToggle.isOn ? 1 : 0);

        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        // Save settings when object is destroyed
        SaveSettings();
    }
}