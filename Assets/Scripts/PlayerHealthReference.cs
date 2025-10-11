using UnityEngine;

public class PlayerHealthReference : MonoBehaviour
{
    [SerializeField] private PlayerHealthData healthData;

    public PlayerHealthData HealthData => healthData;
}