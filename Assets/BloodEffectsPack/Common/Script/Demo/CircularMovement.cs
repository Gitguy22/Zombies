using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BloodEffectsPack
{
    public class CircularMovement : MonoBehaviour
    {
        public float radius = 5f; 
        public float speed = 1f;  

        private Vector3 center;   
        private float angle;     

        void Start()
        {
            center = transform.position;
        }

        void Update()
        {
            angle += speed * Time.deltaTime;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            transform.position = new Vector3(x, transform.position.y, z);
        }
    }
}
