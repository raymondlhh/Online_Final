using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpaceShip : MonoBehaviour
{
    public Animator shipAnimator;
    public bool StartShip = false;
    public string nextSceneName;

    public static SpaceShip Instance;

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
        if (StartShip && shipAnimator != null && !shipAnimator.enabled)
        {
            shipAnimator.enabled = true;
        }

        StartCoroutine("LoadNextScene", 5f);
    }

    public void StartShipAnimation()
    {
        StartShip = true;
    }

    public void LoadNextScene()
    {
        PhotonNetwork.LoadLevel(nextSceneName); // Load next scene
    }
}
