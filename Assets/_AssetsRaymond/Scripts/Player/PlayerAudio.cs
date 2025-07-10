using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerAudioEntry
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

public class PlayerAudio : MonoBehaviour
{
    public List<PlayerAudioEntry> audioEntries;
    public AudioSource customAudioSource; // <-- Add this line for custom assignment

    private AudioSource audioSource;

    void Awake()
    {
        // Use custom AudioSource if assigned, otherwise get from this GameObject
        audioSource = customAudioSource != null ? customAudioSource : GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 3D sound setup
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 100f;
    }

    public void PlaySound(int index)
    {
        if (audioEntries != null && index >= 0 && index < audioEntries.Count && audioEntries[index].clip != null)
        {
            var entry = audioEntries[index];
            audioSource.clip = entry.clip;
            audioSource.volume = entry.volume;
            audioSource.pitch = entry.pitch;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"[PlayerAudio] No audio entry at index: {index}");
        }
    }

    public void PlaySound(string soundName)
    {
        if (audioEntries != null)
        {
            var entry = audioEntries.Find(e => e != null && e.name == soundName && e.clip != null);
            if (entry != null)
            {
                audioSource.clip = entry.clip;
                audioSource.volume = entry.volume;
                audioSource.pitch = entry.pitch;
                audioSource.Play();
                return;
            }
        }
        Debug.LogWarning($"[PlayerAudio] No audio entry with name: {soundName}");
    }
}
