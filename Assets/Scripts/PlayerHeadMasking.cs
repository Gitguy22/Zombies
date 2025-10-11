using UnityEngine;
using ZombieGame.Rendering;

namespace ZombieGame.Player
{
    public class PlayerHeadMasking : MonoBehaviour
    {
        [Header("Head Bone Reference")]
        [SerializeField] Transform headBone;
        [SerializeField] bool autoFindHeadBone = true;
        [SerializeField] string[] headBoneNames = { "Head", "head", "Head_Bone", "mixamorig:Head" };

        [Header("References (Auto-assigned if null)")]
        [SerializeField] Camera playerCamera;
        [SerializeField] SkinnedMeshRenderer characterRenderer;

        [Header("Eye Masking Settings")]
        [SerializeField] Vector3 eyeVolumeSize = new Vector3(0.15f, 0.08f, 0.1f);
        [SerializeField] Vector3 eyeVolumeLocalOffset = new Vector3(0, 0.05f, 0.08f);
        [SerializeField] Vector3 eyeVolumeLocalRotation = Vector3.zero;
        [SerializeField] bool enableOnStart = true;
        [SerializeField] bool followHeadBone = true;

        [Header("Masking Precision")]
        [SerializeField] float maskingFalloff = 0.02f; // Softness at edges
        [SerializeField] bool useEllipsoidMasking = true; // More precise than box
        [SerializeField] Vector3 ellipsoidRadii = new Vector3(0.075f, 0.04f, 0.05f);
        [SerializeField] bool limitToFrontFacing = true; // Only mask vertices facing camera
        [SerializeField] float frontFacingThreshold = 0.3f;

        [Header("Safety Limits")]
        [SerializeField] float maxMaskingDistance = 0.2f; // Max distance from camera to apply masking
        [SerializeField] bool excludeBackVertices = true; // Don't mask vertices behind the head bone
        [SerializeField] LayerMask affectedLayers = -1; // Which layers to affect

        [Header("Debug")]
        [SerializeField] bool showDebugInfo = true;
        [SerializeField] bool showGizmos = true;
        [SerializeField] bool showActualVolumePosition = true;
        [SerializeField] bool showMaskingBounds = true;

        VertexMaskingVolume maskingVolume;
        GameObject maskingVolumeObject;
        bool isSetup = false;

        void Start()
        {
            if (enableOnStart)
            {
                SetupEyeMasking();
            }
        }

        void Update()
        {
            if (maskingVolumeObject != null && headBone != null && followHeadBone)
            {
                ForceUpdatePosition();
            }
        }

        void SetupEyeMasking()
        {
            Debug.Log($"[{gameObject.name}] Starting eye masking setup...");

            FindReferences();

            if (!ValidateReferences())
            {
                Debug.LogError($"[{gameObject.name}] Cannot setup eye masking - missing references!");
                return;
            }

            CreateMaskingVolume();
            ConfigureMaskingVolume();
            isSetup = true;

            Debug.Log($"[{gameObject.name}] Eye masking setup COMPLETE with head bone: {headBone?.name}");
        }

        void FindReferences()
        {
            if (headBone == null && autoFindHeadBone)
            {
                headBone = FindHeadBone();
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null)
                {
                    playerCamera = Camera.main;
                }
                if (playerCamera == null)
                {
                    playerCamera = FindObjectOfType<Camera>();
                }
            }

            if (characterRenderer == null)
            {
                characterRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
                if (characterRenderer == null)
                {
                    characterRenderer = GetComponentInParent<SkinnedMeshRenderer>();
                }
            }
        }

        Transform FindHeadBone()
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>();

            foreach (string headName in headBoneNames)
            {
                foreach (Transform t in allTransforms)
                {
                    if (t.name.Equals(headName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"[{gameObject.name}] Found head bone: {t.name}");
                        return t;
                    }
                }
            }

            foreach (string headName in headBoneNames)
            {
                foreach (Transform t in allTransforms)
                {
                    if (t.name.ToLower().Contains(headName.ToLower()))
                    {
                        Debug.Log($"[{gameObject.name}] Found head bone (partial match): {t.name}");
                        return t;
                    }
                }
            }

