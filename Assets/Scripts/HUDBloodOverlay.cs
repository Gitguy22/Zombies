using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HUDBloodOverlay : MonoBehaviour
{
    [Header("Blood Overlay Images")]
    [SerializeField] Image bloodOverlay90; // 90-75% health
    [SerializeField] Image bloodOverlay75; // 75-50% health  
    [SerializeField] Image bloodOverlay50; // 50-25% health
    [SerializeField] Image bloodOverlay25; // 25-0% health

    [Header("Animation Settings")]
    [SerializeField] float fastFadeSpeed = 8f; // Much faster transitions
    [SerializeField] float pulseSpeed = 1.5f;
    [SerializeField] bool enablePulseAtCritical = true;

    [Header("Alpha Values")]
    [SerializeField] [Range(0f, 1f)] float bloodAlpha90 = 0.15f;
    [SerializeField] [Range(0f, 1f)] float bloodAlpha75 = 0.25f;
    [SerializeField] [Range(0f, 1f)] float bloodAlpha50 = 0.4f;
    [SerializeField] [Range(0f, 1f)] float bloodAlpha25 = 0.6f;

    private PlayerHealth playerHealth;
    private int currentBloodLevel = 5;
    private Coroutine pulseCoroutine;
    private bool isTransitioning = false;

    private void Awake()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            //Debug.LogError("HUDBloodOverlay: No PlayerHealth found!");
            enabled = false;
            return;
        }

        // Initialize all overlays as hidden
        SetBloodOverlayAlpha(bloodOverlay90, 0f);
        SetBloodOverlayAlpha(bloodOverlay75, 0f);
        SetBloodOverlayAlpha(bloodOverlay50, 0f);
        SetBloodOverlayAlpha(bloodOverlay25, 0f);
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthPercentageChanged.AddListener(UpdateBloodOverlay);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthPercentageChanged.RemoveListener(UpdateBloodOverlay);
        }
    }

    private void UpdateBloodOverlay(float healthPercentage)
    {
        int newBloodLevel = GetBloodLevel(healthPercentage);

        if (newBloodLevel != currentBloodLevel)
        {
            currentBloodLevel = newBloodLevel;

            // Instant update for full health, fast transition for others
            if (newBloodLevel == 5) // Full health - instant clear
            {
                InstantClearOverlays();
            }
            else
            {
                if (!isTransitioning)
                {
                    StartCoroutine(FastTransitionBloodOverlay());
                }
            }
        }
    }

    private int GetBloodLevel(float healthPercentage)
    {
        if (healthPercentage <= 0.25f) return 1; // Heavy
        if (healthPercentage <= 0.50f) return 2; // Medium
        if (healthPercentage <= 0.75f) return 3; // Light
        if (healthPercentage <= 0.90f) return 4; // Very light
        return 5; // None (90%+ health)
    }

    private void InstantClearOverlays()
    {
        // Stop any transitions or pulses
        StopAllCoroutines();
        pulseCoroutine = null;
        isTransitioning = false;

        // Instantly hide all overlays
        SetBloodOverlayAlpha(bloodOverlay90, 0f);
        SetBloodOverlayAlpha(bloodOverlay75, 0f);
        SetBloodOverlayAlpha(bloodOverlay50, 0f);
        SetBloodOverlayAlpha(bloodOverlay25, 0f);

        //Debug.Log("Blood overlays instantly cleared - full health");
    }

    private IEnumerator FastTransitionBloodOverlay()
    {
        isTransitioning = true;

        // Stop pulse if running
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // Fast fade out all overlays
        yield return StartCoroutine(FastFadeAllOverlays(0f));

        // Fast fade in appropriate overlay
        switch (currentBloodLevel)
        {
            case 1: // Heavy blood - with pulse
                yield return StartCoroutine(FastFadeOverlay(bloodOverlay25, bloodAlpha25));
                if (enablePulseAtCritical)
                    pulseCoroutine = StartCoroutine(PulseOverlay(bloodOverlay25, bloodAlpha25));
                break;

            case 2: // Medium blood
                yield return StartCoroutine(FastFadeOverlay(bloodOverlay50, bloodAlpha50));
                break;

            case 3: // Light blood
                yield return StartCoroutine(FastFadeOverlay(bloodOverlay75, bloodAlpha75));
                break;

            case 4: // Very light blood
                yield return StartCoroutine(FastFadeOverlay(bloodOverlay90, bloodAlpha90));
                break;
        }

        isTransitioning = false;
        //Debug.Log($"Blood overlay fast transition to level {currentBloodLevel} (Health: {playerHealth.GetHealthPercentage():P0})");
    }

    private IEnumerator FastFadeAllOverlays(float targetAlpha)
    {
        float duration = 1f / fastFadeSpeed; // Much faster

        Coroutine fade1 = StartCoroutine(FastFadeOverlay(bloodOverlay25, targetAlpha, duration));
        Coroutine fade2 = StartCoroutine(FastFadeOverlay(bloodOverlay50, targetAlpha, duration));
        Coroutine fade3 = StartCoroutine(FastFadeOverlay(bloodOverlay75, targetAlpha, duration));
        Coroutine fade4 = StartCoroutine(FastFadeOverlay(bloodOverlay90, targetAlpha, duration));

        yield return fade1;
        yield return fade2;
        yield return fade3;
        yield return fade4;
    }

    private IEnumerator FastFadeOverlay(Image overlay, float targetAlpha, float? customDuration = null)
    {
        if (overlay == null) yield break;

        Color startColor = overlay.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);

        float duration = customDuration ?? (1f / fastFadeSpeed);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            overlay.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        overlay.color = targetColor;
    }

    private IEnumerator PulseOverlay(Image overlay, float maxAlpha)
    {
        if (overlay == null) yield break;

        while (true)
        {
            float minAlpha = maxAlpha * 0.4f;
            float pulseValue = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulseValue);

            SetBloodOverlayAlpha(overlay, currentAlpha);
            yield return null;
        }
    }

    private void SetBloodOverlayAlpha(Image overlay, float alpha)
    {
        if (overlay != null)
        {
            Color color = overlay.color;
            color.a = alpha;
            overlay.color = color;
        }
    }
}