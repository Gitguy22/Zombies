using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoxWeapon
{
    public WeaponData weaponData;
    [Range(0, 100)]
    public float probability = 10f;
}

public class MysteryBox : MonoBehaviour, IInteractable
{
    [Header("Box Configuration")]
    [SerializeField] BoxWeapon[] availableWeapons;
    [SerializeField] WeaponData toyMusicBoxWeapon;
    [SerializeField] int cost = 950;
    [SerializeField] string displayName = "Mystery Box";

    [Header("Locations")]
    [SerializeField] GameObject[] startingLocations;
    [SerializeField] GameObject[] allLocations;

    [Header("Transforms")]
    [SerializeField] Transform itemSpawnPoint;

    [Header("Timing Settings")]
    [SerializeField] float weaponCycleSpeed = 0.2f;
    [SerializeField] float cyclingDuration = 3f;
    [SerializeField] float pickupTimeLimit = 10f;
    [SerializeField] float boxCooldown = 5f;

    [Header("Animation Settings")]
    [SerializeField] float weaponRiseHeight = 1f;
    [SerializeField] float riseSpeed = 2f;
    [SerializeField] float boxRiseHeight = 5f;

    [Header("Toy Box Settings")]
    [SerializeField] int minUsesForToyBox = 3;
    [SerializeField] int maxUsesForToyBox = 7;

    [Header("Audio")]
    [SerializeField] AudioClip purchaseSound;
    [SerializeField] AudioClip cyclingSound;
    [SerializeField] AudioClip revealSound;
    [SerializeField] AudioClip toyBoxLaughSound;

    public enum BoxState
    {
        Ready,
        Opening,
        Cycling,
        WeaponReady,
        Closing,
        Cooldown
    }

    private BoxState currentState = BoxState.Ready;
    private Animator animator;
    private AudioSource audioSource;
    private PointManager pointManager;

    private GameObject currentLocationModel;
    private GameObject displayedWeapon;
    private GameObject authorizedPlayer;
    private WeaponData selectedWeapon;

    private int useCount = 0;
    private int toyBoxTargetUses;
    private bool hasMovedFromStart = false;

    void Start()
    {
        InitializeComponents();
        SetRandomToyBoxTarget();

        // Ensure we have a parent transform for positioning
        if (transform.parent == null)
        {
            Debug.LogError("MysteryBox must be a child object with a parent for positioning!");
            enabled = false;
            return;
        }

        SpawnAtRandomLocation();
        SetState(BoxState.Ready);
    }

    void InitializeComponents()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        pointManager = FindObjectOfType<PointManager>();

        if (pointManager == null)
        {
            Debug.LogError("PointManager not found in scene");
        }

