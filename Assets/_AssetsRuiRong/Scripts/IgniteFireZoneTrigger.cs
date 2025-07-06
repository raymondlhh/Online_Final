using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class IgniteFireZoneTrigger : MonoBehaviour
{
    private FireTorch fireTorch;

    public static IgniteFireZoneTrigger instance;

    public GameObject FirePressUI;

    void Awake()
    {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        fireTorch = GetComponent<FireTorch>();
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
                other.GetComponent<PlayerInteraction>().SetFireTriggerState(true);
                //FirePressUI.SetActive(true);
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
                other.GetComponent<PlayerInteraction>().SetFireTriggerState(false);
                //FirePressUI.SetActive(false);
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
