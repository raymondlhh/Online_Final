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
    public GameObject[] FPS_Hands_ChildGameobjects;
    public GameObject[] Soldier_ChildGameobjects;

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

        // Always set IsSoldier true in ChooseCharacterScene
        if (animator != null)
            animator.SetBool("IsSoldier", true);

        if(photonView.IsMine)
        {
            Debug.Log("I am the local player, showing FPS hands.");
            // Show FPS hands, hide soldier body
            foreach (GameObject go in FPS_Hands_ChildGameobjects) go.SetActive(true);
            // foreach (GameObject go in Soldier_ChildGameobjects) go.SetActive(false);

            //Instantiate PlayerUI
            //GameObject playerUIGameobject = Instantiate(playerUIPrefab);
            //playerUIGameobject.transform.Find("FireButton").GetComponent<Button>().onClick.AddListener(() => shooter.Fire());

            FPSCamera.enabled = true;

            // Handle visibility for different scenes
            if (SceneManager.GetActiveScene().name == "TestCharacterScene")
            {
                // In TestCharacterScene: Show FP_View and FP_PlayerUI, hide TP_View and TP_PlayerUI
                SetFPViewVisibility(true);
                SetTPViewVisibility(false);
            }
        }
        else
        {
            Debug.Log("I am a remote player, showing soldier body.");
            // Hide FPS hands, show soldier body
            foreach (GameObject go in FPS_Hands_ChildGameobjects) go.SetActive(false);
            foreach (GameObject go in Soldier_ChildGameobjects) go.SetActive(true);

            playerMovementController.enabled = false;

            FPSCamera.enabled = false;

            // Handle visibility for different scenes
            if (SceneManager.GetActiveScene().name == "TestCharacterScene")
            {
                // In TestCharacterScene: Show only TP_View and TP_PlayerUI, hide FP_View and FP_PlayerUI
                SetFPViewVisibility(false);
                SetTPViewVisibility(true);
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

    void SetFPViewVisibility(bool isVisible)
    {
        // Find and set FP_View visibility
        Transform fpView = transform.Find("FP_View");
        if (fpView != null)
        {
            fpView.gameObject.SetActive(isVisible);
        }

        // Find and set FP_PlayerUI visibility
        Transform fpPlayerUI = transform.Find("FP_PlayerUI");
        if (fpPlayerUI != null)
        {
            fpPlayerUI.gameObject.SetActive(isVisible);
        }
    }

    void SetTPViewVisibility(bool isVisible)
    {
        // Find and set TP_View visibility
        Transform tpView = transform.Find("TP_View");
        if (tpView != null)
        {
            tpView.gameObject.SetActive(isVisible);
        }

        // Find and set TP_PlayerUI visibility
        Transform tpPlayerUI = transform.Find("TP_PlayerUI");
        if (tpPlayerUI != null)
        {
            tpPlayerUI.gameObject.SetActive(isVisible);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
