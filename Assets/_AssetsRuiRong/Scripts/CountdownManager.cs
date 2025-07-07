using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountdownManager : MonoBehaviour
{
    public static CountdownManager Instance;

    private float countdownDuration = 300f; // 5 minutes in seconds
    private float currentTime = 0f;
    private bool isCounting = false;

    private List<PlayerUICountdown> playerCountdownUIs = new List<PlayerUICountdown>();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterPlayerCountdownUI(PlayerUICountdown ui)
    {
        if (!playerCountdownUIs.Contains(ui))
        {
            playerCountdownUIs.Add(ui);
        }
    }

    public void StartCountdown()
    {
        if (isCounting) return;

        currentTime = countdownDuration;
        isCounting = true;

        foreach (var ui in playerCountdownUIs)
        {
            ui.SetCountdownUIVisible(true);
        }

        StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        while (currentTime > 0)
        {
            yield return new WaitForSeconds(1f);
            currentTime -= 1f;

            foreach (var ui in playerCountdownUIs)
            {
                ui.UpdateCountdownText(currentTime);
            }
        }

        isCounting = false;

        // Optional: Trigger something when countdown finishes
        Debug.Log(" Countdown complete!");
    }
}
