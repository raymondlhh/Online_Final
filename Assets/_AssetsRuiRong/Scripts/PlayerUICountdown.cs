using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUICountdown : MonoBehaviour
{
    private Text countdownTitle;
    private Text countdownTimer;

    void Start()
    {
        // Modify this path to your actual prefab structure
        countdownTitle = transform.root.Find("FP_PlayerUI/RuiRongUI/CountdownMechanicsTitle")?.GetComponent<Text>();
        countdownTimer = transform.root.Find("FP_PlayerUI/RuiRongUI/CountdownMechanics")?.GetComponent<Text>();

        SetCountdownUIVisible(false); // Hide at start

        CountdownManager.Instance.RegisterPlayerCountdownUI(this);
    }

    public void SetCountdownUIVisible(bool isVisible)
    {
        if (countdownTitle != null) countdownTitle.gameObject.SetActive(isVisible);
        if (countdownTimer != null) countdownTimer.gameObject.SetActive(isVisible);
    }

    public void UpdateCountdownText(float time)
    {
        if (countdownTimer != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            countdownTimer.text = $"{minutes:00}:{seconds:00}";

            if (time <= 60f)
            {
                countdownTimer.color = Color.red;
            }
        }
    }
}
