using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BloodEffectsPack
{
    public class ToggleRepeat : MonoBehaviour
    {
        public float toggleDuration = 2.0f;
        public GameObject[] targetGameObjects;

        void Start()
        {
            toggleCoroutine = StartCoroutine(Toggle());
        }

        // Update is called once per frame
        void Update()
        {

        }

        private Coroutine toggleCoroutine;
        private IEnumerator Toggle()
        {
            while(true)
            {
                for(int i = 0; i < targetGameObjects.Length; i++)
                {
                    targetGameObjects[i].SetActive(!targetGameObjects[i].activeSelf);
                }
                yield return new WaitForSeconds(toggleDuration);
            }
        }

        private void OnDestroy()
        {
            if (toggleCoroutine != null)
                StopCoroutine(toggleCoroutine);
        }
    }

}
