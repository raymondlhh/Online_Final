using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GuardAttack : MonoBehaviour
{
    [Header("Attack Type")]
    [Tooltip("Does this guard attack with a gun?")]
    public bool attacksWithGun = false;
    [Tooltip("Does this guard attack with a sword? Only one attack type should be selected.")]
    public bool attacksWithSword = false;

    [Header("Gun Settings")]
    public float gunDamage = 10f;
    public float attackRate = 1f;
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Sword Settings")]
    public float swordDamage = 25f;
    public float swordAttackRange = 2f;


    private Animator animator;
    private GuardMovement guardMovement;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        guardMovement = GetComponent<GuardMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        // This logic should only run on the master client to have authority over AI
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (guardMovement.targetPlayer == null)
        {
            // No player targeted, do nothing related to attacks.
            // GuardMovement script will handle patrolling.
            return;
        }

        // --- Player is targeted ---
        if (isAttacking)
        {
            // If we are in the middle of an attack animation, do nothing else.
            return;
        }
        
        if (Time.time < nextAttackTime)
        {
            // If we are on attack cooldown, do nothing.
            return;
        }

        // Check if we are in range and have stopped moving.
        float distanceToPlayer = Vector3.Distance(transform.position, guardMovement.targetPlayer.position);
        bool inGunRange = attacksWithGun && (distanceToPlayer <= guardMovement.stoppingDistance);
        bool inSwordRange = attacksWithSword && (distanceToPlayer <= swordAttackRange);

        Debug.Log($"[GuardAttack] inSwordRange: {inSwordRange}, inGunRange: {inGunRange}, AgentStopped: {guardMovement.AgentHasStopped()}, isAttacking: {isAttacking}, nextAttackTime: {nextAttackTime}, Time: {Time.time}, attacksWithSword: {attacksWithSword}, distanceToPlayer: {distanceToPlayer}, swordAttackRange: {swordAttackRange}");

        if ((inGunRange || inSwordRange) && guardMovement.AgentHasStopped())
        {
            Debug.Log("[GuardAttack] Starting PerformAttack coroutine");
            StartCoroutine(PerformAttack());
            nextAttackTime = Time.time + attackRate;
        }
    }


    IEnumerator PerformAttack()
    {
        isAttacking = true;

        // --- Turn to face the player before attacking ---
        if (guardMovement.targetPlayer != null)
        {
            Vector3 direction = (guardMovement.targetPlayer.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            
            // Smoothly rotate towards the target over a short time
            float time = 0;
            while(time < 0.2f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, time / 0.2f);
                time += Time.deltaTime;
                yield return null;
            }
            transform.rotation = lookRotation; // Snap to final rotation
        }

        if (attacksWithGun)
        {
            animator.SetBool("IsGunAttacking", true);

            // Aim at the player
            Vector3 directionToPlayer = (guardMovement.targetPlayer.position - firePoint.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);

            // Instantiate bullet
            GameObject bullet = PhotonNetwork.Instantiate(bulletPrefab.name, firePoint.position, lookRotation);
            
            // Set bullet damage
            ProjectileMove projectile = bullet.GetComponent<ProjectileMove>();
            if (projectile != null)
            {
                projectile.damage = gunDamage;
            }

            yield return new WaitForSeconds(attackRate * 0.9f); // Wait for animation to play
            animator.SetBool("IsGunAttacking", false);
        }
        else if (attacksWithSword)
        {
            Debug.Log("[GuardAttack] Setting IsSwordAttacking to true");
            animator.SetBool("IsSwordAttacking", true);
            
            // The actual damage for sword would be done via an animation event or a trigger volume on the sword
            // For now, we'll do a simple sphere cast from the guard
            Collider[] hits = Physics.OverlapSphere(transform.position, swordAttackRange, guardMovement.playerLayerMask);
            foreach (var hit in hits)
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.photonView.RPC("TakeDamageFromAI", RpcTarget.All, swordDamage);
                    // Hit one player, break loop
                    break;
                }
            }
            
            yield return new WaitForSeconds(attackRate * 0.9f); // Wait for animation to play
            Debug.Log("[GuardAttack] Setting IsSwordAttacking to false");
            animator.SetBool("IsSwordAttacking", false);
        }
        
        isAttacking = false;
    }

    // This could be called from an animation event to signal the end of an attack
    public void EndAttack()
    {
        // This is now less relevant if we use triggers, but can be kept for other purposes
    }
} 