using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class SlowGunVFX : MonoBehaviour
{
    public float duration = 10f;
    public SphereCollider triggerCollider;
    public string guardTag = "Guard";
    
    [Header("Slow Effect")]
    public float slowMultiplier = 0.5f; // 50% speed

    // Track slowed guards and their original speeds
    private Dictionary<GuardMovement, float> slowedGuards = new Dictionary<GuardMovement, float>();

    void Awake()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
                triggerCollider.isTrigger = true;
                triggerCollider.radius = 5f;
            }
        }
        else
        {
            triggerCollider.isTrigger = true;
        }
    }

    void Start()
    {
        StartCoroutine(SelfDestructAfterDuration());
    }

    private IEnumerator SelfDestructAfterDuration()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(guardTag))
        {
            GuardMovement guard = other.GetComponent<GuardMovement>();
            if (guard != null && !slowedGuards.ContainsKey(guard))
            {
                NavMeshAgent agent = guard.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    slowedGuards[guard] = agent.speed;
                    agent.speed *= slowMultiplier;
                }
                guard.SetDistractionTarget(transform);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(guardTag))
        {
            GuardMovement guard = other.GetComponent<GuardMovement>();
            if (guard != null && slowedGuards.ContainsKey(guard))
            {
                NavMeshAgent agent = guard.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed = slowedGuards[guard];
                }
                slowedGuards.Remove(guard);
                guard.ClearDistractionTarget();
            }
        }
    }

    private void OnDestroy()
    {
        // Restore all slowed guards
        foreach (var pair in slowedGuards)
        {
            if (pair.Key != null)
            {
                NavMeshAgent agent = pair.Key.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed = pair.Value;
                }
                pair.Key.ClearDistractionTarget();
            }
        }
        slowedGuards.Clear();
    }
} 