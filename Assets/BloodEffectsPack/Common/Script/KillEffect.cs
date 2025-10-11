using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BloodEffectsPack
{
    public class KillEffect : MonoBehaviour
    {
        public float minLifetime = 3.0f;
        public float maxLifetime = 5.0f;
        private float lifetime;
        private Coroutine currentCoroutine;


        void Start()
        {
            lifetime = Random.Range(minLifetime, maxLifetime);
            currentCoroutine = StartCoroutine(Kill());
        }

        private IEnumerator Kill()
        {
            yield return new WaitForSeconds(lifetime);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (currentCoroutine != null)
                StopCoroutine(currentCoroutine);
        }

     
    }
}
