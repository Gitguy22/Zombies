using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    Animator myAnim;

    List<Rigidbody> ragdollRigids;

    // Start is called before the first frame update
    void Start()
    {
        myAnim = GetComponent<Animator>();

        ragdollRigids = new List<Rigidbody>(transform.GetComponentsInChildren<Rigidbody>());
        ragdollRigids.Remove(GetComponent<Rigidbody>());

        DeactivateRagdoll();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ActivateRagdoll()
    {
        myAnim.enabled = false;
        for(int i = 0; i < ragdollRigids.Count; i++)
        {
            ragdollRigids[i].useGravity = true;
            ragdollRigids[i].useGravity = false;            
        }
    }

        void DeactivateRagdoll()
    {
        myAnim.enabled = true;
        for(int i = 0; i < ragdollRigids.Count; i++)
        {
            ragdollRigids[i].useGravity = false;
            ragdollRigids[i].useGravity = true;            
        }
    }
}
