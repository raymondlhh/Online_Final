using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView))]
public class GuardHealth : MonoBehaviour
{
    [Header("Guard Settings")]
    [Tooltip("Guards are invulnerable and cannot be damaged")]
    public bool isInvulnerable = true;
    
    [Header("UI")]
    [SerializeField] private GameObject guardUICanvas;

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
        // Guards are invulnerable by default
        Debug.Log($"<color=green>Guard:</color> {gameObject.name} is invulnerable and cannot be damaged.");
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
        // Guards are invulnerable - ignore all damage
        Debug.Log($"<color=green>Guard:</color> {gameObject.name} is invulnerable. Damage blocked: {amount}");
        
        // Optionally play a "blocked" sound or effect here
        // You could add a shield effect or sound to indicate the guard blocked the attack
    }

    // Public method to check if guard is invulnerable (for other scripts)
    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
} 