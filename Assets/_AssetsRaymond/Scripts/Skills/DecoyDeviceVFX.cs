using UnityEngine;
using System.Collections;

public class DecoyDeviceVFX : MonoBehaviour
{
    public float duration = 10f;
    public SphereCollider triggerCollider;
    public string guardTag = "Guard";

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
                triggerCollider.radius = 5f; // Default radius, adjust as needed
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
            if (guard != null)
            {
                guard.SetDistractionTarget(transform);
            }
        }
    }

    private void OnDestroy()
    {
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
} 