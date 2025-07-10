using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GuardAudioEntry
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

public class GuardAudio : MonoBehaviour
{
    public List<GuardAudioEntry> audioEntries;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 20f;
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
        Debug.LogWarning($"[GuardAudio] No audio entry with name: {soundName}");
    }
}
