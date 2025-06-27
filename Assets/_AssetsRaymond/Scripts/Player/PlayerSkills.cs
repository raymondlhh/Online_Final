using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

public class PlayerSkills : MonoBehaviourPunCallbacks
{
    [Header("Character Skills UI")]
    public GameObject jadenSkillsUI;
    public GameObject aliceSkillsUI;
    public GameObject jackSkillsUI;

    [Header("Timer Display")]
    public TextMeshProUGUI timerText;

    private PhotonView _photonView;

    // Character keys for Photon room properties
    private const string JADEN_KEY = "JadenChosen";
    private const string ALICE_KEY = "AliceChosen";
    private const string JACK_KEY = "JackChosen";

    private void Start()
    {
        _photonView = GetComponentInParent<PhotonView>();

        // We only want to control the UI for the local player.
        if (_photonView != null && _photonView.IsMine)
        {
            UpdateSkillsUI();
            if (GameManager.Instance != null && timerText != null)
            {
                GameManager.Instance.RegisterTimerText(timerText);
            }
        }
        else
        {
            // For remote players, or if there's no PhotonView, disable all skill UIs.
            // This is especially important as this script is on a component in the FP_View.
            if (jadenSkillsUI != null) jadenSkillsUI.SetActive(false);
            if (aliceSkillsUI != null) aliceSkillsUI.SetActive(false);
            if (jackSkillsUI != null) jackSkillsUI.SetActive(false);
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // Only update if the view is ours and properties are relevant
        if (_photonView != null && _photonView.IsMine)
        {
            if (propertiesThatChanged.ContainsKey(JADEN_KEY) || 
                propertiesThatChanged.ContainsKey(ALICE_KEY) || 
                propertiesThatChanged.ContainsKey(JACK_KEY))
            {
                UpdateSkillsUI();
            }
        }
    }

    private void UpdateSkillsUI()
    {
        if (_photonView == null || !_photonView.IsMine || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        Player owner = _photonView.Owner;
        if (owner == null)
        {
            return;
        }

        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        props.TryGetValue(JADEN_KEY, out object jadenActor);
        props.TryGetValue(ALICE_KEY, out object aliceActor);
        props.TryGetValue(JACK_KEY, out object jackActor);

        bool isJaden = jadenActor is int actorNumJaden && actorNumJaden == owner.ActorNumber;
        bool isAlice = aliceActor is int actorNumAlice && actorNumAlice == owner.ActorNumber;
        bool isJack = jackActor is int actorNumJack && actorNumJack == owner.ActorNumber;

        if (jadenSkillsUI != null) jadenSkillsUI.SetActive(isJaden);
        if (aliceSkillsUI != null) aliceSkillsUI.SetActive(isAlice);
        if (jackSkillsUI != null) jackSkillsUI.SetActive(isJack);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
