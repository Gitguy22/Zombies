using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int damageAmount = 40;
    [SerializeField] private Collider attackCollider;
    [SerializeField] private string attackTriggerName = "attack";
    [SerializeField] private string playerTag = "Player";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Animator zombieAnimator;
    private bool canDamage = false;
    private bool hasHitThisAttack = false; // Prevents multiple hits per attack

    private void Awake()
    {
        // Find the animator in the parent hierarchy
        zombieAnimator = GetComponentInParent<Animator>();

        if (zombieAnimator == null)
        {
            Debug.LogError("ZombieAttack: Failed to find Animator component in parent hierarchy.");
        }

        // Ensure the attack collider starts disabled
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
        else
        {
            Debug.LogError("ZombieAttack: Attack collider is not assigned.");
        }
    }

    private void OnEnable()
    {
        // Reset hit flag when component is enabled
        hasHitThisAttack = false;
    }

    private void OnDisable()
    {
        canDamage = false;
        hasHitThisAttack = false;

        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
    }

    private void Update()
    {
        // Check if attack animation is playing
        if (zombieAnimator != null)
        {
            // Get info about the current animator state
            AnimatorStateInfo stateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);

            // Check if we're in an attack state
            if (stateInfo.IsName(attackTriggerName) || stateInfo.IsTag("Attack"))
            {
                // Activate the collider when the attack animation is playing
                if (!canDamage)
                {
                    EnableAttackCollider();
                }
            }
            else
            {
                // Disable the collider when not in attack animation and reset hit flag
                if (canDamage)
                {
                    DisableAttackCollider();
                    hasHitThisAttack = false; // Reset for next attack
                }
            }
        }
    }

    // Called from animation event when attack starts
    public void OnAttackStart()
    {
        EnableAttackCollider();
        hasHitThisAttack = false; // Reset hit flag for new attack

        if (showDebugLogs)
        {
            Debug.Log("ZombieAttack: New attack started, hit flag reset");
        }
    }

    // Called from animation event when attack ends
    public void OnAttackEnd()
    {
        DisableAttackCollider();
        hasHitThisAttack = false; // Reset for next attack
    }

    private void EnableAttackCollider()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = true;
            canDamage = true;

            if (showDebugLogs)
            {
                Debug.Log("ZombieAttack: Attack collider enabled");
            }
        }
    }

    private void DisableAttackCollider()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
            canDamage = false;

            if (showDebugLogs)
            {
                Debug.Log("ZombieAttack: Attack collider disabled");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if we can damage and haven't hit this attack yet
        if (!canDamage || hasHitThisAttack) return;

        // Check if we hit a player
        if (other.CompareTag(playerTag))
        {
            // Try to get the player health reference component
            PlayerHealthReference healthRef = other.GetComponent<PlayerHealthReference>();

            if (healthRef == null)
            {
                // If not on the direct object, try to find it in parent
                healthRef = other.GetComponentInParent<PlayerHealthReference>();
            }

            if (healthRef != null && healthRef.HealthData != null)
            {
                // Apply damage to the player
                healthRef.HealthData.TakeDamage(damageAmount);

                // Mark that we've hit during this attack
                hasHitThisAttack = true;

                if (showDebugLogs)
                {
                    Debug.Log($"ZombieAttack: Hit player for {damageAmount} damage - attack now locked");
                }
            }
            else
            {
                Debug.LogWarning("ZombieAttack: Hit player but couldn't find PlayerHealthReference or PlayerHealthData");
            }
        }
    }
}