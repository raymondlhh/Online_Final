using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class SprintBoostSkill : MonoBehaviourPunCallbacks
{
    [Header("Skill UI")]
    public Image CooldownBar; // Assign an Image with fillAmount for the skill bar
    public TextMeshProUGUI CooldownTime; // Shows countdown

    [Header("Skill Timing")]
    [Tooltip("How long the skill stays active when triggered (seconds)")]
    public float activeDuration = 10f;
    [Tooltip("Cooldown time after skill ends (seconds)")]
    public float cooldownDuration = 30f;

    private bool isActive = false;
    private bool isOnCooldown = false;
    private float timer = 0f;

    // Start is called before the first frame update
    void Start()
    {
        
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

    private void SetCooldownPercent(float percent)
    {
        if (photonView.IsMine)
        {
            if (CooldownBar != null) CooldownBar.fillAmount = percent;
            var props = new ExitGames.Client.Photon.Hashtable();
            props["SkillCooldownPercent"] = percent;
            Photon.Pun.PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        else
        {
            if (CooldownBar != null) CooldownBar.fillAmount = percent;
        }
    }

    private IEnumerator SkillDurationAndCooldown()
    {
        // Skill active phase
        timer = activeDuration;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            SetCooldownPercent(timer / activeDuration);
            yield return null;
        }
        SetCooldownPercent(0f);
        isActive = false;
        // Cooldown phase
        isOnCooldown = true;
        timer = 0f;
        while (timer < cooldownDuration)
        {
            timer += Time.deltaTime;
            SetCooldownPercent(timer / cooldownDuration);
            yield return null;
        }
        SetCooldownPercent(1f);
        isOnCooldown = false;
    }
}
