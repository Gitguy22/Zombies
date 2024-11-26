using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private PlayerInputs playerInput;  // This is the input class
    private InputAction movementAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction pauseAction;

    [SerializeField] GameObject pauseMenu;

    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    private bool isGrounded;
    private float currentSpeed;

    public bool isPaused;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Initialize input actions
        playerInput = new PlayerInputs();  // This is the generated class from PlayerInputs.inputactions
        playerInput.Enable();  // Enable all action maps in the asset

        // Get the input actions from the OnFoot action map
        movementAction = playerInput.OnFoot.Movement;
        jumpAction = playerInput.OnFoot.Jump;
        sprintAction = playerInput.OnFoot.Sprint;
        pauseAction = playerInput.OnFoot.Pause;

        // Register sprint action callbacks
        sprintAction.started += ctx => StartSprinting();
        sprintAction.canceled += ctx => StopSprinting();

        // Set initial speed
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        if (pauseAction.triggered)
        {
            isPaused =! isPaused;
            PauseGame();
        }

        if(isPaused == false)
        {
            HandleMovement();
        }
        

        // Apply gravity
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void HandleMovement()
    {
        // Check if grounded
        isGrounded = controller.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Keep the player grounded
        }

        // Get movement input (X and Z directions)
        Vector2 input = movementAction.ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0, input.y);
        move = transform.TransformDirection(move);

        // Move the player based on input
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Handle jump
        if (isGrounded && jumpAction.triggered)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Pause game
        
    }

    private void StartSprinting()
    {
        currentSpeed = sprintSpeed;
    }

    private void StopSprinting()
    {
        currentSpeed = walkSpeed;
    }

    private void OnDisable()
    {
        // Clean up input callbacks when the object is disabled
        sprintAction.started -= ctx => StartSprinting();
        sprintAction.canceled -= ctx => StopSprinting();
    }

        public void ClickResume()
    {
        isPaused =! isPaused;
        PauseGame();
    }

    public void PauseGame()
    {
        if(isPaused == true)
        {
            Time.timeScale = 0;
            pauseMenu.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            pauseMenu.SetActive(false);
        }
    }
}