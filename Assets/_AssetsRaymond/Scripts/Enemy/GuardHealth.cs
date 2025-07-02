using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView))]
public class GuardHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float startHealth = 100f;
    [Header("UI")]
    [SerializeField] private Image healthBar;
    [SerializeField] private GameObject guardUICanvas;

    private float health;
    private bool isDead = false;

    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private GuardMovement guardMovement;
    private GuardAttack guardShoot;
    private Collider mainCollider;
    private PhotonView photonView;
    private Camera mainCamera;

    void Awake()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        guardMovement = GetComponent<GuardMovement>();
        guardShoot = GetComponent<GuardAttack>();
        mainCollider = GetComponent<Collider>();
        photonView = GetComponent<PhotonView>();
    }

    // Start is called before the first frame update
    void Start()
    {
        health = startHealth;
        // The camera will be found in LateUpdate to ensure it's ready.
        UpdateHealthBar(); // Set initial health bar state
    }

    void LateUpdate()
    {
        // Make the UI always face the player's camera
        if (guardUICanvas != null && guardUICanvas.activeSelf)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                guardUICanvas.transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up);
            }
        }
    }

    [PunRPC]
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;
        UpdateHealthBar(); // Update UI on all clients
        
        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = health / startHealth;
        }
    }

    void Die()
    {
        isDead = true;
        
        // Hide the health bar canvas
        if(guardUICanvas != null)
        {
            guardUICanvas.SetActive(false);
        }
        
        // Trigger death animation
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
        }

        // Disable components
        if (guardMovement != null) guardMovement.enabled = false;
        if (navMeshAgent != null) navMeshAgent.enabled = false;
        if (guardShoot != null) guardShoot.enabled = false;
        if (mainCollider != null) mainCollider.enabled = false;

        // Start coroutine to destroy the object after a delay
        StartCoroutine(DestroyAfterAnimation());
    }

    IEnumerator DestroyAfterAnimation()
    {
        // Wait for the length of the death animation
        if (animator != null)
        {
            // Find the "Death" clip in the animator controller
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            float deathClipLength = 3f; // Default value
            foreach (AnimationClip clip in clips)
            {
                if (clip.name == "Death") // Make sure your death animation clip is named "Death"
                {
                    deathClipLength = clip.length;
                    break;
                }
            }
            yield return new WaitForSeconds(deathClipLength);
        }
        else
        {
            // Fallback if no animator is found
            yield return new WaitForSeconds(2.9f);
        }
        
        // Only the master client should destroy networked objects
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
} 