        if (itemSpawnPoint == null)
        {
            Debug.LogError("Item Spawn Point not assigned");
        }
    }

    void SetRandomToyBoxTarget()
    {
        toyBoxTargetUses = Random.Range(minUsesForToyBox, maxUsesForToyBox + 1);
    }

    void SpawnAtRandomLocation()
    {
        GameObject[] availableLocations;

        // First time: use starting locations only
        if (!hasMovedFromStart)
        {
            availableLocations = startingLocations;
        }
        else
        {
            availableLocations = allLocations;
        }

        if (availableLocations == null || availableLocations.Length == 0)
        {
            Debug.LogError("No locations available for spawning");
            return;
        }

        // Re-enable previous location if it exists
        if (currentLocationModel != null)
        {
            currentLocationModel.SetActive(true);
        }

        // Select random location (exclude current location if there are multiple options)
        GameObject newLocation;
        int attempts = 0;
        do
        {
            int locationIndex = Random.Range(0, availableLocations.Length);
            newLocation = availableLocations[locationIndex];
            attempts++;
        }
        while (newLocation == currentLocationModel && availableLocations.Length > 1 && attempts < 10);

        currentLocationModel = newLocation;

        // Copy complete transform from location
        Transform parentTransform = transform.parent;
        Transform locationTransform = currentLocationModel.transform;

        parentTransform.position = locationTransform.position;
        parentTransform.rotation = locationTransform.rotation;
        currentLocationModel.SetActive(false);

        // Mark that we've moved from start after first relocation
        if (hasMovedFromStart == false && useCount > 0)
        {
            hasMovedFromStart = true;
        }
    }

    void SetState(BoxState newState)
    {
        currentState = newState;

        if (animator != null)
        {
            bool shouldBeOpen = newState == BoxState.Opening ||
                               newState == BoxState.Cycling ||
                               newState == BoxState.WeaponReady;
            animator.SetBool("IsOpen", shouldBeOpen);
        }
    }

    #region IInteractable Implementation

    public void BuyItem(GameObject player)
    {
        if (currentState != BoxState.Ready)
        {
            return;
        }

        if (pointManager == null || pointManager.GetPoints(player) < cost)
        {
            return;
        }

        pointManager.TakePoints(player, cost);
        authorizedPlayer = player;
        useCount++;

        PlaySound(purchaseSound);
        StartCoroutine(BoxSequence());
    }

    public string GetItemName()
    {
        if (currentState == BoxState.Ready)
        {
            return $"Use {displayName} for {cost} points";
        }
        return "";
    }

    public int GetCost()
    {
        return currentState == BoxState.Ready ? cost : 0;
    }

    public bool IsPaidFor()
    {
        return currentState != BoxState.Ready;
    }

    #endregion

    IEnumerator BoxSequence()
    {
        SetState(BoxState.Opening);
        yield return new WaitForSeconds(0.5f);

        selectedWeapon = SelectWeapon();

        if (IsToyMusicBox(selectedWeapon))
        {
            yield return StartCoroutine(HandleToyMusicBox());
            yield break;
        }

        SetState(BoxState.Cycling);
        yield return StartCoroutine(CycleWeapons());

        SetState(BoxState.WeaponReady);
        ShowFinalWeapon();
        PlaySound(revealSound);

        yield return StartCoroutine(WeaponPickupTimer());
    }

    WeaponData SelectWeapon()
    {
        // Check if toy music box should appear
        if (useCount >= toyBoxTargetUses && toyMusicBoxWeapon != null)
        {
            return toyMusicBoxWeapon;
        }

        // Get player's current weapons
        List<string> playerWeaponIDs = GetPlayerWeaponIDs(authorizedPlayer);

        // Filter available weapons (exclude ones player already has)
        List<BoxWeapon> eligibleWeapons = new List<BoxWeapon>();
        foreach (BoxWeapon weapon in availableWeapons)
        {
            if (weapon.weaponData != null && !playerWeaponIDs.Contains(weapon.weaponData.weaponID))
            {
                eligibleWeapons.Add(weapon);
            }
        }

        if (eligibleWeapons.Count == 0)
        {
            // Player has all weapons, return toy music box to trigger relocation
            return toyMusicBoxWeapon;
        }

        // Select weapon based on probability
        float totalProbability = 0f;
        foreach (BoxWeapon weapon in eligibleWeapons)
        {
            totalProbability += weapon.probability;
        }

        float randomValue = Random.Range(0f, totalProbability);
        float currentProbability = 0f;

        foreach (BoxWeapon weapon in eligibleWeapons)
        {
            currentProbability += weapon.probability;
            if (randomValue <= currentProbability)
            {
                return weapon.weaponData;
            }
        }

        return eligibleWeapons[0].weaponData;
    }

    List<string> GetPlayerWeaponIDs(GameObject player)
    {
        List<string> weaponIDs = new List<string>();

        WeaponManager weaponManager = player.GetComponent<WeaponManager>();
        if (weaponManager == null) return weaponIDs;

        List<GameObject> equippedWeapons = weaponManager.GetEquippedWeapons();
        foreach (GameObject weaponObj in equippedWeapons)
        {
            if (weaponObj == null) continue;

            WeaponPickup pickup = weaponObj.GetComponent<WeaponPickup>();
            if (pickup != null && pickup.weaponData != null)
            {
                weaponIDs.Add(pickup.weaponData.weaponID);
                continue;
            }

            Weapon weapon = weaponObj.GetComponent<Weapon>();
            if (weapon != null && weapon.weaponData != null)
            {
                weaponIDs.Add(weapon.weaponData.weaponID);
            }
        }

        return weaponIDs;
    }

    IEnumerator CycleWeapons()
    {
        PlayLoopingSound(cyclingSound);

        int weaponIndex = 0;
        float elapsedTime = 0f;

        while (elapsedTime < cyclingDuration)
        {
            // Show current weapon in cycle
            if (availableWeapons.Length > 0)
            {
                WeaponData currentWeapon = availableWeapons[weaponIndex].weaponData;
                ShowCycleWeapon(currentWeapon, elapsedTime / cyclingDuration);

                weaponIndex = (weaponIndex + 1) % availableWeapons.Length;
            }

            yield return new WaitForSeconds(weaponCycleSpeed);
            elapsedTime += weaponCycleSpeed;
        }

        audioSource.Stop();
        CleanupDisplayWeapon();
    }

    void ShowCycleWeapon(WeaponData weaponData, float riseProgress)
    {
        if (weaponData == null || weaponData.weaponPrefab == null) return;

        CleanupDisplayWeapon();

        displayedWeapon = Instantiate(weaponData.weaponPrefab);

        // Remove interactive components during cycling
        DestroyComponent<WeaponPickup>(displayedWeapon);
        DestroyComponent<Weapon>(displayedWeapon);

        // Calculate position based on current spawn point position
        Vector3 startPos = itemSpawnPoint.position;
        Vector3 targetPos = itemSpawnPoint.position + Vector3.up * weaponRiseHeight;
        Vector3 currentPosition = Vector3.Lerp(startPos, targetPos, riseProgress);

        displayedWeapon.transform.position = currentPosition;
        displayedWeapon.transform.rotation = itemSpawnPoint.rotation;

        // Add visual rotation
        RotateWeapon rotator = displayedWeapon.GetComponent<RotateWeapon>();
        if (rotator == null)
        {
            rotator = displayedWeapon.AddComponent<RotateWeapon>();
        }
    }

    void ShowFinalWeapon()
    {
        if (selectedWeapon == null || selectedWeapon.weaponPrefab == null) return;

        CleanupDisplayWeapon();

        displayedWeapon = Instantiate(selectedWeapon.weaponPrefab);

        // For toy music box, make it a child of spawn point so it follows during animation
        if (IsToyMusicBox(selectedWeapon))
        {
            displayedWeapon.transform.SetParent(itemSpawnPoint);
            displayedWeapon.transform.localPosition = Vector3.zero;
            displayedWeapon.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Regular weapons positioned at raised height
            Vector3 targetPosition = itemSpawnPoint.position + Vector3.up * weaponRiseHeight;
            displayedWeapon.transform.position = targetPosition;
            displayedWeapon.transform.rotation = itemSpawnPoint.rotation;
        }

        // Ensure it has WeaponPickup component for player interaction
        WeaponPickup pickup = displayedWeapon.GetComponent<WeaponPickup>();
        if (pickup == null)
        {
            pickup = displayedWeapon.AddComponent<WeaponPickup>();
        }
        pickup.weaponData = selectedWeapon;

        // Add visual effects
        RotateWeapon rotator = displayedWeapon.GetComponent<RotateWeapon>();
        if (rotator == null)
        {
            rotator = displayedWeapon.AddComponent<RotateWeapon>();
        }
    }

    IEnumerator WeaponPickupTimer()
    {
        float timeRemaining = pickupTimeLimit;

        // Calculate positions fresh at start of timer
        Vector3 startPos = itemSpawnPoint.position;
        Vector3 targetPos = itemSpawnPoint.position + Vector3.up * weaponRiseHeight;

        while (timeRemaining > 0f && displayedWeapon != null)
        {
            // Move weapon down as time decreases
            float progress = timeRemaining / pickupTimeLimit;
            Vector3 currentPosition = Vector3.Lerp(startPos, targetPos, progress);

            if (displayedWeapon != null)
            {
                displayedWeapon.transform.position = currentPosition;
            }

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        // Time expired or weapon was picked up
        CleanupDisplayWeapon();
        yield return StartCoroutine(CloseBox());
    }

    IEnumerator HandleToyMusicBox()
    {
        SetState(BoxState.WeaponReady);

        // Reset toy box counter
        useCount = 0;
        SetRandomToyBoxTarget();

        // Show toy music box briefly
        ShowFinalWeapon();
        yield return new WaitForSeconds(1f);

        // Shake and rise animation with laugh
        StartCoroutine(ShakeAndRiseAnimation());
        PlayGlobalSound(toyBoxLaughSound);

        if (toyBoxLaughSound != null)
        {
            yield return new WaitForSeconds(toyBoxLaughSound.length);
        }

        CleanupDisplayWeapon();

        // Mark that we've moved from start since toy box triggers relocation
        hasMovedFromStart = true;
        SpawnAtRandomLocation();

        yield return StartCoroutine(CloseBox());
    }

    IEnumerator ShakeAndRiseAnimation()
    {
        Vector3 originalPosition = transform.parent.position;
        float shakeDuration = 2f;
        float shakeIntensity = 0.3f;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;

            // Shake effect
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeIntensity, shakeIntensity),
                0f,
                Random.Range(-shakeIntensity, shakeIntensity)
            );

            // Rise effect
            Vector3 riseOffset = Vector3.up * (progress * boxRiseHeight);

            // Move box parent (spawn point will follow as child)
            transform.parent.position = originalPosition + shakeOffset + riseOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Position will be set to new location when SpawnAtRandomLocation is called
    }

    IEnumerator CloseBox()
    {
        SetState(BoxState.Closing);
        yield return new WaitForSeconds(0.5f);

        SetState(BoxState.Cooldown);
        yield return new WaitForSeconds(boxCooldown);

        authorizedPlayer = null;
        SetState(BoxState.Ready);
    }

    bool IsToyMusicBox(WeaponData weapon)
    {
        return toyMusicBoxWeapon != null && weapon != null &&
               weapon.weaponID == toyMusicBoxWeapon.weaponID;
    }

    void CleanupDisplayWeapon()
    {
        if (displayedWeapon != null)
        {
            Destroy(displayedWeapon);
            displayedWeapon = null;
        }
    }

    void DestroyComponent<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component != null)
        {
            Destroy(component);
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void PlayLoopingSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void PlayGlobalSound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, 1f);
        }
    }
}

public class RotateWeapon : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 45f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}