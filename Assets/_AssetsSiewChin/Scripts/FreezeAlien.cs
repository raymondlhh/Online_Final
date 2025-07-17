using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FreezeAlien : MonoBehaviourPunCallbacks
{
    public float attackRadius = 10f;
    public float cooldownTime = 15f;
    public float freezeDuration = 5f;

    public Animator freezeAnimator;

    private float lastAttackTime;

    // Start is called before the first frame update
    void Start()
    {
        freezeAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
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
                    freezeAnimator.SetTrigger("isAttacking");
                    targetPV.RPC("RPC_Freeze", targetPV.Owner, freezeDuration);
                    lastAttackTime = Time.time;

                    break;
                }
            }
        }
    }
}
