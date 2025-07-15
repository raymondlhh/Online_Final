using UnityEngine;
using Photon.Pun;

/// <summary>
/// Helper script to automatically set up proper Photon networking components
/// for guards and villagers to prevent glitching across different clients.
/// </summary>
public class NetworkSetupHelper : MonoBehaviour
{
    [Header("Auto Setup Settings")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool addPhotonTransformView = true;
    [SerializeField] private bool addPhotonRigidbodyView = false; // Usually not needed with NavMeshAgent
    
    [Header("PhotonView Settings")]
    [SerializeField] private ViewSynchronization synchronizationType = ViewSynchronization.UnreliableOnChange;
    [SerializeField] private OwnershipOption ownershipTransfer = OwnershipOption.Fixed;
    [SerializeField] private byte group = 0;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupNetworking();
        }
    }
    
    [ContextMenu("Setup Networking")]
    public void SetupNetworking()
    {
        // Ensure PhotonView exists
        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            photonView = gameObject.AddComponent<PhotonView>();
            Debug.Log($"[NetworkSetupHelper] Added PhotonView to {gameObject.name}");
        }
        
        // Configure PhotonView
        photonView.Synchronization = synchronizationType;
        photonView.OwnershipTransfer = ownershipTransfer;
        photonView.Group = group;
        
        // Add PhotonTransformView if requested
        if (addPhotonTransformView)
        {
            PhotonTransformView transformView = GetComponent<PhotonTransformView>();
            if (transformView == null)
            {
                transformView = gameObject.AddComponent<PhotonTransformView>();
                Debug.Log($"[NetworkSetupHelper] Added PhotonTransformView to {gameObject.name}");
            }
            
            // Configure transform view for smooth movement
            transformView.m_SynchronizePosition = true;
            transformView.m_SynchronizeRotation = true;
            transformView.m_SynchronizeScale = false;
            transformView.m_UseLocal = false;
        }
        
        // Add PhotonRigidbodyView if requested (usually not needed with NavMeshAgent)
        if (addPhotonRigidbodyView)
        {
            PhotonRigidbodyView rigidbodyView = GetComponent<PhotonRigidbodyView>();
            if (rigidbodyView == null)
            {
                rigidbodyView = gameObject.AddComponent<PhotonRigidbodyView>();
                Debug.Log($"[NetworkSetupHelper] Added PhotonRigidbodyView to {gameObject.name}");
            }
        }
        
        // Ensure the PhotonView observes the correct components
        if (photonView.ObservedComponents == null || photonView.ObservedComponents.Count == 0)
        {
            photonView.ObservedComponents = new System.Collections.Generic.List<Component>();
            
            // Add the main movement script
            GuardMovement guardMovement = GetComponent<GuardMovement>();
            if (guardMovement != null)
            {
                photonView.ObservedComponents.Add(guardMovement);
            }
            
            Villager villager = GetComponent<Villager>();
            if (villager != null)
            {
                photonView.ObservedComponents.Add(villager);
            }
            
            // Add transform view if it exists
            PhotonTransformView transformView = GetComponent<PhotonTransformView>();
            if (transformView != null)
            {
                photonView.ObservedComponents.Add(transformView);
            }
            
            // Add rigidbody view if it exists
            PhotonRigidbodyView rigidbodyView = GetComponent<PhotonRigidbodyView>();
            if (rigidbodyView != null)
            {
                photonView.ObservedComponents.Add(rigidbodyView);
            }
        }
        
        Debug.Log($"[NetworkSetupHelper] Networking setup completed for {gameObject.name}");
    }
    
    [ContextMenu("Remove Networking Components")]
    public void RemoveNetworkingComponents()
    {
        // Remove PhotonTransformView
        PhotonTransformView transformView = GetComponent<PhotonTransformView>();
        if (transformView != null)
        {
            DestroyImmediate(transformView);
            Debug.Log($"[NetworkSetupHelper] Removed PhotonTransformView from {gameObject.name}");
        }
        
        // Remove PhotonRigidbodyView
        PhotonRigidbodyView rigidbodyView = GetComponent<PhotonRigidbodyView>();
        if (rigidbodyView != null)
        {
            DestroyImmediate(rigidbodyView);
            Debug.Log($"[NetworkSetupHelper] Removed PhotonRigidbodyView from {gameObject.name}");
        }
        
        // Remove PhotonView
        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            DestroyImmediate(photonView);
            Debug.Log($"[NetworkSetupHelper] Removed PhotonView from {gameObject.name}");
        }
    }
    
    [ContextMenu("Check Networking Setup")]
    public void CheckNetworkingSetup()
    {
        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogWarning($"[NetworkSetupHelper] {gameObject.name} is missing PhotonView component!");
            return;
        }
        
        Debug.Log($"[NetworkSetupHelper] {gameObject.name} networking setup:");
        Debug.Log($"  - PhotonView: {(photonView != null ? "✓" : "✗")}");
        Debug.Log($"  - Synchronization: {photonView.Synchronization}");
        Debug.Log($"  - Ownership Transfer: {photonView.OwnershipTransfer}");
        Debug.Log($"  - Group: {photonView.Group}");
        Debug.Log($"  - Observed Components: {photonView.ObservedComponents?.Count ?? 0}");
        
        PhotonTransformView transformView = GetComponent<PhotonTransformView>();
        Debug.Log($"  - PhotonTransformView: {(transformView != null ? "✓" : "✗")}");
        
        PhotonRigidbodyView rigidbodyView = GetComponent<PhotonRigidbodyView>();
        Debug.Log($"  - PhotonRigidbodyView: {(rigidbodyView != null ? "✓" : "✗")}");
        
        GuardMovement guardMovement = GetComponent<GuardMovement>();
        Debug.Log($"  - GuardMovement: {(guardMovement != null ? "✓" : "✗")}");
        
        Villager villager = GetComponent<Villager>();
        Debug.Log($"  - Villager: {(villager != null ? "✓" : "✗")}");
    }
} 