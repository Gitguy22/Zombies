using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public class PlayerCursor : MonoBehaviour
{
    [Header("Cursor Visual")]
    [SerializeField] Image cursorImage;
    [SerializeField] TextMeshProUGUI playerNumberText;

    [Header("Movement Settings")]
    [SerializeField] float cursorSpeed = 600f;
    [SerializeField] float gamepadSensitivity = 300f;

    private int playerIndex;
    private Color playerColor;
    private InputDevice device;
    private RectTransform rectTransform;

    // References to Input Actions
    private PlayerInputs inputActions;
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    private Vector2 cursorPosition;
    private GameObject currentlyHighlighted;

    private void Awake()
    {
        Debug.Log("PlayerCursor Awake");

        rectTransform = GetComponent<RectTransform>();

        if (cursorImage == null)
            cursorImage = GetComponent<Image>();

        inputActions = new PlayerInputs();
    }

    public void Initialize(int index, Color color, InputDevice inputDevice)
    {
        Debug.Log($"Initializing cursor for player {index + 1} with color {color} and device {inputDevice?.displayName}");

        playerIndex = index;
        playerColor = color;
        device = inputDevice;

        // Set cursor color
        if (cursorImage != null)
        {
            cursorImage.color = playerColor;
        }

        // Set player number if text component exists
        if (playerNumberText != null)
        {
            playerNumberText.text = (playerIndex + 1).ToString();
            playerNumberText.color = Color.white;
        }

        // Setup initial position - offset based on player index
        cursorPosition = new Vector2(
            Screen.width / 2 + (playerIndex * 200) - 300,
            Screen.height / 2
        );

        UpdateCursorPosition();

        // Enable input actions for this cursor
        EnableDevice(device);
    }

    private void EnableDevice(InputDevice device)
    {
        if (device == null)
        {
            Debug.LogWarning("Cannot enable device - device is null");
            return;
        }

        Debug.Log($"Enabling device {device.displayName} for cursor");

        // Enable the appropriate actions
        inputActions.Enable();

        // Access the UI action map from your PlayerInputs
        navigateAction = inputActions.UI.Navigate;
        submitAction = inputActions.UI.Submit;
        cancelAction = inputActions.UI.Cancel;

        // Add event listeners
        submitAction.performed += OnSubmit;
        cancelAction.performed += OnCancel;
    }

    private void OnDisable()
    {
        // Clean up action listeners
        if (submitAction != null)
            submitAction.performed -= OnSubmit;
        if (cancelAction != null)
            cancelAction.performed -= OnCancel;

        inputActions.Disable();
    }

    private void Update()
    {
        UpdateCursorInput();
        RaycastForUIElements();
    }

    private void UpdateCursorInput()
    {
        if (device is Keyboard || device is Mouse)
        {
            // For mouse, get the position directly
            if (Mouse.current != null && playerIndex == 0) // Assuming player 0 gets mouse
            {
                cursorPosition = Mouse.current.position.ReadValue();
            }
            else
            {
                // For keyboard without mouse, use the navigation action
                Vector2 input = navigateAction.ReadValue<Vector2>();
                if (input.magnitude > 0.1f)
                {
                    cursorPosition += input * cursorSpeed * Time.deltaTime;

                    // Clamp to screen bounds
                    cursorPosition.x = Mathf.Clamp(cursorPosition.x, 0, Screen.width);
                    cursorPosition.y = Mathf.Clamp(cursorPosition.y, 0, Screen.height);
                }
            }
        }
        else if (device is Gamepad)
        {
            // For gamepad, use the navigation action
            Vector2 input = navigateAction.ReadValue<Vector2>();
            if (input.magnitude > 0.1f)
            {
                cursorPosition += input * gamepadSensitivity * Time.deltaTime;

                // Clamp to screen bounds
                cursorPosition.x = Mathf.Clamp(cursorPosition.x, 0, Screen.width);
                cursorPosition.y = Mathf.Clamp(cursorPosition.y, 0, Screen.height);
            }
        }

        UpdateCursorPosition();
    }

    private void UpdateCursorPosition()
    {
        if (rectTransform != null)
        {
            rectTransform.position = cursorPosition;
        }
    }

    private void RaycastForUIElements()
    {
        // Use EventSystem.current to raycast and find UI elements under cursor
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = cursorPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Find the first selectable UI element
        GameObject newHighlighted = null;
        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<Selectable>() != null)
            {
                newHighlighted = result.gameObject;
                break;
            }
        }

        // Handle highlight change
        if (newHighlighted != currentlyHighlighted)
        {
            // Unhighlight previous
            if (currentlyHighlighted != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            // Highlight new
            currentlyHighlighted = newHighlighted;
            if (currentlyHighlighted != null)
            {
                EventSystem.current.SetSelectedGameObject(currentlyHighlighted);
            }
        }
    }

    private void OnSubmit(InputAction.CallbackContext context)
    {
        if (currentlyHighlighted != null)
        {
            // Trigger a click on the UI element
            var button = currentlyHighlighted.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        // This is used for going back in menus
    }
}