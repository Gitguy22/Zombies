using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerPointsDisplay : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Assign the PlayerPointsData scriptable object here")]
    [SerializeField] PlayerPointsData pointsData;

    [Header("Display Settings")]
    [Tooltip("Which player's points to display")]
    [SerializeField] PlayerNumber playerNumber;
    [SerializeField] TextMeshProUGUI pointsText;

    [Header("Popup Settings")]
    [SerializeField] GameObject pointsPopupPrefab;
    [SerializeField] Transform popupParent;
    [SerializeField] Color popupColor = Color.red;
    [SerializeField] float popupFontSizeMultiplier = 0.8f;

    [Header("Animation Settings")]
    [SerializeField] float shootDistance = 100f;
    [SerializeField] float coneAngle = 30f;
    [SerializeField] float animationDuration = 1.5f;
    [SerializeField] AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);

    public enum PlayerNumber
    {
        Player1 = 0,
        Player2 = 1,
        Player3 = 2,
        Player4 = 3
    }

    private int lastPoints = -1;
    private float baseFontSize;

    void Start()
    {
        if (pointsText == null)
        {
            pointsText = GetComponent<TextMeshProUGUI>();
            if (pointsText == null)
            {
                Debug.LogError("No TextMeshProUGUI component found on " + gameObject.name);
                enabled = false;
                return;
            }
        }

        if (pointsData == null)
        {
            Debug.LogError("No PlayerPointsData assigned to " + gameObject.name);
            enabled = false;
            return;
        }

        // Store base font size for popups
        baseFontSize = pointsText.fontSize;

        // Set popup parent if not assigned
        if (popupParent == null)
            popupParent = transform;

        // Create popup prefab if not assigned
        if (pointsPopupPrefab == null)
            CreatePopupPrefab();

        // Initial update
        UpdatePointsText();
    }

    void Update()
    {
        UpdatePointsText();
    }

    void UpdatePointsText()
    {
        int currentPoints = GetPlayerPoints();

        // Check if points changed
        if (currentPoints != lastPoints && lastPoints != -1)
        {
            int pointsDifference = currentPoints - lastPoints;

            // Only show popup for point gains
            if (pointsDifference > 0)
            {
                CreatePointsPopup(pointsDifference);
            }
        }

        // Update display text if points changed
        if (currentPoints != lastPoints)
        {
            lastPoints = currentPoints;
            pointsText.text = currentPoints.ToString();
        }
    }

    void CreatePointsPopup(int pointsGained)
    {
        // Instantiate popup
        GameObject popup = Instantiate(pointsPopupPrefab, popupParent);
        TextMeshProUGUI popupText = popup.GetComponent<TextMeshProUGUI>();

        // Configure popup text
        popupText.text = "+" + pointsGained.ToString();
        popupText.color = popupColor;
        popupText.fontSize = baseFontSize * popupFontSizeMultiplier;

        // Position popup at the main text location
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.position = pointsText.rectTransform.position;

        // Start animation
        StartCoroutine(AnimatePopup(popup, popupRect, popupText));
    }

    IEnumerator AnimatePopup(GameObject popup, RectTransform popupRect, TextMeshProUGUI popupText)
    {
        // Calculate random direction within cone
        float randomAngle = Random.Range(-coneAngle * 0.5f, coneAngle * 0.5f);
        Vector3 direction = Quaternion.Euler(0, 0, randomAngle) * Vector3.right;

        Vector3 startPos = popupRect.anchoredPosition;
        Vector3 endPos = startPos + direction * shootDistance;

        Color startColor = popupText.color;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            float t = elapsed / animationDuration;

            // Move popup along curve
            float moveProgress = movementCurve.Evaluate(t);
            popupRect.anchoredPosition = Vector3.Lerp(startPos, endPos, moveProgress);

            // Fade out popup
            float fadeProgress = fadeCurve.Evaluate(t);
            Color currentColor = startColor;
            currentColor.a = fadeProgress;
            popupText.color = currentColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Destroy popup
        Destroy(popup);
    }

    void CreatePopupPrefab()
    {
        // Create popup prefab at runtime
        GameObject prefab = new GameObject("PointsPopup");

        // Add RectTransform
        RectTransform rect = prefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);

        // Add TextMeshPro component
        TextMeshProUGUI text = prefab.AddComponent<TextMeshProUGUI>();
        text.text = "+0";
        text.fontSize = baseFontSize * popupFontSizeMultiplier;
        text.color = popupColor;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;

        pointsPopupPrefab = prefab;
    }

    int GetPlayerPoints()
    {
        switch (playerNumber)
        {
            case PlayerNumber.Player1:
                return pointsData.Player1Points;
            case PlayerNumber.Player2:
                return pointsData.Player2Points;
            case PlayerNumber.Player3:
                return pointsData.Player3Points;
            case PlayerNumber.Player4:
                return pointsData.Player4Points;
            default:
                return 0;
        }
    }

    // Public methods for runtime configuration
    public void SetPointsData(PlayerPointsData newPointsData)
    {
        pointsData = newPointsData;
        UpdatePointsText();
    }

    public void SetPlayerNumber(PlayerNumber player)
    {
        playerNumber = player;
        UpdatePointsText();
    }

    public void SetPlayerNumber(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex <= 3)
        {
            playerNumber = (PlayerNumber)playerIndex;
            UpdatePointsText();
        }
    }
}