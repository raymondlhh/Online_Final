using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class FreezeGunVFX : MonoBehaviour
{
    public float duration = 10f;
    public SphereCollider triggerCollider;
    public string guardTag = "Guard";
    
    [Header("Freeze Effect")]
    public float freezeDuration = 10f;

    // Track frozen guards and their coroutines
    private Dictionary<GuardMovement, Coroutine> frozenGuards = new Dictionary<GuardMovement, Coroutine>();

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
            if (guard != null && !frozenGuards.ContainsKey(guard))
            {
                Coroutine freezeCoroutine = StartCoroutine(FreezeGuard(guard));
                frozenGuards.Add(guard, freezeCoroutine);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(guardTag))
        {
            GuardMovement guard = other.GetComponent<GuardMovement>();
            if (guard != null && frozenGuards.ContainsKey(guard))
            {
                // Optionally unfreeze early if you want guards to thaw when leaving the area
                StopCoroutine(frozenGuards[guard]);
                UnfreezeGuard(guard);
                frozenGuards.Remove(guard);
            }
        }
    }

    private IEnumerator FreezeGuard(GuardMovement guard)
    {
        // Freeze: stop NavMeshAgent and optionally play freeze animation
        NavMeshAgent agent = guard.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
        }
        Animator anim = guard.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsFrozen", true); // Only if you have a frozen state in your animator
        }

        yield return new WaitForSeconds(freezeDuration);

        UnfreezeGuard(guard);
        frozenGuards.Remove(guard);
    }

    private void UnfreezeGuard(GuardMovement guard)
    {
        NavMeshAgent agent = guard.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = false;
        }
        Animator anim = guard.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsFrozen", false);
        }
    }

    private void OnDestroy()
    {
        // Unfreeze all guards if the VFX is destroyed
        foreach (var pair in frozenGuards)
        {
            if (pair.Key != null)
                UnfreezeGuard(pair.Key);
        }
        frozenGuards.Clear();
    }
} 