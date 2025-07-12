using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CatLightBeam : MonoBehaviourPun
{
    public static CatLightBeam instance;

    public float targetYScale = 0.32f;
    public float speed = 0.01f;

    private Vector3 targetScale;
    private bool isScaling = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        StartScaling();
    }

    public void StartScaling()
    {
        targetScale = new Vector3(transform.localScale.x, targetYScale, transform.localScale.z);
        isScaling = true;

        // Notify all clients to scale
        photonView.RPC("RPC_StartScaling", RpcTarget.OthersBuffered, targetYScale);
    }

    void Update()
    {
        if (isScaling)
        {
            Vector3 current = transform.localScale;
            Vector3 next = new Vector3(current.x, Mathf.MoveTowards(current.y, targetScale.y, speed * Time.deltaTime), current.z);
            transform.localScale = next;

            if (Mathf.Abs(next.y - targetScale.y) < 0.001f)
            {
                isScaling = false;
            }
        }
    }

    [PunRPC]
    void RPC_StartScaling(float syncedYScale)
    {
        targetScale = new Vector3(transform.localScale.x, syncedYScale, transform.localScale.z);
        isScaling = true;
    }
}
