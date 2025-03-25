using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponWithProbability
{
    public GameObject weaponPrefab;
    [Range(0, 100)]
    public float probability = 0;
    public string weaponName; // Name of the weapon for display
}

public class MysteryBox : MonoBehaviour, IInteractable
{
    [Header("Available Weapons")]
    [SerializeField] WeaponWithProbability[] weaponsWithProbabilities;

    [Header("Box Settings")]
    [SerializeField] Transform itemTarget;
    public bool isPaidFor;
    public int cost;
    public string displayName = "Mystery Box";
    private string originalDisplayName;
    private int originalCost;

    [Header("Animation Settings")]
    [SerializeField] float weaponFlashInterval = 0.1f; // Time between weapon changes during flash
    [SerializeField] float moveUpDuration = 2.0f; // Time to move up to itemTarget
    [SerializeField] float revealDelay = 6.0f; // Time before revealing the chosen weapon
    [SerializeField] float moveDownDuration = 3.0f; // Time to move back down
    [SerializeField] float cooldownTime = 10.0f; // Cooldown before box can be used again
    [SerializeField] AudioClip weaponFlashingMusic; // Music that plays while weapons are flashing
    [SerializeField] AudioClip toyBoxLaughSound; // Laugh sound for toy music box

    [Header("Box Location Settings")]
    [SerializeField] GameObject[] possibleBoxLocations; // Possible box spawn locations
    [SerializeField] string toyMusicBoxItemName = "Toy Music Box"; // Name to check for special behavior

    private Animator anim;
    private AudioSource audio;
    private PointManager pointManager;
    private bool isAnimating = false;
    private bool isCoolingDown = false;
    private GameObject currentDisplayedWeapon;
    private Vector3 originalPosition;
    private GameObject currentBoxPlaceholder;
    private GameObject selectedWeaponInstance;

    void Start()
    {
        anim = GetComponent<Animator>();
        audio = GetComponent<AudioSource>();
        pointManager = FindObjectOfType<PointManager>();
        if (pointManager == null)
        {
            Debug.LogError("PointManager not found in scene!");
        }

        // Save original values
        originalDisplayName = displayName;
        originalCost = cost;
        originalPosition = transform.position;

        // Validate total probability equals 100%
        float totalProbability = 0;
        foreach (var weapon in weaponsWithProbabilities)
        {
            totalProbability += weapon.probability;
        }

        if (Mathf.Abs(totalProbability - 100f) > 0.01f)
        {
            Debug.LogWarning("MysteryBox: Total weapon probability is " + totalProbability + "%. It should be 100%.");
        }

        // Place the box at a random location if locations are specified
        if (possibleBoxLocations != null && possibleBoxLocations.Length > 0)
        {
            PlaceAtRandomLocation();
        }
    }

    void PlaceAtRandomLocation()
    {
        if (currentBoxPlaceholder != null)
        {
            // Re-enable the previous placeholder
            currentBoxPlaceholder.SetActive(true);
        }

        // Select a random placeholder
        int randomIndex = Random.Range(0, possibleBoxLocations.Length);
        currentBoxPlaceholder = possibleBoxLocations[randomIndex];

        // Move the box to the placeholder position
        transform.position = currentBoxPlaceholder.transform.position;
        transform.rotation = currentBoxPlaceholder.transform.rotation;

        // Disable the placeholder
        currentBoxPlaceholder.SetActive(false);
    }

    public void BuyItem(GameObject player)
    {
        if (!isPaidFor && !isAnimating && !isCoolingDown)
        {
            // Check if player has enough points
            if (pointManager != null && pointManager.GetPoints(player) >= cost)
            {
                // Deduct points
                pointManager.TakePoints(player, cost);
                isPaidFor = true;
                Debug.Log($"Player {player.name} bought {displayName} for {cost} points");

                // Start weapon selection animation
                StartCoroutine(WeaponSelectionAnimation(player));
            }
            else
            {
                Debug.Log($"Not enough points to use {displayName}!");
            }
        }
        else if (isPaidFor && selectedWeaponInstance != null)
        {
            // Player is picking up the weapon
            Debug.Log($"Player {player.name} picked up the weapon!");

            // Give the weapon to the player
            // For now, just destroy the displayed weapon
            Destroy(selectedWeaponInstance);
            selectedWeaponInstance = null;

            // Reset the box
            ResetBox();
        }
    }

