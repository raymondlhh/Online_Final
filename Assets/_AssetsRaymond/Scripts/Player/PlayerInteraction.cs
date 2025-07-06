using UnityEngine;
using Photon.Pun;

public class PlayerInteraction : MonoBehaviour
{
    private PhotonView photonView;

    private bool isInDoorTriggerArea = false;
    private bool isHoldingF_Door = false;

    private bool isInHint1TriggerArea = false;
    
    private bool isFireTriggerArea = false;

    public bool isIgnited = false;

    public static PlayerInteraction Instance;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        Instance = this;
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

        if(isFireTriggerArea)
        {
            if(Input.GetKey(KeyCode.F))
            {
                Debug.Log("player Press F");
                IgniteFireZoneTrigger.instance.TryIgniteFromUI();
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

    public void SetHint1ATriggerState(bool inArea)
    {
        isInHint1TriggerArea = inArea;

        Hint1AManager.Instance.SetLocalUIVisibility(inArea);

    }

    public void SetHint1BTriggerState(bool inArea)
    {
        isInHint1TriggerArea = inArea;

        Hint1BManager.Instance.SetLocalUIVisibility(inArea);

    }

    public void SetFireTriggerState(bool inArea)
    {
        isFireTriggerArea = inArea;
        
        // Show or hide the shared "Press F + Progress Bar" UI
        IgniteFireUIManager.Instance.SetLocalUIVisibility(inArea);
    }
} 