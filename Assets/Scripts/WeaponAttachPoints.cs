using UnityEngine;

public class WeaponAttachPoints : MonoBehaviour
{
    [Header("Hand Attachment Points")]
    [SerializeField] private Transform leftHandAttach;
    [SerializeField] private Transform rightHandAttach;

    [Header("Visual Settings")]
    [SerializeField] private bool showGizmos = true;

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (leftHandAttach != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(leftHandAttach.position, 0.02f);
            Gizmos.DrawRay(leftHandAttach.position, leftHandAttach.forward * 0.1f);
        }

        if (rightHandAttach != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(rightHandAttach.position, 0.02f);
            Gizmos.DrawRay(rightHandAttach.position, rightHandAttach.forward * 0.1f);
        }
    }
}