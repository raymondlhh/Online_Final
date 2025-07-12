using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FireTorch : MonoBehaviourPunCallbacks
{
    public static FireTorch instance;
    
    public float extinguishTime = 5f;
    private bool isLit = false;
    private float timer = 0f;

    public GameObject fire;

    //void Awake()
    //{
    //    instance = this;
    //}

    void Start()
    {
        fire.SetActive(false);
    }


    void Update()
    {
        if (isLit)
        {
            timer += Time.deltaTime;
            if (timer >= extinguishTime && !SixFirePuzzleManager.Instance.AllFiresLit())
            {
                ExtinguishFire();
            }
        }
    }

    public void TryIgnite()
    {
        Debug.Log("Try to ignite!");
        fire.SetActive(true);
        isLit = true;
        Debug.Log("start counteodown from 5s");
        if (!isLit)
        {
            photonView.RPC("RPC_Ignite", RpcTarget.AllBuffered);
        }
        
    }

    [PunRPC]
    void RPC_Ignite()
    {
        if (!isLit)
        {
            isLit = true;
            timer = 0f;
            
            // Add visual fire effect here (e.g., particle system)
            SixFirePuzzleManager.Instance.RegisterLitFire(this);
        }
    }

    public void ExtinguishFire()
    {
        if (isLit)
        {
            isLit = false;
            timer = 0f;
            fire.SetActive(false);
            // Add extinguish effect here
            SixFirePuzzleManager.Instance.UnregisterLitFire(this);
        }
    }

    public bool IsLit()
    {
        return isLit;
    }
}
