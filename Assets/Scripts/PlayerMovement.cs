using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float groundedGravity = -2f;

    [Header("References")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Animator animator;

    // Private variables
    private CharacterController controller;
    private Vector3 playerVelocity;
    private float currentSpeed;
    private bool isGrounded;
    private bool isPaused;

    // Input System
    private PlayerInputs playerInput;
    private InputAction movementAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction pauseAction;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        InitializeInputs();
    }

    private void OnEnable()
    {
        playerInput.Enable();
        RegisterInputCallbacks(true);
    }

    private void OnDisable()
    {
        RegisterInputCallbacks(false);
        playerInput.Disable();
    }

    private void InitializeInputs()
    {
        playerInput = new PlayerInputs();
        movementAction = playerInput.OnFoot.Movement;
        jumpAction = playerInput.OnFoot.Jump;
        sprintAction = playerInput.OnFoot.Sprint;
        pauseAction = playerInput.OnFoot.Pause;
        currentSpeed = walkSpeed;
    }

    private void RegisterInputCallbacks(bool register)
    {
        if (register)
        {
            sprintAction.started += OnSprintStarted;
            sprintAction.canceled += OnSprintCanceled;
            pauseAction.performed += OnPausePerformed;
        }
        else
        {
            sprintAction.started -= OnSprintStarted;
            sprintAction.canceled -= OnSprintCanceled;
            pauseAction.performed -= OnPausePerformed;
        }
    }

    private void OnSprintStarted(InputAction.CallbackContext ctx) => StartSprinting();
    private void OnSprintCanceled(InputAction.CallbackContext ctx) => StopSprinting();
    private void OnPausePerformed(InputAction.CallbackContext ctx) => TogglePause();

    private void Update()
    {
        if (!isPaused)
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        CheckGrounded();
        MovePlayer();
        HandleJump();
        ApplyGravity();
    }

    private void CheckGrounded()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = groundedGravity;
            if (animator != null) animator.SetBool("Jumping", false);
        }
    }

    private void MovePlayer()
    {
        Vector2 input = movementAction.ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0, input.y);

        // Only transform direction if there's actual movement to prevent normalization of zero vectors
        if (move != Vector3.zero)
        {
            move = transform.TransformDirection(move.normalized);
            controller.Move(move * currentSpeed * Time.deltaTime);
        }

        if (animator != null) UpdateAnimations(input);
    }

    private void HandleJump()
    {
        if (isGrounded && jumpAction.triggered)
        {
            Jump();
        }
    }

    private void Jump()
    {
        playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        if (animator != null) animator.SetBool("Jumping", true);
    }

    private void ApplyGravity()
    {
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void StartSprinting()
    {
        currentSpeed = sprintSpeed;
        if (animator != null) animator.SetBool("ForwardRunning", true);
    }

    private void StopSprinting()
    {
        currentSpeed = walkSpeed;
        if (animator != null) animator.SetBool("ForwardRunning", false);
    }

    private void UpdateAnimations(Vector2 input)
    {
        bool isMoving = input != Vector2.zero;
        animator.SetBool("Idle", !isMoving);

        if (isMoving)
        {
            animator.SetBool("Forward", input.y > 0 && currentSpeed == walkSpeed);
            animator.SetBool("Backward", input.y < 0);
            animator.SetBool("Left", input.x < 0);
            animator.SetBool("Right", input.x > 0);
        }
        else
        {
            // Reset movement animations when idle
            animator.SetBool("Forward", false);
            animator.SetBool("Backward", false);
            animator.SetBool("Left", false);
            animator.SetBool("Right", false);
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        UpdatePauseState();
    }

    public void ClickResume()
    {
        isPaused = false;
        UpdatePauseState();
    }

    private void UpdatePauseState()
    {
        Time.timeScale = isPaused ? 0 : 1;
        pauseMenu.SetActive(isPaused);
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }
}