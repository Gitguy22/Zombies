using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyGlassZombie : MonoBehaviour
{
    public Animator zombieAnimator;
    public AudioSource hitSound;
    public float minTimeBetweenHits = 3f;
    public float maxTimeBetweenHits = 6f;

    private float timeToNextHit;

    void Start()
    {
        SetNextHitTime();
    }

    void Update()
    {
        timeToNextHit -= Time.deltaTime;

        if (timeToNextHit <= 0)
        {
            PerformHit();
            SetNextHitTime();
        }
    }

    void SetNextHitTime()
    {
        timeToNextHit = Random.Range(minTimeBetweenHits, maxTimeBetweenHits);
    }

    void PerformHit()
    {
        hitSound.Play();
        zombieAnimator.SetTrigger("ZombiePunching");

        Invoke("ReturnToIdle", 1.4f);
    }

    void ReturnToIdle()
    {
        zombieAnimator.SetTrigger("ZombieIdle");
    }
}
