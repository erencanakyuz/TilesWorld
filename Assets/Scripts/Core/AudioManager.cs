using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("🎵 Audio Configuration")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private int audioSourcePoolSize = 64; // Increased for better performance and consistency

    [Header("🎼 Instrument Audio Clips")]
    [SerializeField] private InstrumentAudioData[] instruments;

    [Header("🎚️ Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.9f;
    [SerializeField] private bool enableNoteFadeOut = false; // Disabled for natural piano sound and better performance
    [SerializeField] private float noteFadeDuration = 0.0f; // Disabled - let notes play naturally

    [Header("🔧 Debugging")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("📊 Performance Monitoring")]
    [SerializeField] private bool enableLatencyMonitoring = true;
    [SerializeField] private float averageLatency = 0f;


    // === ESKİ JAVA OYUNUNDAN: SOUND_RESOURCE_IDXS MAPPING SİSTEMİ ===
    // Bu sistem artık DataStructures.cs/AudioConstants'da merkezi olarak tanımlı

    // Audio Source Pool for low-latency playback
    private Queue<AudioSource> audioSourcePool;
    private List<AudioSource> activeAudioSources;
    private List<FadingAudioSource> fadingAudioSources; // For managing fades in Update()


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
        // If instruments are not assigned in the inspector, try to load them automatically
        if (instruments == null || instruments.Length == 0)
        {
            if (showDebugLogs) Debug.Log("🎵 Instruments not set in Inspector, attempting to auto-load from Resources...");
            LoadInstrumentsFromResources();
        }

        ApplyMobileOptimizations();
        LoadVolumeSettings();
    }

    void InitializeAudioSystem()
    {
        CreateAudioSourcePool();
        ApplyDefaultSettings();
    }


    void CreateAudioSourcePool()
    {
        audioSourcePool = new Queue<AudioSource>();
        activeAudioSources = new List<AudioSource>();
        fadingAudioSources = new List<FadingAudioSource>(); // Initialize the new list

        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject audioObject = new GameObject($"PooledAudioSource_{i}");
            audioObject.transform.SetParent(transform);

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 1.0f;

            audioSourcePool.Enqueue(source);
        }

        GameObject musicObject = new GameObject("MusicAudioSource");
        musicObject.transform.SetParent(transform);
        musicAudioSource = musicObject.AddComponent<AudioSource>();
        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = false;

        GameObject backgroundObject = new GameObject("BackgroundAudioSource");
        backgroundObject.transform.SetParent(transform);
        backgroundAudioSource = backgroundObject.AddComponent<AudioSource>();
        backgroundAudioSource.playOnAwake = false;
        backgroundAudioSource.loop = true;
    }

    void ApplyDefaultSettings()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    void ApplyMobileOptimizations()
    {
        try
        {
#if UNITY_ANDROID || UNITY_IOS
            AudioConfiguration config = AudioSettings.GetConfiguration();
            config.dspBufferSize = 256;
            config.sampleRate = AudioSettings.outputSampleRate;
            AudioSettings.Reset(config);
            if (showDebugLogs) Debug.Log($"📱 Mobile audio optimized: {config.dspBufferSize} samples, {config.sampleRate}Hz");
#endif
        }
        catch (System.Exception)
        {
            // Mobile audio optimization failed - silently continue
        }
    }

    #region Note Playing (Simplified and Enhanced)
    
    /// <summary>
    /// ENHANCED: Play note with automatic volume calculation from note duration
    /// This centralizes volume calculation to eliminate duplicates across systems
    /// </summary>
    public void PlayNote(InstrumentType instrument, int pitch, float volume = 1.0f, bool useJavaMapping = false, int line = 0, float noteDuration = 1.0f)
    {
        // If note duration is provided and volume is default, calculate volume automatically
        if (noteDuration > 1.0f && Mathf.Approximately(volume, 1.0f))
        {
            volume = CalculateNoteVolume(noteDuration);
        }
        
        PlayNoteInternal(instrument, pitch, volume, useJavaMapping, line);
    }
    
    /// <summary>
    /// Calculate note volume based on JSON duration value (centralized from InteractiveMusicSystem)
    /// </summary>
    public float CalculateNoteVolume(float duration)
    {
        // Duration from JSON (1-9) maps to volume (0.3-1.0)
        return Mathf.Lerp(0.3f, 1.0f, (duration - 1) / 8f);
    }
    
    /// <summary>
    /// Internal note playing implementation
    /// </summary>
    private void PlayNoteInternal(InstrumentType instrument, int pitch, float volume, bool useJavaMapping, int line)
    {
        int instrumentId = (int)instrument;
        if (instrumentId < 0 || instrumentId >= instruments.Length || instruments[instrumentId].noteClips == null || instruments[instrumentId].noteClips.Length == 0)
        {
            if (showDebugLogs) Debug.LogWarning($"🎵 AudioManager: Instrument '{instrument}' is not configured or has no audio clips. Aborting PlayNote.");
            return;
        }

        int maxIndex = instruments[instrumentId].noteClips.Length - 1;

        // --- CENTRALIZED PITCH CALCULATION ---
        // All mapping logic (Java-style + instrument offset) is now in one place.
        int finalPitch;
        if (useJavaMapping)
        {
            try
            {
                finalPitch = AudioConstants.GetFinalSoundIndex(instrument, line, pitch, maxIndex);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"🎵 AudioConstants.GetFinalSoundIndex failed: {ex.Message}. Using fallback mapping.");
                finalPitch = Mathf.Clamp(pitch, 0, maxIndex);
            }
        }
        else
        {
            // If not using Java mapping, just use the raw pitch, but still clamp it.
            finalPitch = Mathf.Clamp(pitch, 0, maxIndex);
        }

        AudioSource audioSource = GetAvailableAudioSource(volume, finalPitch, instrument);
        if (audioSource == null) return;

        AudioClip clip = GetNoteClip(instrument, finalPitch);

        // Debug log removed - was spamming console with every note

        if (clip == null)
        {
            if (showDebugLogs) Debug.LogWarning($"🎵 Audio clip not found for {instrument} pitch {finalPitch}");
            // Return the source to the pool if we abort early, since it won't be used
            activeAudioSources.Remove(audioSource);
            audioSourcePool.Enqueue(audioSource);
            return;
        }

        audioSource.clip = clip;
        audioSource.volume = volume * sfxVolume * masterVolume;
        audioSource.Play();


        if (enableNoteFadeOut)
        {
            // Instead of starting a coroutine, add to the list to be managed by Update()
            fadingAudioSources.Add(new FadingAudioSource
            {
                source = audioSource,
                fadeTimer = noteFadeDuration,
                initialVolume = audioSource.volume
            });
            // The source is removed from the active list as it's now managed by the fade-out process
            activeAudioSources.Remove(audioSource);
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

            _ = MonitorMusicPlaybackAsync();
        }
    }

    public void StopMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicAudioSource != null && !musicAudioSource.isPlaying)
        {
            musicAudioSource.UnPause();
        }
    }

    async Awaitable MonitorMusicPlaybackAsync()
    {
        while (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            OnMusicTimeUpdate?.Invoke(musicAudioSource.time);
            await Awaitable.NextFrameAsync();
        }
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
        if (instruments.Length > 0 && instruments[0].noteClips.Length > 0)
        {
            PlayNote(InstrumentType.Piano, 0, 1.0f);
        }
    }

    public float GetAverageLatency() => averageLatency;
    public int GetActiveSourceCount() => activeAudioSources.Count;
    public int GetPooledSourceCount() => audioSourcePool.Count;
    #endregion

    void Update()
    {
        // Optimize: Only run cleanup every 5 frames instead of every frame
        if (Time.frameCount % 5 == 0)
        {
            RecycleFinishedAudioSources();
            ProcessFadeOuts();
        }

        if (enableLatencyMonitoring && Time.frameCount % 60 == 0)
        {
            if (averageLatency > 25f)
            {
                Debug.LogWarning($"⚠️ Audio performance warning - Avg latency: {averageLatency:F2}ms");
            }
        }
    }

    void RecycleFinishedAudioSources()
    {
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

    void ProcessFadeOuts()
    {
        for (int i = fadingAudioSources.Count - 1; i >= 0; i--)
        {
            var fadingSource = fadingAudioSources[i];
            fadingSource.fadeTimer -= Time.deltaTime;

            if (fadingSource.fadeTimer <= 0)
            {
                // Fade is complete, recycle the source
                fadingSource.source.Stop();
                fadingSource.source.volume = fadingSource.initialVolume; // Reset volume
                audioSourcePool.Enqueue(fadingSource.source);
                fadingAudioSources.RemoveAt(i);
                

            }
            else
            {
                // CRITICAL FIX: Guard against divide by zero if noteFadeDuration is 0
                float safeDuration = Mathf.Max(0.001f, noteFadeDuration);
                fadingSource.source.volume = Mathf.Lerp(0f, fadingSource.initialVolume, fadingSource.fadeTimer / safeDuration);
            }
        }
    }

    void OnDestroy()
    {
        PlayerPrefs.Save();
    }

    AudioSource GetAvailableAudioSource(float noteVolume = 1.0f, int notePitch = 0, InstrumentType instrument = InstrumentType.Piano)
    {
        if (audioSourcePool.Count > 0)
        {
            AudioSource source = audioSourcePool.Dequeue();
            activeAudioSources.Add(source);
            return source;
        }

        // Expand pool dynamically if needed
        if (showDebugLogs) Debug.LogWarning($"🎵 Audio pool exhausted! Expanding from {audioSourcePoolSize} sources.");
        
        GameObject audioObject = new GameObject($"PooledAudioSource_{audioSourcePoolSize}");
        audioObject.transform.SetParent(transform);
        
        AudioSource newSource = audioObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.volume = 1.0f;
        
        audioSourcePoolSize++; // Track pool growth
        activeAudioSources.Add(newSource);
        
        return newSource;
    }






    public AudioClip GetNoteClip(InstrumentType instrument, int pitch)
    {
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

        AudioClip audioClip = LoadAudioFromAssets(instrument, pitch);
        if (audioClip != null)
        {
            return audioClip;
        }

        Debug.LogError($"🎵 [PRODUCTION] Missing audio file for {instrument} pitch {pitch} - no fallback available!");
        return null;
    }

    /// <summary>
    /// Load audio clip directly from Resources folder with fallback paths
    /// </summary>
    AudioClip LoadAudioFromAssets(InstrumentType instrument, int pitch)
    {
        string instrumentFolder = instrument.ToString();
        
        // Build file name based on instrument
        string fileName = instrument switch
        {
            InstrumentType.Piano => $"piano_snd{pitch:D3}",
            InstrumentType.Harp => $"harp_snd{pitch:D3}",
            InstrumentType.Guitar => $"acustic_guitar_snd{pitch:D3}",
            _ => $"unknown_snd{pitch:D3}"
        };

        // Try primary path
        string primaryPath = $"Audio/{instrumentFolder}/{fileName}";
        AudioClip clip = Resources.Load<AudioClip>(primaryPath);
        if (clip != null) return clip;

        // Try fallback paths
        clip = Resources.Load<AudioClip>($"{instrumentFolder}/{fileName}");
        if (clip != null) return clip;

        clip = Resources.Load<AudioClip>(fileName);
        if (clip != null) return clip;

        // Guitar alternative naming
        if (instrument == InstrumentType.Guitar)
        {
            string altFileName = $"classic_guitar_snd{pitch:D3}";
            
            clip = Resources.Load<AudioClip>($"Audio/{instrumentFolder}/{altFileName}");
            if (clip != null) return clip;
            
            clip = Resources.Load<AudioClip>($"{instrumentFolder}/{altFileName}");
            if (clip != null) return clip;
            
            clip = Resources.Load<AudioClip>(altFileName);
            if (clip != null) return clip;
        }

        if (showDebugLogs) Debug.LogWarning($"🎵 Could not load audio for {instrument} pitch {pitch}!");
        return null;
    }

    /// <summary>
    /// Loads all instrument audio clips from the Resources folder.
    /// This is a fallback for when instruments are not assigned in the Inspector.
    /// It expects a folder structure like: Resources/Audio/[InstrumentType]/[clip_name].ogg
    /// </summary>
    private void LoadInstrumentsFromResources()
    {
        var loadedInstruments = new List<InstrumentAudioData>();

        foreach (InstrumentType instrumentType in System.Enum.GetValues(typeof(InstrumentType)))
        {
            string instrumentName = instrumentType.ToString();
            string path = $"Audio/{instrumentName}";

            var clips = Resources.LoadAll<AudioClip>(path);

            if (clips != null && clips.Length > 0)
            {
                // Sort clips alphabetically to ensure consistent pitch mapping
                System.Array.Sort(clips, (a, b) => a.name.CompareTo(b.name));

                var instrumentData = new InstrumentAudioData
                {
                    instrumentType = instrumentType,
                    instrumentName = instrumentName,
                    noteClips = clips
                };
                loadedInstruments.Add(instrumentData);

                if (showDebugLogs) Debug.Log($"✅ Loaded {clips.Length} clips for instrument '{instrumentName}' from '{path}'");
            }
            else
            {
                if (showDebugLogs) Debug.LogWarning($"⚠️ No audio clips found for instrument '{instrumentName}' at path '{path}'");
            }
        }

        instruments = loadedInstruments.ToArray();

        if (instruments.Length == 0)
        {
            Debug.LogError("❌ FATAL: No instruments could be loaded at all! Check Resources/Audio folder structure. Notes will be silent.");
        }
    }

    /// <summary>
    /// Helper function to safely get the number of clips for an instrument.
    /// Used by external testing scripts.
    /// </summary>
    public int GetInstrumentClipCount(InstrumentType instrument)
    {
        int instrumentId = (int)instrument;
        if (instrumentId >= 0 && instrumentId < instruments.Length && instruments[instrumentId] != null)
        {
            return instruments[instrumentId].noteClips.Length;
        }
        return 0;
    }

    // Helper class to manage fade-out in Update() instead of using expensive coroutines
    private class FadingAudioSource
    {
        public AudioSource source;
        public float fadeTimer;
        public float initialVolume;
    }

}

[System.Serializable]
public class InstrumentAudioData
{
    public InstrumentType instrumentType;
    public AudioClip[] noteClips;
    public string instrumentName;
}