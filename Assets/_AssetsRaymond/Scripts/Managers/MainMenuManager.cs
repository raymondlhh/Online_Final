using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Add this to use SceneManager

public class MainMenuManager : MonoBehaviour
{
    void Start()
    {
        // BGM is now handled automatically by AudioManager
    }

    // Called when the Start button is pressed
    public void StartButtonPressed()
    {
        SceneManager.LoadScene("LobbyScene"); // Replace with your exact scene name
    }
}
