using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BloodEffectsPack
{
    public class ProjectorSpawner : MonoBehaviour
    {
        [HideInInspector] public int ignoreLayerMask;
        public bool destroyAfter = true;
        public GameObject sourcePrefab;
        private GameObject currentPrefab;
        private Coroutine currentCoroutine;

        [Header("Loop")]
        public bool isLoop = false;
        public float loopingLifetime = Mathf.Infinity;
        private float loopingLifetimeCounter = 0.0f;

        [Header("Delay")]
        public float delay_min = 0.0f;
        public float delay_max = 0.0f;

        [Header("Lifetime")]

        public float lifetime_min = 1.0f;
        public float lifetime_max = 1.0f;
        private float lifetimeCounter = 0.0f;

        [Header("StartPos")]
        public Vector3 startPosOffset = Vector3.zero;

        [Header("Size")]
        public float startSize_min = 1.0f;
        public float startSize_max = 1.0f;

        [Header("Rotation")]
        public float startRotation_min = 0.0f;
        public float startRotation_max = 0.0f;

        [Header("CurveControl")]
        public AnimationCurve frameCurve;
        public AnimationCurve scaleCurve;
        public AnimationCurve opacityCurve;

  
        void OnEnable()
        {
            if (currentCoroutine != null)
                StopCoroutine(currentCoroutine);

            lifetimeCounter = 0.0f;
            loopingLifetimeCounter = 0.0f;

            if (currentPrefab != null)
                Destroy(currentPrefab.gameObject);

            currentCoroutine = StartCoroutine(Spawn());
            
        }

        public void ResetAndInitialize(int value)
        {
            if (currentCoroutine != null)
                StopCoroutine(currentCoroutine);

            lifetimeCounter = 0.0f;
            loopingLifetimeCounter = 0.0f;

            if (currentPrefab != null)
                Destroy(currentPrefab.gameObject);

            ignoreLayerMask = value;
            currentCoroutine = StartCoroutine(Spawn());
        }


        void Update()
        {

        }

        private IEnumerator Spawn()
        {
            float delay = Random.Range(delay_min, delay_max);
            float lifetime = Random.Range(lifetime_min, lifetime_max);
            float startProjectorSize = Random.Range(startSize_min, startSize_max);
            float startRotation = Random.Range(startRotation_min, startRotation_max);
            yield return new WaitForSeconds(delay);
            currentPrefab = Instantiate(sourcePrefab);

            Projector currentProjector = currentPrefab.GetComponent<Projector>();
            //toggle projector for sorting order
            currentProjector.enabled = false;
            currentProjector.enabled = true;

            currentProjector.material = Instantiate(currentProjector.material);
            currentProjector.ignoreLayers = ignoreLayerMask;
            currentPrefab.transform.SetParent(transform);


            currentPrefab.transform.localPosition = startPosOffset;


            currentPrefab.transform.localScale = Vector3.one;
            currentPrefab.transform.localEulerAngles = new Vector3(90f, 0f, 0.0f);
            currentPrefab.transform.RotateAround(transform.position, Vector3.up, startRotation);


            while (lifetimeCounter <= lifetime && loopingLifetimeCounter <= loopingLifetime)
            {
                lifetimeCounter += Time.deltaTime;
                loopingLifetimeCounter += Time.deltaTime;

                float samplePos = Mathf.Clamp01(lifetimeCounter / lifetime);

                float scaleValue = scaleCurve.Evaluate(samplePos);
                int frameValue = Mathf.FloorToInt(frameCurve.Evaluate(samplePos));
                float opacityValue = opacityCurve.Evaluate(samplePos);

                currentProjector.material.SetFloat("_Frame", frameValue);
                currentProjector.material.SetFloat("_Opacity", opacityValue);
                currentProjector.orthographicSize = scaleValue * startProjectorSize;


                if (isLoop && (lifetimeCounter > lifetime))
                {
                    lifetimeCounter = lifetimeCounter - lifetime;

                    currentPrefab.transform.localPosition = startPosOffset;
                    startRotation = Random.Range(startRotation_min, startRotation_max);
                    currentPrefab.transform.localEulerAngles = new Vector3(90f, 0f, 0.0f);
                    currentPrefab.transform.RotateAround(transform.position, Vector3.up, startRotation);

                    //toggle projector for sorting order
                    currentProjector.enabled = false;
                    currentProjector.enabled = true;
                }
                yield return null;
            }
            if (destroyAfter)
                Destroy(gameObject);
        }
        private int LayerMaskToLayer(LayerMask mask)
        {
            int layer = mask.value;
            for (int i = 0; i < 32; i++)
            {
                if ((layer & (1 << i)) != 0)
                    return i;
            }
            return 0; // Default to layer 0 if none is set
        }
        private void OnDestroy()
        {
            if (currentCoroutine != null)
                StopCoroutine(currentCoroutine);
        }
    }
}
