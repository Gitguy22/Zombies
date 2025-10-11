using UnityEngine;

[CreateAssetMenu(fileName = "BloodEffectsData", menuName = "Weapon/Blood Effects Data")]
public class BloodEffectsData : ScriptableObject
{
    [Header("Blood Splash Effects")]
    public GameObject[] bloodSplashPrefabs;
}