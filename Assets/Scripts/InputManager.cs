using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInputs playerInput;
    private PlayerInputs.OnFootActions onFoot;

    private PlayerMovement movement;
    private PlayerLook look;

    void Awake()
    {
        playerInput = new PlayerInputs();
        onFoot = playerInput.OnFoot;

        movement = GetComponent<PlayerMovement>();
        look = GetComponent<PlayerLook>();

        //onFoot.Jump.performed += ctx => movement.Jump();
    }

    void Update()
    {
        //movement.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
        look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        onFoot.Enable();
    }

    private void OnDisable()
    {
        onFoot.Disable();
    }
}