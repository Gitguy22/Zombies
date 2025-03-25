using UnityEngine;

public class CameraStayStill : MonoBehaviour
{
    private PlayerLook playerLookScript;
    private Vector3 localPositionOffset;

    void Start()
    {
        // Find the PlayerLook script on the parent
        playerLookScript = GetComponentInParent<PlayerLook>();

        if (playerLookScript == null)
        {
            Debug.LogError("CameraAnimationFix: No PlayerLook script found on parent objects");
            return;
        }

        // Store the initial local position offset from the parent
        localPositionOffset = transform.localPosition;
    }

    void LateUpdate()
    {
        // We want to keep the rotation that PlayerLook sets
        // But reset the local position to ignore animation effects
        transform.localPosition = localPositionOffset;

        // The PlayerLook script already handles rotation with:
        // cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        // So we don't need to modify rotation here
    }
}