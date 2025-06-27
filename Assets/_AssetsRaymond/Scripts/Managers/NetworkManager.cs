using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Connection Status")]
    public Text connectionStatusText;

    [Header("Login UI Panel")]
    public InputField playerNameInput;
    public GameObject Login_UI_Panel;

    [Header("Game Options UI Panel")]
    public GameObject GameOptions_UI_Panel;

    [Header("Create RoomUI Panel")]
    public GameObject CreateRoom_UI_Panel;
    public InputField roomNameInputField;
    public InputField maxPlayerInputField;

    [Header("Inside Room UI Panel")]
    public GameObject InsideRoom_UI_Panel;
    public Text roomInfoText;
    public GameObject playerListPrefab;
    public GameObject playerListContent;
    public GameObject startGameButton;
    public Text roomNameText;
    public Text maxPlayersText;

    [Header("Room List UI Panel")]
    public GameObject RoomList_UI_Panel;
    public GameObject roomListEntryPrefab;
    public GameObject roomListParentGameobject;

    [Header("Join Random Room UI Panel")]
    public GameObject JoinRandomRoom_UI_Panel;

    [Header("Loading Panel")]
    public GameObject LoadingPanel;

    private Dictionary<string, RoomInfo> cachedRoomList;
    private Dictionary<string, GameObject> roomListGameobjects;
    private Dictionary<int, GameObject> playerListGameobjects;

    #region Unity Methods

    // Start is called before the first frame update
    void Start()
    {
        ActivatePanel(Login_UI_Panel.name);

        cachedRoomList = new Dictionary<string, RoomInfo>();
        roomListGameobjects = new Dictionary<string, GameObject>();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    // Update is called once per frame
    void Update()
    {
        connectionStatusText.text = "Connection status: " + PhotonNetwork.NetworkClientState;
    }
    #endregion

    #region UI Callbacks

    private void PlayButtonClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Button Pressed");
        }
    }

    public void OnLoginButtonClicked()
    {
        PlayButtonClickSound();
        string playerName = playerNameInput.text;
        if (!string.IsNullOrEmpty(playerName))
        {
            PhotonNetwork.LocalPlayer.NickName = playerName;
            PhotonNetwork.ConnectUsingSettings();
        }

        else
        {
            Debug.Log("Playername is invalid");
        }
    }

    public void OnCreateRoomButtonCliked()
    {
        PlayButtonClickSound();
        // Validate and get room name
        string roomName = roomNameInputField.text;
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = "Room" + Random.Range(1000, 10000);
        }

        // Validate and get max players
        int maxPlayers = 3; // Default value
        if (!string.IsNullOrEmpty(maxPlayerInputField.text))
        {
            int parsedValue;
            if (int.TryParse(maxPlayerInputField.text, out parsedValue))
            {
                maxPlayers = Mathf.Clamp(parsedValue, 1, 3);
            }
        }

        // Create room options
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)maxPlayers;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        // Create the room
        Debug.Log($"Creating room: {roomName} with max players: {maxPlayers}");
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Room creation failed: {message}");
        // Generate a new room name and try again
        string newRoomName = "Room" + Random.Range(1000, 10000);
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 3;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        PhotonNetwork.CreateRoom(newRoomName, roomOptions);
    }

    public void onCancelButtonClicked()
    {
        PlayButtonClickSound();
        ActivatePanel(GameOptions_UI_Panel.name);
    }

    public void OnShowRoomListButtonClicked()
    {
        PlayButtonClickSound();
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        ActivatePanel(RoomList_UI_Panel.name);
    }

    public void OnBackButtonClicked()
    {
        PlayButtonClickSound();
        if(PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }
        ActivatePanel(GameOptions_UI_Panel.name);
    }
    public void OnLeaveGameButtonClicked()
    {
        PlayButtonClickSound();
        PhotonNetwork.LeaveRoom();
    }
    public void OnJoinRandomRoomButtonClicked()
    {
        PlayButtonClickSound();
        ActivatePanel(JoinRandomRoom_UI_Panel.name);
        PhotonNetwork.JoinRandomRoom();
    }

    public void OnStartGameButtonClicked()
    {
        PlayButtonClickSound();
        if (PhotonNetwork.IsMasterClient)
        {
            ActivatePanel(LoadingPanel.name);
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    

    #endregion

    #region Photon Callbacks
    public override void OnConnected()
    {
        Debug.Log("Connected to the Internet");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " is connected to Photon ");
        ActivatePanel(GameOptions_UI_Panel.name);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log(PhotonNetwork.CurrentRoom.Name + " is created");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " joined to " + PhotonNetwork.CurrentRoom.Name);
        ActivatePanel(InsideRoom_UI_Panel.name);

        if (playerListGameobjects == null)
        {
            playerListGameobjects = new Dictionary<int, GameObject>();
        }

        UpdateRoomUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateRoomUI();
    }

    public override void OnLeftRoom()
    {
        ActivatePanel(GameOptions_UI_Panel.name);

        foreach (GameObject playerListGameobject in playerListGameobjects.Values)
        {
            Destroy(playerListGameobject);
        }
        playerListGameobjects.Clear();
        playerListGameobjects = null;
    }

    private void UpdateRoomUI()
    {
        // Update Room Info
        roomNameText.text = "Room Name: " + PhotonNetwork.CurrentRoom.Name;
        maxPlayersText.text = "Max Player: " + PhotonNetwork.CurrentRoom.PlayerCount + " / " + PhotonNetwork.CurrentRoom.MaxPlayers;
        
        // Clear previous player list
        foreach (GameObject playerListGameobject in playerListGameobjects.Values)
        {
            Destroy(playerListGameobject);
        }
        playerListGameobjects.Clear();

        // Regenerate player list
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject playerListGameobject = Instantiate(playerListPrefab);
            playerListGameobject.transform.SetParent(playerListContent.transform);
            playerListGameobject.transform.localScale = Vector3.one;

            playerListGameobject.transform.Find("PlayerNameText").GetComponent<Text>().text = player.NickName;

            playerListGameobject.transform.Find("PlayerIndicator").gameObject.SetActive(
                player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber
            );

            playerListGameobjects.Add(player.ActorNumber, playerListGameobject);
        }

        // Update Start Game Button
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        ClearRoomListView();

        foreach (var roomListGameobject in roomListGameobjects.Values)
        {
            Destroy(roomListGameobject);
        }

        roomListGameobjects.Clear();

        foreach (RoomInfo room in roomList)
        {
            Debug.Log(room.Name);
            if (!room.IsOpen || !room.IsVisible || room.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    cachedRoomList.Remove(room.Name);
                }
            }

            else
            {
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    cachedRoomList[room.Name] = room;
                }

                else
                {
                    cachedRoomList.Add(room.Name, room);
                }
            }
        }

        foreach (RoomInfo room in cachedRoomList.Values)
        {
            GameObject roomListEntryGameobject = Instantiate(roomListEntryPrefab);
            roomListEntryGameobject.transform.SetParent(roomListParentGameobject.transform);
            roomListEntryGameobject.transform.localScale = Vector3.one;

            roomListEntryGameobject.transform.Find("RoomNameText").GetComponent<Text>().text = room.Name;
            roomListEntryGameobject.transform.Find("RoomPlayersText").GetComponent<Text>().text = room.PlayerCount + "/" + room.MaxPlayers;
            roomListEntryGameobject.transform.Find("JoinRoomButton").GetComponent<Button>().onClick.AddListener(() => OnJoinRoomButtonClicked(room.Name));

            roomListGameobjects.Add(room.Name, roomListEntryGameobject);
        }
    }

    public override void OnLeftLobby()
    {
        ClearRoomListView();
        cachedRoomList.Clear();
    }
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log(message);
        string roomName = "Room" + Random.Range(1000, 10000);
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 3;

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    #endregion

    #region Public Methods

    void OnJoinRoomButtonClicked(string _roomName)
    {
        PlayButtonClickSound();
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        PhotonNetwork.JoinRoom(_roomName);
    }

    void ClearRoomListView()
    {
        foreach (var roomListGameobject in roomListGameobjects.Values)
        {
            Destroy(roomListGameobject);
        }
        roomListGameobjects.Clear();
    }
    public void ActivatePanel(string panelToBeActivated)
    {
        Login_UI_Panel.SetActive(panelToBeActivated.Equals(Login_UI_Panel.name));
        GameOptions_UI_Panel.SetActive(panelToBeActivated.Equals(GameOptions_UI_Panel.name));
        CreateRoom_UI_Panel.SetActive(panelToBeActivated.Equals(CreateRoom_UI_Panel.name));
        InsideRoom_UI_Panel.SetActive(panelToBeActivated.Equals(InsideRoom_UI_Panel.name));
        RoomList_UI_Panel.SetActive(panelToBeActivated.Equals(RoomList_UI_Panel.name));
        JoinRandomRoom_UI_Panel.SetActive(panelToBeActivated.Equals(JoinRandomRoom_UI_Panel.name));
        LoadingPanel.SetActive(panelToBeActivated.Equals(LoadingPanel.name));
    }
    #endregion

}
