using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideManager : MonoBehaviour
{
    public static HideManager instance;

    [Header("UI Elements")]
    public GameObject hideUI;
    public GameObject unhideUI;

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        hideUI.SetActive(false);
        unhideUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetLocalUIVisibility(bool visible)
    {
        hideUI.SetActive(visible);
    }

    public void SetLocalUIVisibility2(bool visible)
    {
        unhideUI.SetActive(visible);
    }
}
