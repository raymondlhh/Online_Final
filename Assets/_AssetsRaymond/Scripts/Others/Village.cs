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
    
    private PhotonView photonView;
    private Rigidbody rb;
    private NavMeshAgent navAgent;
    
    // Player detection and running behavior
    private bool isRunningFromPlayer = false;
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

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
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

        // Initialize based on village type
        if (villageType == VillageType.Walking)
        {
            startPosition = transform.position;
            SetNewWaypoint();
            StartCoroutine(PatrolRoutine());
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
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        currentWaypoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        
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
            // Player detected! Start running and show danger mark
            lastPlayerDetectionTime = Time.time;
            lastKnownPlayerPosition = closestPlayerPosition;
            lastPlayerDistance = closestDistance;
            
            if (!isRunningFromPlayer)
            {
                StartRunningFromPlayer();
            }
            ShowDangerMark();
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

    private void CheckDangerMarkVisibility()
    {
        // Hide danger mark if no player has been detected for a while
        if (dangerMarkVisible && Time.time - lastPlayerDetectionTime > dangerMarkHideDelay)
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
        
        // Stop patrolling if walking village
        if (villageType == VillageType.Walking && navAgent != null)
        {
            navAgent.isStopped = true;
        }
        
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
        
        // Resume normal behavior based on village type
        if (villageType == VillageType.Walking && navAgent != null)
        {
            navAgent.isStopped = false;
            navAgent.speed = walkSpeed; // Reset to walk speed
            SetNewWaypoint();
            StartCoroutine(PatrolRoutine());
        }
        else
        {
            // Stop movement for standing villages
            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
            }
        }
        
        Debug.Log($"<color=yellow>Village:</color> No players nearby. Returning to normal behavior.");
    }

    private IEnumerator RunningBehavior()
    {
        while (isRunningFromPlayer)
        {
            // Calculate direction away from last known player position
            Vector3 directionAwayFromPlayer = (transform.position - lastKnownPlayerPosition).normalized;
            float currentDistanceFromPlayer = Vector3.Distance(transform.position, lastKnownPlayerPosition);
            
            // If too close to player, run directly away
            if (currentDistanceFromPlayer < minimumSafeDistance)
            {
                runDirection = directionAwayFromPlayer;
            }
            else
            {
                // If at safe distance, add some randomness to make it harder to predict
                if (Time.time - lastDirectionChangeTime > directionChangeInterval)
                {
                    // Mix running away with random direction
                    Vector3 randomDirection = GetRandomDirection();
                    runDirection = Vector3.Lerp(directionAwayFromPlayer, randomDirection, 0.3f).normalized;
                    lastDirectionChangeTime = Time.time;
                }
            }
            
            // Apply movement based on village type
            if (villageType == VillageType.Walking && navAgent != null)
            {
                // Use NavMeshAgent for walking villages - run to a point far away from player
                Vector3 targetPosition = transform.position + runDirection * runSpeed * 2f;
                navAgent.SetDestination(targetPosition);
                navAgent.speed = runSpeed; // Ensure running speed
            }
            else
            {
                // Use Rigidbody for standing villages
                if (rb != null && !rb.isKinematic)
                {
                    Vector3 targetVelocity = runDirection * runSpeed;
                    // Keep Y velocity unchanged to maintain gravity
                    rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
                }
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