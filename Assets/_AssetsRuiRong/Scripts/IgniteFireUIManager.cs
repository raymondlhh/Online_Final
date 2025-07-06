using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class IgniteFireUIManager : MonoBehaviour
{
    public static IgniteFireUIManager Instance;

    [Header("UI Elements")]
    public GameObject IgniteFireUI;

    

    private HashSet<int> playersInArea = new HashSet<int>();

    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        IgniteFireUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Called when the player enters the door area
    public void AddPlayer(int viewID)
    {
        if (!playersInArea.Contains(viewID))
        {
            playersInArea.Add(viewID);
        }

        if (PhotonView.Find(viewID).IsMine)
        {
            SetLocalUIVisibility(true);
        }
    }

    // Called when the player exits the door area
    public void RemovePlayer(int viewID)
    {
        if (playersInArea.Contains(viewID))
        {
            playersInArea.Remove(viewID);
        }

        if (PhotonView.Find(viewID).IsMine)
        {
            SetLocalUIVisibility(false);
        }
    }

    // Called to show/hide the UI on local player's screen
    public void SetLocalUIVisibility(bool visible)
    {
        IgniteFireUI.SetActive(visible);
    }

    
}
