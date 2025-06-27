using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    public float speed = 3.5f;
    [Header("AI Detection")]
    public float detectionRadius = 10f;
    [Range(0, 360)]
    public float detectionAngle = 90f;
    public LayerMask playerLayerMask;
    public float stoppingDistance = 5f;

    private NavMeshAgent navMeshAgent;
    private Animator animator;
    public Transform targetPlayer { get; private set; }
    private Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (animator != null)
        {
            animator.SetBool("IsSoldier", true);
        }
        
        navMeshAgent.speed = speed;

        if (GameManager.Instance != null && GameManager.Instance.enemySpawners.Length > 0)
        {
            patrolPoints = GameManager.Instance.enemySpawners;
            GoToNextPatrolPoint();
        }
        else
        {
            Debug.LogError("No spawn points found for enemy patrol.", this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Add a safety check to ensure the agent is on the NavMesh before proceeding.
        if (!navMeshAgent.isOnNavMesh)
        {
            return;
        }
        
        FindPlayer();
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
            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.SetDestination(targetPlayer.position);
        }
        else
        {
            // Patrol
            navMeshAgent.stoppingDistance = 0f;
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
            {
                GoToNextPatrolPoint();
            }
        }
    }
    
    void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        // Pick a new random patrol point that is different from the current one
        int newPatrolIndex = currentPatrolIndex;
        // Safety for single patrol point to avoid infinite loop.
        if (patrolPoints.Length > 1) 
        {
            while (newPatrolIndex == currentPatrolIndex)
            {
                newPatrolIndex = Random.Range(0, patrolPoints.Length);
            }
        }
        currentPatrolIndex = newPatrolIndex;
        
        // Add a null check here to prevent errors during scene transitions.
        if (patrolPoints[currentPatrolIndex] != null)
        {
            navMeshAgent.destination = patrolPoints[currentPatrolIndex].position;
        }
    }

    void UpdateAnimator()
    {
        // Get the world-space velocity of the agent
        Vector3 velocity = navMeshAgent.velocity;
        
        // Transform the world velocity to the enemy's local space
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
    }

    public bool AgentHasStopped()
    {
        // Check if the agent is on the NavMesh, has reached its stopping distance, and has a very low velocity.
        if (!navMeshAgent.isOnNavMesh) return false;
        return !navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && navMeshAgent.velocity.sqrMagnitude < 0.1f;
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
    }
}
