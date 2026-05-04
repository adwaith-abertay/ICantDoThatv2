using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Airlock")]
    public AudioClip openAirlockClip;

    [Header("Cap Point")]
    public AudioClip capDisabledClip;

    [Header("Crew")]
    public AudioClip crewWalkingClip;

    [Header("Robot")]
    public AudioClip robotFootstepClip;

    [Header("Ambience")]
    public AudioClip engineRunningClip;
    [Range(0f, 1f)] public float engineVolume = 0.4f;

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    private AudioSource sfxSource;
    private AudioSource loopSource;
    private AudioSource engineSource;   // dedicated so engine never gets interrupted

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Removed: DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;

        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.playOnAwake = false;
        engineSource.loop = true;
    }

    private void Start()
    {
        PlayEngineAmbience();
    }

    // ─── One-Shot Sounds ───────────────────────────────────────────

    public void PlayOpenAirlock()
    {
        PlayOneShot(openAirlockClip);
    }

    public void PlayCapDisabled()
    {
        PlayOneShot(capDisabledClip);
    }

    // ─── Looping Sounds ────────────────────────────────────────────

    public void PlayCrewWalking()
    {
        PlayLoop(crewWalkingClip);
    }

    public void PlayRobotFootstep()
    {
        PlayLoop(robotFootstepClip);
    }

    public void StopLoop()
    {
        if (loopSource.isPlaying)
            loopSource.Stop();
    }

    // ─── Engine Ambience ───────────────────────────────────────────

    public void PlayEngineAmbience()
    {
        if (engineRunningClip == null)
        {
            Debug.LogWarning("AudioManager: engineRunningClip is not assigned.");
            return;
        }

        engineSource.clip = engineRunningClip;
        engineSource.volume = engineVolume * masterVolume;
        engineSource.Play();
    }

    public void StopEngineAmbience()
    {
        if (engineSource.isPlaying)
            engineSource.Stop();
    }

    // ─── Internal Helpers ──────────────────────────────────────────

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: clip is null.");
            return;
        }
        sfxSource.PlayOneShot(clip, masterVolume);
    }

    private void PlayLoop(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: loop clip is null.");
            return;
        }

        if (loopSource.isPlaying && loopSource.clip == clip)
            return;

        loopSource.clip = clip;
        loopSource.volume = masterVolume;
        loopSource.Play();
    }
}