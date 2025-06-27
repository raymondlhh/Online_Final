using UnityEngine;
using Photon.Pun;

public class PlayerInteraction : MonoBehaviour
{
    private PhotonView photonView;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    void Start()
    {
        // This script should only run on the local player's instance
        if (!photonView.IsMine)
        {
            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Door"))
        {
            // The DoorController is on a sibling object. Go up to the parent, then search down its children.
            Door doorController = other.transform.parent.GetComponentInChildren<Door>();
            if (doorController != null)
            {
                // Get the PhotonView from the door object to send the RPC.
                PhotonView doorPhotonView = doorController.GetComponent<PhotonView>();
                doorPhotonView.RPC("PlayerEntered", RpcTarget.MasterClient);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Door"))
        {
            // The DoorController is on a sibling object. Go up to the parent, then search down its children.
            Door doorController = other.transform.parent.GetComponentInChildren<Door>();
            if (doorController != null)
            {
                // Get the PhotonView from the door object to send the RPC.
                PhotonView doorPhotonView = doorController.GetComponent<PhotonView>();
                doorPhotonView.RPC("PlayerExited", RpcTarget.MasterClient);
            }
        }
    }
} 