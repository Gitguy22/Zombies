using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles UI navigation and slider editing for individual players.
/// This script is automatically added by PlayerUIPanel.
/// </summary>
public class PlayerUINavigator : MonoBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] float sliderAdjustSpeed = 10.0f;
    [SerializeField] Color sliderEditColor = Color.yellow;
    [SerializeField] Color sliderNormalColor = Color.white;

    private PlayerInput playerInput;
    private MultiplayerEventSystem eventSystem;
    private GameObject firstSelected;

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    private bool isEditingSlider = false;
    private Slider currentSlider;
    private ColorBlock originalSliderColors;

    public void Initialize(PlayerInput input, MultiplayerEventSystem eventSys, GameObject firstSelectedButton)
    {
        playerInput = input;
        eventSystem = eventSys;
        firstSelected = firstSelectedButton;
        // Get UI actions
        navigateAction = playerInput.actions["Navigate"];
        submitAction = playerInput.actions["Submit"];
        cancelAction = playerInput.actions["Cancel"];

        // Subscribe to actions
        if (submitAction != null)
            submitAction.performed += OnSubmit;
        if (cancelAction != null)
            cancelAction.performed += OnCancel;

        //////Debug.Log($"PlayerUINavigator initialized for Player {playerInput.playerIndex + 1}");
    }

    void Update()
    {
        // Handle slider editing
        if (isEditingSlider && currentSlider != null && navigateAction != null)
        {
            Vector2 navigate = navigateAction.ReadValue<Vector2>();

            if (Mathf.Abs(navigate.x) > 0.1f)
            {
                float range = currentSlider.maxValue - currentSlider.minValue;
                float step = range * sliderAdjustSpeed;

                currentSlider.value += navigate.x * step * Time.unscaledDeltaTime;
            }
        }
    }

    void OnSubmit(InputAction.CallbackContext context)
    {
        if (eventSystem?.currentSelectedGameObject == null) return;

        Slider slider = eventSystem.currentSelectedGameObject.GetComponent<Slider>();

        if (slider != null)
        {
            if (!isEditingSlider)
            {
                EnterSliderEditMode(slider);
            }
            else if (currentSlider == slider)
            {
                ExitSliderEditMode();
            }
        }
        else
        {
            // Handle button/toggle presses
            Button button = eventSystem.currentSelectedGameObject.GetComponent<Button>();
            Toggle toggle = eventSystem.currentSelectedGameObject.GetComponent<Toggle>();

            if (button != null)
            {
                button.onClick.Invoke();
            }
            else if (toggle != null)
            {
                toggle.isOn = !toggle.isOn;
            }
        }
    }

    void OnCancel(InputAction.CallbackContext context)
    {
        if (isEditingSlider)
        {
            ExitSliderEditMode();
        }
        else
        {
            // Resume game when cancel is pressed
            PauseManager pauseManager = playerInput.GetComponent<PauseManager>();
            if (pauseManager != null)
            {
                pauseManager.ForceResume();
            }
        }
    }

    void EnterSliderEditMode(Slider slider)
    {
        isEditingSlider = true;
        currentSlider = slider;

        // Store original colors and apply edit color
        originalSliderColors = slider.colors;
        var colors = slider.colors;
        colors.selectedColor = sliderEditColor;
        slider.colors = colors;

        //////Debug.Log($"Player {playerInput.playerIndex + 1}: Entered slider edit mode");
    }

    void ExitSliderEditMode()
    {
        if (currentSlider != null)
        {
            // Restore original colors
            var colors = originalSliderColors;
            colors.selectedColor = sliderNormalColor;
            currentSlider.colors = colors;
        }

        isEditingSlider = false;
        currentSlider = null;

        //////Debug.Log($"Player {playerInput.playerIndex + 1}: Exited slider edit mode");
    }

    public bool IsEditingSlider() => isEditingSlider;

    void OnDestroy()
    {
        // Unsubscribe from actions
        if (submitAction != null)
            submitAction.performed -= OnSubmit;
        if (cancelAction != null)
            cancelAction.performed -= OnCancel;
    }
}