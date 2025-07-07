using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideManager : MonoBehaviour
{
    public static HideManager instance;

    [Header("UI Elements")]
    public GameObject hideUI;

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        hideUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetLocalUIVisibility(bool visible)
    {
        hideUI.SetActive(visible);
    }
}
