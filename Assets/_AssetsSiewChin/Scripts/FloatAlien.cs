using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FloatAlien : MonoBehaviourPunCallbacks
{
    public float attackRadius = 10f;
    public float cooldownTime = 5f;
    public float floatDuration = 2f;

    public Animator floatAnimator;

    private float lastAttackTime;

    void Start()
    {
        floatAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, attackRadius);

        foreach (var hit in hitPlayers)
        {
            if (hit.CompareTag("Player") && Time.time >= lastAttackTime + cooldownTime)
            {
                Debug.Log("Collide with player!");
                
                PhotonView targetPV = hit.GetComponent<PhotonView>();
                if (targetPV != null)
                {
                    floatAnimator.SetTrigger("isAttacking");
                    targetPV.RPC("RPC_Float", targetPV.Owner, floatDuration);
                    lastAttackTime = Time.time;

                    break;
                }
            }
        }

    }
}
