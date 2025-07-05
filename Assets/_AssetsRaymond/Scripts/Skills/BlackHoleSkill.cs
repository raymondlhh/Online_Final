using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Photon.Pun;

public class BlackHoleSkill : MonoBehaviourPunCallbacks
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

    // Black Hole Skill
    [Header("Black Hole Skill")]
    public GameObject TP_BlackHole; // Assign in inspector or via hierarchy (TP_View)
    public GameObject FP_BlackHole; // Assign in inspector or via hierarchy (FP_View)
    public GameObject WeaponCrosshair; // Assign in inspector or via hierarchy
    public float BlackHoleMoveForce = 10f; // How fast the decoy moves forward
    private bool BlackHoleReadyToThrow = false;

    [Header("Black Hole VFX")]
    public GameObject BlackHoleVFXPrefab; // Assign the VFX prefab in inspector
    public Camera playerCamera; // Assign the player's camera in inspector

    [Header("Third Person View")]
    public Animator TP_Animator; // Assign in inspector
    public bool isThirdPersonView = true; // Set this based on your camera system

    void Start()
    {
        ResetUI();
        SyncCooldownBarToPhoton();
        if (TP_BlackHole != null) TP_BlackHole.SetActive(false);
        if (WeaponCrosshair != null) WeaponCrosshair.SetActive(false);
        if (FP_BlackHole != null) FP_BlackHole.SetActive(false);
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // Activate skill
        if (Input.GetKeyDown(KeyCode.E) && !isActive && !isOnCooldown)
        {
            if (skillRoutine != null) StopCoroutine(skillRoutine);
            skillRoutine = StartCoroutine(SkillActiveAndCooldownRoutine());
            // Show in TP_View for all clients
            photonView.RPC("ShowTPBlackHole", RpcTarget.All, true);
            // Show in FP_View only for local player
            if (photonView.IsMine && FP_BlackHole != null)
                FP_BlackHole.SetActive(true);
            if (WeaponCrosshair != null) WeaponCrosshair.SetActive(true);
            BlackHoleReadyToThrow = true;
        }
    }

    private IEnumerator SkillActiveAndCooldownRoutine()
    {
        isActive = true;
        isOnCooldown = false;
        timer = activeDuration;
        bool decoyThrown = false;
        if (TP_BlackHole != null && !TP_BlackHole.activeSelf)
            TP_BlackHole.SetActive(true);
        if (WeaponCrosshair != null && !WeaponCrosshair.activeSelf)
            WeaponCrosshair.SetActive(true);
        if (isThirdPersonView && TP_BlackHole != null)
            TP_BlackHole.SetActive(true);
        BlackHoleReadyToThrow = true;
        while (timer > 0f && isActive && !decoyThrown)
        {
            timer -= Time.deltaTime;
            UpdateUI(timer, activeDuration, true);
            SyncCooldownBarToPhoton();
            // Check for left mouse click to throw decoy
            if (BlackHoleReadyToThrow && Input.GetMouseButtonDown(0))
            {
                Debug.Log("[PlayerSkillDetails] Left mouse clicked - attempting to spawn black hole");
                
                if (playerCamera == null)
                {
                    Debug.LogError("[PlayerSkillDetails] playerCamera is null!");
                    continue;
                }
                
                if (BlackHoleVFXPrefab == null)
                {
                    Debug.LogError("[PlayerSkillDetails] BlackHoleVFXPrefab is null!");
                    continue;
                }
                
                Debug.Log($"[PlayerSkillDetails] BlackHoleVFXPrefab name: {BlackHoleVFXPrefab.name}");
                
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                RaycastHit hit;
                
                Debug.Log($"[PlayerSkillDetails] Casting ray from {playerCamera.transform.position} in direction {playerCamera.transform.forward}");
                
                if (Physics.Raycast(ray, out hit, 100f))
                {
                    Debug.Log($"[PlayerSkillDetails] Raycast hit at {hit.point} on object: {hit.collider.name}");
                    
                    try
                    {
                        GameObject spawnedBlackHole = PhotonNetwork.Instantiate(BlackHoleVFXPrefab.name, hit.point, Quaternion.identity);
                        // Debug.Log($"[PlayerSkillDetails] Successfully spawned black hole: {spawnedBlackHole.name}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[PlayerSkillDetails] Failed to spawn black hole: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning("[PlayerSkillDetails] Raycast did not hit anything within 100 units");
                }
                // TP_View: Play animation and hide BlackHole
                if (isThirdPersonView && TP_Animator != null)
                    TP_Animator.SetBool("isSwordAttacking", true);
                photonView.RPC("ShowTPBlackHole", RpcTarget.All, false);
                if (photonView.IsMine && FP_BlackHole != null)
                    FP_BlackHole.SetActive(false);
                if (isThirdPersonView && TP_Animator != null)
                    StartCoroutine(ResetSwordAttackAnim());
                BlackHoleReadyToThrow = false;
                decoyThrown = true;
                break; // Immediately end active phase and go to cooldown
            }
            yield return null;
        }
        isActive = false;
        UpdateUI(0, activeDuration, true);
        SyncCooldownBarToPhoton();
        // Always hide BlackHole and WeaponCrosshair at end of active phase
        photonView.RPC("ShowTPBlackHole", RpcTarget.All, false);
        if (photonView.IsMine && FP_BlackHole != null)
            FP_BlackHole.SetActive(false);
        if (WeaponCrosshair != null) WeaponCrosshair.SetActive(false);
        BlackHoleReadyToThrow = false;
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
        if (TP_BlackHole != null) TP_BlackHole.SetActive(false);
        if (FP_BlackHole != null) FP_BlackHole.SetActive(false);
        if (WeaponCrosshair != null) WeaponCrosshair.SetActive(false);
    }

    [PunRPC]
    public void ShowTPBlackHole(bool show)
    {
        if (TP_BlackHole != null)
            TP_BlackHole.SetActive(show);
    }
}
