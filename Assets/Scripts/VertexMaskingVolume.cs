using UnityEngine;

namespace ZombieGame.Rendering
{
    public class VertexMaskingVolume : MonoBehaviour
    {
        [Header("Volume Settings")]
        [SerializeField] Vector3 volumeSize = Vector3.one;
        [SerializeField] bool isActive = true;

        [Header("Precision Settings")]
        [SerializeField] float maskingFalloff = 0.02f;
        [SerializeField] bool useEllipsoidMasking = true;
        [SerializeField] bool limitToFrontFacing = true;
        [SerializeField] float frontFacingThreshold = 0.3f;
        [SerializeField] float maxMaskingDistance = 0.2f;
        [SerializeField] bool excludeBackVertices = true;

        [Header("References (Auto-assigned if null)")]
        [SerializeField] Camera targetCamera;
        [SerializeField] Renderer targetRenderer;

        [Header("Visual Debug")]
        [SerializeField] bool showGizmos = true;
        [SerializeField] bool showDebugInfo = false;
        [SerializeField] Color gizmoColor = new Color(1, 0, 0, 0.3f);

        Material originalMaterial;
        Material maskingMaterial;
        Shader maskingShader;

        Vector3 lastPosition;
        Quaternion lastRotation;
        Vector3 lastScale;

        void Start()
        {
            InitializeReferences();
            SetupMaskingMaterial();

            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastScale = transform.localScale;
        }

        void InitializeReferences()
        {
            maskingShader = Resources.Load<Shader>("Shaders/VertexMasking");
            if (maskingShader == null)
            {
                maskingShader = Shader.Find("Custom/VertexMasking");
                if (maskingShader == null)
                {
                    Debug.LogError("VertexMasking shader not found! Place shader in Resources/Shaders/ folder.");
                    return;
                }
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    targetCamera = FindObjectOfType<Camera>();
                }
                if (targetCamera == null)
                {
                    Transform playerRoot = transform.root;
                    targetCamera = playerRoot.GetComponentInChildren<Camera>();
                }
            }

            if (targetRenderer == null)
            {
                Transform playerRoot = transform.root;
                targetRenderer = playerRoot.GetComponentInChildren<SkinnedMeshRenderer>();
                if (targetRenderer == null)
                {
                    targetRenderer = playerRoot.GetComponentInChildren<MeshRenderer>();
                }
            }

            if (targetCamera == null)
            {
                Debug.LogError($"[{gameObject.name}] No camera found! Assign manually or ensure Camera.main exists.");
            }
            if (targetRenderer == null)
            {
                Debug.LogError($"[{gameObject.name}] No renderer found! Assign manually or check hierarchy.");
            }
        }

        void SetupMaskingMaterial()
        {
            if (targetRenderer == null || maskingShader == null) return;

            originalMaterial = targetRenderer.material;
            maskingMaterial = new Material(maskingShader);

            if (originalMaterial.HasProperty("_MainTex"))
            {
                maskingMaterial.SetTexture("_MainTex", originalMaterial.GetTexture("_MainTex"));
            }
            if (originalMaterial.HasProperty("_Color"))
            {
                maskingMaterial.SetColor("_Color", originalMaterial.GetColor("_Color"));
            }

            targetRenderer.material = maskingMaterial;

            Debug.Log($"[{gameObject.name}] Masking material setup complete!");

            ForceUpdateShaderProperties();
        }

        void Update()
        {
            if (maskingMaterial != null && targetCamera != null)
            {
                bool transformChanged =
                    Vector3.Distance(transform.position, lastPosition) > 0.001f ||
                    Quaternion.Angle(transform.rotation, lastRotation) > 0.1f ||
                    Vector3.Distance(transform.localScale, lastScale) > 0.001f;

                if (transformChanged || Time.frameCount % 5 == 0)
                {
                    ForceUpdateShaderProperties();

                    lastPosition = transform.position;
                    lastRotation = transform.rotation;
                    lastScale = transform.localScale;
                }
            }
        }

