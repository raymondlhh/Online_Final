using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
// using UnityEditor.SearchService;

public class FireTorch : MonoBehaviourPunCallbacks
{
    public static FireTorch instance;
    
    private float extinguishTime = 10f;
    private bool isLit = false;
    private float timer = 0f;

    public GameObject fire;
    string scene;

    //void Awake()
    //{
    //    instance = this;
    //}

    void Start()
    {
        fire.SetActive(false);
        scene = SceneManager.GetActiveScene().name;
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
        //Debug.Log("Try to ignite!");
        
        
        //Debug.Log("start counteodown from 5s");
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
            fire.SetActive(true);
            // Add visual fire effect here (e.g., particle system)
            Debug.Log("RPC_Ignite: Trying to register fire to SixFirePuzzleManager.");

                if (SixFirePuzzleManager.Instance != null)
                {
                    SixFirePuzzleManager.Instance.RegisterLitFire(this);
                }
                else
                {
                    Debug.LogError("SixFirePuzzleManager.Instance is NULL on this client!");
                }

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
