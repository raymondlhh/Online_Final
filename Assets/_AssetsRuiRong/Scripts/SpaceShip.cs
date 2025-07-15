using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpaceShip : MonoBehaviourPunCallbacks
{
    public Animator shipAnimator;
    public bool StartShip = false;
    public string nextSceneName;

    public static SpaceShip Instance;

    private bool isAnimationStarted = false;

    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (shipAnimator == null)
        {
            shipAnimator = GetComponent<Animator>();
        }

        // Disable the Animator at start
        if (shipAnimator != null)
        {
            shipAnimator.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartShipAnimation()
    {
        if (!isAnimationStarted)
        {
            isAnimationStarted = true;
            photonView.RPC("RPC_StartShipAnimation", RpcTarget.All);
        }
    }

    [PunRPC]
    public void RPC_StartShipAnimation()
    {
        if (shipAnimator != null)
        {
            shipAnimator.enabled = true;
        }

        // Start coroutine only on the MasterClient to load scene after animation
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(LoadNextSceneAfterDelay(5f)); // Adjust time to match animation
        }
    }

    IEnumerator LoadNextSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PhotonNetwork.LoadLevel(nextSceneName);
    }
}
