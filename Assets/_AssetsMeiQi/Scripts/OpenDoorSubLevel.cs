using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OpenDoorSubLevel : MonoBehaviourPun
{
    public float moveAmount = 2.5f;
    public float moveSpeed = 2f;

    private Vector3 originalPosition;
    private Vector3 openPosition;
    private Vector3 targetPosition;

    private bool shouldMove = false;
    public bool isOpen = false;
    private bool initialized = false;

    public static OpenDoorSubLevel instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        originalPosition = transform.position;
        openPosition = originalPosition + new Vector3(0, moveAmount, 0);
        targetPosition = originalPosition; // default target
        initialized = true;
    }

    void Update()
    {
        if (shouldMove)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Stop moving when close enough
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                shouldMove = false;
            }
        }
    }

    public void TriggerMove()
    {
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_TriggerDoor", RpcTarget.AllBuffered);
        }
        // Uncomment this if using in offline mode
        // else
        // {
        //     RPC_TriggerDoor();
        // }
    }

    [PunRPC]
    void RPC_TriggerDoor()
    {
        if (!initialized)
        {
            originalPosition = transform.position;
            openPosition = originalPosition + new Vector3(0, moveAmount, 0);
            initialized = true;
        }

        if (isOpen)
        {
            targetPosition = originalPosition;  // Close door
        }
        else
        {
            targetPosition = openPosition;     // Open door
        }

        isOpen = !isOpen;
        shouldMove = true;
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}
