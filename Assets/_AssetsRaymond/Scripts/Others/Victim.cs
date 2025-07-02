using UnityEngine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class Victim : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject dangerMark;
    [SerializeField] private GameObject safeMark;

    private bool isSaved = false;
    private bool isConnected = false;
    private Coroutine connectionCoroutine;
    private Transform playerConnectionSlot;
    private PhotonView photonView;
    private int connectedPlayerViewID = 0;
    private Rigidbody rb;
    
    // Save zone detection
    private bool wasInSaveZone = false;
    private Collider[] saveZoneColliders;
    private bool saveZoneRPCInProgress = false; // Prevent duplicate RPC calls

    public bool IsSaved()
    {
        return isSaved;
    }

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }

    void Start()
    {
        // Victim should be affected by gravity by default.
        // It will become kinematic only when connected to a player.
        rb.isKinematic = false;

        // Set initial state for the marks.
        if (dangerMark != null)
        {
            dangerMark.SetActive(true);
        }
        if (safeMark != null)
        {
            safeMark.SetActive(false);
        }
        
        // Find all save zone colliders in the scene
        GameObject[] saveZones = GameObject.FindGameObjectsWithTag("SaveZone");
        saveZoneColliders = new Collider[saveZones.Length];
        for (int i = 0; i < saveZones.Length; i++)
        {
            saveZoneColliders[i] = saveZones[i].GetComponent<Collider>();
        }
        
        // Start checking for save zone entry/exit
        StartCoroutine(CheckSaveZoneStatus());
    }
    
    private IEnumerator CheckSaveZoneStatus()
    {
        while (true)
        {
            bool currentlyInSaveZone = IsInSaveZone();
            
            // Check for entry
            if (currentlyInSaveZone && !wasInSaveZone && !isSaved && !saveZoneRPCInProgress)
            {
                saveZoneRPCInProgress = true;
                // Tell the Master Client a victim has entered the save zone.
                photonView.RPC(nameof(RPC_VictimEnteredSaveZone), RpcTarget.MasterClient);
            }
            // Check for exit
            else if (!currentlyInSaveZone && wasInSaveZone && isSaved && !saveZoneRPCInProgress)
            {
                saveZoneRPCInProgress = true;
                // Tell the Master Client a victim has left the save zone.
                photonView.RPC(nameof(RPC_VictimLeftSaveZone), RpcTarget.MasterClient);
            }
            
            wasInSaveZone = currentlyInSaveZone;
            yield return new WaitForSeconds(0.2f); // Check every 0.2 seconds
        }
    }
    
    private bool IsInSaveZone()
    {
        if (saveZoneColliders == null) return false;
        
        foreach (Collider saveZoneCollider in saveZoneColliders)
        {
            if (saveZoneCollider != null && saveZoneCollider.bounds.Contains(transform.position))
            {
                return true;
            }
        }
        return false;
    }

    [PunRPC]
    private void RPC_VictimEnteredSaveZone()
    {
        if (isSaved) 
        {
            saveZoneRPCInProgress = false;
            return;
        }
        
        Debug.Log($"<color=green>Victim:</color> Entered save zone. Connected to player: {connectedPlayerViewID}");
        
        // This runs on the Master Client to update the authoritative count.
        GameManager.Instance.UpdateVillagesSavedCount(1);

        // Broadcast to all clients that this victim is saved and should detach.
        photonView.RPC(nameof(RPC_SetSavedState), RpcTarget.All, true);
        
        // Notify the connected player to release this victim
        if (connectedPlayerViewID != 0)
        {
            PhotonView playerView = PhotonView.Find(connectedPlayerViewID);
            if (playerView != null)
            {
                playerView.RPC("RPC_ForceReleaseVictim", RpcTarget.All);
            }
        }
        
        saveZoneRPCInProgress = false;
    }

    [PunRPC]
    private void RPC_VictimLeftSaveZone()
    {
        if (!isSaved) 
        {
            saveZoneRPCInProgress = false;
            return;
        }

        Debug.Log($"<color=orange>Victim:</color> Left save zone.");

        // This runs on the Master Client.
        GameManager.Instance.UpdateVillagesSavedCount(-1);

        // Broadcast to all clients that this victim is no longer saved.
        photonView.RPC(nameof(RPC_SetSavedState), RpcTarget.All, false);
        
        saveZoneRPCInProgress = false;
    }

    [PunRPC]
    private void RPC_SetSavedState(bool state)
    {
        isSaved = state;
        
        Debug.Log($"<color=blue>Victim:</color> Saved state changed to: {state}");

        // Toggle the danger/safe marks based on the saved state.
        if (dangerMark != null)
        {
            dangerMark.SetActive(!isSaved);
        }
        if (safeMark != null)
        {
            safeMark.SetActive(isSaved);
        }

        // The logic for auto-detaching when entering the save zone has been removed.
        // The victim will now remain connected.
    }

    [PunRPC]
    public void GetConnectedToPlayer(int connectorViewID, float duration)
    {
        // The check preventing connection to a saved victim has been removed.
        // Players can now connect to victims inside the save zone.
        this.connectedPlayerViewID = connectorViewID;

        PhotonView connectorView = PhotonView.Find(connectorViewID);
        if (connectorView == null) return;

        if (connectionCoroutine != null) StopCoroutine(connectionCoroutine);
        connectionCoroutine = StartCoroutine(VictimConnectionLifetime(connectorView.transform, duration));
    }

    [PunRPC]
    public void ForceDetachFromPlayer()
    {
        ForceDetachFromPlayer_Internal();
    }

    private void ForceDetachFromPlayer_Internal()
    {
        if (isConnected)
        {
            if (connectionCoroutine != null)
            {
                StopCoroutine(connectionCoroutine);
            }
            DetachFromPlayer();
        }
    }

    private void DetachFromPlayer()
    {
        isConnected = false;
        playerConnectionSlot = null;
        connectionCoroutine = null;
        connectedPlayerViewID = 0;

        // Victim is no longer controlled by a player, let physics take over.
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    private IEnumerator VictimConnectionLifetime(Transform targetSlot, float duration)
    {
        isConnected = true;
        playerConnectionSlot = targetSlot;

        // When connected, the victim should not be affected by physics.
        // Its movement is controlled directly.
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        float remainingDuration = duration;
        while (remainingDuration > 0f && isConnected)
        {
            if (playerConnectionSlot != null)
            {
                transform.position = playerConnectionSlot.position;
            }
            yield return new WaitForSeconds(0.1f);
            remainingDuration -= 0.1f;
        }

        DetachFromPlayer();
    }
    
    private void OnDestroy()
    {
        Debug.Log($"<color=red>Victim:</color> Being destroyed! Saved: {isSaved}, Connected: {isConnected}, ConnectedPlayer: {connectedPlayerViewID}");
    }
    
    private void OnDisable()
    {
        Debug.Log($"<color=red>Victim:</color> Being disabled! Saved: {isSaved}, Connected: {isConnected}, ConnectedPlayer: {connectedPlayerViewID}");
    }
} 