using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("🎵 Audio Configuration")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private int audioSourcePoolSize = 20;

    [Header("🎼 Instrument Audio Clips")]
    [SerializeField] private InstrumentAudioData[] instruments;

    [Header("🎚️ Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.9f;

    [Header("📊 Performance Monitoring")]
    [SerializeField] private bool enableLatencyMonitoring = true;
    [SerializeField] private float averageLatency = 0f;

    // Audio Source Pool for low-latency playback
    private Queue<AudioSource> audioSourcePool;
    private List<AudioSource> activeAudioSources;

    // Current playing music
    private AudioSource musicAudioSource;
    private AudioSource backgroundAudioSource;

    // Audio timing synchronization
    public float CurrentMusicTime => musicAudioSource != null && musicAudioSource.isPlaying
        ? musicAudioSource.time : 0f;

    public bool IsMusicPlaying => musicAudioSource != null && musicAudioSource.isPlaying;

    // Events
    public System.Action<float> OnMusicTimeUpdate;
    public System.Action OnMusicFinished;

    void Awake()
    {
        InitializeAudioSystem();
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Apply mobile audio optimizations (from our test results)
        ApplyMobileOptimizations();
    }

    void InitializeAudioSystem()
    {
        // Initialize audio source pool
        audioSourcePool = new Queue<AudioSource>();
        activeAudioSources = new List<AudioSource>();

        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject audioObject = new GameObject($"PooledAudioSource_{i}");
            audioObject.transform.SetParent(transform);

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 1.0f;

            audioSourcePool.Enqueue(source);
        }

        // Create dedicated music audio source
        GameObject musicObject = new GameObject("MusicAudioSource");
        musicObject.transform.SetParent(transform);
        musicAudioSource = musicObject.AddComponent<AudioSource>();
        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = false;

        // Create background audio source for rhythm tracks
        GameObject backgroundObject = new GameObject("BackgroundAudioSource");
        backgroundObject.transform.SetParent(transform);
        backgroundAudioSource = backgroundObject.AddComponent<AudioSource>();
        backgroundAudioSource.playOnAwake = false;
        backgroundAudioSource.loop = true;

        Debug.Log($"🎵 AudioManager initialized with {audioSourcePoolSize} pooled sources");
    }

    void ApplyMobileOptimizations()
    {
        // Apply optimizations from our successful mobile tests
        try
        {
#if UNITY_ANDROID || UNITY_IOS
            AudioConfiguration config = AudioSettings.GetConfiguration();
            config.dspBufferSize = 256;  // Low latency buffer (tested and working)
            config.sampleRate = AudioSettings.outputSampleRate;
            AudioSettings.Reset(config);
            
            Debug.Log($"📱 Mobile audio optimized: {config.dspBufferSize} samples, {config.sampleRate}Hz");
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"📱 Could not apply mobile audio optimizations: {e.Message}");
        }

        // Set target frame rate for consistent audio performance
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    #region Note Playing (Low-Latency)
    public void PlayNote(InstrumentType instrument, int noteIndex, float velocity = 1.0f)
    {
        if (enableLatencyMonitoring)
        {
            float startTime = Time.realtimeSinceStartup;
            StartCoroutine(MeasureLatency(startTime));
        }

        AudioClip clip = GetNoteClip(instrument, noteIndex);
        if (clip != null)
        {
            AudioSource source = GetPooledAudioSource();
            if (source != null)
            {
                source.clip = clip;
                source.volume = velocity * sfxVolume * masterVolume;
                source.Play();

                StartCoroutine(ReturnToPoolAfterPlayback(source, clip.length));
            }
        }
        else
        {
            Debug.LogWarning($"🎵 Note clip not found: {instrument} - {noteIndex}");
        }
    }

    IEnumerator MeasureLatency(float startTime)
    {
        yield return null; // Wait one frame
        float latency = (Time.realtimeSinceStartup - startTime) * 1000f;
        UpdateLatencyMonitoring(latency);
    }

    void UpdateLatencyMonitoring(float latency)
    {
        // Simple moving average
        averageLatency = (averageLatency * 0.9f) + (latency * 0.1f);

        if (latency > 20f) // Our target threshold
        {
            Debug.LogWarning($"⚠️ High audio latency detected: {latency:F2}ms (avg: {averageLatency:F2}ms)");
        }
    }

    AudioClip GetNoteClip(InstrumentType instrument, int noteIndex)
    {
        foreach (var instrumentData in instruments)
        {
            if (instrumentData.instrumentType == instrument)
            {
                if (noteIndex >= 0 && noteIndex < instrumentData.noteClips.Length)
                {
                    return instrumentData.noteClips[noteIndex];
                }
            }
        }
        return null;
    }

    AudioSource GetPooledAudioSource()
    {
        if (audioSourcePool.Count > 0)
        {
            AudioSource source = audioSourcePool.Dequeue();
            activeAudioSources.Add(source);
            return source;
        }

        Debug.LogWarning("🎵 Audio source pool exhausted! Consider increasing pool size.");
        return null;
    }

    IEnumerator ReturnToPoolAfterPlayback(AudioSource source, float clipLength)
    {
        yield return new WaitForSeconds(clipLength + 0.1f); // Small buffer

        if (activeAudioSources.Contains(source))
        {
            activeAudioSources.Remove(source);
            source.Stop();
            source.clip = null;
            audioSourcePool.Enqueue(source);
        }
    }
    #endregion

    #region Music Playback
    public void PlayMusic(AudioClip musicClip, float startTime = 0f)
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.clip = musicClip;
            musicAudioSource.volume = musicVolume * masterVolume;
            musicAudioSource.time = startTime;
            musicAudioSource.Play();

            StartCoroutine(MonitorMusicPlayback());

            Debug.Log($"🎵 Playing music: {musicClip.name} from {startTime:F2}s");
        }
    }

    public void StopMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
            Debug.Log("🎵 Music stopped");
        }
    }

    public void PauseMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause();
            Debug.Log("⏸️ Music paused");
        }
    }

    public void ResumeMusic()
    {
        if (musicAudioSource != null && !musicAudioSource.isPlaying)
        {
            musicAudioSource.UnPause();
            Debug.Log("▶️ Music resumed");
        }
    }

    IEnumerator MonitorMusicPlayback()
    {
        while (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            OnMusicTimeUpdate?.Invoke(musicAudioSource.time);
            yield return null;
        }

        // Music finished
        OnMusicFinished?.Invoke();
    }
    #endregion

    #region Volume Controls
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicAudioSource != null)
            musicAudioSource.volume = musicVolume * masterVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.9f);
    }
    #endregion

    #region Audio Testing Integration
    public void TestAudioLatency()
    {
        // Integration with our existing test systems
        var mobileAudioTester = FindObjectOfType<MobileAudioTester>();
        if (mobileAudioTester != null)
        {
            mobileAudioTester.TestMobileAudioLatency();
        }

        var audioLatencyTester = FindObjectOfType<AudioLatencyTester>();
        if (audioLatencyTester != null)
        {
            audioLatencyTester.TestAudioLatency();
        }
    }

    public float GetAverageLatency() => averageLatency;
    public int GetActiveSourceCount() => activeAudioSources.Count;
    public int GetPooledSourceCount() => audioSourcePool.Count;
    #endregion

    void Update()
    {
        // Monitor audio performance in real-time
        if (enableLatencyMonitoring && Time.frameCount % 60 == 0) // Check every second
        {
            if (averageLatency > 25f) // Above our comfort zone
            {
                Debug.LogWarning($"⚠️ Audio performance warning - Avg latency: {averageLatency:F2}ms");
            }
        }
    }

    void OnDestroy()
    {
        PlayerPrefs.Save();
    }
}

[System.Serializable]
public class InstrumentAudioData
{
    public InstrumentType instrumentType;
    public AudioClip[] noteClips; // 45 clips per instrument (as per original game)
    public string instrumentName;
}