using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class PlayerReviver : MonoBehaviourPunCallbacks
{
    [Header("Revive Settings")]
    [SerializeField] private KeyCode reviveKey = KeyCode.F;
    [SerializeField] private float raycastDistance = 5f;
    [SerializeField] private LayerMask playerLayerMask = -1;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject revivePromptUI;
    [SerializeField] private TextMeshProUGUI revivePromptText;
    
    private PlayerHealth revivablePlayer;
    private Camera playerCamera;
    private string currentPlayerType = "";

    // Character keys for Photon room properties
    private const string JADEN_KEY = "JadenChosen";
    private const string ALICE_KEY = "AliceChosen";
    private const string JACK_KEY = "JackChosen";

    void Start()
    {
        // Hide UI at start
        if (revivePromptUI != null)
        {
            revivePromptUI.SetActive(false);
        }

        // Get the player camera for raycasting
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("PlayerReviver: No camera found for raycasting!");
        }
    }

    void Update()
    {
        if (playerCamera == null) return;

        // Perform raycast to detect dead players
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        
        if (Physics.Raycast(ray, out hit, raycastDistance, playerLayerMask))
        {
            // Check if we hit a player
            PhotonView hitPlayerView = hit.collider.GetComponentInParent<PhotonView>();
            if (hitPlayerView != null && !hitPlayerView.IsMine)
            {
                PlayerHealth hitPlayerHealth = hitPlayerView.GetComponent<PlayerHealth>();
                if (hitPlayerHealth != null && hitPlayerHealth.IsDowned)
                {
                    // Found a dead player, set as revivable target
                    SetRevivableTarget(hitPlayerHealth);
                    return;
                }
            }
        }

        // No dead player found, clear target
        ClearRevivableTarget();
    }

    void LateUpdate()
    {
        // Handle revive input
        if (Input.GetKeyDown(reviveKey) && revivablePlayer != null)
        {
            TryRevive();
        }
    }

    public void SetRevivableTarget(PlayerHealth target)
    {
        if (target == null || target.photonView.IsMine) return;

        revivablePlayer = target;
        
        // Get the player type
        currentPlayerType = GetPlayerType(target.photonView.Owner);
        
        if (revivePromptUI != null)
        {
            revivePromptUI.SetActive(true);
        }
        if (revivePromptText != null)
        {
            string playerName = target.photonView.Owner.NickName;
            string typeText = !string.IsNullOrEmpty(currentPlayerType) ? $" ({currentPlayerType})" : "";
            revivePromptText.text = $"Press F to Revive {playerName}{typeText}";
        }
    }

    public void ClearRevivableTarget(PlayerHealth target = null)
    {
        // Only clear the target if it's the one we are currently tracking, or if no specific target is provided
        if (target == null || revivablePlayer == target)
        {
            revivablePlayer = null;
            currentPlayerType = "";
            if (revivePromptUI != null)
            {
                revivePromptUI.SetActive(false);
            }
        }
    }

    private string GetPlayerType(Photon.Realtime.Player player)
    {
        if (player == null || PhotonNetwork.CurrentRoom == null) return "";

        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        
        // Check which character this player has chosen
        if (props.ContainsKey(JADEN_KEY) && props[JADEN_KEY] != null && 
            (int)props[JADEN_KEY] == player.ActorNumber)
        {
            return "Jaden";
        }
        else if (props.ContainsKey(ALICE_KEY) && props[ALICE_KEY] != null && 
                 (int)props[ALICE_KEY] == player.ActorNumber)
        {
            return "Alice";
        }
        else if (props.ContainsKey(JACK_KEY) && props[JACK_KEY] != null && 
                 (int)props[JACK_KEY] == player.ActorNumber)
        {
            return "Jack";
        }
        
        return "";
    }

    void TryRevive()
    {
        if (revivablePlayer != null)
        {
            string revivedPlayerName = revivablePlayer.photonView.Owner.NickName;
            string typeText = !string.IsNullOrEmpty(currentPlayerType) ? $" ({currentPlayerType})" : "";

            // Call the Revive RPC on the downed player
            revivablePlayer.photonView.RPC("Revive", RpcTarget.All);
            
            // Clear the target to prevent accidental multi-revives
            revivablePlayer = null;
            currentPlayerType = "";
            if (revivePromptUI != null)
            {
                revivePromptUI.SetActive(false);
            }
            
            Debug.Log($"Revived player {revivedPlayerName}{typeText}");
        }
    }

    // Optional: Draw raycast in scene view for debugging
    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Vector3 rayStart = playerCamera.transform.position;
            Vector3 rayDirection = playerCamera.transform.forward * raycastDistance;
            Gizmos.DrawRay(rayStart, rayDirection);
        }
    }
} 