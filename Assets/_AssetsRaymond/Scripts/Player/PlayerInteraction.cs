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
    private bool isHidden = false;
    private Renderer[] renderers;
    private CapsuleCollider capsuleCollider;
    private PlayerMovement playerMovement;

    private bool isSpinTriggerArea = false;

    private bool isFireSTriggerArea = false;

    public bool isIgnited = false;

    private GameObject currentTarget;

    private HideZoneTrigger currentHideZone;
    private IgniteFireZoneTrigger currentFireTriggerZone;
    private RotateStatue currentRotateStatue;
    private IgniteFireSZoneTrigger currentFireSZoneTrigger;

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
        renderers = GetComponentsInChildren<Renderer>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        playerMovement = GetComponent<PlayerMovement>();
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

        if(isHideTriggerArea && Input.GetKeyDown(KeyCode.Q))
        {
            if (isHidden)
            {
                photonView.RPC("RPC_Unhide", RpcTarget.All, currentHideZone.UnHideSpot.position);

            }
            else
            {
                if (currentHideZone != null)
                {
                    Debug.Log("Hiding at: " + currentHideZone.HideSpot.position);
                    photonView.RPC("RPC_Hide", RpcTarget.All, currentHideZone.HideSpot.position);
                }
                else
                {
                    Debug.LogWarning("currentHideZone is null ¡ª cannot hide!");
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

        if (isSpinTriggerArea)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, 2f);

            if (hits.Length > 0)
            {
                // Sort hits by distance
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                // Define priority order (highest first)
                string[] spinPriority = { "SpinE", "SpinD", "SpinC", "SpinB", "SpinA" };

                GameObject newTarget = null;
                SpinPuzzle newRing = null;

                // Loop through priority list
                foreach (string spinName in spinPriority)
                {
                    foreach (RaycastHit hit in hits)
                    {
                        if (hit.collider != null && hit.collider.name == spinName &&
                            hit.collider.TryGetComponent<SpinPuzzle>(out SpinPuzzle ring))
                        {
                            newTarget = hit.collider.gameObject;
                            newRing = ring;
                            break;
                        }
                    }
                    if (newTarget != null) break;
                }

                if (newTarget != currentTarget)
                {
                    ClearHighlight();

                    if (newRing != null)
                    {
                        currentTarget = newTarget;
                        newRing.SetHighlight(true);
                    }
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    if (currentTarget != null && currentTarget.TryGetComponent<SpinPuzzle>(out SpinPuzzle ring))
                    {
                        ring.TryRotate();
                    }
                }
            }
            else
            {
                ClearHighlight();
            }
        }

        if(isFireSTriggerArea)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("player Press F");
                currentFireSZoneTrigger.TryIgniteFromUI();
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

    public void SetFireSTriggerState(bool inArea, IgniteFireSZoneTrigger triggerZone = null)
    {
        isFireSTriggerArea = inArea;

        if (inArea)
            currentFireSZoneTrigger = triggerZone;
        else
            currentFireSZoneTrigger = null;
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

    public void SetHideTriggerState(bool inArea, HideZoneTrigger triggerZone = null)
    {
        isHideTriggerArea = inArea;
        if (inArea)
            currentHideZone = triggerZone;
        else
            currentHideZone = null;

        if(isHidden)
        {
            HideManager.instance.SetLocalUIVisibility(inArea);
        }else if(!isHidden)
        {
            HideManager.instance.SetLocalUIVisibility2(inArea);
        }
        
    }

    public void SetSpinTriggerState(bool inArea)
    {
        isSpinTriggerArea = inArea;
    }

    void ClearHighlight()
    {
        if (currentTarget != null && currentTarget.TryGetComponent<SpinPuzzle>(out SpinPuzzle ring))
        {
            ring.SetHighlight(false);
            currentTarget = null;
        }
    }

    [PunRPC]
    void RPC_Hide(Vector3 hidePos)
    {
        Debug.Log("RPC_Hide received. Moving to " + hidePos);
        transform.position = hidePos;
        isHidden = true;

        foreach (Renderer rend in renderers)
            rend.enabled = false;

        // Disable movement
        if (playerMovement != null)
        {
            playerMovement.CanMove = false;     
            playerMovement.rb.useGravity = false; 
            playerMovement.rb.velocity = Vector3.zero;

        }
            

        // Disable collider
        if (capsuleCollider != null)
            capsuleCollider.enabled = false;

        // Disable gravity
        
    }

    [PunRPC]
    void RPC_Unhide(Vector3 unhidePos)
    {
        isHidden = false;
        transform.position = unhidePos;
        // Enable visuals
        foreach (Renderer rend in renderers)
        {
            rend.enabled = true;
        }

        // Enable movement
        if (playerMovement != null)
        {
            playerMovement.CanMove = true;
            playerMovement.rb.useGravity = true;
        }

        

        // Enable collider
        if (capsuleCollider != null)
            capsuleCollider.enabled = true;
    }
} 