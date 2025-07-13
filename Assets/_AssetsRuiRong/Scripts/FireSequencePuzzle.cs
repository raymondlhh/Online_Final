using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Unity.Burst.CompilerServices;


public class FireSequencePuzzle : MonoBehaviourPunCallbacks
{
    public int fireIndex; // 0 to 5 (based on correct sequence)
    private bool isLit = false;
    public GameObject fireEffect;

    // Start is called before the first frame update
    void Start()
    {
        fireEffect.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TryIgnite()
    {
        // Tell MasterClient we want to ignite
        if (PhotonNetwork.IsMasterClient)
        {
            //FireSequencePuzzleManager.Instance.AttemptIgnite(this.fireIndex);
        }
        else
        {
            photonView.RPC("RPC_RequestIgnite", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    void RPC_RequestIgnite()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            //FireSequencePuzzleManager.Instance.AttemptIgnite(this.fireIndex);
        }
    }

    [PunRPC]
    public void RPC_Ignite()
    {
        isLit = true;
        if (fireEffect != null)
            fireEffect.SetActive(true);
    }

    [PunRPC]
    public void RPC_Extinguish()
    {
        isLit = false;
        if (fireEffect != null)
            fireEffect.SetActive(false);
    }

    public void Ignite()
    {
        photonView.RPC("RPC_Ignite", RpcTarget.AllBuffered);
    }

    public void Extinguish()
    {
        photonView.RPC("RPC_Extinguish", RpcTarget.AllBuffered);
    }

    public bool IsLit() => isLit;
}