            Debug.LogWarning($"[{gameObject.name}] Head bone not found automatically. Please assign manually.");
            return null;
        }

        bool ValidateReferences()
        {
            bool isValid = true;

            if (playerCamera == null)
            {
                Debug.LogError($"[{gameObject.name}] Player camera not found!");
                isValid = false;
            }

            if (characterRenderer == null)
            {
                Debug.LogError($"[{gameObject.name}] Character renderer not found!");
                isValid = false;
            }

            if (headBone == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Head bone not found! Masking will use fixed position.");
            }

            return isValid;
        }

        void CreateMaskingVolume()
        {
            Debug.Log($"[{gameObject.name}] Creating masking volume...");

            if (maskingVolumeObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(maskingVolumeObject);
                }
                else
                {
                    DestroyImmediate(maskingVolumeObject);
                }
            }

            maskingVolumeObject = new GameObject("EyeMaskingVolume");
            maskingVolumeObject.transform.SetParent(null, false);

            ForceUpdatePosition();

            maskingVolume = maskingVolumeObject.AddComponent<VertexMaskingVolume>();

            Debug.Log($"[{gameObject.name}] Masking volume created successfully!");
        }

        void ForceUpdatePosition()
        {
            if (maskingVolumeObject == null)
            {
                Debug.LogError($"[{gameObject.name}] Cannot update position - masking volume object is null!");
                return;
            }

            if (headBone != null && followHeadBone)
            {
                Vector3 worldPosition = headBone.TransformPoint(eyeVolumeLocalOffset);
                Quaternion worldRotation = headBone.rotation * Quaternion.Euler(eyeVolumeLocalRotation);

                maskingVolumeObject.transform.position = worldPosition;
                maskingVolumeObject.transform.rotation = worldRotation;
                maskingVolumeObject.transform.localScale = Vector3.one;
            }
            else
            {
                Vector3 fallbackPos = transform.position + Vector3.up * 1.7f + transform.forward * 0.1f;
                maskingVolumeObject.transform.position = fallbackPos;
                maskingVolumeObject.transform.rotation = transform.rotation * Quaternion.Euler(eyeVolumeLocalRotation);
                maskingVolumeObject.transform.localScale = Vector3.one;
            }
        }

        void ConfigureMaskingVolume()
        {
            if (maskingVolume == null)
            {
                Debug.LogError($"[{gameObject.name}] Cannot configure masking volume - component is null!");
                return;
            }

            // Configure the volume with precision settings
            maskingVolume.SetVolumeSize(useEllipsoidMasking ? ellipsoidRadii * 2f : eyeVolumeSize);
            maskingVolume.SetTargetCamera(playerCamera);
            maskingVolume.SetTargetRenderer(characterRenderer);

            // Set precision parameters
            maskingVolume.SetMaskingFalloff(maskingFalloff);
            maskingVolume.SetUseEllipsoidMasking(useEllipsoidMasking);
            maskingVolume.SetLimitToFrontFacing(limitToFrontFacing, frontFacingThreshold);
            maskingVolume.SetMaxMaskingDistance(maxMaskingDistance);
            maskingVolume.SetExcludeBackVertices(excludeBackVertices);

            if (!maskingVolume.IsValid())
            {
                Debug.LogWarning($"[{gameObject.name}] Masking volume setup may be incomplete!");
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Masking volume configured successfully!");
            }
        }

        public void EnableEyeMasking()
        {
            if (maskingVolume == null)
            {
                SetupEyeMasking();
            }
            else
            {
                maskingVolume.SetMaskingActive(true);
            }
        }

        public void DisableEyeMasking()
        {
            if (maskingVolume != null)
            {
                maskingVolume.SetMaskingActive(false);
            }
        }

        public void SetHeadBone(Transform newHeadBone)
        {
            headBone = newHeadBone;
            Debug.Log($"[{gameObject.name}] Head bone set to: {(headBone != null ? headBone.name : "NULL")}");
        }

        public void SetFollowHeadBone(bool follow)
        {
            followHeadBone = follow;
            Debug.Log($"[{gameObject.name}] Follow head bone set to: {follow}");
        }

        public void UpdateEyeSettings(Vector3 newSize, Vector3 newLocalOffset, Vector3 newLocalRotation)
        {
            eyeVolumeSize = newSize;
            eyeVolumeLocalOffset = newLocalOffset;
            eyeVolumeLocalRotation = newLocalRotation;

            if (maskingVolume != null)
            {
                maskingVolume.SetVolumeSize(useEllipsoidMasking ? ellipsoidRadii * 2f : eyeVolumeSize);
            }
        }

        public void UpdatePrecisionSettings(float newFalloff, bool useEllipsoid, Vector3 newEllipsoidRadii)
        {
            maskingFalloff = newFalloff;
            useEllipsoidMasking = useEllipsoid;
            ellipsoidRadii = newEllipsoidRadii;

            if (maskingVolume != null)
            {
                maskingVolume.SetMaskingFalloff(maskingFalloff);
                maskingVolume.SetUseEllipsoidMasking(useEllipsoidMasking);
                maskingVolume.SetVolumeSize(useEllipsoidMasking ? ellipsoidRadii * 2f : eyeVolumeSize);
            }
        }

        void OnDestroy()
        {
            if (maskingVolumeObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(maskingVolumeObject);
                }
                else
                {
                    DestroyImmediate(maskingVolumeObject);
                }
            }
        }

        void OnValidate()
        {
            if (eyeVolumeSize.x <= 0) eyeVolumeSize.x = 0.01f;
            if (eyeVolumeSize.y <= 0) eyeVolumeSize.y = 0.01f;
            if (eyeVolumeSize.z <= 0) eyeVolumeSize.z = 0.01f;

            if (ellipsoidRadii.x <= 0) ellipsoidRadii.x = 0.01f;
            if (ellipsoidRadii.y <= 0) ellipsoidRadii.y = 0.01f;
            if (ellipsoidRadii.z <= 0) ellipsoidRadii.z = 0.01f;

            if (Application.isPlaying && maskingVolume != null)
            {
                ConfigureMaskingVolume();
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            Vector3 gizmoPosition;
            Quaternion gizmoRotation;

            if (headBone != null && followHeadBone)
            {
                gizmoPosition = headBone.TransformPoint(eyeVolumeLocalOffset);
                gizmoRotation = headBone.rotation * Quaternion.Euler(eyeVolumeLocalRotation);
            }
            else
            {
                gizmoPosition = transform.position + Vector3.up * 1.7f + transform.forward * 0.1f;
                gizmoRotation = transform.rotation * Quaternion.Euler(eyeVolumeLocalRotation);
            }

            Matrix4x4 oldMatrix = Gizmos.matrix;

            // Draw masking volume
            Gizmos.matrix = Matrix4x4.TRS(gizmoPosition, gizmoRotation, Vector3.one);

            if (useEllipsoidMasking)
            {
                Gizmos.color = new Color(0, 1, 1, 0.2f);
                Gizmos.DrawSphere(Vector3.zero, Mathf.Max(ellipsoidRadii.x, ellipsoidRadii.y, ellipsoidRadii.z));
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(Vector3.zero, Mathf.Max(ellipsoidRadii.x, ellipsoidRadii.y, ellipsoidRadii.z));
            }
            else
            {
                Gizmos.color = new Color(0, 1, 1, 0.2f);
                Gizmos.DrawCube(Vector3.zero, eyeVolumeSize);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(Vector3.zero, eyeVolumeSize);
            }

            // Draw safety bounds
            if (showMaskingBounds)
            {
                Gizmos.color = new Color(1, 1, 0, 0.1f);
                Gizmos.DrawSphere(Vector3.zero, maxMaskingDistance);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(Vector3.zero, maxMaskingDistance);
            }

            // Draw actual volume position (red)
            if (showActualVolumePosition && maskingVolumeObject != null)
            {
                Gizmos.matrix = Matrix4x4.TRS(maskingVolumeObject.transform.position,
                                             maskingVolumeObject.transform.rotation,
                                             Vector3.one);
                Gizmos.color = new Color(1, 0, 0, 0.2f);
                if (useEllipsoidMasking)
                {
                    Gizmos.DrawSphere(Vector3.zero, Mathf.Max(ellipsoidRadii.x, ellipsoidRadii.y, ellipsoidRadii.z));
                }
                else
                {
                    Gizmos.DrawCube(Vector3.zero, eyeVolumeSize);
                }
            }

            Gizmos.matrix = oldMatrix;

            // Draw head bone reference
            if (headBone != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, headBone.position);
                Gizmos.DrawWireSphere(headBone.position, 0.03f);

                // Draw head bone axes
                Gizmos.color = Color.red;
                Gizmos.DrawRay(headBone.position, headBone.forward * 0.1f);
                Gizmos.color = Color.green;
                Gizmos.DrawRay(headBone.position, headBone.up * 0.1f);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(headBone.position, headBone.right * 0.1f);
            }
        }

        // Public getters
        public Transform GetHeadBone() => headBone;
        public bool IsSetup() => isSetup;
        public GameObject GetMaskingVolumeObject() => maskingVolumeObject;
        public bool IsFollowingHeadBone() => followHeadBone && headBone != null;
    }
}