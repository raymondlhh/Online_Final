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
    [Header("Player Choosing Panel")]
    public GameObject choosePlayerPanel;
    public Button JadenSelectButton;
    public Button AliceSelectButton;
    public Button JackSelectButton;
    public GameObject JadenChoosenPanel;
    public GameObject AliceChoosenPanel;
    public GameObject JackChoosenPanel;
    public Button JadenCancelButton;
    public Button AliceCancelButton;
    public Button JackCancelButton;
    public GameObject countdownObject;
    public TextMeshProUGUI countdownText; // Assign to the "Time" child in your Countdown object

    [Header("In-Game UI")]

    // Character keys for Photon room properties
    public const string JADEN_KEY = "JadenChosen";
    public const string ALICE_KEY = "AliceChosen";
    public const string JACK_KEY = "JackChosen";

    private PlayerAppearance appearance;
    private bool countdownStarted = false;

    void Start()
    {
        // Button listeners
        JadenSelectButton.onClick.AddListener(() => OnSelectCharacter(JADEN_KEY));
        AliceSelectButton.onClick.AddListener(() => OnSelectCharacter(ALICE_KEY));
        JackSelectButton.onClick.AddListener(() => OnSelectCharacter(JACK_KEY));
        JadenCancelButton.onClick.AddListener(() => OnCancelCharacter(JADEN_KEY));
        AliceCancelButton.onClick.AddListener(() => OnCancelCharacter(ALICE_KEY));
        JackCancelButton.onClick.AddListener(() => OnCancelCharacter(JACK_KEY));

        UpdateCharacterPanels();

        // Find and assign the local player's appearance controller
        StartCoroutine(FindLocalAppearanceController());

        // Pause the game until all players have chosen
        PauseGameplay();
    }

    IEnumerator FindLocalAppearanceController()
    {
        while (appearance == null)
        {
            foreach (var controller in FindObjectsOfType<PlayerAppearance>())
            {
                if (controller.photonView.IsMine)
                {
                    appearance = controller;
                    break;
                }
            }
            yield return null;
        }
    }

    void OnSelectCharacter(string key)
    {
        if (PlayerAudio.Instance != null)
        {
            PlayerAudio.Instance.PlaySFX("Button Pressed");
        }
        
        if (!IsCharacterTaken(key) && !LocalPlayerHasChosenCharacter())
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props[key] = PhotonNetwork.LocalPlayer.ActorNumber;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            // Change skin locally
            if (appearance != null)
            {
            }
        }
    }

    void OnCancelCharacter(string key)
    {
        if (PlayerAudio.Instance != null)
        {
            PlayerAudio.Instance.PlaySFX("Button Pressed");
        }

        if (IsCharacterTakenByMe(key))
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props[key] = null;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    bool IsCharacterTaken(string key)
    {
        return PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key) &&
               PhotonNetwork.CurrentRoom.CustomProperties[key] != null;
    }

    bool IsCharacterTakenByMe(string key)
    {
        return PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key) &&
               PhotonNetwork.CurrentRoom.CustomProperties[key] != null &&
               (int)PhotonNetwork.CurrentRoom.CustomProperties[key] == PhotonNetwork.LocalPlayer.ActorNumber;
    }

    bool LocalPlayerHasChosenCharacter()
    {
        return IsCharacterTakenByMe(JADEN_KEY) || IsCharacterTakenByMe(ALICE_KEY) || IsCharacterTakenByMe(JACK_KEY);
    }

    List<int> GetAllChosenActorNumbers()
    {
        List<int> chosen = new List<int>();
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        string[] keys = { JADEN_KEY, ALICE_KEY, JACK_KEY };
        foreach (string key in keys)
        {
            if (props.ContainsKey(key) && props[key] != null)
                chosen.Add((int)props[key]);
        }
        return chosen;
    }

    bool AllPlayersHaveChosen()
    {
        var chosen = GetAllChosenActorNumbers();
        return chosen.Distinct().Count() == PhotonNetwork.CurrentRoom.PlayerCount;
    }

    void UpdateCharacterPanels()
    {
        bool localPlayerHasChosen = LocalPlayerHasChosenCharacter();

        // Jaden
        bool jadenTaken = IsCharacterTaken(JADEN_KEY);
        bool jadenMine = IsCharacterTakenByMe(JADEN_KEY);
        JadenSelectButton.gameObject.SetActive(!jadenTaken && !localPlayerHasChosen);
        JadenChoosenPanel.SetActive(jadenTaken);
        JadenCancelButton.gameObject.SetActive(jadenMine);

        // Alice
        bool aliceTaken = IsCharacterTaken(ALICE_KEY);
        bool aliceMine = IsCharacterTakenByMe(ALICE_KEY);
        AliceSelectButton.gameObject.SetActive(!aliceTaken && !localPlayerHasChosen);
        AliceChoosenPanel.SetActive(aliceTaken);
        AliceCancelButton.gameObject.SetActive(aliceMine);

        // Jack
        bool jackTaken = IsCharacterTaken(JACK_KEY);
        bool jackMine = IsCharacterTakenByMe(JACK_KEY);
        JackSelectButton.gameObject.SetActive(!jackTaken && !localPlayerHasChosen);
        JackChoosenPanel.SetActive(jackTaken);
        JackCancelButton.gameObject.SetActive(jackMine);

        // Panel and cursor
        if (AllPlayersHaveChosen())
        {
            if (!countdownStarted)
            {
                countdownStarted = true;
                StartCoroutine(StartCountdown());
            }
        }
        else
        {
            countdownObject.SetActive(false);
            choosePlayerPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            countdownStarted = false;
            // Pause the game if not all have chosen
            PauseGameplay();
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        UpdateCharacterPanels();
    }

    IEnumerator StartCountdown()
    {
        countdownObject.SetActive(true);
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }
        countdownObject.SetActive(false);
        choosePlayerPanel.SetActive(false);
        ResumeGameplay();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGameTimer();
            GameManager.Instance.HideSpawners();
        }
    }

    void PauseGameplay()
    {
        var localPlayerMovement = FindObjectsOfType<PlayerMovement>().FirstOrDefault(p => p.GetComponent<Photon.Pun.PhotonView>()?.IsMine == true);
        if (localPlayerMovement != null)
            localPlayerMovement.enabled = false;

        var localPlayerShoot = FindObjectsOfType<PlayerAttack>().FirstOrDefault(s => s.GetComponent<Photon.Pun.PhotonView>()?.IsMine == true);
        if (localPlayerShoot != null)
            localPlayerShoot.enabled = false;
    }

    void ResumeGameplay()
    {
        var localPlayerMovement = FindObjectsOfType<PlayerMovement>().FirstOrDefault(p => p.GetComponent<Photon.Pun.PhotonView>()?.IsMine == true);
        if (localPlayerMovement != null)
            localPlayerMovement.enabled = true;

        var localPlayerShoot = FindObjectsOfType<PlayerAttack>().FirstOrDefault(s => s.GetComponent<Photon.Pun.PhotonView>()?.IsMine == true);
        if (localPlayerShoot != null)
            localPlayerShoot.enabled = true;
    }
}
