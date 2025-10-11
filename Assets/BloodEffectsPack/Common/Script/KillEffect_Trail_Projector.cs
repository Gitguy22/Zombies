using System.Collections;
using UnityEngine;

namespace BloodEffectsPack
{
    public class KillEffect_Trail_Projector : MonoBehaviour
    {
        private void Start()
        {
            // Start the coroutine that checks for children every 3 seconds.
            StartCoroutine(CheckForChildrenAndDestroy());
        }

        private IEnumerator CheckForChildrenAndDestroy()
        {
            while (true)
            {
                yield return new WaitForSeconds(3f);

                // Check if the GameObject has no children.
                if (transform.childCount == 0)
                {
                    // Destroy this GameObject if it has no children.
                    Destroy(gameObject);
                    // Exit the coroutine to avoid running further checks.
                    yield break;
                }
            }
        }
    }
}