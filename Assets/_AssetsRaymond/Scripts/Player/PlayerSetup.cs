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

    // Add references for FP_Name and TP_Name
    [SerializeField] private TMPro.TextMeshProUGUI FP_Name;
    [SerializeField] private TMPro.TextMeshProUGUI TP_Name;

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

        // Assign FP_Name if not set in Inspector
        if (FP_Name == null)
        {
            var fpNameTransform = transform.Find("FP_PlayerUI/PlayerPanels/PlayerProfile/FP_Name");
            if (fpNameTransform != null)
                FP_Name = fpNameTransform.GetComponent<TMPro.TextMeshProUGUI>();
        }
        // Assign TP_Name if not set in Inspector
        if (TP_Name == null)
        {
            var tpNameTransform = transform.Find("TP_PlayerUI/Canvas/TP_Name");
            if (tpNameTransform != null)
                TP_Name = tpNameTransform.GetComponent<TMPro.TextMeshProUGUI>();
        }

        // Set FP_Name and TP_Name visibility and text based on scene and ownership
        if (FP_Name != null)
        {
            FP_Name.text = photonView.Owner.NickName;
            // In TestCharactersScene, always set FP_Name color to white
            if (scene == "TestCharactersScene")
                FP_Name.color = Color.white;
            else
                FP_Name.color = photonView.IsMine ? Color.green : Color.white;
            // Only show FP_Name for local player and not in ChooseCharacterScene
            FP_Name.gameObject.SetActive(photonView.IsMine && scene != "ChooseCharacterScene");
        }
        if (TP_Name != null)
        {
            TP_Name.text = photonView.Owner.NickName;
            TP_Name.color = photonView.IsMine ? Color.green : Color.white;
            // In ChooseCharacterScene, show TP_Name for everyone; otherwise, only for remote players
            if (scene == "ChooseCharacterScene")
                TP_Name.gameObject.SetActive(true);
            else
                TP_Name.gameObject.SetActive(!photonView.IsMine);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
