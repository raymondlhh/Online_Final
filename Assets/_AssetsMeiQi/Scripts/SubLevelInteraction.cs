using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Burst.CompilerServices;
using Photon.Realtime;

public class SubLevelInteraction : MonoBehaviour
{
    private PhotonView photonView;

    public static SubLevelInteraction Instance;
    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        Instance = this;
    }

    private bool ActivateScript = false;
    private bool isChooseLevel = false;
    private bool isChoosing = false;

    private GameObject currentTarget;

    // Start is called before the first frame update
    void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false;
        }
        string scene = SceneManager.GetActiveScene().name;
        if(scene == "SubLevel")
        {
            ActivateScript = true;
        }
        else
        {
            ActivateScript = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return;

        if (ActivateScript)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2f))
            {
                if (hit.collider.CompareTag("SubLevelDoor"))
                {
                    currentTarget = hit.collider.gameObject;
                    var doorScript = currentTarget.GetComponent<OpenDoorSubLevel>();

                    if (doorScript.IsOpen())
                    {
                        SubLevelManager.Instance.ShowCloseUI();
                    }
                    else
                    {
                        SubLevelManager.Instance.ShowOpenUI();
                    }

                    // Open door on F press
                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        PhotonView doorPV = currentTarget.GetComponent<PhotonView>();
                        if (doorPV != null)
                        {
                            doorPV.RPC("RPC_TriggerDoor", RpcTarget.AllBuffered);
                        }
                    }
                    return;
                }

                if (hit.collider.CompareTag("MedicalBed"))
                {
                    currentTarget = hit.collider.gameObject;
                    Debug.Log("Hit MedicalBed!");
                    SubLevelManager.Instance.ShowMedicalUI();

                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        PhotonView bedView = currentTarget.GetComponent<PhotonView>();
                        if (bedView != null)
                        {
                            Debug.Log("Found!");
                            bedView.RPC("RPC_LieDown", RpcTarget.AllBuffered, photonView.ViewID);
                        }
                        else
                        {
                            Debug.Log("Not Found!");
                        }
                    }
                    return;
                }


            }
            // If not looking at door, hide UI
            currentTarget = null;
            SubLevelManager.Instance.HideUI();

            if(isChooseLevel)
            {
                if(Input.GetKeyDown(KeyCode.F))
                {
                    
                    PlayerMovement moveScript = GetComponent<PlayerMovement>();
                    if (!isChoosing)
                    {
                        SubLevelManager.Instance.ShowLevelUI(false);
                        SubLevelManager.Instance.ShowChooseLevelUI(true);
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                        if (moveScript != null)
                        {
                            moveScript.CanMove = false;
                            moveScript.CanLook = false;
                        }
                        isChoosing= true;
                    }else if(isChoosing)
                    {
                        SubLevelManager.Instance.ShowChooseLevelUI(false);
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                        isChoosing = false;
                        if (moveScript != null)
                        {
                            moveScript.CanMove = true;
                            moveScript.CanLook = true;
                        }
                    }
                    
                }
               

            }
        }
        
    }

    public void SetChooseLevelUI(bool inArea)
    {
        isChooseLevel = inArea;
        Debug.Log("SetChooseLevelUI called. inArea: " + inArea);
        SubLevelManager.Instance.ShowLevelUI(inArea);
    }
}
