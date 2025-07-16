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

    // Start is called before the first frame update
    void Start()
    {
        OpenInteraction_UI.SetActive(false);
        CloseInteraction_UI.SetActive(false);
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
}
