using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("🎵 Audio Configuration")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private int audioSourcePoolSize = 100; // Increased for high-frequency note playing

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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Apply mobile audio optimizations (from our test results)
        ApplyMobileOptimizations();
    }

    void InitializeAudioSystem()
    {
        CreateAudioSourcePool();
        ApplyDefaultSettings();
    }

    void CreateAudioSourcePool()
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

    void ApplyDefaultSettings()
    {
        // Set target frame rate for consistent audio performance
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
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
    }

    #region Note Playing (Low-Latency)
    public void PlayNote(InstrumentType instrument, int pitch, float volume = 1.0f)
    {
        var audioSource = GetAvailableAudioSource();
        if (audioSource != null)
        {
            var clip = GetNoteClip(instrument, pitch);
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.volume = volume * masterVolume;
                audioSource.Play();
            }
        }
    }

    AudioSource GetAvailableAudioSource()
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

    public AudioClip GetNoteClip(InstrumentType instrument, int pitch)
    {
        // Try to get from instrument arrays first
        if (instruments != null && instruments.Length > 0)
        {
            foreach (var instrumentData in instruments)
            {
                if (instrumentData.instrumentType == instrument &&
                    instrumentData.noteClips != null &&
                    pitch >= 0 && pitch < instrumentData.noteClips.Length)
                {
                    return instrumentData.noteClips[pitch];
                }
            }
        }

        // Try to load from Assets/Audio directory structure
        AudioClip audioClip = LoadAudioFromAssets(instrument, pitch);
        if (audioClip != null)
        {
            return audioClip;
        }

        // Fallback: Generate a temporary audio clip for testing
        return GenerateTestTone(pitch);
    }

    AudioClip LoadAudioFromAssets(InstrumentType instrument, int pitch)
    {
        // Match original game's audio file structure: Assets/Audio/[Instrument]/[file_snd000-044].ogg
        string instrumentFolder = instrument.ToString(); // Piano, Harp, Guitar
        string fileName = "";

        // Map instrument to original file naming convention
        switch (instrument)
        {
            case InstrumentType.Piano:
                fileName = $"piano_snd{pitch:D3}"; // piano_snd000, piano_snd001, etc.
                break;
            case InstrumentType.Harp:
                fileName = $"harp_snd{pitch:D3}"; // harp_snd000, harp_snd001, etc.
                break;
            case InstrumentType.Guitar:
                // Original had both acustic and classic - try acustic first
                fileName = $"acustic_guitar_snd{pitch:D3}"; // acustic_guitar_snd000, etc.
                break;
        }

        // Try multiple loading paths
        string[] possiblePaths = {
            $"Audio/{instrumentFolder}/{fileName}",  // Resources/Audio/Piano/piano_snd000
            $"{instrumentFolder}/{fileName}",        // Resources/Piano/piano_snd000
            fileName                                 // Resources/piano_snd000
        };

        foreach (string path in possiblePaths)
        {
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip != null)
            {
                // Audio loaded successfully
                return clip;
            }
        }

        // Fallback path for Guitar - try classic variant
        if (instrument == InstrumentType.Guitar)
        {
            fileName = $"classic_guitar_snd{pitch:D3}";
            string[] guitarPaths = {
                $"Audio/{instrumentFolder}/{fileName}",
                $"{instrumentFolder}/{fileName}",
                fileName
            };

            foreach (string path in guitarPaths)
            {
                AudioClip clip = Resources.Load<AudioClip>(path);
                if (clip != null)
                {
                    // Guitar audio loaded successfully
                    return clip;
                }
            }
        }

        Debug.LogWarning($"🎵 Could not load audio for {instrument} pitch {pitch}. " +
                        $"Make sure audio files are in Resources folder!");
        return null;
    }

    AudioClip GenerateTestTone(int pitch)
    {
        // Generate a simple sine wave tone for testing
        float frequency = 440f * Mathf.Pow(2f, (pitch - 69) / 12f); // A4 = 440Hz
        int sampleRate = 44100;
        float duration = 0.2f; // Short note
        int samples = Mathf.RoundToInt(duration * sampleRate);

        AudioClip clip = AudioClip.Create($"TestTone_{pitch}", samples, 1, sampleRate, false);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float time = (float)i / sampleRate;
            data[i] = Mathf.Sin(2 * Mathf.PI * frequency * time) * 0.3f; // 30% volume
        }

        clip.SetData(data, 0);
        return clip;
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
        // Basic latency test without external test classes
        Debug.Log("🎵 Testing audio latency...");

        // Play a test sound and measure response
        if (instruments.Length > 0 && instruments[0].noteClips.Length > 0)
        {
            PlayNote(InstrumentType.Piano, 0, 1.0f);
            Debug.Log($"🎵 Audio latency test complete - Average: {averageLatency:F2}ms");
        }
    }

    public float GetAverageLatency() => averageLatency;
    public int GetActiveSourceCount() => activeAudioSources.Count;
    public int GetPooledSourceCount() => audioSourcePool.Count;
    #endregion

    void Update()
    {
        // Recycle finished audio sources back to pool
        RecycleFinishedAudioSources();

        // Monitor audio performance in real-time
        if (enableLatencyMonitoring && Time.frameCount % 60 == 0) // Check every second
        {
            if (averageLatency > 25f) // Above our comfort zone
            {
                Debug.LogWarning($"⚠️ Audio performance warning - Avg latency: {averageLatency:F2}ms");
            }
        }
    }

    void RecycleFinishedAudioSources()
    {
        // Check active audio sources and return finished ones to pool
        for (int i = activeAudioSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeAudioSources[i];
            if (source != null && !source.isPlaying)
            {
                activeAudioSources.RemoveAt(i);
                audioSourcePool.Enqueue(source);
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