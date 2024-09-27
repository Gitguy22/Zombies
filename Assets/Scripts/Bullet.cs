using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public LayerMask collisionMask; // Layers the bullet can interact with
    private Vector3 lastPosition;
    Rigidbody rb;

    private void Awake()
    {
        // Initialize lastPosition to the current position of the bullet
        lastPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        if (rb != null) return;
        Debug.Log("Hi");
    }

    private void Update()
    {
        // Calculate the distance the bullet will travel this frame based on Rigidbody velocity

        //

        // Get the current velocity of the bullet
        Vector3 currentVelocity = rb.velocity;
        float distanceThisFrame = currentVelocity.magnitude * Time.deltaTime;

        // Perform raycast to check for collision between last position and current position
        RaycastHit hit;
        Debug.Log(lastPosition);
        if (Physics.Raycast(lastPosition, currentVelocity.normalized, out hit, distanceThisFrame, collisionMask))
        {
            // Check if the hit object is a Limb and handle the hit
            Limb limb = hit.transform.GetComponent<Limb>();
            if (limb != null)
            {
                limb.GetHit();
            }

            // Handle other collision logic
            OnHitObject(hit);
        }

        // Update the last position for the next frame
        lastPosition = transform.position;
    }

    private void OnHitObject(RaycastHit hit)
    {
        // Destroy the bullet on hit
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the hit object is a Limb and handle the hit
        Limb limb = collision.transform.GetComponent<Limb>();
        if (limb != null)
        {
            limb.GetHit();
        }

        // Destroy the bullet on collision
        Destroy(gameObject);
    }
}