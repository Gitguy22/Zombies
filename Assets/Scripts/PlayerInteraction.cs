using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 4f;
    public LayerMask interactableLayer;
    public Camera playerCamera;
    public float holdTime = 1.0f;  // Time in seconds needed to hold the button

    [Header("UI Elements")]
    public TextMeshProUGUI interactionText;
    public GameObject interactionPrompt;

    // Private variables
    private float currentHoldTime = 0f;
    private bool isHolding = false;
    private IInteractable currentInteractable;
    private GameObject currentInteractableObject;
    private PlayerInputs playerInput;
    private InputAction interactAction;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        // Initialize input actions
        playerInput = new PlayerInputs();
        playerInput.Enable();

        interactAction = playerInput.OnFoot.InteractAction;
        interactAction.performed += OnInteractStarted;
        interactAction.canceled += OnInteractCanceled;
    }

    private void OnDisable()
    {
        interactAction.performed -= OnInteractStarted;
        interactAction.canceled -= OnInteractCanceled;
    }

    private void Update()
    {
        CheckForInteractable();
        ProcessInteraction();
    }

    private float interactableLoseTime = 0.2f;
    private float interactableTimer = 0f;

    private void CheckForInteractable()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.green);

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactableTimer = 0f;

                if (interactable != currentInteractable)
                {
                    currentInteractable = interactable;
                    currentInteractableObject = hit.collider.gameObject;
                    UpdateInteractionUI(true);
                }
                return;
            }
        }

        if (currentInteractable != null)
        {
            interactableTimer += Time.deltaTime;
            if (interactableTimer >= interactableLoseTime)
            {
                currentInteractable = null;
                currentInteractableObject = null;
                UpdateInteractionUI(false);
            }
        }
    }

    private void UpdateInteractionUI(bool show)
    {
        if (interactionPrompt != null)
        {
            if (show && interactionText != null && currentInteractable != null)
            {
                // Check if the item has already been paid for
                if (currentInteractableObject.GetComponent<IInteractable>() is Door door && door.IsPaidFor())
                {
                    interactionPrompt.SetActive(false);
                    return;
                }

                string itemName = currentInteractable.GetItemName();
                int cost = currentInteractable.GetCost();
                string buttonName = interactAction.GetBindingDisplayString();

                interactionText.text = $"Hold <b>{buttonName}</b> to buy {itemName} for {cost} points";
                interactionPrompt.SetActive(true);
            }
            else
            {
                interactionPrompt.SetActive(false);
            }
        }
    }

    private void ProcessInteraction()
    {
        if (isHolding && currentInteractable != null)
        {
            currentHoldTime += Time.deltaTime;

            if (currentHoldTime >= holdTime)
            {
                currentInteractable.BuyItem(gameObject);
                ResetInteraction();
            }
        }
    }

    private void OnInteractStarted(InputAction.CallbackContext context)
    {
        if (currentInteractable != null)
        {
            isHolding = true;
            currentHoldTime = 0f;
        }
    }

    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        ResetInteraction();
    }

    private void ResetInteraction()
    {
        isHolding = false;
        currentHoldTime = 0f;
        UpdateInteractionUI(currentInteractable != null);
    }
}
