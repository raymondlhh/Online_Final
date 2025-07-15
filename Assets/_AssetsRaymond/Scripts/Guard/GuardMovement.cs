using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PhotonView))]
public class GuardMovement : MonoBehaviourPun, IPunObservable
{
    public enum PatrolType
    {
        RandomPatrol,
        PatrolPath
    }

    [Header("Movement Settings")]
    public float speed = 3.5f;
    public float walkSpeed = 2f;
    public PatrolType patrolType = PatrolType.RandomPatrol;
    
    [Header("AI Detection")]
    public float detectionRadius = 10f;
    [Range(0, 360)]
    public float detectionAngle = 90f;
    public LayerMask playerLayerMask;
    public float stoppingDistance = 5f;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 15f;
    [SerializeField] private float waypointReachDistance = 1f;
    [SerializeField] private float waypointWaitTime = 2f;
    public PatrolPath patrolPath; // Optional patrol path for walking type

    [Header("Network Settings")]
    [SerializeField] private float networkUpdateRate = 20f; // Updates per second
    [SerializeField] private float positionThreshold = 0.1f; // Minimum distance change to send update
    [SerializeField] private float rotationThreshold = 5f; // Minimum rotation change to send update

    private NavMeshAgent navMeshAgent;
    private Animator animator;
    public Transform targetPlayer { get; private set; }
    private int currentPatrolIndex = 0;

    // Patrol behavior variables
    private Vector3 startPosition;
    private Vector3 currentWaypoint;
    private bool isPatrolling = false;
    private Coroutine patrolCoroutine;
    private bool isWaitingAtWaypoint = false;

    // Network synchronization variables
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;
    private float lastNetworkUpdateTime;
    private bool isInitialized = false;

    // Animation synchronization variables
    private float networkHorizontal;
    private float networkVertical;
    private bool networkIsRunning;
    private bool lastSentIsRunning;
    private float lastSentHorizontal;
    private float lastSentVertical;

    // Attack animation synchronization variables
    private bool networkIsGunAttacking;
    private bool networkIsSwordAttacking;
    private bool lastSentIsGunAttacking;
    private bool lastSentIsSwordAttacking;

    // Smoothing buffer for reducing jitter
    private Vector3[] positionBuffer = new Vector3[3];
    private int bufferIndex = 0;

    // Decoy distraction logic
    private Transform distractionTarget = null;
    public void SetDistractionTarget(Transform distraction)
    {
        distractionTarget = distraction;
        if (navMeshAgent != null && photonView.IsMine)
        {
            navMeshAgent.SetDestination(distractionTarget.position);
        }
    }
    public void ClearDistractionTarget()
    {
        distractionTarget = null;
    }

    private GuardAudio guardAudio;
    private bool hasPlayedAlertSFX = false;
    private bool hasPlayedAttackSFX = false;

    public bool isMale;
    public bool isFemale;
    public bool isMummy;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        guardAudio = GetComponent<GuardAudio>();
        
        // Initialize network variables
        networkPosition = transform.position;
        networkRotation = transform.rotation;
        lastSentPosition = transform.position;
        lastSentRotation = transform.rotation;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (animator != null)
        {
            animator.SetBool("IsSoldier", true);
        }
        
        navMeshAgent.speed = walkSpeed;
        SetupNavAgent();

        startPosition = transform.position;

