using UnityEngine;

public class PowerupRotation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 1.0f;
    [SerializeField] private float minRotationDuration = 2.0f;
    [SerializeField] private float maxRotationDuration = 5.0f;
    [SerializeField] private float rotationThreshold = 5.0f;

    private Quaternion targetRotation;
    private float rotationTimer;

    private void Start()
    {
        SetRandomTargetRotation();
        rotationTimer = Random.Range(minRotationDuration, maxRotationDuration);
    }

    private void Update()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);
        rotationTimer -= Time.deltaTime;

        if (angleToTarget < rotationThreshold || rotationTimer <= 0)
        {
            SetRandomTargetRotation();
            rotationTimer = Random.Range(minRotationDuration, maxRotationDuration);
        }
    }

    private void SetRandomTargetRotation()
    {
        Quaternion currentTarget = targetRotation;
        Quaternion newTarget;
        float angleBetween = 0f;

        do
        {
            Vector3 randomEuler = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(0f, 360f),
                Random.Range(-10f, 10f)
            );

            newTarget = Quaternion.Euler(randomEuler);
            angleBetween = Quaternion.Angle(currentTarget, newTarget);
        }
        while (angleBetween < 40f);

        targetRotation = newTarget;
    }
}