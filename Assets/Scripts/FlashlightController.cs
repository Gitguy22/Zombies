using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    [SerializeField] GameObject flashlight;

    bool isPaused = false;
    PlayerInputs playerInput;
    InputAction flashlightAction;

    private void Awake()
    {
        playerInput = new PlayerInputs();
        flashlightAction = playerInput.OnFoot.Flashlight;
    }

    private void OnEnable()
    {
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }

    void Start()
    {
        // Start with flashlight on
        if (flashlight != null)
            flashlight.SetActive(true);
    }

    void Update()
    {
        if (!isPaused && flashlightAction.triggered)
        {
            if (flashlight != null)
                flashlight.SetActive(!flashlight.activeSelf);
        }
    }
}