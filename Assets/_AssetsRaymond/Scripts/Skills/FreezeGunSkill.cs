using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Photon.Pun;

public class FreezeGunSkill : MonoBehaviourPunCallbacks
{
    [Header("Skill UI")]
    public Image CooldownBar; // Assign an Image with fillAmount for the skill bar
    public TextMeshProUGUI CooldownTime; // Shows countdown

    [Header("Skill Timing")]
    [Tooltip("How long the skill stays active when triggered (seconds)")]
    public float activeDuration = 15f;
    [Tooltip("Cooldown time after skill ends (seconds)")]
    public float cooldownDuration = 30f;

    private int skillIndex = -1; // Assigned from SkillIndex property
    private bool isActive = false;
    private bool isOnCooldown = false;
    private float timer = 0f;

    private Coroutine skillRoutine;

    // Freeze Gun Skill
    [Header("Freeze Gun Skill")]
    public GameObject FreezeGun; // Assign in inspector or via hierarchy
    public GameObject WeaponCrosshair; // Assign in inspector or via hierarchy
    public float freezeGunMoveForce = 10f; // How fast the freeze gun moves forward
    private bool freezeGunReadyToFire = false;

    [Header("Freeze Gun VFX")]
    public GameObject FreezeGunVFXPrefab; // Assign the VFX prefab in inspector
    public Camera playerCamera; // Assign the player's camera in inspector

    [Header("Third Person View")]
    public Animator TP_Animator; // Assign in inspector
    public bool isThirdPersonView = true; // Set this based on your camera system

    void Start()
    {
        ResetUI();
        // Set initial cooldown bar to full (ready) and sync to Photon
        SyncCooldownBarToPhoton();
        // Hide FreezeGun and WeaponCrosshair at start
        if (FreezeGun != null) FreezeGun.SetActive(false);
        if (WeaponCrosshair != null) WeaponCrosshair.SetActive(false);
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // Activate skill
        if (Input.GetKeyDown(KeyCode.E) && !isActive && !isOnCooldown)
        {
            if (skillRoutine != null) StopCoroutine(skillRoutine);
            skillRoutine = StartCoroutine(SkillActiveAndCooldownRoutine());
            if (FreezeGun != null) FreezeGun.SetActive(true);
            if (WeaponCrosshair != null) WeaponCrosshair.SetActive(true);
            freezeGunReadyToFire = true;
        }
    }

    private IEnumerator SkillActiveAndCooldownRoutine()
    {
        isActive = true;
        isOnCooldown = false;
        timer = activeDuration;
        bool freezeGunFired = false;
        if (FreezeGun != null && !FreezeGun.activeSelf)
            FreezeGun.SetActive(true);
        if (WeaponCrosshair != null && !WeaponCrosshair.activeSelf)
            WeaponCrosshair.SetActive(true);
        if (isThirdPersonView && FreezeGun != null)
            FreezeGun.SetActive(true);
        freezeGunReadyToFire = true;
        while (timer > 0f && isActive && !freezeGunFired)
        {
            timer -= Time.deltaTime;
            UpdateUI(timer, activeDuration, true);
            SyncCooldownBarToPhoton();
            // Check for left mouse click to fire freeze gun
            if (freezeGunReadyToFire && Input.GetMouseButtonDown(0))
            {
                Debug.Log("[FreezeGunSkill] Left mouse clicked - attempting to fire freeze gun");
                
                if (playerCamera == null)
                {
                    Debug.LogError("[FreezeGunSkill] playerCamera is null!");
                    continue;
                }
                
                if (FreezeGunVFXPrefab == null)
                {
                    Debug.LogError("[FreezeGunSkill] FreezeGunVFXPrefab is null!");
                    continue;
                }
                
                Debug.Log($"[FreezeGunSkill] FreezeGunVFXPrefab name: {FreezeGunVFXPrefab.name}");
                
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                RaycastHit hit;
                
                Debug.Log($"[FreezeGunSkill] Casting ray from {playerCamera.transform.position} in direction {playerCamera.transform.forward}");
                
                if (Physics.Raycast(ray, out hit, 100f))
                {
                    Debug.Log($"[FreezeGunSkill] Raycast hit at {hit.point} on object: {hit.collider.name}");
                    
                    try
                    {
                        GameObject spawnedFreezeGun = PhotonNetwork.Instantiate(FreezeGunVFXPrefab.name, hit.point, Quaternion.identity);
                        Debug.Log($"[FreezeGunSkill] Successfully spawned freeze gun effect: {spawnedFreezeGun.name}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[FreezeGunSkill] Failed to spawn freeze gun effect: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning("[FreezeGunSkill] Raycast did not hit anything within 100 units");
                }
                // TP_View: Play animation and hide FreezeGun
                if (isThirdPersonView && TP_Animator != null)
                    TP_Animator.SetBool("isSwordAttacking", true);
                if (isThirdPersonView && FreezeGun != null)
                    FreezeGun.SetActive(false);
                if (isThirdPersonView && TP_Animator != null)
                    StartCoroutine(ResetSwordAttackAnim());
                freezeGunReadyToFire = false;
                freezeGunFired = true;
                break; // Immediately end active phase and go to cooldown
            }
            yield return null;
        }
        isActive = false;
        UpdateUI(0, activeDuration, true);
        SyncCooldownBarToPhoton();
        // Always hide FreezeGun and WeaponCrosshair at end of active phase
        if (FreezeGun != null) FreezeGun.SetActive(false);
        if (WeaponCrosshair != null) WeaponCrosshair.SetActive(false);
        freezeGunReadyToFire = false;
        // Start cooldown for 30s
        isOnCooldown = true;
        timer = 0f;
        while (timer < cooldownDuration)
        {
            timer += Time.deltaTime;
            UpdateUI(timer, cooldownDuration, false);
            SyncCooldownBarToPhoton();
            yield return null;
        }
        isOnCooldown = false;
        UpdateUI(cooldownDuration, cooldownDuration, false);
        SyncCooldownBarToPhoton();
    }

    // Coroutine to reset the animation parameter
    private IEnumerator ResetSwordAttackAnim()
    {
        yield return new WaitForSeconds(0.5f); // Adjust to match your animation length
        if (TP_Animator != null)
            TP_Animator.SetBool("isSwordAttacking", false);
    }

    private void UpdateUI(float t, float max, bool isActivePhase)
    {
        if (CooldownBar != null)
        {
            if (isActivePhase)
                CooldownBar.fillAmount = t / max; // Decrease from 1 to 0
            else
                CooldownBar.fillAmount = t / max; // Increase from 0 to 1
        }
        if (CooldownTime != null)
        {
            int seconds = Mathf.CeilToInt(isActivePhase ? t : (max - t));
            CooldownTime.text = seconds.ToString();
        }
    }

    private void SyncCooldownBarToPhoton()
    {
        if (photonView != null && photonView.IsMine && CooldownBar != null)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props["SkillCooldownPercent"] = CooldownBar.fillAmount;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    private void ResetUI()
    {
        if (CooldownBar != null) CooldownBar.fillAmount = 1f;
        if (CooldownTime != null) CooldownTime.text = "";
        if (FreezeGun != null) FreezeGun.SetActive(false);
        if (WeaponCrosshair != null) WeaponCrosshair.SetActive(false);
    }
}