    private IEnumerator WeaponSelectionAnimation(GameObject player)
    {
        isAnimating = true;

        // Open the box
        if (anim != null)
        {
            anim.SetTrigger("openBox");
            anim.SetBool("stayOpen", true);
        }

        if (audio != null)
        {
            audio.clip = weaponFlashingMusic;
            audio.Play();
        }

        // Select the weapon that will actually be given
        GameObject selectedWeaponPrefab = SelectRandomWeapon();
        string selectedWeaponName = GetWeaponName(selectedWeaponPrefab);
        bool isToyMusicBox = selectedWeaponName == toyMusicBoxItemName;

        // Start flashing weapons and moving up
        float elapsedTime = 0f;
        Vector3 startPos = transform.position + new Vector3(0, 1, 0); // Slightly above box

        // Create initial random weapon
        if (currentDisplayedWeapon != null)
            Destroy(currentDisplayedWeapon);

        currentDisplayedWeapon = Instantiate(GetRandomWeaponPrefab(), startPos, Quaternion.identity);

        // Move up while flashing
        while (elapsedTime < moveUpDuration)
        {
            // Flash weapons
            if (Time.time % weaponFlashInterval < 0.01f)
            {
                Destroy(currentDisplayedWeapon);
                currentDisplayedWeapon = Instantiate(GetRandomWeaponPrefab(), Vector3.Lerp(startPos, itemTarget.position, elapsedTime / moveUpDuration), Quaternion.identity);
            }

            // Move current weapon
            if (currentDisplayedWeapon != null)
            {
                currentDisplayedWeapon.transform.position = Vector3.Lerp(startPos, itemTarget.position, elapsedTime / moveUpDuration);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Continue flashing at target position
        elapsedTime = 0f;
        while (elapsedTime < revealDelay)
        {
            // Flash weapons at target position
            if (Time.time % weaponFlashInterval < 0.01f)
            {
                Destroy(currentDisplayedWeapon);
                currentDisplayedWeapon = Instantiate(GetRandomWeaponPrefab(), itemTarget.position, Quaternion.identity);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Stop flashing and show the real weapon
        Destroy(currentDisplayedWeapon);
        selectedWeaponInstance = Instantiate(selectedWeaponPrefab, itemTarget.position, Quaternion.identity);

        // Change display name and cost
        displayName = $"Pick up: {selectedWeaponName}";
        cost = 0;

        // Stop the music
        if (audio != null && audio.isPlaying)
        {
            audio.Stop();
        }

        // Special behavior for toy music box
        if (isToyMusicBox)
        {
            yield return new WaitForSeconds(1.0f);

            // Play laugh sound
            if (audio != null && toyBoxLaughSound != null)
            {
                audio.clip = toyBoxLaughSound;
                audio.Play();
            }

            // Close the box
            if (anim != null)
            {
                anim.SetBool("stayOpen", false);
                anim.SetTrigger("closeBox");
            }

            yield return new WaitForSeconds(0.5f);

            // Shake and rise
            float shakeDuration = 2.0f;
            elapsedTime = 0f;
            Vector3 originalBoxPos = transform.position;
            Vector3 targetRisePos = originalBoxPos + new Vector3(0, 3, 0);

            while (elapsedTime < shakeDuration)
            {
                float shakeIntensity = 0.2f;
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity)
                );

                transform.position = Vector3.Lerp(originalBoxPos, targetRisePos, elapsedTime / shakeDuration) + shakeOffset;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Move to a new location
            PlaceAtRandomLocation();

            // Reset the box
            ResetBox();
            isPaidFor = false;
            isAnimating = false;

            yield break;
        }

        // Wait for the player to pick up or timeout
        elapsedTime = 0f;
        while (elapsedTime < moveDownDuration && selectedWeaponInstance != null)
        {
            // Move the weapon back down
            if (selectedWeaponInstance != null)
            {
                selectedWeaponInstance.transform.position = Vector3.Lerp(
                    itemTarget.position,
                    startPos,
                    elapsedTime / moveDownDuration
                );
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // If weapon wasn't picked up, destroy it
        if (selectedWeaponInstance != null)
        {
            Destroy(selectedWeaponInstance);
            selectedWeaponInstance = null;
        }

        // Close the box
        if (anim != null)
        {
            anim.SetBool("stayOpen", false);
            anim.SetTrigger("closeBox");
        }

        // Reset the box
        ResetBox();

        // Start cooldown
        StartCoroutine(Cooldown());
    }

    private IEnumerator Cooldown()
    {
        isCoolingDown = true;
        yield return new WaitForSeconds(cooldownTime);
        isCoolingDown = false;
    }

    private void ResetBox()
    {
        // Reset display name and cost
        displayName = originalDisplayName;
        cost = originalCost;

        // Reset paid status
        isPaidFor = false;
        isAnimating = false;
    }

    private GameObject GetRandomWeaponPrefab()
    {
        int randomIndex = Random.Range(0, weaponsWithProbabilities.Length);
        return weaponsWithProbabilities[randomIndex].weaponPrefab;
    }

    private string GetWeaponName(GameObject weaponPrefab)
    {
        foreach (var weapon in weaponsWithProbabilities)
        {
            if (weapon.weaponPrefab == weaponPrefab)
            {
                return weapon.weaponName;
            }
        }
        return "Unknown Weapon";
    }

    private GameObject SelectRandomWeapon()
    {
        float randomValue = Random.Range(0f, 100f);
        float cumulativeProbability = 0f;

        foreach (var weapon in weaponsWithProbabilities)
        {
            cumulativeProbability += weapon.probability;
            if (randomValue <= cumulativeProbability)
            {
                return weapon.weaponPrefab;
            }
        }

        // Fallback in case of rounding errors
        if (weaponsWithProbabilities.Length > 0)
        {
            return weaponsWithProbabilities[weaponsWithProbabilities.Length - 1].weaponPrefab;
        }

        return null;
    }

    public string GetItemName()
    {
        return displayName;
    }

    public int GetCost()
    {
        return cost;
    }

    public bool IsPaidFor()
    {
        return isPaidFor;
    }
}