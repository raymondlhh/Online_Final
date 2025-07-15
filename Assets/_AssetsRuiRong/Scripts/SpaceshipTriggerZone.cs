using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpaceshipTriggerZone : MonoBehaviour
{
    public GameObject OnBoard_UI;
    public GameObject Warning_UI;

    // Start is called before the first frame update
    void Start()
    {
        OnBoard_UI.SetActive(false);
        Warning_UI.SetActive(false);
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
                if(CrystalManager.Instance.AllCollected)
                {
                    OnBoard_UI.SetActive(true);
                    other.GetComponent<PlayerInteraction>().SetOnBoardTriggerState(true);
                }
                else
                {
                    Warning_UI.SetActive(true);
                }
                
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
                if (CrystalManager.Instance.AllCollected)
                {
                    other.GetComponent<PlayerInteraction>().SetOnBoardTriggerState(false);
                    OnBoard_UI.SetActive(false);
                }
                else
                {
                    Warning_UI.SetActive(false);
                }
            }
        }
    }
}