        void ForceUpdateShaderProperties()
        {
            if (maskingMaterial == null) return;

            Vector3 worldCenter = transform.position;
            Vector3 worldSize = Vector3.Scale(volumeSize, transform.lossyScale);

            // Basic properties
            maskingMaterial.SetVector("_VolumeCenter", worldCenter);
            maskingMaterial.SetVector("_VolumeSize", worldSize);
            maskingMaterial.SetFloat("_MaskingActive", isActive ? 1f : 0f);

            // Precision properties
            maskingMaterial.SetFloat("_MaskingFalloff", maskingFalloff);
            maskingMaterial.SetFloat("_UseEllipsoidMasking", useEllipsoidMasking ? 1f : 0f);
            maskingMaterial.SetFloat("_LimitToFrontFacing", limitToFrontFacing ? 1f : 0f);
            maskingMaterial.SetFloat("_FrontFacingThreshold", frontFacingThreshold);
            maskingMaterial.SetFloat("_MaxMaskingDistance", maxMaskingDistance);
            maskingMaterial.SetFloat("_ExcludeBackVertices", excludeBackVertices ? 1f : 0f);

            if (targetCamera != null)
            {
                maskingMaterial.SetVector("_CameraPosition", targetCamera.transform.position);
                maskingMaterial.SetVector("_CameraForward", targetCamera.transform.forward);
            }
        }

        // Public methods for precision control
        public void SetMaskingFalloff(float falloff)
        {
            maskingFalloff = falloff;
            ForceUpdateShaderProperties();
        }

        public void SetUseEllipsoidMasking(bool useEllipsoid)
        {
            useEllipsoidMasking = useEllipsoid;
            ForceUpdateShaderProperties();
        }

        public void SetLimitToFrontFacing(bool limit, float threshold)
        {
            limitToFrontFacing = limit;
            frontFacingThreshold = threshold;
            ForceUpdateShaderProperties();
        }

        public void SetMaxMaskingDistance(float maxDistance)
        {
            maxMaskingDistance = maxDistance;
            ForceUpdateShaderProperties();
        }

        public void SetExcludeBackVertices(bool exclude)
        {
            excludeBackVertices = exclude;
            ForceUpdateShaderProperties();
        }

        public void SetVolumeSize(Vector3 size)
        {
            volumeSize = size;
            ForceUpdateShaderProperties();
        }

        public void SetMaskingActive(bool active)
        {
            isActive = active;
            ForceUpdateShaderProperties();
        }

        public void SetTargetRenderer(Renderer renderer)
        {
            if (targetRenderer != renderer)
            {
                if (targetRenderer != null && originalMaterial != null)
                {
                    targetRenderer.material = originalMaterial;
                }

                targetRenderer = renderer;
                SetupMaskingMaterial();
            }
        }

        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            ForceUpdateShaderProperties();
        }

        public bool IsValid()
        {
            return targetCamera != null && targetRenderer != null && maskingShader != null && maskingMaterial != null;
        }

        void OnDestroy()
        {
            if (targetRenderer != null && originalMaterial != null)
            {
                targetRenderer.material = originalMaterial;
            }

            if (maskingMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(maskingMaterial);
                }
                else
                {
                    DestroyImmediate(maskingMaterial);
                }
            }
        }

        void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            Gizmos.color = gizmoColor;
            if (useEllipsoidMasking)
            {
                Gizmos.DrawSphere(Vector3.zero, Mathf.Max(volumeSize.x, volumeSize.y, volumeSize.z) * 0.5f);
            }
            else
            {
                Gizmos.DrawCube(Vector3.zero, volumeSize);
            }

            Gizmos.color = Color.red;
            if (useEllipsoidMasking)
            {
                Gizmos.DrawWireSphere(Vector3.zero, Mathf.Max(volumeSize.x, volumeSize.y, volumeSize.z) * 0.5f);
            }
            else
            {
                Gizmos.DrawWireCube(Vector3.zero, volumeSize);
            }

            Gizmos.matrix = oldMatrix;
        }
    }
}