using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class HintMenu : MonoBehaviour
{
    [SerializeField] private InputAction hideAction;

    private Coroutine hideCoroutine;

    void OnEnable()
    {
        // Enable the hide action and subscribe to it
        hideAction.Enable();
        hideAction.performed += OnHidePerformed;

        // Start the auto-hide coroutine
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    void OnDisable()
    {
        // Disable the hide action and unsubscribe
        hideAction.Disable();
        hideAction.performed -= OnHidePerformed;

        // Stop the coroutine if it's running
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
    }

    private IEnumerator HideAfterDelay()
    {
        // Wait for 15 seconds
        yield return new WaitForSeconds(15f);

        // Hide the menu
        gameObject.SetActive(false);
    }

    private void OnHidePerformed(InputAction.CallbackContext context)
    {
        // Toggle visibility when the hide action is performed
        gameObject.SetActive(!gameObject.activeSelf);

        // If showing the menu again, restart the auto-hide timer
        if (gameObject.activeSelf)
        {
            if (hideCoroutine != null)
                StopCoroutine(hideCoroutine);

            hideCoroutine = StartCoroutine(HideAfterDelay());
        }
    }
}