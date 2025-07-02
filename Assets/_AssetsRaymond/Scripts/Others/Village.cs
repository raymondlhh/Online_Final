using UnityEngine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class Village : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject dangerMark;
    [SerializeField] private GameObject safeMark;

    [Header("Player Detection")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask playerLayerMask = 1; // Default layer
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private float runAwayDistance = 15f;
    [SerializeField] private float directionChangeInterval = 2f;
    
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

    // Player detection and running behavior
    private bool isRunningFromPlayer = false;
    private Vector3 runDirection;
    private Coroutine runningCoroutine;
    private float lastDirectionChangeTime;

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
        // Village should be affected by gravity by default.
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
        
        // Start player detection
        StartCoroutine(PlayerDetectionRoutine());
    }

    private IEnumerator PlayerDetectionRoutine()
    {
        while (true)
        {
            if (!isConnected && !isSaved) // Only run if not connected to player and not saved
            {
                CheckForNearbyPlayers();
            }
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
    }

    private void CheckForNearbyPlayers()
    {
        // Use Physics.OverlapSphere to detect players within range
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayerMask);
        
        if (nearbyColliders.Length > 0)
        {
            // Player detected! Start running
            if (!isRunningFromPlayer)
            {
                StartRunningFromPlayer();
            }
        }
        else
        {
            // No players nearby, stop running
            if (isRunningFromPlayer)
            {
                StopRunningFromPlayer();
            }
        }
    }

    private void StartRunningFromPlayer()
    {
        isRunningFromPlayer = true;
        lastDirectionChangeTime = Time.time;
        
        // Set initial random direction
        runDirection = GetRandomDirection();
        
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }
        runningCoroutine = StartCoroutine(RunningBehavior());
        
        Debug.Log($"<color=yellow>Village:</color> Player detected! Starting to run away.");
    }

    private void StopRunningFromPlayer()
    {
        isRunningFromPlayer = false;
        
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }
        
        // Stop movement
        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
        }
        
        Debug.Log($"<color=yellow>Village:</color> No players nearby. Stopping movement.");
    }

    private IEnumerator RunningBehavior()
    {
        while (isRunningFromPlayer && !isConnected && !isSaved)
        {
            // Change direction periodically
            if (Time.time - lastDirectionChangeTime > directionChangeInterval)
            {
                runDirection = GetRandomDirection();
                lastDirectionChangeTime = Time.time;
            }
            
            // Apply movement
            if (rb != null && !rb.isKinematic)
            {
                Vector3 targetVelocity = runDirection * runSpeed;
                // Keep Y velocity unchanged to maintain gravity
                rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    private Vector3 GetRandomDirection()
    {
        // Get a random direction on the XZ plane (horizontal)
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        return randomDirection.normalized;
    }

    // Visualize the detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        if (isRunningFromPlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, runDirection * 2f);
        }
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
                // Tell the Master Client a village has entered the save zone.
                photonView.RPC(nameof(RPC_VillageEnteredSaveZone), RpcTarget.MasterClient);
            }
            // Check for exit
            else if (!currentlyInSaveZone && wasInSaveZone && isSaved && !saveZoneRPCInProgress)
            {
                saveZoneRPCInProgress = true;
                // Tell the Master Client a village has left the save zone.
                photonView.RPC(nameof(RPC_VillageLeftSaveZone), RpcTarget.MasterClient);
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
    private void RPC_VillageEnteredSaveZone()
    {
        if (isSaved) 
        {
            saveZoneRPCInProgress = false;
            return;
        }
        
        Debug.Log($"<color=green>Village:</color> Entered save zone. Connected to player: {connectedPlayerViewID}");
        
        // This runs on the Master Client to update the authoritative count.
        GameManager.Instance.UpdateVillagesSavedCount(1);

        // Broadcast to all clients that this village is saved and should detach.
        photonView.RPC(nameof(RPC_SetSavedState), RpcTarget.All, true);
        
        // Notify the connected player to release this village
        if (connectedPlayerViewID != 0)
        {
            PhotonView playerView = PhotonView.Find(connectedPlayerViewID);
            if (playerView != null)
            {
                playerView.RPC("RPC_ForceReleaseVillage", RpcTarget.All);
            }
        }
        
        saveZoneRPCInProgress = false;
    }

    [PunRPC]
    private void RPC_VillageLeftSaveZone()
    {
        if (!isSaved) 
        {
            saveZoneRPCInProgress = false;
            return;
        }

        Debug.Log($"<color=orange>Village:</color> Left save zone.");

        // This runs on the Master Client.
        GameManager.Instance.UpdateVillagesSavedCount(-1);

        // Broadcast to all clients that this village is no longer saved.
        photonView.RPC(nameof(RPC_SetSavedState), RpcTarget.All, false);
        
        saveZoneRPCInProgress = false;
    }

    [PunRPC]
    private void RPC_SetSavedState(bool state)
    {
        isSaved = state;
        
        Debug.Log($"<color=blue>Village:</color> Saved state changed to: {state}");

        // Toggle the danger/safe marks based on the saved state.
        if (dangerMark != null)
        {
            dangerMark.SetActive(!isSaved);
        }
        if (safeMark != null)
        {
            safeMark.SetActive(isSaved);
        }

        // Stop running if saved
        if (isSaved && isRunningFromPlayer)
        {
            StopRunningFromPlayer();
        }

        // The logic for auto-detaching when entering the save zone has been removed.
        // The village will now remain connected.
    }

    [PunRPC]
    public void GetConnectedToPlayer(int connectorViewID, float duration)
    {
        // The check preventing connection to a saved village has been removed.
        // Players can now connect to villages inside the save zone.
        this.connectedPlayerViewID = connectorViewID;

        PhotonView connectorView = PhotonView.Find(connectorViewID);
        if (connectorView == null) return;

        // Stop running when connected to player
        if (isRunningFromPlayer)
        {
            StopRunningFromPlayer();
        }

        if (connectionCoroutine != null) StopCoroutine(connectionCoroutine);
        connectionCoroutine = StartCoroutine(VillageConnectionLifetime(connectorView.transform, duration));
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

        // Village is no longer controlled by a player, let physics take over.
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    private IEnumerator VillageConnectionLifetime(Transform targetSlot, float duration)
    {
        isConnected = true;
        playerConnectionSlot = targetSlot;

        // When connected, the village should not be affected by physics.
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
        Debug.Log($"<color=red>Village:</color> Being destroyed! Saved: {isSaved}, Connected: {isConnected}, ConnectedPlayer: {connectedPlayerViewID}");
    }
    
    private void OnDisable()
    {
        Debug.Log($"<color=red>Village:</color> Being disabled! Saved: {isSaved}, Connected: {isConnected}, ConnectedPlayer: {connectedPlayerViewID}");
    }
} 