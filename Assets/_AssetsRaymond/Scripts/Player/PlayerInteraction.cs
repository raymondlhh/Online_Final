using UnityEngine;
using Photon.Pun;

public class PlayerInteraction : MonoBehaviour
{
    private PhotonView photonView;

    private bool isInDoorTriggerArea = false;
    private bool isHoldingF_Door = false;

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

    void Update()
    {
        if (!photonView.IsMine) return;

        if (isInDoorTriggerArea)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                isHoldingF_Door = true;
                DoubleDoorManager.Instance.UpdatePlayerHolding(photonView.ViewID, true);
            }

            if (Input.GetKeyUp(KeyCode.F))
            {
                isHoldingF_Door = false;
                DoubleDoorManager.Instance.UpdatePlayerHolding(photonView.ViewID, false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        
    }

    private void OnTriggerExit(Collider other)
    {
        
    }

    public void SetDoorTriggerState(bool inArea)
    {
        isInDoorTriggerArea = inArea;

        // Show or hide the shared "Press F + Progress Bar" UI
        DoubleDoorManager.Instance.SetLocalUIVisibility(inArea);

        // If player leaves the area while holding F
        if (!inArea && isHoldingF_Door)
        {
            isHoldingF_Door = false;
            DoubleDoorManager.Instance.UpdatePlayerHolding(photonView.ViewID, false);
        }
    }
} 