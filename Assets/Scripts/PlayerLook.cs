using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    public float xRotation = 0f;
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;

    private float bobFrequency = 10f;
    private float bobAmount = 0.05f;
    private float bobTimer = 0f;
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

    //public void UpdateCameraBobbing()
    //{
        //if (isSprinting)
        //{
            //bobTimer += Time.deltaTime * bobFrequency;
            //cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, Mathf.Sin(bobTimer) * bobAmount, cam.transform.localPosition.z);
        //}
        //else
        //{
            //bobTimer = 0f;
            //cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, 0, cam.transform.localPosition.z);
        //}
    //}

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }

    void Update()
    {
        //UpdateCameraBobbing();
    }
}