using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SlowAlien : MonoBehaviourPunCallbacks
{
    public float attackRadius = 10f;
    public float cooldownTime = 15f;
    public float slowDuration = 5f;

    public Animator slowAnimator;

    private float lastAttackTime;

    // Start is called before the first frame update
    void Start()
    {
        slowAnimator = GetComponent<Animator>();
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
                    slowAnimator.SetTrigger("isAttacking");
                    targetPV.RPC("RPC_Slow", targetPV.Owner, slowDuration);
                    lastAttackTime = Time.time;

                    break;
                }
            }
        }
    }
}