        // Only the master client controls AI behavior
        if (PhotonNetwork.IsMasterClient)
        {
            if (patrolType == PatrolType.PatrolPath && patrolPath != null && patrolPath.waypoints.Count > 0)
            {
                currentPatrolIndex = 0;
                currentWaypoint = patrolPath.waypoints[currentPatrolIndex].position;
                navMeshAgent.SetDestination(currentWaypoint);
                StartCoroutine(PatrolRoutine());
            }
            else // RandomPatrol
            {
                SetNewRandomWaypoint();
                StartCoroutine(PatrolRoutine());
            }
        }
        else
        {
            // Non-master clients disable NavMeshAgent to prevent conflicts
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = false;
            }
        }
        
        isInitialized = true;
    }

    private void SetupNavAgent()
    {
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = walkSpeed;
            navMeshAgent.angularSpeed = 120f;
            navMeshAgent.acceleration = 8f;
            navMeshAgent.stoppingDistance = 0.1f;
            navMeshAgent.radius = 0.5f;
            navMeshAgent.height = 2f;
        }
    }

    // Update is called once per frame
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
            
            // Update animator for non-master clients
            UpdateAnimator();
            return;
        }

        // Add a safety check to ensure the agent is on the NavMesh before proceeding.
        if (!navMeshAgent.isOnNavMesh)
        {
            return;
        }
        
        // If distracted by decoy, move to distraction target
        if (distractionTarget != null)
        {
            navMeshAgent.speed = speed;
            navMeshAgent.stoppingDistance = 0.5f;
            navMeshAgent.SetDestination(distractionTarget.position);
            UpdateAnimator();
            // Optionally, you can add logic to check if reached decoy and then idle or look around
            return;
        }

        FindPlayer();
        
        // Play alert SFX when starting to chase
        if (targetPlayer != null && !hasPlayedAlertSFX)
        {
            if (guardAudio != null)
            {
                if (isMale)
                    guardAudio.PlaySound("ManHey");
                else if (isFemale)
                    guardAudio.PlaySound("WomenHey");
                else if (isMummy)
                    guardAudio.PlaySound("MummySound");
            }
            hasPlayedAlertSFX = true;
        }
        if (targetPlayer == null && hasPlayedAlertSFX)
        {
            hasPlayedAlertSFX = false;
        }

        // Play attack SFX when starting to attack
        bool isAttacking = false;
        if (targetPlayer != null && navMeshAgent.remainingDistance <= stoppingDistance)
        {
            isAttacking = true;
        }
        if (isAttacking && !hasPlayedAttackSFX)
        {
            if (guardAudio != null)
                guardAudio.PlaySound("Attack");
            hasPlayedAttackSFX = true;
        }
        if (!isAttacking && hasPlayedAttackSFX)
        {
            hasPlayedAttackSFX = false;
        }
        Move();
        UpdateAnimator();
    }

    void FindPlayer()
    {
        targetPlayer = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerLayerMask);

        foreach (var hit in hits)
        {
            Vector3 directionToPlayer = (hit.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToPlayer) < detectionAngle / 2)
            {
                // Player is in the cone of vision
                targetPlayer = hit.transform;
                break; // Found a player, no need to check others
            }
        }
    }

    void Move()
    {
        if (targetPlayer != null)
        {
            // Chase the player
            navMeshAgent.speed = speed; // Use running speed when chasing
            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.SetDestination(targetPlayer.position);
            
            // Stop patrol when chasing
            if (patrolCoroutine != null)
            {
                StopCoroutine(patrolCoroutine);
                patrolCoroutine = null;
            }
        }
        else
        {
            // Return to patrol speed
            navMeshAgent.speed = walkSpeed;
            navMeshAgent.stoppingDistance = 0f;
            
            // Resume patrol if not already patrolling
            if (patrolCoroutine == null && !isWaitingAtWaypoint)
            {
                if (patrolType == PatrolType.PatrolPath && patrolPath != null)
                {
                    patrolCoroutine = StartCoroutine(PatrolRoutine());
                }
                else
                {
                    patrolCoroutine = StartCoroutine(PatrolRoutine());
                }
            }
        }
    }

    private void SetNewRandomWaypoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        currentWaypoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        if (navMeshAgent != null && targetPlayer == null)
        {
            navMeshAgent.SetDestination(currentWaypoint);
        }
    }

    private void SetNextPatrolPathWaypoint()
    {
        if (patrolPath != null && patrolPath.waypoints.Count > 0)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPath.waypoints.Count;
            currentWaypoint = patrolPath.waypoints[currentPatrolIndex].position;
            if (navMeshAgent != null && targetPlayer == null)
            {
                navMeshAgent.SetDestination(currentWaypoint);
            }
        }
    }

    private IEnumerator PatrolRoutine()
    {
        while (targetPlayer == null)
        {
            if (navMeshAgent != null && navMeshAgent.remainingDistance <= waypointReachDistance)
            {
                if (patrolType == PatrolType.PatrolPath && patrolPath != null && patrolPath.waypoints.Count > 0)
                {
                    SetNextPatrolPathWaypoint();
                }
                else // RandomPatrol
                {
                    SetNewRandomWaypoint();
                }
                yield return new WaitForSeconds(waypointWaitTime);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        if (PhotonNetwork.IsMasterClient)
        {
            // Master client: calculate and send animation parameters
            // Get the world-space velocity of the agent
            Vector3 velocity = navMeshAgent.velocity;
            
            // Transform the world velocity to the guard's local space
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);

            // Normalize the local velocity to get values between -1 and 1 for the blend tree
            float horizontal = localVelocity.x / navMeshAgent.speed;
            float vertical = localVelocity.z / navMeshAgent.speed;

            // Set the float values in the animator
            animator.SetFloat("Horizontal", horizontal);
            animator.SetFloat("Vertical", vertical);

            // The IsRunning bool is still useful for transitions between idle and moving states
            bool isRunning = velocity.sqrMagnitude > 0.1f;
            animator.SetBool("IsRunning", isRunning);

            // Get current attack animation states from animator
            bool isGunAttacking = animator.GetBool("IsGunAttacking");
            bool isSwordAttacking = animator.GetBool("IsSwordAttacking");

            // Store values for network synchronization
            networkHorizontal = horizontal;
            networkVertical = vertical;
            networkIsRunning = isRunning;
            networkIsGunAttacking = isGunAttacking;
            networkIsSwordAttacking = isSwordAttacking;
        }
        else
        {
            // Non-master client: use network-synchronized animation parameters
            animator.SetFloat("Horizontal", networkHorizontal);
            animator.SetFloat("Vertical", networkVertical);
            animator.SetBool("IsRunning", networkIsRunning);
            animator.SetBool("IsGunAttacking", networkIsGunAttacking);
            animator.SetBool("IsSwordAttacking", networkIsSwordAttacking);
        }
    }

    public bool AgentHasStopped()
    {
        // Check if the agent is on the NavMesh, has reached its stopping distance, and has a very low velocity.
        if (!navMeshAgent.isOnNavMesh) return false;
        return !navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && navMeshAgent.velocity.sqrMagnitude < 0.1f;
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
            if (Mathf.Abs(networkHorizontal - lastSentHorizontal) > 0.1f ||
                Mathf.Abs(networkVertical - lastSentVertical) > 0.1f ||
                networkIsRunning != lastSentIsRunning ||
                networkIsGunAttacking != lastSentIsGunAttacking ||
                networkIsSwordAttacking != lastSentIsSwordAttacking)
            {
                shouldSendUpdate = true;
                lastSentHorizontal = networkHorizontal;
                lastSentVertical = networkVertical;
                lastSentIsRunning = networkIsRunning;
                lastSentIsGunAttacking = networkIsGunAttacking;
                lastSentIsSwordAttacking = networkIsSwordAttacking;
            }
            
            // Send data if there are significant changes
            if (shouldSendUpdate)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(navMeshAgent.velocity);
                stream.SendNext(targetPlayer != null);
                
                // Send animation parameters
                stream.SendNext(networkHorizontal);
                stream.SendNext(networkVertical);
                stream.SendNext(networkIsRunning);
                stream.SendNext(networkIsGunAttacking);
                stream.SendNext(networkIsSwordAttacking);
            }
        }
        else
        {
            // Network guard, receive data
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            Vector3 velocity = (Vector3)stream.ReceiveNext();
            bool hasTarget = (bool)stream.ReceiveNext();
            
            // Receive animation parameters
            networkHorizontal = (float)stream.ReceiveNext();
            networkVertical = (float)stream.ReceiveNext();
            networkIsRunning = (bool)stream.ReceiveNext();
            networkIsGunAttacking = (bool)stream.ReceiveNext();
            networkIsSwordAttacking = (bool)stream.ReceiveNext();
            
            // Apply improved lag compensation
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            if (lag > 0.1f) // Only compensate for significant lag
            {
                networkPosition += velocity * lag;
            }
            
            // Add to smoothing buffer for non-master clients
            if (!PhotonNetwork.IsMasterClient)
            {
                positionBuffer[bufferIndex] = networkPosition;
                bufferIndex = (bufferIndex + 1) % positionBuffer.Length;
                
                // Calculate smoothed position
                Vector3 smoothedPosition = Vector3.zero;
                for (int i = 0; i < positionBuffer.Length; i++)
                {
                    smoothedPosition += positionBuffer[i];
                }
                networkPosition = smoothedPosition / positionBuffer.Length;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the detection radius
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw the field of view
        Gizmos.color = Color.yellow;
        Vector3 fovLine1 = Quaternion.AngleAxis(detectionAngle / 2, transform.up) * transform.forward * detectionRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-detectionAngle / 2, transform.up) * transform.forward * detectionRadius;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);

        // Draw patrol area for random patrol
        if (patrolType == PatrolType.RandomPatrol)
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
    }
} 