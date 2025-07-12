using UnityEngine;
using Photon.Pun;

public class PlayerInteraction : MonoBehaviour
{
    private PhotonView photonView;

    private bool isInDoorTriggerArea = false;
    private bool isHoldingF_Door = false;

    private bool isInHint1TriggerArea = false;
    
    private bool isFireTriggerArea = false;

    private bool isStatueTriggerArea = false;

    private bool isHideTriggerArea = false;

    public bool isIgnited = false;

    private HideZoneTrigger currentHideZone;
    private IgniteFireZoneTrigger currentFireTriggerZone;
    private RotateStatue currentRotateStatue;

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
            if (Input.GetKey(KeyCode.F))
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
            if(Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("player Press F");
                currentFireTriggerZone.TryIgniteFromUI();
            }
        }

        if(isHideTriggerArea)
        {
            if(Input.GetKeyDown(KeyCode.Q))
            {
                if (currentHideZone != null)
                {
                    transform.position = currentHideZone.HideSpot.position;
                    Debug.Log("Player hid in pot at: " + currentHideZone.HideSpot.position);
                }
            }
        }

        if(isStatueTriggerArea)
        {
            if(Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("Start Rotating");
                currentRotateStatue.StartRotate();
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

    public void SetFireTriggerState(bool inArea, IgniteFireZoneTrigger triggerZone = null)
    {
        isFireTriggerArea = inArea;

        if (inArea)
            currentFireTriggerZone = triggerZone;
        else
            currentFireTriggerZone = null;

        IgniteFireUIManager.Instance.SetLocalUIVisibility(inArea);
    }

    public void SetStatueTriggerState(bool inArea, RotateStatue triggerZone = null)
    {
        isStatueTriggerArea = inArea;

        if (inArea)
            currentRotateStatue = triggerZone;
        else
            currentRotateStatue = null;

        //IgniteFireUIManager.Instance.SetLocalUIVisibility(inArea);
    }

    public void SetHideTriggerState(bool inArea)
    {
        isHideTriggerArea = inArea;
        HideManager.instance.SetLocalUIVisibility(inArea);
    }

    
} 