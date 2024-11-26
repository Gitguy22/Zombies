using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyZombieAnimation : MonoBehaviour
{

    UnityEngine.AI.NavMeshAgent agent;
    Animator animator;

    void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponent<Animator>(); 
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetBool("ZombieIdle", agent.velocity.magnitude == 0);
        animator.SetBool("ZombieWalk", agent.velocity.magnitude > 0.1f);
    }
}
