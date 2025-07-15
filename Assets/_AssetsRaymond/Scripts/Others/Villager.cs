using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class Villager : MonoBehaviourPun, IPunObservable
{
    public enum VillagerType
    {
        Standing,
        Walking
    }

    [Header("Villager Type")]
    [SerializeField] private VillagerType villagerType = VillagerType.Standing;

    [Header("Components")]
    private Animator animator; // Reference to the Animator component
    [SerializeField] private Transform safePoint; // Reference to the safezone transform

    [Header("Player Detection")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float detectionAngle = 120f; // Field of view angle in degrees
    [SerializeField] private LayerMask playerLayerMask = 1; // Default layer
    [SerializeField] private float runSpeed = 5f; // Increased run speed
    [SerializeField] private float runAwayDistance = 25f; // Increased distance
    [SerializeField] private float minimumSafeDistance = 20f; // Minimum distance to maintain from player
    [SerializeField] private float directionChangeInterval = 1.5f; // Faster direction changes
    [SerializeField] private float dangerMarkHideDelay = 3f; // Time to hide danger mark after no player detected

    [Header("Walking Villager Settings")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float waypointReachDistance = 1f;
    public PatrolPath patrolPath; // Optional patrol path for walking type

    [Header("Network Settings")]
    [SerializeField] private float networkUpdateRate = 20f; // Updates per second
    [SerializeField] private float positionThreshold = 0.1f; // Minimum distance change to send update
    [SerializeField] private float rotationThreshold = 5f; // Minimum rotation change to send update
    
    private PhotonView photonView;
    private Rigidbody rb;
    private NavMeshAgent navAgent;
    
    // Player detection and running behavior
    private bool isRunningFromPlayer = false;
    private bool hasSeenPlayer = false; // New flag to remember if the village has seen a player
    private Vector3 runDirection;
    private Coroutine runningCoroutine;
    private float lastDirectionChangeTime;
    private float lastPlayerDetectionTime;
    private bool dangerMarkVisible = false;
    private Vector3 lastKnownPlayerPosition;
    private float lastPlayerDistance;

    // Walking village specific
    private Vector3 startPosition;
    private Vector3 currentWaypoint;
    private bool isPatrolling = false;
    private bool isReturningToStart = false; // For standing villagers

    // Animation parameters
    private const string IS_WALKING_PARAM = "isWalking";
    private const string IS_RUNNING_PARAM = "isRunning";

    private Coroutine returnCoroutine; // Track return-to-start coroutine

    private bool isInSafeZone = false; // Track if villager is in the safe zone

    private bool hasReactedToPlayer = false; // For standing type: only react once

    private int currentPatrolIndex = 0;

    private AudioSource audioSource;
    private bool hasPlayedScream = false;

    // Network synchronization variables
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;
    private float lastNetworkUpdateTime;
    private bool isInitialized = false;

    // Animation synchronization variables
    private bool networkIsWalking;
    private bool networkIsRunning;
    private bool networkIsCrouching;
    private bool lastSentIsWalking;
    private bool lastSentIsRunning;
    private bool lastSentIsCrouching;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        audioSource = GetComponent<AudioSource>();

        // Get or add Animator component
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = gameObject.AddComponent<Animator>();
                Debug.LogWarning("No Animator component found on Village. Please assign the Villager_Animator controller.");
            }
        }

        // Setup NavMeshAgent for walking villages
        if (villagerType == VillagerType.Walking)
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<NavMeshAgent>();
            }
            SetupNavAgent();
        }

        // Initialize network variables
        networkPosition = transform.position;
        networkRotation = transform.rotation;
        lastSentPosition = transform.position;
        lastSentRotation = transform.rotation;
    }

    void Start()
    {
        // Village should be affected by gravity by default.
        rb.isKinematic = false;

        // Initialize animation state
        UpdateAnimationState(false, false);

        // Initialize based on village type
        if (villagerType == VillagerType.Walking)
        {
            startPosition = transform.position;
            SetNewWaypoint();
            
            // Only master client controls AI behavior
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(PatrolRoutine());
                // Start with walking animation for walking villages
                UpdateAnimationState(true, false);
            }
            else
            {
                // Non-master clients disable NavMeshAgent to prevent conflicts
                if (navAgent != null)
                {
                    navAgent.enabled = false;
                }
            }
        }
        else
        {
            // For standing villagers, store their initial position
            startPosition = transform.position;
        }
        
        // Only master client handles player detection
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(PlayerDetectionRoutine());
        }
        
        isInitialized = true;
    }

    private void SetupNavAgent()
    {
        if (navAgent != null)
        {
            navAgent.speed = walkSpeed;
            navAgent.angularSpeed = 120f;
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = 0.1f;
            navAgent.radius = 0.5f;
            navAgent.height = 2f;
        }
    }

    private void SetNewWaypoint()
    {
        if (patrolPath != null && patrolPath.waypoints.Count > 0)
        {
            // Use patrol path waypoints
            currentWaypoint = patrolPath.waypoints[currentPatrolIndex].position;
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPath.waypoints.Count;
        }
        else
        {
            // Use random patrol if no path assigned
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            currentWaypoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        if (navAgent != null && !isRunningFromPlayer && PhotonNetwork.IsMasterClient)
        {
            navAgent.SetDestination(currentWaypoint);
        }
    }

    private IEnumerator PatrolRoutine()
    {
        while (villagerType == VillagerType.Walking && !isRunningFromPlayer && PhotonNetwork.IsMasterClient)
        {
            if (navAgent != null && navAgent.remainingDistance <= waypointReachDistance)
            {
                SetNewWaypoint();
            }
            
            // Update animation state during patrol
            UpdatePatrolAnimation();
            
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator PlayerDetectionRoutine()
    {
        while (PhotonNetwork.IsMasterClient)
        {
            CheckForNearbyPlayers();
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
    }

    private void CheckForNearbyPlayers()
    {
        // For standing type: if already reacted, do nothing
        if (villagerType == VillagerType.Standing && hasReactedToPlayer)
            return;
        if (hasSeenPlayer)
        {
            // Already seen a player, ignore further detection
            return;
        }
        // Use Physics.OverlapSphere to detect players within range
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayerMask);
        
        bool playerDetected = false;
        float closestDistance = float.MaxValue;
        Vector3 closestPlayerPosition = Vector3.zero;
        Collider closestPlayerCollider = null;
        
        foreach (Collider playerCollider in nearbyColliders)
        {
            if (playerCollider != null)
            {
                // Ignore cloaked players
                if (playerCollider.gameObject.layer == LayerMask.NameToLayer("CloakedPlayer"))
                    continue;
                // Calculate direction to player
                Vector3 directionToPlayer = (playerCollider.transform.position - transform.position).normalized;
                float distanceToPlayer = Vector3.Distance(transform.position, playerCollider.transform.position);
                
                // Calculate angle between village's forward direction and direction to player
                float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
                
                // Check if player is within the detection angle (half angle on each side)
                if (angleToPlayer <= detectionAngle * 0.5f)
                {
                    playerDetected = true;
                    // Track the closest player
                    if (distanceToPlayer < closestDistance)
                    {
                        closestDistance = distanceToPlayer;
                        closestPlayerPosition = playerCollider.transform.position;
                        closestPlayerCollider = playerCollider;
                    }
                }
            }
        }
        
        if (playerDetected)
        {
            // Play scream SFX once per detection event
            if (!hasPlayedScream && audioSource != null && audioSource.clip != null)
            {
                audioSource.pitch = 1f; // Always set pitch to 1 before playing
                audioSource.Play();
                hasPlayedScream = true;
            }
            // For standing type: react by crouching, hide danger mark, and stop further detection
            if (villagerType == VillagerType.Standing && !hasReactedToPlayer)
            {
                if (animator != null)
                {
                    animator.SetBool("isCrouching", true);
                    networkIsCrouching = true; // Store for network sync
                }
                hasReactedToPlayer = true;
                return;
            }
            // Player detected! Start running and show danger mark
            lastPlayerDetectionTime = Time.time;
            lastKnownPlayerPosition = closestPlayerPosition;
            lastPlayerDistance = closestDistance;
            
            if (!isRunningFromPlayer)
            {
                hasSeenPlayer = true; // Mark as seen
                StartRunningFromPlayer();
            }
        }
    }

    private void StartRunningFromPlayer()
    {
        isRunningFromPlayer = true;
        lastDirectionChangeTime = Time.time;
        Debug.Log($"[Village] StartRunningFromPlayer called. isRunningFromPlayer={isRunningFromPlayer}, hasSeenPlayer={hasSeenPlayer}");

        // For walking villages, do not stop the NavMeshAgent. Set its speed and destination to the safe zone.
        if (villagerType == VillagerType.Walking && navAgent != null)
        {
            navAgent.enabled = true;
            // Warp to nearest NavMesh position before setting destination
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
            {
                navAgent.Warp(hit.position);
                Debug.Log($"[Village] (Walking) NavMeshAgent warped to NavMesh at {hit.position}");
            }
            else
            {
                Debug.LogWarning($"[Village] (Walking) Could not find NavMesh near {transform.position}");
            }
            navAgent.isStopped = false;
            navAgent.speed = runSpeed;
            if (safePoint != null)
            {
                // Also check safePoint is on NavMesh
                if (NavMesh.SamplePosition(safePoint.position, out hit, 1.0f, NavMesh.AllAreas))
                {
                    bool setDest = navAgent.SetDestination(hit.position);
                    Debug.Log($"[Village] (Walking) NavMeshAgent destination set to {hit.position}, SetDestination returned {setDest}");
                    Debug.Log($"[Village] (Walking) After SetDestination: pathStatus={navAgent.pathStatus}, hasPath={navAgent.hasPath}, pathPending={navAgent.pathPending}");
                }
                else
                {
                    bool setDest = navAgent.SetDestination(safePoint.position);
                    Debug.LogWarning($"[Village] (Walking) SafePoint {safePoint.position} not on NavMesh, using original position, SetDestination returned {setDest}");
                    Debug.Log($"[Village] (Walking) After SetDestination: pathStatus={navAgent.pathStatus}, hasPath={navAgent.hasPath}, pathPending={navAgent.pathPending}");
                }
            }
            Debug.Log($"[Village] (Walking) NavMeshAgent enabled: {navAgent.enabled}, isOnNavMesh: {navAgent.isOnNavMesh}, isStopped: {navAgent.isStopped}, hasPath: {navAgent.hasPath}");
        }
        else // For standing villagers, enable NavMeshAgent for pathfinding
        {
            if (navAgent != null)
            {
                navAgent.enabled = true;
                navAgent.Warp(transform.position); // Ensure agent is on the NavMesh
                bool onNavMesh = UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var hit, 0.1f, UnityEngine.AI.NavMesh.AllAreas);
                Debug.Log($"[Village] Standing agent onNavMesh: {onNavMesh}");
                navAgent.isStopped = false;
                navAgent.speed = runSpeed;
                if (safePoint != null)
                {
                    navAgent.SetDestination(safePoint.position);
                    Debug.Log($"[Village] (Standing) NavMeshAgent enabled, warped, and destination set to {safePoint.position}");
                }
                // Disable Rigidbody physics while using NavMeshAgent
                rb.isKinematic = true;
            }
        }

        // Stop any existing movement coroutines
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
        runningCoroutine = StartCoroutine(RunningBehavior());

        // Set animation to running while going to safe zone
        UpdateAnimationState(false, true);

        Debug.Log($"<color=yellow>Village:</color> Player detected! Going to safe zone.");
    }

    private void StopRunningFromPlayer()
    {
        // Only used for standing villagers now, or after reaching safe zone
        isRunningFromPlayer = false;
        Debug.Log($"[Village] StopRunningFromPlayer called. isRunningFromPlayer={isRunningFromPlayer}, hasSeenPlayer={hasSeenPlayer}");
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }
        if (villagerType == VillagerType.Walking && navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.speed = walkSpeed; // Reset to walk speed
            if (isInSafeZone)
            {
                UpdateAnimationState(false, false);
                return;
            }
        }
        else
        {
            if (!isReturningToStart)
            {
                if (returnCoroutine != null)
                {
                    StopCoroutine(returnCoroutine);
                }
                returnCoroutine = StartCoroutine(ReturnToStartPosition());
            }
        }
    }

    private IEnumerator RunningBehavior()
    {
        Debug.Log("[Village] RunningBehavior coroutine started.");
        while (isRunningFromPlayer && PhotonNetwork.IsMasterClient)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayerMask);
            bool chasedPlayerInvisible = true;
            foreach (Collider col in colliders)
            {
                if (col != null && col.gameObject.layer != LayerMask.NameToLayer("CloakedPlayer"))
                {
                    chasedPlayerInvisible = false;
                    break;
                }
            }
            if (chasedPlayerInvisible && villagerType == VillagerType.Standing)
            {
                StopRunningFromPlayer();
                yield break;
            }
            Vector3 targetPosition;
            if (safePoint != null)
            {
                targetPosition = safePoint.position;
            }
            else
            {
                targetPosition = transform.position + (transform.position - lastKnownPlayerPosition).normalized * runSpeed * 2f;
            }
            if (villagerType == VillagerType.Standing && navAgent != null && navAgent.enabled)
            {
                navAgent.SetDestination(targetPosition);
                Debug.Log($"[Village] (Standing) NavMeshAgent destination updated to {targetPosition}");
            }
            if (villagerType == VillagerType.Walking && navAgent != null)
            {
                navAgent.SetDestination(targetPosition);
                navAgent.speed = runSpeed;
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    Debug.Log("[Village] (Walking) Robust arrival at safe zone detected. Destroying villager.");
                    Photon.Pun.PhotonNetwork.Destroy(gameObject);
                    yield break;
                }
            }
            if (villagerType == VillagerType.Standing && safePoint != null && Vector3.Distance(transform.position, safePoint.position) < 1.5f)
            {
                Debug.Log("[Village] Reached safe zone. Stopping running behavior.");
                StopRunningFromPlayer();
                yield break;
            }
            yield return new WaitForFixedUpdate();
        }
        Debug.Log("[Village] RunningBehavior coroutine ended.");
    }

    private Vector3 GetRandomDirection()
    {
        // Get a random direction on the XZ plane (horizontal)
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        return randomDirection.normalized;
    }

    /// <summary>
    /// Updates the animation state based on the village's current behavior
    /// </summary>
    /// <param name="isWalking">Whether the village is currently walking</param>
    /// <param name="isRunning">Whether the village is currently running</param>
    private void UpdateAnimationState(bool isWalking, bool isRunning)
    {
        if (animator == null) return;

        if (PhotonNetwork.IsMasterClient)
        {
            // Master client: set animation parameters and store for network sync
            animator.SetBool(IS_WALKING_PARAM, isWalking);
            animator.SetBool(IS_RUNNING_PARAM, isRunning);
            
            // Store values for network synchronization
            networkIsWalking = isWalking;
            networkIsRunning = isRunning;
        }
        else
        {
            // Non-master client: use network-synchronized animation parameters
            animator.SetBool(IS_WALKING_PARAM, networkIsWalking);
            animator.SetBool(IS_RUNNING_PARAM, networkIsRunning);
        }
    }

    /// <summary>
    /// Updates animation state for walking villages during patrol
    /// </summary>
    private void UpdatePatrolAnimation()
    {
        if (villagerType == VillagerType.Walking && !isRunningFromPlayer)
        {
            // Check if the village is actually moving
            bool isMoving = navAgent != null && navAgent.velocity.magnitude > 0.1f;
            UpdateAnimationState(isMoving, false);
        }
    }

    private IEnumerator ReturnToStartPosition()
    {
        isReturningToStart = true;
        Debug.Log("[Village] ReturnToStartPosition coroutine started.");
        // Set animation to running while returning
        UpdateAnimationState(false, true);
        // For standing villagers, use NavMeshAgent to return
        if (villagerType == VillagerType.Standing && navAgent != null)
        {
            navAgent.enabled = true;
            navAgent.Warp(transform.position); // Ensure agent is on the NavMesh
            bool onNavMesh = UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var hit, 0.1f, UnityEngine.AI.NavMesh.AllAreas);
            Debug.Log($"[Village] Standing agent onNavMesh: {onNavMesh}");
            navAgent.isStopped = false;
            navAgent.speed = runSpeed;
            navAgent.SetDestination(startPosition);
            rb.isKinematic = true;
            Debug.Log($"[Village] (Standing) NavMeshAgent enabled, warped, and returning to {startPosition}");
            while (Vector3.Distance(transform.position, startPosition) > 0.5f)
            {
                navAgent.SetDestination(startPosition);
                yield return new WaitForFixedUpdate();
            }
            navAgent.isStopped = true;
            navAgent.enabled = false;
            rb.isKinematic = false;
        }
        else // fallback for walking villagers
        {
            while (Vector3.Distance(transform.position, startPosition) > 0.5f)
            {
                yield return new WaitForFixedUpdate();
            }
        }
        // Stop movement
        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
        }
        // Set animation to walking and hide danger mark
        UpdateAnimationState(true, false);
        // Reset hasSeenPlayer so the process can repeat
        hasSeenPlayer = false;
        isReturningToStart = false;
        Debug.Log("[Village] ReturnToStartPosition coroutine ended.");
    }

    // Update method for network synchronization
    void Update()
    {
        // Handle network synchronization for non-master clients
        if (!PhotonNetwork.IsMasterClient)
        {
            // Smoothly interpolate to network position with better smoothing
            if (isInitialized)
            {
                // Use smoother interpolation with damping
                float interpolationSpeed = Mathf.Clamp(networkUpdateRate * Time.deltaTime, 0.01f, 0.5f);
                
                // Calculate distance to target
                float distanceToTarget = Vector3.Distance(transform.position, networkPosition);
                
                // Only interpolate if we're not too close to avoid jitter
                if (distanceToTarget > 0.01f)
                {
                    transform.position = Vector3.Lerp(transform.position, networkPosition, interpolationSpeed);
                }
                
                // Smooth rotation interpolation
                float rotationDistance = Quaternion.Angle(transform.rotation, networkRotation);
                if (rotationDistance > 0.1f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, interpolationSpeed);
                }
            }
            
            // Update animation state for non-master clients
            if (animator != null)
            {
                animator.SetBool(IS_WALKING_PARAM, networkIsWalking);
                animator.SetBool(IS_RUNNING_PARAM, networkIsRunning);
                animator.SetBool("isCrouching", networkIsCrouching);
            }
            return;
        }
    }

    // Network synchronization
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Only send updates if there are significant changes to reduce network spam
            bool shouldSendUpdate = false;
            
            // Check if position has changed significantly
            float positionChange = Vector3.Distance(transform.position, lastSentPosition);
            if (positionChange > positionThreshold)
            {
                shouldSendUpdate = true;
                lastSentPosition = transform.position;
            }
            
            // Check if rotation has changed significantly
            float rotationChange = Quaternion.Angle(transform.rotation, lastSentRotation);
            if (rotationChange > rotationThreshold)
            {
                shouldSendUpdate = true;
                lastSentRotation = transform.rotation;
            }
            
            // Check if animation parameters have changed
            if (networkIsWalking != lastSentIsWalking ||
                networkIsRunning != lastSentIsRunning ||
                networkIsCrouching != lastSentIsCrouching)
            {
                shouldSendUpdate = true;
                lastSentIsWalking = networkIsWalking;
                lastSentIsRunning = networkIsRunning;
                lastSentIsCrouching = networkIsCrouching;
            }
            
            // Send data if there are significant changes
            if (shouldSendUpdate)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                
                // Send velocity if we have a NavMeshAgent
                Vector3 velocity = Vector3.zero;
                if (navAgent != null && navAgent.enabled)
                {
                    velocity = navAgent.velocity;
                }
                stream.SendNext(velocity);
                
                // Send state information
                stream.SendNext(isRunningFromPlayer);
                stream.SendNext(hasSeenPlayer);
                
                // Send animation parameters
                stream.SendNext(networkIsWalking);
                stream.SendNext(networkIsRunning);
                stream.SendNext(networkIsCrouching);
            }
        }
        else
        {
            // Network villager, receive data
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            Vector3 velocity = (Vector3)stream.ReceiveNext();
            bool runningFromPlayer = (bool)stream.ReceiveNext();
            bool seenPlayer = (bool)stream.ReceiveNext();
            
            // Receive animation parameters
            networkIsWalking = (bool)stream.ReceiveNext();
            networkIsRunning = (bool)stream.ReceiveNext();
            networkIsCrouching = (bool)stream.ReceiveNext();
            
            // Apply improved lag compensation
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            if (lag > 0.1f) // Only compensate for significant lag
            {
                networkPosition += velocity * lag;
            }
            
            // Update local state for non-master clients
            isRunningFromPlayer = runningFromPlayer;
            hasSeenPlayer = seenPlayer;
            
            // Update animation state for non-master clients
            if (animator != null)
            {
                animator.SetBool(IS_WALKING_PARAM, networkIsWalking);
                animator.SetBool(IS_RUNNING_PARAM, networkIsRunning);
                animator.SetBool("isCrouching", networkIsCrouching);
            }
        }
    }

    // Visualize the detection radius and angle in the editor
    private void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw minimum safe distance
        Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
        Gizmos.DrawWireSphere(transform.position, minimumSafeDistance);
        
        // Draw detection angle cone
        Gizmos.color = Color.red;
        float halfAngle = detectionAngle * 0.5f;
        Vector3 leftDirection = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
        Vector3 rightDirection = Quaternion.Euler(0, halfAngle, 0) * transform.forward;
        
        Gizmos.DrawRay(transform.position, leftDirection * detectionRadius);
        Gizmos.DrawRay(transform.position, rightDirection * detectionRadius);
        
        // Draw arc to show the detection cone
        int segments = 20;
        Vector3 previousPoint = transform.position + leftDirection * detectionRadius;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 currentDirection = Quaternion.Euler(0, currentAngle, 0) * transform.forward;
            Vector3 currentPoint = transform.position + currentDirection * detectionRadius;
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
        
        // Draw last known player position when running
        if (isRunningFromPlayer && Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 1f);
            Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
        }
        
        // Draw patrol area for walking villages
        if (villagerType == VillagerType.Walking)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, patrolRadius);
            
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentWaypoint, 0.5f);
                Gizmos.DrawLine(transform.position, currentWaypoint);
            }
        }
        
        if (isRunningFromPlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, runDirection * 2f);
        }
    }
    
    private void OnDestroy()
    {
        Debug.Log($"<color=red>Village:</color> Being destroyed!");
    }
    
    private void OnDisable()
    {
        Debug.Log($"<color=red>Village:</color> Being disabled!");
    }
} 