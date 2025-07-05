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
        
    }

    private void OnTriggerExit(Collider other)
    {
        
    }
} 