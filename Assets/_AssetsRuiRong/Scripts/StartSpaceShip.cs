using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class StartSpaceShip : MonoBehaviour
{
    public static StartSpaceShip Instance;

    [Header("Progress Settings")]
    public GameObject seat1ProgressUI;
    public float fillTime = 5f;
    public Slider progressBar;



    private float fillAmount = 0f;
    private bool isFilling = false;



    void Awake()
    {
        Instance = this;
    }


    // Start is called before the first frame update
    void Start()
    {
        seat1ProgressUI.SetActive(false);
        //spaceship = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isFilling)
        {
            fillAmount += Time.deltaTime;
            progressBar.value = fillAmount / fillTime;

            if (fillAmount >= fillTime)
            {
                isFilling = false;
                Debug.Log("Progress complete. Starting ship animation.");
                SpaceShip.Instance.StartShipAnimation();
                seat1ProgressUI.SetActive(false);
                //PhotonNetwork.LoadLevel(nextSceneName); // Load next scene
            }
        }
    }

    public void ShowSeat1ProgressBar()
    {
        seat1ProgressUI.SetActive(true);
        //StartCoroutine(FillProgressBar());
    }

    public void FillProgressBarUpdate()
    {
        if (!isFilling)
        {
            isFilling = true;
        }
    }

    public void ResetProgressBar()
    {
        isFilling = false;
        fillAmount = 0f;
        progressBar.value = 0f;
    }
}
