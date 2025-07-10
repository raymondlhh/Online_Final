using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class SprintBoostSkill : MonoBehaviourPunCallbacks
{
    [Header("Skill Timing")]
    [Tooltip("How long the skill stays active when triggered (seconds)")]
    public float activeDuration = 10f;
    [Tooltip("Cooldown time after skill ends (seconds)")]
    public float cooldownDuration = 30f;

    [Header("References")]
    public PlayerMovement playerMovement; // Assign in inspector or via GetComponentInParent
    public GameObject SprintBoostPanel; // Assign in inspector

    [Header("Boost Settings")]
    public float walkSpeedBoost = 6f;
    public float runSpeedBoost = 10f;

    private bool isActive = false;
    private bool isOnCooldown = false;
    private float timer = 0f;
    private float originalWalkSpeed;
    private float originalRunSpeed;

    // Reference to PlayerSkills for centralized UI management
    private PlayerSkills playerSkills;
    private PlayerAudio playerAudio;

    // Start is called before the first frame update
    void Start()
    {
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (SprintBoostPanel != null) SprintBoostPanel.SetActive(false);
        
        // Get reference to PlayerSkills
        playerSkills = GetComponentInParent<PlayerSkills>();
        if (playerSkills == null)
            playerSkills = FindObjectOfType<PlayerSkills>();
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
        // Save original speeds
        if (playerMovement != null)
        {
            originalWalkSpeed = playerMovement.walkSpeed;
            originalRunSpeed = playerMovement.runSpeed;
            playerMovement.walkSpeed = walkSpeedBoost;
            playerMovement.runSpeed = runSpeedBoost;
        }
        if (playerAudio != null) playerAudio.PlayLoop("SprintBoost");
        if (SprintBoostPanel != null) SprintBoostPanel.SetActive(true);
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (playerSkills != null) playerSkills.UpdateSkillDurationUI(timer, activeDuration);
            playerSkills?.SyncCooldownBarToPhoton();
            yield return null;
        }
        isActive = false;
        // Restore speeds
        if (playerMovement != null)
        {
            playerMovement.walkSpeed = originalWalkSpeed;
            playerMovement.runSpeed = originalRunSpeed;
        }
        if (playerAudio != null) playerAudio.StopLoop();
        if (SprintBoostPanel != null) SprintBoostPanel.SetActive(false);
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
}
