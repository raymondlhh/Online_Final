using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Handles animation synchronization across the network for AI characters
/// Ensures animations are visible to all players even when master client changes
/// </summary>
public class AnimationNetworkSync : MonoBehaviourPun, IPunObservable
{
    [Header("Animation Parameters")]
    [SerializeField] private string horizontalParam = "Horizontal";
    [SerializeField] private string verticalParam = "Vertical";
    [SerializeField] private string isRunningParam = "IsRunning";
    [SerializeField] private string isWalkingParam = "isWalking";
    [SerializeField] private string isCrouchingParam = "isCrouching";
    
    [Header("Network Settings")]
    [SerializeField] private float animationUpdateRate = 10f; // Updates per second
    [SerializeField] private bool interpolateAnimations = true;
    
    private Animator animator;
    private float lastAnimationUpdateTime;
    
    // Network animation parameters
    private float networkHorizontal;
    private float networkVertical;
    private bool networkIsRunning;
    private bool networkIsWalking;
    private bool networkIsCrouching;
    
    // Last sent values for change detection
    private float lastSentHorizontal;
    private float lastSentVertical;
    private bool lastSentIsRunning;
    private bool lastSentIsWalking;
    private bool lastSentIsCrouching;
    
    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[AnimationNetworkSync] No Animator found on {gameObject.name}");
        }
    }
    
    void Update()
    {
        if (animator == null) return;
        
        // Handle animation updates for non-master clients
        if (!PhotonNetwork.IsMasterClient)
        {
            if (interpolateAnimations)
            {
                // Smoothly interpolate animation parameters
                float currentHorizontal = animator.GetFloat(horizontalParam);
                float currentVertical = animator.GetFloat(verticalParam);
                
                float interpolationSpeed = animationUpdateRate * Time.deltaTime;
                animator.SetFloat(horizontalParam, Mathf.Lerp(currentHorizontal, networkHorizontal, interpolationSpeed));
                animator.SetFloat(verticalParam, Mathf.Lerp(currentVertical, networkVertical, interpolationSpeed));
            }
            else
            {
                // Direct assignment
                animator.SetFloat(horizontalParam, networkHorizontal);
                animator.SetFloat(verticalParam, networkVertical);
            }
            
            // Set boolean parameters
            animator.SetBool(isRunningParam, networkIsRunning);
            animator.SetBool(isWalkingParam, networkIsWalking);
            animator.SetBool(isCrouchingParam, networkIsCrouching);
        }
    }
    
    /// <summary>
    /// Set animation parameters (call this from the master client)
    /// </summary>
    public void SetAnimationParameters(float horizontal, float vertical, bool isRunning, bool isWalking = false, bool isCrouching = false)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        networkHorizontal = horizontal;
        networkVertical = vertical;
        networkIsRunning = isRunning;
        networkIsWalking = isWalking;
        networkIsCrouching = isCrouching;
        
        // Update local animator immediately
        if (animator != null)
        {
            animator.SetFloat(horizontalParam, horizontal);
            animator.SetFloat(verticalParam, vertical);
            animator.SetBool(isRunningParam, isRunning);
            animator.SetBool(isWalkingParam, isWalking);
            animator.SetBool(isCrouchingParam, isCrouching);
        }
    }
    
    /// <summary>
    /// Set boolean animation parameter
    /// </summary>
    public void SetBoolParameter(string paramName, bool value)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        switch (paramName)
        {
            case "IsRunning":
                networkIsRunning = value;
                break;
            case "isWalking":
                networkIsWalking = value;
                break;
            case "isCrouching":
                networkIsCrouching = value;
                break;
        }
        
        if (animator != null)
        {
            animator.SetBool(paramName, value);
        }
    }
    
    /// <summary>
    /// Set float animation parameter
    /// </summary>
    public void SetFloatParameter(string paramName, float value)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        switch (paramName)
        {
            case "Horizontal":
                networkHorizontal = value;
                break;
            case "Vertical":
                networkVertical = value;
                break;
        }
        
        if (animator != null)
        {
            animator.SetFloat(paramName, value);
        }
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send animation parameters
            stream.SendNext(networkHorizontal);
            stream.SendNext(networkVertical);
            stream.SendNext(networkIsRunning);
            stream.SendNext(networkIsWalking);
            stream.SendNext(networkIsCrouching);
        }
        else
        {
            // Receive animation parameters
            networkHorizontal = (float)stream.ReceiveNext();
            networkVertical = (float)stream.ReceiveNext();
            networkIsRunning = (bool)stream.ReceiveNext();
            networkIsWalking = (bool)stream.ReceiveNext();
            networkIsCrouching = (bool)stream.ReceiveNext();
        }
    }
    
    /// <summary>
    /// Force update animation parameters (useful when master client changes)
    /// </summary>
    public void ForceAnimationUpdate()
    {
        if (animator != null)
        {
            animator.SetFloat(horizontalParam, networkHorizontal);
            animator.SetFloat(verticalParam, networkVertical);
            animator.SetBool(isRunningParam, networkIsRunning);
            animator.SetBool(isWalkingParam, networkIsWalking);
            animator.SetBool(isCrouchingParam, networkIsCrouching);
        }
    }
    
    /// <summary>
    /// Get current network animation values
    /// </summary>
    public (float horizontal, float vertical, bool isRunning, bool isWalking, bool isCrouching) GetNetworkAnimationValues()
    {
        return (networkHorizontal, networkVertical, networkIsRunning, networkIsWalking, networkIsCrouching);
    }
} 