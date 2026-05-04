using UnityEngine;

public class AudioSpawner : MonoBehaviour
{
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = false;

    private void Start()
    {
        if (audioClip == null)
        {
            Debug.LogWarning("AudioSpawner: No audio clip assigned!");
            return;
        }

        GameObject audioObj = new GameObject("SpawnedAudio_" + audioClip.name);
        AudioSource source = audioObj.AddComponent<AudioSource>();

        source.clip = audioClip;
        source.volume = volume;
        source.loop = loop;
        source.spatialBlend = 0f; // 2D - not affected by listener position
        source.playOnAwake = false;
        source.Play();

        if (!loop)
            Destroy(audioObj, audioClip.length);
    }
}