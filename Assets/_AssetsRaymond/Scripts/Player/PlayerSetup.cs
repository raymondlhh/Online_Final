using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerSetup : MonoBehaviourPunCallbacks
{
    public GameObject[] FirstPersonObjects;
    public GameObject[] ThirdPersonObjects;

    // public GameObject playerUIPrefab;
    private PlayerMovement playerMovementController;

    public Camera FPSCamera;

    private Animator animator;

    private PlayerAttack shooter;

    public TextMeshProUGUI playerNameText;

    // Start is called before the first frame update
    void Start()
    {
        shooter = GetComponent<PlayerAttack>();
        animator = GetComponent<Animator>();   
        playerMovementController = GetComponent<PlayerMovement>();
        var playerVisibility = GetComponent<PlayerVisibility>();
        string scene = SceneManager.GetActiveScene().name;

        // Always set IsSoldier true in ChooseCharacterScene
        if (animator != null)
            animator.SetBool("IsSoldier", true);

        // Visibility logic for both scenes
        if (scene == "ChooseCharacterScene" && playerVisibility != null)
        {
            // Everyone sees only TP_View and TP_PlayerUI
            playerVisibility.SetFirstPersonVisibility(false);
            playerVisibility.SetThirdPersonVisibility(true);
        }
        else if (scene == "TestCharactersScene" && playerVisibility != null)
        {
            if (photonView.IsMine)
            {
                // Local player: see only FP_View and FP_PlayerUI
                playerVisibility.SetFirstPersonVisibility(true);
                playerVisibility.SetThirdPersonVisibility(false);
                if (playerMovementController != null)
                {
                    playerMovementController.CanMove = true;
                    playerMovementController.CanLook = true;
                }
            }
            else
            {
                // Remote players: see only TP_View and TP_PlayerUI
                playerVisibility.SetFirstPersonVisibility(false);
                playerVisibility.SetThirdPersonVisibility(true);
            }
        }

        // Find the PlayerNameText in the hierarchy
        if (playerNameText == null)
        {
            Transform nameTextTransform = transform.Find("PlayerHealthAndName/Canvas/PlayerNameText");
            if (nameTextTransform == null)
            {
                Debug.LogError("PlayerNameText not found! Check the hierarchy path.");
            }
            else
            {
                playerNameText = nameTextTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        // Set the player name
        if (playerNameText != null)
        {
            playerNameText.text = photonView.Owner.NickName;
            // Set color: green if this is the local player, white otherwise
            if (photonView.IsMine)
            {
                playerNameText.color = Color.green;
            }
            else
            {
                playerNameText.color = Color.white;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
