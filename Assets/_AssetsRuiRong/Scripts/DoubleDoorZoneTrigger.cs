using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;


public class DoubleDoorZoneTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
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
                other.GetComponent<PlayerInteraction>().SetDoorTriggerState(true);
                DoubleDoorManager.Instance.AddPlayer(pv.ViewID);
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
                other.GetComponent<PlayerInteraction>().SetDoorTriggerState(false);
                DoubleDoorManager.Instance.RemovePlayer(pv.ViewID);
            }
        }
    }
}
