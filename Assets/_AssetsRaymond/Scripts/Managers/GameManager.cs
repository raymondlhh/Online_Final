using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;

// The PhotonView is no longer needed on the GameManager.
// You will need to remove it from the GameObject in the Unity Editor.
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [SerializeField]
    GameObject playerPrefab;

    [SerializeField]
    public Transform[] playerSpawners;

    private bool isCursorLocked = true;

    [Header("Game Timer")]
    [SerializeField] private float gameDuration = 900f; // 15 minutes in seconds
    private float timeRemaining;
    private bool timerIsRunning = false;
    private TextMeshProUGUI timerText;

    [Header("Game Mode")]
    [SerializeField] private bool isTesting = false;

    [Header("Spawner Visuals")]
    public GameObject[] spawnerVisuals;

    [Header("Enemy Spawning")]
    public GameObject[] enemyPrefabs;
    public Transform[] enemySpawners;
    public int maxEnemies = 40;
    public float spawnInterval = 10f;
    private bool gameHasEnded = false;

    [Header("Victim Spawning")]
    public GameObject[] victimPrefabs;
    public Transform[] victimSpawners;
    private int totalVictimsToSpawn;
    private int victimsSaved = 0;
    private TextMeshProUGUI victimsText;
    private const string VICTIMS_TO_SPAWN_KEY = "VictimsToSpawn";
    private const string VICTIMS_SAVED_KEY = "VictimsSaved";
    private const string GAMEOVER_REASON_KEY = "GameOverReason";
    private const string GAMEOVER_KEY = "GameOver";

    private float enemyDebugLogTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        
        // Ensure player is spawned when the scene loads
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            // Add a small delay to ensure everything is initialized
            StartCoroutine(SpawnPlayerDelayed());
        }
    }

    private IEnumerator SpawnPlayerDelayed()
    {
        yield return new WaitForSeconds(0.2f); // A small delay is still good practice.

        // The old logic relying on player properties was causing race conditions on restart.
        // This new logic is simpler and more robust: if our player doesn't exist in the scene, spawn it.
        var myPlayer = FindObjectsOfType<PlayerHealth>().FirstOrDefault(p => p.photonView.IsMine);
        if (myPlayer == null)
        {
            SpawnPlayerAtSpawner(PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // The GameManager is specific to the GameScene, so this is the most reliable
        // place to trigger BGM changes when using PhotonNetwork.LoadLevel.
        if (PlayerAudio.Instance != null)
        {
            Debug.Log("<color=orange>GameManager:</color> Starting GameScene, setting BGM volume to 0.1.");
            PlayerAudio.Instance.SetBGMVolume(0.1f);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(EnemySpawnRoutine());
            StartCoroutine(SetupVictims());
        }
    }

    private IEnumerator SetupVictims()
    {
        // Wait a frame to ensure all players are in
        yield return null;

        if (isTesting)
        {
            totalVictimsToSpawn = 1;
            Debug.Log("<color=yellow>--- TESTING MODE ENABLED: Spawning only 1 victim. ---</color>");
        }
        else
        {
            totalVictimsToSpawn = Random.Range(3, 7); // 3 to 6 victims
        }

        // Store in room properties so all players have the same goal
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props[VICTIMS_TO_SPAWN_KEY] = totalVictimsToSpawn;
        props[VICTIMS_SAVED_KEY] = 0; // Initialize saved count
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        // Get a list of unique spawn points
        List<Transform> availableSpawners = new List<Transform>(victimSpawners);
        for (int i = 0; i < totalVictimsToSpawn; i++)
        {
            if (availableSpawners.Count == 0) break; // Not enough spawners
            if (victimPrefabs.Length == 0)
            {
                Debug.LogError("No victim prefabs assigned in GameManager.");
                break;
            }

            int spawnIndex = Random.Range(0, availableSpawners.Count);
            Transform spawnPoint = availableSpawners[spawnIndex];
            availableSpawners.RemoveAt(spawnIndex); // Ensure spawner is unique

            // Randomly select one of the victim prefabs to spawn
            GameObject victimToSpawn = victimPrefabs[Random.Range(0, victimPrefabs.Length)];
            PhotonNetwork.Instantiate(victimToSpawn.name, spawnPoint.position, spawnPoint.rotation);
        }
    }

    public void UpdateVictimsSavedCount(int change)
    {
        // This should only ever be called on the Master Client.
        if (!PhotonNetwork.IsMasterClient) return;

        // Get current count from room properties.
        int currentSaved = 0;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(VICTIMS_SAVED_KEY, out object saved))
        {
            currentSaved = (int)saved;
        }
    
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props[VICTIMS_SAVED_KEY] = currentSaved + change;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private IEnumerator EnemySpawnRoutine()
    {
        // Add a short delay to prevent race conditions on game start.
        yield return new WaitForSeconds(1.5f);

        while (PhotonNetwork.InRoom) // This condition will stop the loop when we leave the room.
        {
            // The Master Client is responsible for spawning.
            if (PhotonNetwork.IsMasterClient)
            {
                if (GameObject.FindGameObjectsWithTag("Enemy").Length < maxEnemies)
                {
                    SpawnEnemy();
                }
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        // Add a guard clause to ensure spawners exist.
        if (enemySpawners == null || enemySpawners.Length == 0) return;

        int randomPrefabIndex = Random.Range(0, enemyPrefabs.Length);
        int randomSpawnerIndex = Random.Range(0, enemySpawners.Length);

        GameObject enemyPrefab = enemyPrefabs[randomPrefabIndex];
        Transform spawnPoint = enemySpawners[randomSpawnerIndex];

        // Add a null check to prevent errors when a scene is reloaded.
        // The spawner transform might be destroyed before this coroutine stops.
        if (spawnPoint != null)
        {
            PhotonNetwork.Instantiate(enemyPrefab.name, spawnPoint.position, spawnPoint.rotation);
        }
    }

    // Method to spawn a player at their designated spawner (called when revived)
    public void SpawnPlayerAtSpawner(int playerActorNumber)
    {
        if (playerPrefab != null && playerSpawners.Length >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            int playerIndex = playerActorNumber - 1; // ActorNumber starts at 1
            playerIndex = Mathf.Clamp(playerIndex, 0, playerSpawners.Length - 1);

            Transform spawnPoint = playerSpawners[playerIndex];
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.Log("Place playerPrefab or assign all spawners!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Toggle cursor lock with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isCursorLocked = !isCursorLocked;
            Cursor.lockState = isCursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isCursorLocked;
        }

        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else if (timerIsRunning)
            {
                timerIsRunning = false;
                Debug.Log("Time has run out!");

                // The Master Client is responsible for ending the game.
                if (PhotonNetwork.IsMasterClient && !gameHasEnded)
                {
                    var props = new ExitGames.Client.Photon.Hashtable();
                    props[GAMEOVER_KEY] = true;
                    props[GAMEOVER_REASON_KEY] = "TIME_OUT";
                    PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                    gameHasEnded = true;
                }
            }
        }

        // Win and Loss Condition Checks (Master Client only)
        if (PhotonNetwork.IsMasterClient && !gameHasEnded)
        {
            // Win if all required victims are saved
            if (totalVictimsToSpawn > 0 && victimsSaved >= totalVictimsToSpawn)
            {
                // Set a room property to signal the win, instead of using an RPC.
                // This avoids the need for a PhotonView on the GameManager.
                var props = new ExitGames.Client.Photon.Hashtable();
                props["GameWon"] = true;
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);

                gameHasEnded = true; // Set locally to prevent sending this multiple times.
            }
        }

        // Debug log for total enemies & Victim fall check
        enemyDebugLogTimer += Time.deltaTime;
        if (enemyDebugLogTimer >= 2f)
        {
            enemyDebugLogTimer = 0f;
            // Only log if we are in a room, to prevent errors in the menu.
            if(PhotonNetwork.InRoom)
            {
                Debug.Log($"<color=#FFD700>Real-time enemy count:</color> {GameObject.FindGameObjectsWithTag("Enemy").Length}");
            }
            
            // Master Client checks for fallen victims periodically
            if (PhotonNetwork.IsMasterClient && !gameHasEnded)
            {
                GameObject[] victims = GameObject.FindGameObjectsWithTag("Victim");
                foreach (var victim in victims)
                {
                    if (victim.transform.position.y < -30f)
                    {
                        var props = new ExitGames.Client.Photon.Hashtable();
                        props[GAMEOVER_KEY] = true;
                        props[GAMEOVER_REASON_KEY] = "VICTIM_DEAD";
                        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                        gameHasEnded = true; 

                        // Destroy the victim so this check doesn't run every frame on them.
                        PhotonNetwork.Destroy(victim);
                        break; 
                    }
                }
            }
        }
    }

    public void StartGameTimer()
    {
        if (timerText == null)
        {
            Debug.LogWarning("Timer Text is not assigned. Timer will not be displayed.");
        }
        timeRemaining = gameDuration;
        timerIsRunning = true;
    }

    public void RegisterTimerText(TextMeshProUGUI text)
    {
        timerText = text;
    }

    void DisplayTime(float timeToDisplay)
    {
        if (timerText == null) return;

        if (timeToDisplay < 0)
        {
            timeToDisplay = 0;
        }

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void HideSpawners()
    {
        if (spawnerVisuals != null)
        {
            foreach (GameObject spawner in spawnerVisuals)
            {
                if (spawner != null)
                {
                    spawner.SetActive(false);
                }
            }
        }
    }

    // Photon callbacks
    public override void OnJoinedRoom()
    {
        // Spawn the local player when they join the room
        if (PhotonNetwork.IsConnectedAndReady)
        {
            // Set IsAlive property
            var props = new ExitGames.Client.Photon.Hashtable();
            props["IsAlive"] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            SpawnPlayerAtSpawnerByIndex();
        }
    }

    // Spawn player at spawner based on their index in the player list
    public void SpawnPlayerAtSpawnerByIndex()
    {
        Player[] players = PhotonNetwork.PlayerList;
        int myIndex = -1;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                myIndex = i;
                break;
            }
        }

        if (myIndex >= 0 && myIndex < playerSpawners.Length)
        {
            Transform spawnPoint = playerSpawners[myIndex];
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError("Player index out of range or spawner not set up!");
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        // Set IsAlive property for new player
        var props = new ExitGames.Client.Photon.Hashtable();
        props["IsAlive"] = true;
        newPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // Check if victim properties have changed
        if (propertiesThatChanged.ContainsKey(VICTIMS_TO_SPAWN_KEY) || propertiesThatChanged.ContainsKey(VICTIMS_SAVED_KEY))
        {
            // Update local values from the definitive source: room properties
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(VICTIMS_SAVED_KEY, out object saved))
            {
                victimsSaved = (int)saved;
            }
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(VICTIMS_TO_SPAWN_KEY, out object total))
            {
                totalVictimsToSpawn = (int)total;
            }

            // Update the UI
            if (victimsText != null)
            {
                victimsText.text = $"{victimsSaved} / {totalVictimsToSpawn}";
            }
        }

        // Check if the game has been won
        if (propertiesThatChanged.ContainsKey("GameWon"))
        {
            // Find the local player's health script and trigger the succeed screen
            var myPlayer = FindObjectsOfType<PlayerHealth>().FirstOrDefault(p => p.photonView.IsMine);
            if (myPlayer != null)
            {
                myPlayer.TriggerSucceed();
            }
        }

        // Check if the game is over (loss condition)
        if (propertiesThatChanged.ContainsKey(GAMEOVER_KEY))
        {
            var myPlayer = FindObjectsOfType<PlayerHealth>().FirstOrDefault(p => p.photonView.IsMine);
            if (myPlayer != null)
            {
                string reason = (string)PhotonNetwork.CurrentRoom.CustomProperties[GAMEOVER_REASON_KEY];
                myPlayer.TriggerGameOver(reason);
            }
        }
    }

    public void RegisterVictimText(TextMeshProUGUI text)
    {
        victimsText = text;
        Debug.Log("<color=orange>GameManager:</color> Victim Text has been registered.");
        // Immediately update the text with the current values, in case this was registered late.
        if (victimsText != null)
        {
            victimsText.text = $"{victimsSaved} / {totalVictimsToSpawn}";
            Debug.Log($"<color=orange>GameManager:</color> Immediately updating registered text to {victimsSaved} / {totalVictimsToSpawn}.");
        }
    }

    public void LeaveGame()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        // This is now called on the persistent GameManager.
        // It's safe to disconnect and then load the menu.
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // Now that we are fully disconnected, load the main menu.
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");

        // AFTER loading the menu, this persistent manager's job is done.
        // Destroying it ensures a fresh one from the new scene is used next time,
        // preventing reference errors.
        Destroy(gameObject);
    }
}
