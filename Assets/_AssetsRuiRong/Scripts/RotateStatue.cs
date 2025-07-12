using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class RotateStatue : MonoBehaviourPunCallbacks
{
    
    public GameObject HintUI;

    // Start is called before the first frame update
    void Start()
    {
        HintUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                HintUI.SetActive(true);
                other.GetComponent<PlayerInteraction>().SetStatueTriggerState(true, this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                HintUI.SetActive(false);
                other.GetComponent<PlayerInteraction>().SetStatueTriggerState(false, null);
            }
        }
    }

    public void StartRotate()
    {
        Debug.Log("Tri calling RPC_RotateStatue");
        photonView.RPC("RPC_rotateStatue", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void RPC_rotateStatue()
    {
        transform.Rotate(0f, 10f, 0f);
    }
}
