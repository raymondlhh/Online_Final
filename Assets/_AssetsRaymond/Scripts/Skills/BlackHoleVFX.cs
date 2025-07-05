using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class BlackHoleVFX : MonoBehaviour
{
    public float duration = 10f;
    public SphereCollider triggerCollider;
    public string guardTag = "Guard";
    
    [Header("Pulling Effect")]
    public float pullStrength = 5f;
    public float pullRadius = 8f;
    public float pullInterval = 0.1f; // How often to apply pull force
    
    private List<GuardMovement> affectedGuards = new List<GuardMovement>();
    private Coroutine pullCoroutine;

    void Awake()
    {
        // Ensure there is a trigger collider
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
                triggerCollider.isTrigger = true;
                triggerCollider.radius = pullRadius; // Use pull radius for trigger
            }
        }
        else
        {
            triggerCollider.isTrigger = true;
            triggerCollider.radius = pullRadius;
        }
    }

    void Start()
    {
        StartCoroutine(SelfDestructAfterDuration());
        pullCoroutine = StartCoroutine(PullGuardsCoroutine());
    }

    private IEnumerator SelfDestructAfterDuration()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    private IEnumerator PullGuardsCoroutine()
    {
        while (true)
        {
            // Find all guards within pull radius
            Collider[] guards = Physics.OverlapSphere(transform.position, pullRadius);
            affectedGuards.Clear();
            
            foreach (var col in guards)
            {
                if (col.CompareTag(guardTag))
                {
                    GuardMovement guard = col.GetComponent<GuardMovement>();
                    if (guard != null)
                    {
                        affectedGuards.Add(guard);
                        ApplyPullForce(guard);
                    }
                }
            }
            
            yield return new WaitForSeconds(pullInterval);
        }
    }

    private void ApplyPullForce(GuardMovement guard)
    {
        if (guard == null) return;
        
        // Calculate direction to black hole center
        Vector3 directionToCenter = (transform.position - guard.transform.position).normalized;
        float distance = Vector3.Distance(guard.transform.position, transform.position);
        
        // Apply stronger force when closer to center (inverse square law effect)
        float distanceMultiplier = 1f - (distance / pullRadius);
        float currentPullStrength = pullStrength * distanceMultiplier;
        
        // Apply force to the guard's NavMeshAgent
        if (guard.GetComponent<NavMeshAgent>() != null)
        {
            NavMeshAgent agent = guard.GetComponent<NavMeshAgent>();
            Vector3 pullVelocity = directionToCenter * currentPullStrength;
            
            // Add the pull velocity to the agent's current velocity
            agent.velocity += pullVelocity * Time.deltaTime;
            
            // Ensure the agent doesn't exceed its maximum speed
            if (agent.velocity.magnitude > agent.speed)
            {
                agent.velocity = agent.velocity.normalized * agent.speed;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(guardTag))
        {
            GuardMovement guard = other.GetComponent<GuardMovement>();
            if (guard != null)
            {
                guard.SetDistractionTarget(transform);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(guardTag))
        {
            GuardMovement guard = other.GetComponent<GuardMovement>();
            if (guard != null)
            {
                guard.ClearDistractionTarget();
            }
        }
    }

    private void OnDestroy()
    {
        // Stop the pull coroutine
        if (pullCoroutine != null)
        {
            StopCoroutine(pullCoroutine);
        }
        
        // On destroy, notify all guards in range to resume patrol
        Collider[] guards = Physics.OverlapSphere(transform.position, triggerCollider.radius);
        foreach (var col in guards)
        {
            if (col.CompareTag(guardTag))
            {
                GuardMovement guard = col.GetComponent<GuardMovement>();
                if (guard != null)
                {
                    guard.ClearDistractionTarget();
                }
            }
        }
    }
    
    // Visual debugging
    private void OnDrawGizmosSelected()
    {
        // Draw pull radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pullRadius);
        
        // Draw pull force direction for affected guards
        Gizmos.color = Color.yellow;
        foreach (var guard in affectedGuards)
        {
            if (guard != null)
            {
                Vector3 direction = (transform.position - guard.transform.position).normalized;
                Gizmos.DrawLine(guard.transform.position, guard.transform.position + direction * 2f);
            }
        }
    }
} 