using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class PlayerUI : MonoBehaviourPunCallbacks
{
    [Header("Random Skill Selected")]
    public GameObject randomSkillPanel;

    private bool countdownStarted = false;

    void Start()
    {
        // Show the randomSkillPanel for 5 seconds, then hide
        if (randomSkillPanel != null)
        {
            randomSkillPanel.SetActive(true);
            StartCoroutine(HideRandomSkillPanelAfterDelay(5f));
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
