using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Hint1BZoneTrigger : MonoBehaviour
{
    // Start is called before the first frame update
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
                other.GetComponent<PlayerInteraction>().SetHint1BTriggerState(true);
                Hint1BManager.Instance.AddPlayer(pv.ViewID);
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
                other.GetComponent<PlayerInteraction>().SetHint1BTriggerState(false);
                Hint1BManager.Instance.RemovePlayer(pv.ViewID);
            }
        }
    }

}
