using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SubLevelManager : MonoBehaviourPunCallbacks
{
    public static SubLevelManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public GameObject OpenInteraction_UI;
    public GameObject CloseInteraction_UI;
    public GameObject MedicalBed_UI;
    public GameObject ChooseLevel_UI;
    public GameObject Level_UI;
    public string SceneName;

    // Start is called before the first frame update
    void Start()
    {
        OpenInteraction_UI.SetActive(false);
        CloseInteraction_UI.SetActive(false);
        MedicalBed_UI.SetActive(false);
        ChooseLevel_UI.SetActive(false);
        Level_UI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowOpenUI()
    {
        OpenInteraction_UI.SetActive(true);
        CloseInteraction_UI.SetActive(false);
    }

    public void ShowCloseUI()
    {
        CloseInteraction_UI.SetActive(true);
        OpenInteraction_UI.SetActive(false);
    }

    public void HideUI()
    {
        OpenInteraction_UI.SetActive(false);
        CloseInteraction_UI.SetActive(false);
        MedicalBed_UI.SetActive(false);
    }

    public void ShowMedicalUI()
    {
        MedicalBed_UI.SetActive(true);
    }

    public void ShowLevelUI(bool InArea)
    {
        ChooseLevel_UI.SetActive(InArea);
    }

    public void ShowChooseLevelUI(bool TF)
    {
        Level_UI.SetActive(TF);
    }

    public void GotoNextScene()
    {

        Debug.Log("Go to EmpireScene");
        PhotonNetwork.LoadLevel(SceneName);


    }
}
