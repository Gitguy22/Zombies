using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BloodVFX
{
    public class EffectController : MonoBehaviour
    {

        [Space(10)]
        public int spawnNumber = 5;
        public float spawnRadius = 1.0f;
        public float minScale = 0.2f;
        public float maxScale = 0.6f;


        public float spread = 1.0f;
        public float minForce = 0.0f;
        public float maxForce = 100.0f;






        public GameObject[] prefabs;
        private Rigidbody[] rigids;
       

        // Start is called before the first frame update
        void OnEnable()
        {
            for(int i = 0; i < spawnNumber; i++)
            {
                Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
                int randomIndex = Random.Range(0, prefabs.Length);
                GameObject spawnObj = Instantiate(prefabs[randomIndex], spawnPos, Random.rotation);
                

                float scale = Random.Range(minScale, maxScale);
                spawnObj.transform.localScale = Vector3.one * scale;


                Vector3 forceStartPos = Vector3.Lerp(transform.position - transform.forward * 2.0f, transform.position, spread);
                Vector3 forceDir = Vector3.Normalize(spawnObj.transform.position - forceStartPos);
                spawnObj.GetComponent<Rigidbody>().AddForce(forceDir * Random.Range(minForce, maxForce));

            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2.0f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Vector3.Lerp(transform.position - transform.forward * 2.0f, transform.position, spread), 0.1f);
        }


}




}

