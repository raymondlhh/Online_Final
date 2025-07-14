using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class IgniteFireSZoneTrigger : MonoBehaviour
{
    public FireSequencePuzzle fireTorch;

    public GameObject FirePressUI;


    // Start is called before the first frame update
    void Start()
    {
        fireTorch = GetComponent<FireSequencePuzzle>();
        FirePressUI.SetActive(false);
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
                other.GetComponent<PlayerInteraction>().SetFireSTriggerState(true, this);
                FirePressUI.SetActive(true);
            }

            //if(PlayerInteraction.Instance.isIgnited)
            //{
            //    fireTorch.TryIgnite();
            //}
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                other.GetComponent<PlayerInteraction>().SetFireSTriggerState(false, null);
                FirePressUI.SetActive(false);
            }
        }
    }

    public void TryIgniteFromUI()
    {
        //var fireTorch = fireTorchObject.GetComponent<FireTorch>();
        if (fireTorch != null)
        {
            fireTorch.TryIgnite();
        }
    }
}
