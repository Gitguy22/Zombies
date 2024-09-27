using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private PlayerInputs playerInput;  // This is your generated input class
    private InputAction movementAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    private bool isGrounded;
    private float currentSpeed;

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

        // Register sprint action callbacks
        sprintAction.started += ctx => StartSprinting();
        sprintAction.canceled += ctx => StopSprinting();

        // Set initial speed
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        // Handle movement and jumping logic
        HandleMovement();

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
}