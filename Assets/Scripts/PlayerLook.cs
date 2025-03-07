using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    public float xRotation = 0f;
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;

    public GameObject lookPoint;
    public float maxDistance = 100f;
    public LayerMask layerMask;

    private bool isSprinting = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }



    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;

        xRotation -= (mouseY * Time.deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSensitivity);
        
    }

    void UpdateLookPoint()
    {
        // Create a ray from the center of the camera viewport
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        RaycastHit hit;

        // Cast a ray and check if it hits something
        if (Physics.Raycast(ray, out hit, maxDistance, layerMask))
        {
            // Move the look point object to the hit position
            lookPoint.transform.position = hit.point;
        }
        else
        {
            // If the ray doesn't hit anything, position the look point at a fixed distance
            lookPoint.transform.position = ray.origin + ray.direction * maxDistance;
        }
    }

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }

    void Update()
    {
        UpdateLookPoint();
    }
}