using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PauseManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button muteButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Text muteButtonText;

    private bool isPaused = false;
    private PhotonView photonView;
    private PlayerMovement playerMovement;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Start()
    {
        // This script should only run on the local player's instance.
        if (!photonView.IsMine)
        {
            // Destroy the pause manager on other players' prefabs to avoid input conflicts.
            Destroy(this);
            return;
        }

        // Hide the panel at the start of the game.
        pausePanel.SetActive(false);

        // Add listeners for the buttons.
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonPressed);
        }
        if (muteButton != null)
        {
            muteButton.onClick.AddListener(OnMuteButtonPressed);
        }

        // Initialize the mute button text based on the current audio listener state.
        UpdateMuteButtonText();
    }

    void Update()
    {
        // Toggle pause menu with the Escape key.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            // Unlock and show cursor when paused.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (playerMovement != null) playerMovement.CanLook = false;
        }
        else
        {
            // Lock and hide cursor when unpaused.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (playerMovement != null) playerMovement.CanLook = true;
        }
    }

    private void OnContinueButtonPressed()
    {
        // Play sound, then unpause.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Button Pressed");
        }
        TogglePause();
    }

    private void OnMuteButtonPressed()
    {
        // Play sound, then toggle mute.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Button Pressed");
            AudioManager.Instance.ToggleMasterMute();
        }
        UpdateMuteButtonText();
    }

    private void UpdateMuteButtonText()
    {
        if (muteButtonText != null && AudioManager.Instance != null)
        {
            // Check the global mute state from the AudioListener.
            if (AudioListener.pause)
            {
                muteButtonText.text = "Unmute";
            }
            else
            {
                muteButtonText.text = "Mute";
            }
        }
    }
} 