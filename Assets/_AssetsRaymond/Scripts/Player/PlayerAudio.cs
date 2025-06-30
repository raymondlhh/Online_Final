using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerAudio : MonoBehaviour
{
    public static PlayerAudio Instance { get; private set; }

    [Header("Sound Effects")]
    public PlayerSound[] sfxSounds;
    
    [Header("Background Music")]
    public PlayerSound[] bgmSounds;
    
    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource bgmSource;

    private Dictionary<string, PlayerSound> sfxDictionary = new Dictionary<string, PlayerSound>();
    private Dictionary<string, PlayerSound> bgmDictionary = new Dictionary<string, PlayerSound>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        Debug.Log("<color=cyan>AudioManager:</color> Awake() finished. Initializing sources and dictionaries.");
        InitializeAudioSources();
        InitializeSoundDictionaries();
        
        // Subscribe to the scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("<color=cyan>AudioManager:</color> Subscribed to sceneLoaded event.");
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks when the object is destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("<color=cyan>AudioManager:</color> Unsubscribed from sceneLoaded event.");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        Debug.Log($"<color=cyan>AudioManager:</color> New scene loaded: '{sceneName}'");

        if (sceneName == "MainMenuScene" || sceneName == "LobbyScene")
        {
            Debug.Log($"<color=cyan>AudioManager:</color> Scene is a menu/lobby. Attempting to play 'Main Menu & Lobby Scene' BGM and set volume to 0.5.");
            PlayBGM("Main Menu & Lobby Scene");
            SetBGMVolume(0.5f);
        }
        // The GameScene BGM is now handled entirely by the GameManager.
        // This OnSceneLoaded method in AudioManager is now only responsible for menu/lobby music.
    }

    void Start()
    {
        // Start logic can be placed here if needed in the future
    }

    void InitializeAudioSources()
    {
        // Ensure this persistent object has the one and only Audio Listener
        if (GetComponent<AudioListener>() == null)
        {
            gameObject.AddComponent<AudioListener>();
            Debug.Log("<color=cyan>AudioManager:</color> Added central AudioListener to self.");
        }

        // Create SFX AudioSource if not assigned
        if (sfxSource == null)
        {
            GameObject sfxObject = new GameObject("SFX AudioSource");
            sfxObject.transform.SetParent(transform);
            sfxSource = sfxObject.AddComponent<AudioSource>();
        }

        // Create BGM AudioSource if not assigned
        if (bgmSource == null)
        {
            GameObject bgmObject = new GameObject("BGM AudioSource");
            bgmObject.transform.SetParent(transform);
            bgmSource = bgmObject.AddComponent<AudioSource>();
            bgmSource.loop = true; // BGM typically loops
        }
    }

    void InitializeSoundDictionaries()
    {
        // Initialize SFX dictionary
        foreach (PlayerSound sound in sfxSounds)
        {
            if (sound != null && !string.IsNullOrEmpty(sound.name))
            {
                sfxDictionary[sound.name] = sound;
            }
        }

        // Initialize BGM dictionary
        foreach (PlayerSound sound in bgmSounds)
        {
            if (sound != null && !string.IsNullOrEmpty(sound.name))
            {
                bgmDictionary[sound.name] = sound;
            }
        }
    }

    public void PlaySFX(string soundName)
    {
        if (sfxDictionary.ContainsKey(soundName))
        {
            PlayerSound sound = sfxDictionary[soundName];
            sfxSource.PlayOneShot(sound.clip, sound.volume);
        }
        else
        {
            Debug.LogWarning($"SFX sound '{soundName}' not found!");
        }
    }

    public void PlayBGM(string soundName)
    {
        Debug.Log($"<color=green>AudioManager.PlayBGM():</color> Received request to play '{soundName}'.");
        if (bgmDictionary.ContainsKey(soundName))
        {
            PlayerSound sound = bgmDictionary[soundName];
            Debug.Log($"<color=green>AudioManager.PlayBGM():</color> Sound '{soundName}' found. Playing clip '{sound.clip.name}'.");
            
            // Stop current BGM if playing and it's a different clip
            if (bgmSource.isPlaying && bgmSource.clip != sound.clip)
            {
                bgmSource.Stop();
            }
            
            // Only play if the source is not already playing this exact clip
            if (bgmSource.clip != sound.clip || !bgmSource.isPlaying)
            {
                bgmSource.clip = sound.clip;
                bgmSource.volume = sound.volume;
                bgmSource.pitch = sound.pitch;
                bgmSource.loop = sound.loop;
                bgmSource.Play();
                Debug.Log($"<color=green>AudioManager.PlayBGM():</color> Music started.");
            }
            else
            {
                Debug.Log($"<color=yellow>AudioManager.PlayBGM():</color> Music '{soundName}' is already playing. No action taken.");
            }
        }
        else
        {
            Debug.LogWarning($"<color=red>AudioManager.PlayBGM():</color> BGM sound '{soundName}' not found in dictionary! Check for typos in the inspector or code.");
        }
    }

    public void StopBGM()
    {
        if (bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = Mathf.Clamp01(volume);
    }

    public void SetBGMVolume(float volume)
    {
        float newVolume = Mathf.Clamp01(volume);
        bgmSource.volume = newVolume;
        Debug.Log($"<color=blue>AudioManager:</color> BGM volume set to {newVolume}");
    }

    public void ToggleMasterMute()
    {
        AudioListener.pause = !AudioListener.pause;
        Debug.Log($"<color=magenta>AudioManager:</color> Master mute toggled. AudioListener.pause is now {AudioListener.pause}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
