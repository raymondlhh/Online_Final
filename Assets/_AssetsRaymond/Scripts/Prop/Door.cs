using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Animator))]
public class Door : MonoBehaviour
{
    private Animator animator;
    private PhotonView photonView;

    // A list of players currently in the trigger, ONLY used by the Master Client.
    private readonly List<int> playersInTrigger = new List<int>();

    void Awake()
    {
        animator = GetComponent<Animator>();
        photonView = GetComponent<PhotonView>();
    }

    [PunRPC]
    private void RPC_SetDoorState(bool open)
    {
        // This RPC is called by the Master Client and runs on all clients.
        animator.SetBool("IsOpen", open);
    }

    [PunRPC]
    private void PlayerEntered(PhotonMessageInfo info)
    {
        // This logic only runs on the Master Client, who is the authority for the door.
        if (!PhotonNetwork.IsMasterClient)
            return;

        int actorNumber = info.Sender.ActorNumber;
        if (!playersInTrigger.Contains(actorNumber))
        {
            playersInTrigger.Add(actorNumber);
        }

        // If this is the first player to enter, open the door for everyone.
        if (playersInTrigger.Count == 1)
        {
            photonView.RPC(nameof(RPC_SetDoorState), RpcTarget.All, true);
        }
    }

    [PunRPC]
    private void PlayerExited(PhotonMessageInfo info)
    {
        // This logic only runs on the Master Client.
        if (!PhotonNetwork.IsMasterClient)
            return;
            
        playersInTrigger.Remove(info.Sender.ActorNumber);

        // If that was the last player to leave, close the door for everyone.
        if (playersInTrigger.Count == 0)
        {
            photonView.RPC(nameof(RPC_SetDoorState), RpcTarget.All, false);
        }
    }
} 