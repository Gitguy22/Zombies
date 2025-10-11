using UnityEngine;
using TMPro;

public class RoundCounterDisplay : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Assign the RoundData scriptable object here")]
    [SerializeField] private RoundData roundData;

    [Header("Display Settings")]
    [SerializeField] private TextMeshProUGUI roundText;

    private int lastRound = -1;

    private void Start()
    {
        if (roundText == null)
        {
            roundText = GetComponent<TextMeshProUGUI>();
            if (roundText == null)
            {
                Debug.LogError("No TextMeshProUGUI component found on " + gameObject.name);
                enabled = false;
                return;
            }
        }

        if (roundData == null)
        {
            Debug.LogError("No RoundData assigned to " + gameObject.name);
            enabled = false;
            return;
        }

        // Initial update
        UpdateRoundText();
    }

    private void Update()
    {
        UpdateRoundText();
    }

    private void UpdateRoundText()
    {
        int currentRound = roundData.CurrentRound;

        // Only update text if round has changed
        if (currentRound != lastRound)
        {
            lastRound = currentRound;
            roundText.text = currentRound.ToString();
        }
    }

    // Public method that can be called to assign the round data at runtime if needed
    public void SetRoundData(RoundData newRoundData)
    {
        roundData = newRoundData;
        UpdateRoundText();
    }
}