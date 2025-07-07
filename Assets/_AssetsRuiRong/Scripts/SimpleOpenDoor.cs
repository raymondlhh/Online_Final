using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SimpleOpenDoor : MonoBehaviour
{
    public Animator GateAnimator;

    // Start is called before the first frame update
    void Start()
    {
        GateAnimator.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GateAnimator.enabled=true;
        }
    }
}
