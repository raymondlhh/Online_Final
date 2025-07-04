using UnityEngine;
using Photon.Pun;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class Village : MonoBehaviour
{
    public enum VillageType
    {
        Standing,
        Walking
    }

    [Header("Village Type")]
    [SerializeField] private VillageType villageType = VillageType.Standing;

    [Header("Components")]
    [SerializeField] private GameObject dangerMark;
    [SerializeField] private Animator animator; // Reference to the Animator component
    [SerializeField] private Transform safeZone; // Reference to the safezone transform

    [Header("Player Detection")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float detectionAngle = 120f; // Field of view angle in degrees
    [SerializeField] private LayerMask playerLayerMask = 1; // Default layer
    [SerializeField] private float runSpeed = 5f; // Increased run speed
    [SerializeField] private float runAwayDistance = 25f; // Increased distance
    [SerializeField] private float minimumSafeDistance = 20f; // Minimum distance to maintain from player
    [SerializeField] private float directionChangeInterval = 1.5f; // Faster direction changes
    [SerializeField] private float dangerMarkHideDelay = 3f; // Time to hide danger mark after no player detected

    [Header("Walking Village Settings")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float waypointReachDistance = 1f;
    [SerializeField] private PatrolPath patrolPath; // Optional patrol path for walking type
    
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

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

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
        if (villageType == VillageType.Walking)
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<NavMeshAgent>();
            }
            SetupNavAgent();
        }
    }

    void Start()
    {
        // Village should be affected by gravity by default.
        rb.isKinematic = false;

        // Set initial state for the danger mark (hidden by default)
        if (dangerMark != null)
        {
            dangerMark.SetActive(false);
        }

        // Initialize animation state
        UpdateAnimationState(false, false);

        // Initialize based on village type
        if (villageType == VillageType.Walking)
        {
            startPosition = transform.position;
            SetNewWaypoint();
            StartCoroutine(PatrolRoutine());
            
            // Start with walking animation for walking villages
            UpdateAnimationState(true, false);
        }
        else
        {
            // For standing villagers, store their initial position
            startPosition = transform.position;
        }
        
        // Start player detection
        StartCoroutine(PlayerDetectionRoutine());
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
        if (navAgent != null && !isRunningFromPlayer)
        {
            navAgent.SetDestination(currentWaypoint);
        }
    }

    private IEnumerator PatrolRoutine()
    {
        while (villageType == VillageType.Walking && !isRunningFromPlayer)
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
        while (true)
        {
            CheckForNearbyPlayers();
            CheckDangerMarkVisibility();
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
    }

    private void CheckForNearbyPlayers()
    {
        // For standing type: if already reacted, do nothing
        if (villageType == VillageType.Standing && hasReactedToPlayer)
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
        
        foreach (Collider playerCollider in nearbyColliders)
        {
            if (playerCollider != null)
            {
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
                    }
                }
            }
        }
        
        if (playerDetected)
        {
            // For standing type: react by crouching, hide danger mark, and stop further detection
            if (villageType == VillageType.Standing && !hasReactedToPlayer)
            {
                if (animator != null)
                    animator.SetBool("isCrouching", true);
                if (dangerMark != null)
                    dangerMark.SetActive(false);
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
            ShowDangerMark();
        }
    }

    private void CheckDangerMarkVisibility()
    {
        // Hide danger mark if no player has been detected for a while
        // Only hide if not running to safe zone
        if (dangerMarkVisible && !isRunningFromPlayer && Time.time - lastPlayerDetectionTime > dangerMarkHideDelay)
        {
            HideDangerMark();
        }
    }

    private void ShowDangerMark()
    {
        if (dangerMark != null && !dangerMarkVisible)
        {
            dangerMark.SetActive(true);
            dangerMarkVisible = true;
        }
    }

    private void HideDangerMark()
    {
        if (dangerMark != null && dangerMarkVisible)
        {
            dangerMark.SetActive(false);
            dangerMarkVisible = false;
        }
    }

    private void StartRunningFromPlayer()
    {
        isRunningFromPlayer = true;
        lastDirectionChangeTime = Time.time;
        Debug.Log($"[Village] StartRunningFromPlayer called. isRunningFromPlayer={isRunningFromPlayer}, hasSeenPlayer={hasSeenPlayer}");

        // For walking villages, do not stop the NavMeshAgent. Set its speed and destination to the safe zone.
        if (villageType == VillageType.Walking && navAgent != null)
        {
            navAgent.enabled = true;
            navAgent.isStopped = false;
            navAgent.speed = runSpeed;
            if (safeZone != null)
                navAgent.SetDestination(safeZone.position);
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
                if (safeZone != null)
                {
                    navAgent.SetDestination(safeZone.position);
                    Debug.Log($"[Village] (Standing) NavMeshAgent enabled, warped, and destination set to {safeZone.position}");
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

        // Ensure danger mark is visible
        ShowDangerMark();

        Debug.Log($"<color=yellow>Village:</color> Player detected! Going to safe zone.");
    }

    private void StopRunningFromPlayer()
    {
        isRunningFromPlayer = false;
        Debug.Log($"[Village] StopRunningFromPlayer called. isRunningFromPlayer={isRunningFromPlayer}, hasSeenPlayer={hasSeenPlayer}");
        // Do not reset hasSeenPlayer here, so village doesn't react to new players
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }
        if (villageType == VillageType.Walking && navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.speed = walkSpeed; // Reset to walk speed
            // If in safe zone, always remain idle and hide danger mark
            if (isInSafeZone)
            {
                UpdateAnimationState(false, false);
                if (dangerMark != null)
                    dangerMark.SetActive(false);
                return;
            }
            // (If not in safe zone, do nothing else)
        }
        else
        {
            // For standing villagers, after reaching safe zone, return to initial position
            if (!isReturningToStart)
            {
                if (returnCoroutine != null)
                {
                    StopCoroutine(returnCoroutine);
                }
                returnCoroutine = StartCoroutine(ReturnToStartPosition());
            }
        }
        Debug.Log($"<color=yellow>Village:</color> No players nearby. Returning to normal behavior.");
    }

    private IEnumerator RunningBehavior()
    {
        Debug.Log("[Village] RunningBehavior coroutine started.");
        while (isRunningFromPlayer)
        {
            Vector3 targetPosition;
            if (safeZone != null)
            {
                // Run towards the safezone
                targetPosition = safeZone.position;
            }
            else
            {
                // Fallback: run away from player as before
                targetPosition = transform.position + (transform.position - lastKnownPlayerPosition).normalized * runSpeed * 2f;
            }
            // For standing villagers, update NavMeshAgent destination
            if (villageType == VillageType.Standing && navAgent != null && navAgent.enabled)
            {
                navAgent.SetDestination(targetPosition);
                Debug.Log($"[Village] (Standing) NavMeshAgent destination updated to {targetPosition}");
            }
            // For walking villagers, update NavMeshAgent destination
            if (villageType == VillageType.Walking && navAgent != null)
            {
                navAgent.SetDestination(targetPosition);
                navAgent.speed = runSpeed; // Ensure running speed
                // Robust arrival check using NavMeshAgent
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    Debug.Log("[Village] (Walking) Robust arrival at safe zone detected. Destroying villager.");
                    Destroy(gameObject);
                    yield break;
                }
            }
            // Fallback: Optional, keep old check for standing type
            if (villageType == VillageType.Standing && safeZone != null && Vector3.Distance(transform.position, safeZone.position) < 1.5f)
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
        if (animator != null)
        {
            animator.SetBool(IS_WALKING_PARAM, isWalking);
            animator.SetBool(IS_RUNNING_PARAM, isRunning);
        }
    }

    /// <summary>
    /// Updates animation state for walking villages during patrol
    /// </summary>
    private void UpdatePatrolAnimation()
    {
        if (villageType == VillageType.Walking && !isRunningFromPlayer)
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
        if (dangerMark != null)
            dangerMark.SetActive(true);
        // For standing villagers, use NavMeshAgent to return
        if (villageType == VillageType.Standing && navAgent != null)
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
        if (dangerMark != null)
            dangerMark.SetActive(false);
        // Reset hasSeenPlayer so the process can repeat
        hasSeenPlayer = false;
        isReturningToStart = false;
        Debug.Log("[Village] ReturnToStartPosition coroutine ended.");
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
        if (villageType == VillageType.Walking)
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