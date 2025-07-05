using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Photon.Pun;

public class PlayerSkillDetails : MonoBehaviourPunCallbacks
{
    [Header("Skill UI")]
    public Image CooldownBar; // Assign an Image with fillAmount for the skill bar
    public TextMeshProUGUI timerText; // Shows countdown

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

    void Start()
    {
        // Get the assigned skill index from the player's properties
        PhotonView pv = GetComponentInParent<PhotonView>();
        if (pv != null && pv.Owner != null && pv.Owner.CustomProperties.ContainsKey("SkillIndex"))
        {
            skillIndex = (int)pv.Owner.CustomProperties["SkillIndex"];
        }
        ResetUI();
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        if (Input.GetKeyDown(KeyCode.E) && !isActive && !isOnCooldown)
        {
            if (skillRoutine != null) StopCoroutine(skillRoutine);
            skillRoutine = StartCoroutine(SkillActiveAndCooldownRoutine());
        }
    }

    private IEnumerator SkillActiveAndCooldownRoutine()
    {
        // Activate skill for 15s
        isActive = true;
        isOnCooldown = false;
        timer = activeDuration;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            UpdateUI(timer, activeDuration, true);
            yield return null;
        }
        isActive = false;
        UpdateUI(0, activeDuration, true);

        // Start cooldown for 30s
        isOnCooldown = true;
        timer = 0f;
        while (timer < cooldownDuration)
        {
            timer += Time.deltaTime;
            UpdateUI(timer, cooldownDuration, false);
            yield return null;
        }
        isOnCooldown = false;
        UpdateUI(cooldownDuration, cooldownDuration, false);
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
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(isActivePhase ? t : (max - t));
            timerText.text = seconds.ToString();
        }
    }

    private void ResetUI()
    {
        if (CooldownBar != null) CooldownBar.fillAmount = 1f;
        if (timerText != null) timerText.text = "";
    }
}
