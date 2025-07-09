using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class TeleportSkill : MonoBehaviourPunCallbacks
{
    [Header("Skill Timing")]
    [Tooltip("How long the skill stays active when triggered (seconds)")]
    public float activeDuration = 15f;
    [Tooltip("Cooldown time after skill ends (seconds)")]
    public float cooldownDuration = 30f;

    [Header("References")]
    public Camera playerCamera; // Assign in inspector or via GetComponentInParent
    public GameObject TeleportCrosshair; // Assign in inspector

    private bool isActive = false;
    private bool isOnCooldown = false;
    private float timer = 0f;
    private bool canTeleport = false;

    // Reference to PlayerSkills for centralized UI management
    private PlayerSkills playerSkills;
    private PlayerAudio playerAudio;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize UI, cooldowns, etc.
        if (playerCamera == null) playerCamera = GetComponentInParent<PlayerMovement>()?.playerCamera;
        if (TeleportCrosshair != null) TeleportCrosshair.SetActive(false);
        
        // Get reference to PlayerSkills
        playerSkills = GetComponentInParent<PlayerSkills>();
        if (playerSkills == null)
            playerSkills = FindObjectOfType<PlayerSkills>();

        // Get reference to PlayerAudio
        playerAudio = GetComponentInParent<PlayerAudio>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return;
        if (Input.GetKeyDown(KeyCode.E) && !isActive && !isOnCooldown)
        {
            isActive = true;
            isOnCooldown = false;
            StartCoroutine(SkillDurationAndCooldown());
        }

        // Teleport logic during active phase
        if (isActive && canTeleport && Input.GetMouseButtonDown(0))
        {
            // Immediately set skill to used state
            if (playerSkills != null)
            {
                playerSkills.SetSkillUsed();
            }
            
            TryTeleport();
        }
    }

    private void UpdateUI(float t, float max, bool isActivePhase)
    {
        if (playerSkills != null)
        {
            playerSkills.UpdateSkillUI(t, max, isActivePhase);
            playerSkills.SyncCooldownBarToPhoton();
        }
    }

    private IEnumerator SkillDurationAndCooldown()
    {
        // Skill active phase
        timer = activeDuration;
        canTeleport = true;
        if (TeleportCrosshair != null) TeleportCrosshair.SetActive(true);

        while (timer > 0f && canTeleport)
        {
            timer -= Time.deltaTime;
            if (playerSkills != null) playerSkills.UpdateSkillDurationUI(timer, activeDuration);
            playerSkills?.SyncCooldownBarToPhoton();
            yield return null;
        }
        isActive = false;
        canTeleport = false;
        if (TeleportCrosshair != null) TeleportCrosshair.SetActive(false);

        // Set bar to full at the start of cooldown
        if (playerSkills != null) playerSkills.SetSkillBarFull();
        if (playerSkills != null) playerSkills.UpdateSkillCooldownUI(0, cooldownDuration);
        playerSkills?.SyncCooldownBarToPhoton();
        // Cooldown phase
        isOnCooldown = true;
        timer = 0f;
        while (timer < cooldownDuration)
        {
            timer += Time.deltaTime;
            if (playerSkills != null) playerSkills.UpdateSkillCooldownUI(timer, cooldownDuration);
            playerSkills?.SyncCooldownBarToPhoton();
            yield return null;
        }
        if (playerSkills != null) playerSkills.UpdateSkillCooldownUI(cooldownDuration, cooldownDuration);
        playerSkills?.SyncCooldownBarToPhoton();
        isOnCooldown = false;
    }

    private void TryTeleport()
    {
        if (playerCamera == null) return;

        // Play teleport audio
        if (playerAudio != null)
            playerAudio.PlaySound("Teleport");

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            // Teleport the player to the hit point (keep y offset if needed)
            Vector3 targetPosition = hit.point;
            // Optionally, adjust y to keep player above ground
            targetPosition.y += 1.0f; // Adjust as needed for your player height

            // Move the player (networked)
            transform.root.position = targetPosition;

            // End the skill immediately after teleport
            canTeleport = false;
        }
    }
}
