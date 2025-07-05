using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

[System.Serializable]
public class Skill
{
    public string skillName;
    public GameObject skillImageObject; // Assign ImageIce, ImageInvisible, etc.
    [TextArea]
    public string skillDescription;
}

public class PlayerUI : MonoBehaviourPunCallbacks
{
    [Header("Random Skill Selected")]
    public GameObject randomSkillPanel;
    public Text skillNameText;
    public Text skillDescriptionText;
    [SerializeField] private float delayShow;
    [SerializeField] private float delayHide;
    public List<Skill> allSkills; // Assign 6 skills in inspector

    private bool countdownStarted = false;

    void Start()
    {
        if (photonView.IsMine)
        {
            AssignUniqueSkillToPlayer();
        }
    }

    void AssignUniqueSkillToPlayer()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            var players = PhotonNetwork.PlayerList;
            var skillIndices = Enumerable.Range(0, allSkills.Count).OrderBy(x => Random.value).ToList();
            for (int i = 0; i < players.Length; i++)
            {
                int skillIndex = skillIndices[i];
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props["SkillIndex"] = skillIndex;
                players[i].SetCustomProperties(props);
            }
        }
        StartCoroutine(ShowSkillUIAfterDelay(delayShow));
    }

    IEnumerator ShowSkillUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("SkillIndex"))
        {
            int skillIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties["SkillIndex"];
            ShowSkillUI(skillIndex);
        }
    }

    void ShowSkillUI(int skillIndex)
    {
        if (randomSkillPanel != null)
        {
            randomSkillPanel.SetActive(true);
            foreach (var skill in allSkills)
                skill.skillImageObject.SetActive(false);
            allSkills[skillIndex].skillImageObject.SetActive(true);
            if (skillNameText != null)
                skillNameText.text = allSkills[skillIndex].skillName;
            if (skillDescriptionText != null)
                skillDescriptionText.text = allSkills[skillIndex].skillDescription;
            StartCoroutine(HideRandomSkillPanelAfterDelay(delayHide));
        }
    }

    private IEnumerator HideRandomSkillPanelAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (randomSkillPanel != null)
            randomSkillPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UpdateCharacterPanels() { }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        UpdateCharacterPanels();
    }
}
