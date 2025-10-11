using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Image centerDot;
    [SerializeField] private Image[] crosshairLines = new Image[4]; // In this order: Top, Right, Bottom, Left
    [SerializeField] private CanvasGroup crosshairGroup;
    [SerializeField] private GameObject playerObject;

    [Header("Default Appearance")]
    [SerializeField] private Sprite dotSprite;
    [SerializeField] private Sprite lineSprite;
    [SerializeField] private Color crosshairColor = Color.white;
    [SerializeField] private float dotSize = 4f;
    [SerializeField] private float lineLength = 10f;
    [SerializeField] private float lineWidth = 2f;
    [SerializeField] private float lineDistance = 5f;

    [Header("Dynamic Behavior")]
    [SerializeField] private bool dynamicCrosshair = true;
    [SerializeField] private float maxSpread = 20f;
    [SerializeField] private float spreadMultiplier = 1.5f;
    [SerializeField] private float aimAlpha = 0.3f;
    [SerializeField] private float transitionSpeed = 10f;

    // Private references
    private RectTransform centerTransform;
    private RectTransform[] lineTransforms = new RectTransform[4];
    private Weapon currentWeapon;
    private WeaponData currentWeaponData;
    private WeaponManager weaponManager;
    private PlayerMovement playerMovement;

    // Movement tracking
    private float currentSpread;
    private float targetSpread;

    // Track last weapon to detect changes
    private GameObject lastWeaponObject;

    private void Awake()
    {
        // Initialize RectTransforms
        if (centerDot != null)
            centerTransform = centerDot.GetComponent<RectTransform>();

        for (int i = 0; i < crosshairLines.Length; i++)
        {
            if (crosshairLines[i] != null)
                lineTransforms[i] = crosshairLines[i].GetComponent<RectTransform>();
        }

        // Find player references
        if (playerObject == null)
            playerObject = transform.root.gameObject;

        weaponManager = playerObject.GetComponent<WeaponManager>();
        playerMovement = playerObject.GetComponent<PlayerMovement>();

        // Setup crosshair appearance
        SetupCrosshair();
    }

    private void Start()
    {
        // Subscribe to weapon change events
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged += OnWeaponChanged;
        }

        // Force initial weapon check
        StartCoroutine(InitialWeaponCheck());
    }

    private IEnumerator InitialWeaponCheck()
    {
        // Wait a frame to ensure weapon manager is ready
        yield return null;

        // Force check for current weapon
        CheckForWeaponChange();
    }

    private void Update()
    {
        // Check for weapon changes every frame to catch any missed events
        CheckForWeaponChange();

        UpdateCrosshairSpread();
    }

    private void CheckForWeaponChange()
    {
        if (weaponManager == null) return;

        GameObject currentWeaponObject = weaponManager.GetCurrentWeapon();

        // If weapon changed, update crosshair
        if (currentWeaponObject != lastWeaponObject)
        {
            lastWeaponObject = currentWeaponObject;
            UpdateCurrentWeapon(currentWeaponObject);
        }
    }

    private void UpdateCurrentWeapon(GameObject weaponObject)
    {
        if (weaponObject != null)
        {
            currentWeapon = weaponObject.GetComponent<Weapon>();
            if (currentWeapon != null && currentWeapon.weaponData != null)
            {
                currentWeaponData = currentWeapon.weaponData;
                ApplyCrosshairFromWeapon(currentWeaponData);
                //Debug.Log($"Crosshair updated for weapon: {currentWeaponData.weaponName}");
            }
        }
        else
        {
            // No weapon equipped
            currentWeapon = null;
            currentWeaponData = null;
            SetupCrosshair(); // Use defaults
        }
    }

    // INITIAL SETUP - Makes the crosshair look right from the start
    private void SetupCrosshair()
    {
        //Debug.Log("Setting up crosshair...");

        // Configure center dot
        if (centerDot != null)
        {
            centerDot.sprite = dotSprite;
            centerDot.color = crosshairColor;
            centerTransform.sizeDelta = new Vector2(dotSize, dotSize);
        }

        // Configure each line
        for (int i = 0; i < crosshairLines.Length; i++)
        {
            if (crosshairLines[i] == null) continue;

            // Set appearance
            crosshairLines[i].sprite = lineSprite;
            crosshairLines[i].color = crosshairColor;

            // ALWAYS set size as if the sprite is vertical (width, length)
            lineTransforms[i].sizeDelta = new Vector2(lineWidth, lineLength);

            // Position each line in the correct orientation
            switch (i)
            {
                case 0: // TOP
                    lineTransforms[i].anchoredPosition = new Vector2(0, lineDistance);
                    lineTransforms[i].localRotation = Quaternion.identity;
                    break;

                case 1: // RIGHT
                    lineTransforms[i].anchoredPosition = new Vector2(lineDistance, 0);
                    lineTransforms[i].localRotation = Quaternion.Euler(0, 0, 90);
                    break;

                case 2: // BOTTOM
                    lineTransforms[i].anchoredPosition = new Vector2(0, -lineDistance);
                    lineTransforms[i].localRotation = Quaternion.identity;
                    break;

                case 3: // LEFT
                    lineTransforms[i].anchoredPosition = new Vector2(-lineDistance, 0);
                    lineTransforms[i].localRotation = Quaternion.Euler(0, 0, 90);
                    break;
            }
        }

        // Set initial spread
        currentSpread = lineDistance;
        targetSpread = lineDistance;
    }

    // When weapon changes
    private void OnWeaponChanged(string weaponName)
    {
        // Force immediate weapon check
        CheckForWeaponChange();
    }

    // Apply weapon-specific crosshair settings
    private void ApplyCrosshairFromWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return;

        // Skip if no crosshair defined
        if (weaponData.crosshairSprite == null)
        {
            SetupCrosshair(); // Use defaults
            return;
        }

        // Configure center dot
        if (centerDot != null)
        {
            centerDot.sprite = weaponData.crosshairSprite;
            centerDot.color = weaponData.crosshairColor;
            centerTransform.sizeDelta = new Vector2(weaponData.crosshairSize, weaponData.crosshairSize);
        }

        // Configure each line
        for (int i = 0; i < crosshairLines.Length; i++)
        {
            if (crosshairLines[i] == null) continue;

            // Set appearance
            crosshairLines[i].sprite = weaponData.crosshairLineSprite;
            crosshairLines[i].color = weaponData.crosshairColor;

            // ALWAYS set size as if the sprite is vertical (width, length)
            lineTransforms[i].sizeDelta = new Vector2(weaponData.crosshairLineWidth, weaponData.crosshairLineLength);

            // Position each line in the correct orientation
            switch (i)
            {
                case 0: // TOP
                    lineTransforms[i].anchoredPosition = new Vector2(0, weaponData.crosshairLineDistance);
                    lineTransforms[i].localRotation = Quaternion.identity;
                    break;

                case 1: // RIGHT
                    lineTransforms[i].anchoredPosition = new Vector2(weaponData.crosshairLineDistance, 0);
                    lineTransforms[i].localRotation = Quaternion.Euler(0, 0, 90);
                    break;

                case 2: // BOTTOM
                    lineTransforms[i].anchoredPosition = new Vector2(0, -weaponData.crosshairLineDistance);
                    lineTransforms[i].localRotation = Quaternion.identity;
                    break;

                case 3: // LEFT
                    lineTransforms[i].anchoredPosition = new Vector2(-weaponData.crosshairLineDistance, 0);
                    lineTransforms[i].localRotation = Quaternion.Euler(0, 0, 90);
                    break;
            }
        }

        // Update spread values
        dynamicCrosshair = weaponData.dynamicCrosshair;
        maxSpread = weaponData.crosshairMaxSpread;
        currentSpread = weaponData.crosshairLineDistance;
        targetSpread = currentSpread;
    }

    // Update crosshair based on player movement, weapon state, etc.
    private void UpdateCrosshairSpread()
    {
        if (currentWeapon == null || currentWeaponData == null) return;

        float baseDistance = currentWeaponData.crosshairLineDistance;
        float spreadFactor = 1.0f;
        bool isAiming = currentWeapon.IsAiming();

        // Apply movement factors
        if (playerMovement != null)
        {
            if (playerMovement.IsRunning())
                spreadFactor *= 2.0f;
            else if (playerMovement.IsWalking())
                spreadFactor *= 1.5f;

            if (!playerMovement.IsGrounded())
                spreadFactor *= 2.0f;
        }

        // Apply weapon state factors
        if (currentWeapon.IsFiring())
            spreadFactor *= 1.5f;

        // Reduce spread when aiming
        if (isAiming)
            spreadFactor *= 0.5f;

        // Calculate target spread
        if (dynamicCrosshair)
            targetSpread = Mathf.Clamp(baseDistance * spreadFactor, baseDistance, maxSpread);
        else
            targetSpread = baseDistance;

        // Smoothly interpolate current spread
        currentSpread = Mathf.Lerp(currentSpread, targetSpread, Time.deltaTime * transitionSpeed);

        // Apply spread to line positions
        if (lineTransforms[0] != null) // TOP
            lineTransforms[0].anchoredPosition = new Vector2(0, currentSpread);

        if (lineTransforms[1] != null) // RIGHT
            lineTransforms[1].anchoredPosition = new Vector2(currentSpread, 0);

        if (lineTransforms[2] != null) // BOTTOM
            lineTransforms[2].anchoredPosition = new Vector2(0, -currentSpread);

        if (lineTransforms[3] != null) // LEFT
            lineTransforms[3].anchoredPosition = new Vector2(-currentSpread, 0);

        // Update crosshair visibility when aiming
        if (crosshairGroup != null && currentWeaponData.hideCrosshairWhenAiming)
        {
            float targetAlpha = isAiming ? aimAlpha : 1.0f;
            crosshairGroup.alpha = Mathf.Lerp(crosshairGroup.alpha, targetAlpha,
                                             Time.deltaTime * transitionSpeed);
        }
    }

    // Public method to flash crosshair when hitting targets
    public void SetCrosshairColor(Color color, float duration = 0.1f)
    {
        StartCoroutine(FlashColorRoutine(color, duration));
    }

    private IEnumerator FlashColorRoutine(Color color, float duration)
    {
        // Store original color
        Color originalColor = currentWeaponData != null ?
                             currentWeaponData.crosshairColor : crosshairColor;

        // Set flash color
        SetAllCrosshairColors(color);

        // Wait for duration
        yield return new WaitForSeconds(duration);

        // Restore original color
        SetAllCrosshairColors(originalColor);
    }

    private void SetAllCrosshairColors(Color color)
    {
        if (centerDot != null)
            centerDot.color = color;

        foreach (Image line in crosshairLines)
        {
            if (line != null)
                line.color = color;
        }
    }

    private void OnDestroy()
    {
        if (weaponManager != null)
            weaponManager.OnWeaponChanged -= OnWeaponChanged;
    }
}