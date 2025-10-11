using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

public class PowerSwitch : MonoBehaviour, IInteractable
{
    [Header("Power System")]
    [SerializeField] PowerSystemData powerSystemData;

    [Header("Animation")]
    [SerializeField] Animator switchAnimator;
    [SerializeField] string animationTrigger = "TurnOn";
    [SerializeField] float animationDelay = 0.5f;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip powerOnSound;
    [SerializeField] AudioClip powerOffSound;

    [Header("UI")]
    [SerializeField] string interactPrompt = "Activate Power";
    [SerializeField] string powerAlreadyOnPrompt = "Power is already on";

    bool isPaidFor = false;

    void Awake()
    {
        // Get Animator if not assigned
        if (switchAnimator == null)
        {
            switchAnimator = GetComponent<Animator>();
        }
    }

    void Start()
    {
        // Debug animator info
        DebugAnimatorInfo();

        // Set initial state
        if (powerSystemData.IsPowerOn)
        {
            isPaidFor = true;
            // Jump to On state without transition
            if (switchAnimator != null)
            {
                switchAnimator.Play("On", 0, 1f);
            }
        }
    }

    void DebugAnimatorInfo()
    {
        if (switchAnimator == null)
        {
            //Debug.LogError("No Animator found on " + gameObject.name);
            return;
        }

        if (switchAnimator.runtimeAnimatorController == null)
        {
            //Debug.LogError("No Animator Controller assigned to " + gameObject.name);
            return;
        }

        //Debug.Log($"Animator Controller: {switchAnimator.runtimeAnimatorController.name}");
        //Debug.Log("Available parameters:");
        foreach (var param in switchAnimator.parameters)
        {
            //Debug.Log($"  - '{param.name}' (Type: {param.type})");
        }
    }

    public void BuyItem(GameObject player)
    {
        if (powerSystemData.IsPowerOn)
        {
            //Debug.Log("Power is already on!");
            return;
        }

        StartCoroutine(TurnOnPower());
        isPaidFor = true;
    }

    public string GetItemName()
    {
        return powerSystemData.IsPowerOn ? powerAlreadyOnPrompt : interactPrompt;
    }

    public int GetCost()
    {
        return 0;
    }

    public bool IsPaidFor()
    {
        return isPaidFor || powerSystemData.IsPowerOn;
    }

    IEnumerator TurnOnPower()
    {
        // Try to play animation
        if (switchAnimator != null)
        {
            // Method 1: Try setting trigger
            try
            {
                switchAnimator.SetTrigger(animationTrigger);
            }
            catch (System.Exception e)
            {
                //Debug.LogError($"Failed to set trigger: {e.Message}");
                // Method 2: Fallback to playing state directly
                switchAnimator.Play("On");
            }
        }

        // Play sound
        if (audioSource != null && powerOnSound != null)
        {
            audioSource.PlayOneShot(powerOnSound);
        }

        // Wait for animation
        yield return new WaitForSeconds(animationDelay);

        // Turn on power
        powerSystemData.SetPowerState(true);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}