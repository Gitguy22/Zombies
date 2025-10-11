using System.Collections;
using UnityEngine;

namespace BloodEffectsPack
{
    public class BloodGush : MonoBehaviour
    {
        [Header("Blood Gush Settings")]
        [SerializeField] float gushDuration = 3.0f;
        [SerializeField] float fadeOutDuration = 1.0f;
        [SerializeField] GameObject[] bloodGushObjects;

        [Header("Auto Start")]
        [SerializeField] bool startOnAwake = false;

        private Coroutine gushCoroutine;

        void Start()
        {
            if (startOnAwake)
            {
                StartBloodGush();
            }
        }

        public void StartBloodGush()
        {
            if (gushCoroutine != null)
            {
                StopCoroutine(gushCoroutine);
            }

            gushCoroutine = StartCoroutine(BloodGushSequence());
        }

        private IEnumerator BloodGushSequence()
        {
            // Activate all blood gush objects
            for (int i = 0; i < bloodGushObjects.Length; i++)
            {
                if (bloodGushObjects[i] != null)
                {
                    bloodGushObjects[i].SetActive(true);
                }
            }

            // Wait for gush duration
            yield return new WaitForSeconds(gushDuration);

            // Fade out phase
            float elapsedTime = 0f;

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = 1f - (elapsedTime / fadeOutDuration);

                // Apply fade to all blood objects with renderers
                for (int i = 0; i < bloodGushObjects.Length; i++)
                {
                    if (bloodGushObjects[i] != null)
                    {
                        ApplyFadeToObject(bloodGushObjects[i], alpha);
                    }
                }

                yield return null;
            }

            // Deactivate all blood gush objects
            for (int i = 0; i < bloodGushObjects.Length; i++)
            {
                if (bloodGushObjects[i] != null)
                {
                    bloodGushObjects[i].SetActive(false);
                    // Reset alpha back to 1 for next use
                    ApplyFadeToObject(bloodGushObjects[i], 1f);
                }
            }

            gushCoroutine = null;
        }

        private void ApplyFadeToObject(GameObject obj, float alpha)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color color = mat.color;
                        color.a = alpha;
                        mat.color = color;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (gushCoroutine != null)
            {
                StopCoroutine(gushCoroutine);
            }
        }
    }
}