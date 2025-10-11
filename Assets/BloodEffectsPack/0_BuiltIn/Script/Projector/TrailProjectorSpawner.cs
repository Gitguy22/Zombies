using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BloodEffectsPack
{
    public class TrailProjectorSpawner : MonoBehaviour
    {
        [HideInInspector] public int ignoreLayerMask;
        public GameObject projectorController_prefab;
        [Header("Spawn")]
        public float spawnDistance_min = 1.0f;
        public float spawnDistance_max = 1.0f;

        [Header("Duration")]
        public float duration = Mathf.Infinity;
        private float timeCounter = 0.0f;

        [Header("Size")]
        public float startSize_min = 1.0f;
        public float startSize_max = 1.0f;


        private Vector3 lastPosition;
        private float distanceTraveled = 0.0f;

        private GameObject projectorSpawnerGrp = null;


        void Start()
        {
            projectorSpawnerGrp = new GameObject(gameObject.name + "_ProjectorSpawner_GRP");
            projectorSpawnerGrp.AddComponent<KillEffect_Trail_Projector>();
        }

        void OnEnable()
        {
            timeCounter = 0.0f;
            distanceTraveled = 0.0f;
            lastPosition = transform.position;
        }
        void Update()
        {
            timeCounter += Time.deltaTime;
            distanceTraveled += Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;

            float distanceThreshold = Random.Range(spawnDistance_min, spawnDistance_max);
            if (distanceTraveled >= distanceThreshold)
            {
                SpawnProjector();
                distanceTraveled = 0f; 
            }

            if(timeCounter >= duration)
            {  
                Destroy(gameObject);
            }
        }
        void SpawnProjector()
        {
            GameObject projector_spawner = Instantiate(projectorController_prefab, transform.position + Vector3.up * 1.0f, Quaternion.identity);
            ProjectorSpawner spawner = projector_spawner.GetComponent<ProjectorSpawner>();
            spawner.startSize_max = startSize_max;
            spawner.startSize_min = startSize_min;

            spawner.ResetAndInitialize(ignoreLayerMask);
            if (projectorSpawnerGrp != null)
                projector_spawner.transform.SetParent(projectorSpawnerGrp.transform);
        }
    }
}
