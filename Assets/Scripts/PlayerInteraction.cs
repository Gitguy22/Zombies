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
    public float holdTime = 1.0f;

    [Header("UI Elements")]
    public TextMeshProUGUI interactionText;
    public GameObject interactionPrompt;

    [Header("Controller")]
    [SerializeField] bool showControllerPrompts = true;

    // Private variables
    private float currentHoldTime = 0f;
    private bool isHolding = false;
    private IInteractable currentInteractable;
    private GameObject currentInteractableObject;
    private PlayerInput playerInput;
    private InputAction interactAction;
    private PlayerController playerController;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        playerInput = GetComponent<PlayerInput>();
        playerController = GetComponent<PlayerController>();

        if (playerInput != null)
        {
            interactAction = playerInput.actions["InteractAction"];
            interactAction.performed += OnInteractStarted;
            interactAction.canceled += OnInteractCanceled;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractStarted;
            interactAction.canceled -= OnInteractCanceled;
        }
    }

    private void Update()
    {
        // Check pause state
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused())
            return;

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
/*            // Debug what we hit
            Debug.Log($"Hit object: {hit.collider.name}");
            Debug.Log($"Hit object layer: {hit.collider.gameObject.layer} ({LayerMask.LayerToName(hit.collider.gameObject.layer)})");
            Debug.Log($"Hit object position: {hit.collider.transform.position}");
            Debug.Log($"Hit object root: {hit.collider.transform.root.name}");*/

            // Check ALL IInteractable components on this object
            IInteractable[] interactables = hit.collider.GetComponents<IInteractable>();
            //Debug.Log($"IInteractable components on hit object: {interactables.Length}");

            foreach (IInteractable inter in interactables)
            {
                MonoBehaviour component = inter as MonoBehaviour;
                if (component != null)
                {
                    //Debug.Log($"- Component: {component.GetType().Name} (enabled: {component.enabled}) (active: {component.gameObject.activeInHierarchy})");
                }
            }

            // Check for IInteractable in parent
            IInteractable parentInteractable = hit.collider.GetComponentInParent<IInteractable>();
            if (parentInteractable != null)
            {
                MonoBehaviour parentComp = parentInteractable as MonoBehaviour;
                if (parentComp != null)
                {
                    //Debug.Log($"Parent has IInteractable: {parentComp.GetType().Name} on {parentComp.gameObject.name} (enabled: {parentComp.enabled})");
                }
            }

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable == null)
                interactable = hit.collider.GetComponentInParent<IInteractable>();

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
                if (currentInteractable.IsPaidFor())
                {
                    interactionPrompt.SetActive(false);
                    return;
                }

                string newText = currentInteractable.GetItemName();
                int cost = currentInteractable.GetCost();

                // Get the correct button prompt based on input device
                string buttonName = GetButtonPrompt();

                interactionText.text = $"Hold {buttonName} to {newText}";
                interactionPrompt.SetActive(true);
            }
            else
            {
                interactionPrompt.SetActive(false);
            }
        }
    }

    private string GetButtonPrompt()
    {
        if (playerInput == null) return "[Interact]";

        // Check if using gamepad
        bool usingGamepad = playerInput.currentControlScheme == "Gamepad";

        if (usingGamepad && showControllerPrompts)
        {
            // Return appropriate controller button icon/text
            return "[X]";
        }
        else
        {
            // Return keyboard key
            return $"[{interactAction.GetBindingDisplayString()}]";
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