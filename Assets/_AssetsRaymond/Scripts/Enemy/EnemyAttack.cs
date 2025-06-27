using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Type")]
    [Tooltip("Does this enemy attack with a gun?")]
    public bool attacksWithGun = false;
    [Tooltip("Does this enemy attack with a sword? Only one attack type should be selected.")]
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
    private EnemyMovement enemyMovement;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        enemyMovement = GetComponent<EnemyMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        // This logic should only run on the master client to have authority over AI
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (enemyMovement.targetPlayer == null)
        {
            // No player targeted, do nothing related to attacks.
            // EnemyMovement script will handle patrolling.
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
        float distanceToPlayer = Vector3.Distance(transform.position, enemyMovement.targetPlayer.position);
        bool inGunRange = attacksWithGun && (distanceToPlayer <= enemyMovement.stoppingDistance);
        bool inSwordRange = attacksWithSword && (distanceToPlayer <= swordAttackRange);

        if ((inGunRange || inSwordRange) && enemyMovement.AgentHasStopped())
        {
            // We are in range and have stopped. Time to attack.
            StartCoroutine(PerformAttack());
            nextAttackTime = Time.time + attackRate;
        }
    }


    IEnumerator PerformAttack()
    {
        isAttacking = true;

        // --- Turn to face the player before attacking ---
        if (enemyMovement.targetPlayer != null)
        {
            Vector3 direction = (enemyMovement.targetPlayer.position - transform.position).normalized;
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
            Vector3 directionToPlayer = (enemyMovement.targetPlayer.position - firePoint.position).normalized;
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
            animator.SetBool("IsSwordAttacking", true);
            
            // The actual damage for sword would be done via an animation event or a trigger volume on the sword
            // For now, we'll do a simple sphere cast from the enemy
            Collider[] hits = Physics.OverlapSphere(transform.position, swordAttackRange, enemyMovement.playerLayerMask);
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
