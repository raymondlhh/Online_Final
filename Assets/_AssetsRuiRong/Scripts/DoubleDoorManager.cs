using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class DoubleDoorManager : MonoBehaviour
{
    public static DoubleDoorManager Instance;

    [Header("UI Elements")]
    public GameObject doorUI;        // The combined UI (e.g. "Press F to Open" + progress bar)
    public Slider progressBar;       // Slider inside that UI

    [Header("Door Settings")]
    public float maxProgress = 5f;         // Time required to open the door
    public float speedPerPlayer = 0.5f;    // Speed multiplier per holding player

    private Dictionary<int, bool> playersInArea = new Dictionary<int, bool>(); // ViewID => isHolding
    private float currentProgress = 0f;
    private bool doorOpened = false;

    [Header("Door Animation")]
    public Animator doorAnimator;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        progressBar.maxValue = maxProgress;
        progressBar.value = 0f;
        doorUI.SetActive(false);

        if (doorAnimator != null)
        {
            doorAnimator.enabled = false; // Disable animator at the start
        }
    }

    void Update()
    {
        if (doorOpened) return;

        int holdingCount = 0;
        foreach (var isHolding in playersInArea.Values)
        {
            if (isHolding) holdingCount++;
        }

        if (holdingCount > 0)
        {
            float speed = holdingCount * speedPerPlayer;
            currentProgress += speed * Time.deltaTime;
            currentProgress = Mathf.Min(currentProgress, maxProgress);
            progressBar.value = currentProgress;

            if (!doorUI.activeSelf)
                doorUI.SetActive(true);

            if (currentProgress >= maxProgress)
            {
                doorOpened = true;
                OnDoorOpened();
            }
        }
        else
        {
            // Players are nearby but no one is holding
            progressBar.value = currentProgress;
        }
    }

    // Called when the player enters the door area
    public void AddPlayer(int viewID)
    {
        if (!playersInArea.ContainsKey(viewID))
        {
            playersInArea.Add(viewID, false);
        }
    }

    // Called when the player exits the door area
    public void RemovePlayer(int viewID)
    {
        if (playersInArea.ContainsKey(viewID))
        {
            playersInArea.Remove(viewID);
        }

        if (playersInArea.Count == 0 && !doorOpened)
        {
            // Hide the UI when no one is nearby
            doorUI.SetActive(false);
            currentProgress = 0f;
            progressBar.value = 0f;
        }
    }

    // Called when a player presses/releases F
    public void UpdatePlayerHolding(int viewID, bool isHolding)
    {
        if (playersInArea.ContainsKey(viewID))
        {
            playersInArea[viewID] = isHolding;
        }
    }

    // Called to show/hide the UI on local player's screen
    public void SetLocalUIVisibility(bool visible)
    {
        doorUI.SetActive(visible);
    }

    // When door is fully opened
    void OnDoorOpened()
    {
        Debug.Log("Door has been opened!");


        Destroy(doorUI); //.SetActive(false);

        if (doorAnimator != null)
        {
            doorAnimator.enabled = true;
        }

    }
}
