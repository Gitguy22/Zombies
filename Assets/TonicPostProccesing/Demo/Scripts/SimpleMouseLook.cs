using UnityEngine;
using Tonic.PostProcessing;

namespace Tonic.PostProcessingDemo
{
    public class OrbitCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 5f;
        public float minDistance = 2f;
        public float maxDistance = 15f;
        public float zoomSpeed = 2f;
        public float orbitSpeed = 3f;
        public float verticalClamp = 80f;

        private float rotationX;
        private float rotationY;
        private TonicPostProcessing tonic;

        private bool effectsEnabled = true;

        void Start()
        {
            if (target == null)
                Debug.LogWarning("OrbitCamera: No target assigned.");

            Vector3 offset = transform.position - target.position;
            distance = offset.magnitude;

            Vector3 dir = offset.normalized;
            rotationX = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            rotationY = Mathf.Asin(dir.y) * Mathf.Rad2Deg;

            // Auto-find TonicPostProcessing on main camera
            tonic = GetComponent<TonicPostProcessing>();
            if (tonic == null)
                Debug.LogWarning("OrbitCamera: No TonicPostProcessing component found on main camera.");
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Orbit
            if (Input.GetMouseButton(1))
            {
                rotationX += Input.GetAxis("Mouse X") * orbitSpeed;
                rotationY -= Input.GetAxis("Mouse Y") * orbitSpeed;
                rotationY = Mathf.Clamp(rotationY, -verticalClamp, verticalClamp);
            }

            // Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            // Position
            float rotYRad = rotationY * Mathf.Deg2Rad;
            float rotXRad = rotationX * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                distance * Mathf.Sin(rotXRad) * Mathf.Cos(rotYRad),
                distance * Mathf.Sin(rotYRad),
                distance * Mathf.Cos(rotXRad) * Mathf.Cos(rotYRad)
            );

            transform.position = target.position + offset;
            transform.LookAt(target.position);

            // --- Toggle FX ---
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleAllTonicEffects();
            }
        }

        void ToggleAllTonicEffects()
        {
            if (tonic == null) return;

            effectsEnabled = !effectsEnabled;

            foreach (var effect in tonic.GetAllEffects())
            {
                effect.enabled = effectsEnabled;
            }
        }
    }
}
