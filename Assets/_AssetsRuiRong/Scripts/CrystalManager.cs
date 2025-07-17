using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CrystalManager : MonoBehaviourPunCallbacks
{
    public static CrystalManager Instance;

    public int totalCrystals = 3;
    private int crystalsCollected = 0;

    public bool AllCollected = true;

    private List<PlayerUICrystal> playerUIs = new List<PlayerUICrystal>();

    private void Awake()
    {
        Instance = this;
    }


    // Start is called before the first frame update
    void Start()
    {
        AllCollected = true;
        crystalsCollected = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RegisterPlayerUI(PlayerUICrystal ui)
    {
        if (!playerUIs.Contains(ui))
        {
            playerUIs.Add(ui);
            ui.UpdateCrystalUI(crystalsCollected, totalCrystals);
        }
    }

    public void CollectCrystal()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            crystalsCollected = Mathf.Min(crystalsCollected + 1, totalCrystals);
            photonView.RPC("RPC_UpdateCrystals", RpcTarget.All, crystalsCollected);
        }
    }

    [PunRPC]
    private void RPC_UpdateCrystals(int collected)
    {
        crystalsCollected = collected;

        foreach (PlayerUICrystal ui in playerUIs)
        {
            ui.UpdateCrystalUI(collected, totalCrystals);
        }

        if (crystalsCollected >= totalCrystals)
        {
            AllCollected = true;
            Debug.Log("All crystals collected! Starting countdown...");
            CountdownManager.Instance.StartCountdown();
        }
    }
}
