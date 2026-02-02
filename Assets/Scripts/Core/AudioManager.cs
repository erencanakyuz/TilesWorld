using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("ğŸµ Audio Configuration")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private int audioSourcePoolSize = 128; // Increased for better performance and consistency
    [SerializeField] private int mobileMinAudioSources = 128;
    [SerializeField] private int poolGrowSize = 16;
    [Header("Voice Management")]
    [SerializeField] private int maxAudioSources = 256;
    [SerializeField] private bool allowVoiceStealing = true;
    [SerializeField] private bool allowPoolGrowth = false;
    [SerializeField] private int notePriority = 64;
    [SerializeField] private int musicPriority = 128;

    [Header("ğŸ¼ Instrument Audio Clips")]
    [SerializeField] private InstrumentAudioData[] instruments;

    [Header("ğŸšï¸ Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.9f;
    [SerializeField] private bool enableNoteFadeOut = false; // Disabled for natural piano sound and better performance
    [SerializeField] private float noteFadeDuration = 0.0f; // Disabled - let notes play naturally

    [Header("Debugging")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showMissingClipWarnings = true;

    [Header("Performance Monitoring")]
    [SerializeField] private bool enableLatencyMonitoring = true;
    [SerializeField] private float averageLatency = 0f;

    [Header("Audio Load Safety")]
    [SerializeField] private bool skipPlayIfClipNotLoaded = true;


    // === ESKÄ° JAVA OYUNUNDAN: SOUND_RESOURCE_IDXS MAPPING SÄ°STEMÄ° ===
    // Bu sistem artÄ±k DataStructures.cs/AudioConstants'da merkezi olarak tanÄ±mlÄ±

    // Audio Source Pool for low-latency playback
    private Queue<AudioSource> audioSourcePool;
    private List<AudioSource> activeAudioSources;
    private List<FadingAudioSource> fadingAudioSources; // For managing fades in Update()
    private Dictionary<InstrumentType, InstrumentAudioData> instrumentLookup;
    private HashSet<InstrumentType> prewarmedInstruments;
    private bool isPrewarming;
    private int droppedNotesNoClip;
    private int droppedNotesNotLoaded;
    private int droppedNotesNoVoice;
    private int stolenVoices;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private readonly HashSet<int> missingClipWarnings = new HashSet<int>();
    private readonly HashSet<int> notLoadedWarnings = new HashSet<int>();
#endif


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
            if (showDebugLogs) Debug.Log("ğŸµ Instruments not set in Inspector, attempting to auto-load from Resources...");
            LoadInstrumentsFromResources();
        }

        BuildInstrumentLookup();
        ApplyMobileOptimizations();
        LoadVolumeSettings();
    }

    void InitializeAudioSystem()
    {
        CreateAudioSourcePool();
        ApplyDefaultSettings();
        prewarmedInstruments = new HashSet<InstrumentType>();
        instrumentLookup = new Dictionary<InstrumentType, InstrumentAudioData>();
    }


    void CreateAudioSourcePool()
    {
        audioSourcePool = new Queue<AudioSource>();
        activeAudioSources = new List<AudioSource>();
        fadingAudioSources = new List<FadingAudioSource>(); // Initialize the new list

        if (Application.isMobilePlatform && audioSourcePoolSize < mobileMinAudioSources)
        {
            audioSourcePoolSize = mobileMinAudioSources;
        }
        if (maxAudioSources < audioSourcePoolSize)
        {
            maxAudioSources = audioSourcePoolSize;
        }


        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject audioObject = new GameObject($"PooledAudioSource_{i}");
            audioObject.transform.SetParent(transform);

            AudioSource source = audioObject.AddComponent<AudioSource>();
            ConfigurePooledSource(source);

            audioSourcePool.Enqueue(source);
        }

        GameObject musicObject = new GameObject("MusicAudioSource");
        musicObject.transform.SetParent(transform);
        musicAudioSource = musicObject.AddComponent<AudioSource>();
        ConfigureMusicSource(musicAudioSource);
        musicAudioSource.loop = false;

        GameObject backgroundObject = new GameObject("BackgroundAudioSource");
        backgroundObject.transform.SetParent(transform);
        backgroundAudioSource = backgroundObject.AddComponent<AudioSource>();
        ConfigureMusicSource(backgroundAudioSource);
        backgroundAudioSource.loop = true;
    }

    void ApplyDefaultSettings()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    void ConfigurePooledSource(AudioSource source)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.loop = false;
        source.volume = 1.0f;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.priority = Mathf.Clamp(notePriority, 0, 256);
    }

    void ConfigureMusicSource(AudioSource source)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.volume = 1.0f;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.priority = Mathf.Clamp(musicPriority, 0, 256);
    }

    void ApplyMobileOptimizations()
    {
        try
        {
#if UNITY_ANDROID || UNITY_IOS
            AudioConfiguration config = AudioSettings.GetConfiguration();
            config.dspBufferSize = 256;
            config.sampleRate = AudioSettings.outputSampleRate;
            config.numRealVoices = Mathf.Max(config.numRealVoices, audioSourcePoolSize);
            config.numVirtualVoices = Mathf.Max(config.numVirtualVoices, audioSourcePoolSize * 2);
            AudioSettings.Reset(config);
            if (showDebugLogs) Debug.Log($"ğŸ“± Mobile audio optimized: {config.dspBufferSize} samples, {config.sampleRate}Hz");
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
        if (!TryGetInstrumentData(instrument, out var instrumentData) ||
            instrumentData.noteClips == null || instrumentData.noteClips.Length == 0)
        {
            if (showDebugLogs) Debug.LogWarning($"AudioManager: Instrument '{instrument}' is not configured or has no audio clips. Aborting PlayNote.");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            droppedNotesNoClip++;
#endif
            return;
        }

        int maxIndex = instrumentData.noteClips.Length - 1;

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
                Debug.LogError($"ğŸµ AudioConstants.GetFinalSoundIndex failed: {ex.Message}. Using fallback mapping.");
                finalPitch = Mathf.Clamp(pitch, 0, maxIndex);
            }
        }
        else
        {
            // If not using Java mapping, just use the raw pitch, but still clamp it.
            finalPitch = Mathf.Clamp(pitch, 0, maxIndex);
        }

        AudioSource audioSource = GetAvailableAudioSource(volume, finalPitch, instrument);
        if (audioSource == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            droppedNotesNoVoice++;
#endif
            return;
        }

        AudioClip clip = GetNoteClip(instrument, finalPitch);

        // Debug log removed - was spamming console with every note

        if (clip == null)
        {
            if (showDebugLogs) Debug.LogWarning($"ğŸµ Audio clip not found for {instrument} pitch {finalPitch}");
            // Return the source to the pool if we abort early, since it won't be used
            activeAudioSources.Remove(audioSource);
            audioSourcePool.Enqueue(audioSource);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            droppedNotesNoClip++;
#endif
            return;
        }

        // Avoid blocking spikes if clip is not fully loaded yet
        if (skipPlayIfClipNotLoaded && clip.loadState != AudioDataLoadState.Loaded)
        {
            if (clip.loadState == AudioDataLoadState.Unloaded || clip.loadState == AudioDataLoadState.Failed)
            {
                clip.LoadAudioData();
            }
            activeAudioSources.Remove(audioSource);
            audioSourcePool.Enqueue(audioSource);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            droppedNotesNotLoaded++;
#endif
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

    public float GetMasterVolume()
    {
        return masterVolume;
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

    #region Prewarm
    public void PrewarmInstrumentClips(InstrumentType instrument)
    {
        _ = PrewarmInstrumentClipsAsync(instrument);
    }

    public async Awaitable PrewarmInstrumentClipsAsync(InstrumentType instrument)
    {
        if (prewarmedInstruments == null)
        {
            prewarmedInstruments = new HashSet<InstrumentType>();
        }

        if (prewarmedInstruments.Contains(instrument)) return;

        if (isPrewarming)
        {
            while (isPrewarming)
            {
                await Awaitable.NextFrameAsync();
            }

            if (prewarmedInstruments.Contains(instrument)) return;
        }

        isPrewarming = true;

        try
        {
            if (instruments == null || instruments.Length == 0)
            {
                LoadInstrumentsFromResources();
            }

            if (!TryGetInstrumentData(instrument, out var instrumentData) ||
                instrumentData.noteClips == null || instrumentData.noteClips.Length == 0)
            {
                return;
            }

            int counter = 0;
            int clipsPerFrame = 6;
            int maxWaitFrames = 300;
            for (int i = 0; i < instrumentData.noteClips.Length; i++)
            {
                var clip = instrumentData.noteClips[i];
                if (clip == null) continue;

                if (clip.loadState == AudioDataLoadState.Unloaded || clip.loadState == AudioDataLoadState.Failed)
                {
                    clip.LoadAudioData();
                }

                if (clip.loadState == AudioDataLoadState.Loading)
                {
                    int waitFrames = 0;
                    while (clip.loadState == AudioDataLoadState.Loading && waitFrames < maxWaitFrames)
                    {
                        waitFrames++;
                        await Awaitable.NextFrameAsync();
                    }
                }

                counter++;
                if (counter >= clipsPerFrame)
                {
                    counter = 0;
                    await Awaitable.NextFrameAsync();
                }
            }

            prewarmedInstruments.Add(instrument);
        }
        finally
        {
            isPrewarming = false;
        }
    }

    public async Awaitable EnsureInstrumentReadyAsync(InstrumentType instrument)
    {
        if (prewarmedInstruments != null && prewarmedInstruments.Contains(instrument)) return;
        await PrewarmInstrumentClipsAsync(instrument);
    }

    public bool IsInstrumentPrewarmed(InstrumentType instrument)
    {
        return prewarmedInstruments != null && prewarmedInstruments.Contains(instrument);
    }
    #endregion

    #region Audio Testing Integration
    public void TestAudioLatency()
    {
        if (GetInstrumentClipCount(InstrumentType.Piano) > 0)
        {
            PlayNote(InstrumentType.Piano, 0, 1.0f);
        }
    }

    public float GetAverageLatency() => averageLatency;
    public int GetActiveSourceCount() => activeAudioSources != null ? activeAudioSources.Count : 0;
    public int GetPooledSourceCount() => audioSourcePool != null ? audioSourcePool.Count : 0;
    public void GetDropStats(out int noClip, out int notLoaded, out int noVoice, out int stolen)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        noClip = droppedNotesNoClip;
        notLoaded = droppedNotesNotLoaded;
        noVoice = droppedNotesNoVoice;
        stolen = stolenVoices;
#else
        noClip = 0;
        notLoaded = 0;
        noVoice = 0;
        stolen = 0;
#endif
    }

    public void ResetDropStats()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        droppedNotesNoClip = 0;
        droppedNotesNotLoaded = 0;
        droppedNotesNoVoice = 0;
        stolenVoices = 0;
#endif
    }

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
                Debug.LogWarning($"âš ï¸ Audio performance warning - Avg latency: {averageLatency:F2}ms");
            }
        }
    }

    void RecycleFinishedAudioSources()
    {
        if (activeAudioSources == null || audioSourcePool == null) return;

        for (int i = activeAudioSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeAudioSources[i];
            if (source == null)
            {
                activeAudioSources.RemoveAt(i);
                continue;
            }

            if (!source.isPlaying)
            {
                source.clip = null;
                source.volume = 1.0f;
                activeAudioSources.RemoveAt(i);
                audioSourcePool.Enqueue(source);
                

            }
        }
    }

    void ProcessFadeOuts()
    {
        if (fadingAudioSources == null || audioSourcePool == null) return;

        for (int i = fadingAudioSources.Count - 1; i >= 0; i--)
        {
            var fadingSource = fadingAudioSources[i];
            if (fadingSource == null || fadingSource.source == null)
            {
                fadingAudioSources.RemoveAt(i);
                continue;
            }

            fadingSource.fadeTimer -= Time.deltaTime;

            if (fadingSource.fadeTimer <= 0)
            {
                // Fade is complete, recycle the source
                fadingSource.source.Stop();
                fadingSource.source.clip = null;
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
        if (audioSourcePool == null || activeAudioSources == null) return null;

        if (audioSourcePool.Count == 0)
        {
            RecycleFinishedAudioSources();
        }

        if (audioSourcePool.Count == 0 && allowVoiceStealing)
        {
            var stolenSource = StealOldestActiveSource();
            if (stolenSource != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                stolenVoices++;
#endif
                activeAudioSources.Add(stolenSource);
                return stolenSource;
            }
        }

        if (audioSourcePool.Count == 0 && allowPoolGrowth)
        {
            int availableRoom = Mathf.Max(0, maxAudioSources - audioSourcePoolSize);
            if (availableRoom > 0)
            {
                int growCount = Mathf.Min(Mathf.Max(1, poolGrowSize), availableRoom);
                if (showDebugLogs) Debug.LogWarning($"Audio pool exhausted. Expanding from {audioSourcePoolSize} sources.");
                GrowAudioSourcePool(growCount);
            }
        }

        if (audioSourcePool.Count > 0)
        {
            AudioSource source = audioSourcePool.Dequeue();
            activeAudioSources.Add(source);
            return source;
        }

        return null;
    }

    void GrowAudioSourcePool(int count)
    {
        if (count <= 0) return;

        int availableRoom = Mathf.Max(0, maxAudioSources - audioSourcePoolSize);
        int finalCount = Mathf.Min(count, availableRoom);
        if (finalCount <= 0) return;

        int startIndex = audioSourcePoolSize;
        for (int i = 0; i < finalCount; i++)
        {
            GameObject audioObject = new GameObject($"PooledAudioSource_{startIndex + i}");
            audioObject.transform.SetParent(transform);

            AudioSource newSource = audioObject.AddComponent<AudioSource>();
            ConfigurePooledSource(newSource);
            audioSourcePool.Enqueue(newSource);
        }

        audioSourcePoolSize += finalCount; // Track pool growth
    }
    AudioSource StealOldestActiveSource()
    {
        if (activeAudioSources == null || activeAudioSources.Count == 0) return null;

        int oldestIndex = -1;
        float oldestTime = -1f;

        for (int i = activeAudioSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeAudioSources[i];
            if (source == null)
            {
                activeAudioSources.RemoveAt(i);
                continue;
            }

            if (!source.isPlaying)
            {
                activeAudioSources.RemoveAt(i);
                if (audioSourcePool != null)
                {
                    audioSourcePool.Enqueue(source);
                }
                continue;
            }

            if (source.time > oldestTime)
            {
                oldestTime = source.time;
                oldestIndex = i;
            }
        }

        if (oldestIndex < 0) return null;

        AudioSource stolenSource = activeAudioSources[oldestIndex];
        activeAudioSources.RemoveAt(oldestIndex);
        stolenSource.Stop();
        stolenSource.clip = null;
        return stolenSource;
    }

    /// <summary>
    /// PERFORMANCE CRITICAL: Get audio clip from preloaded cache only.
    /// NO Resources.Load calls in hot path to prevent gameplay freezes.
    /// </summary>
    public AudioClip GetNoteClip(InstrumentType instrument, int pitch)
    {
        if (TryGetInstrumentData(instrument, out var instrumentData))
        {
            if (instrumentData.noteClips != null &&
                pitch >= 0 && pitch < instrumentData.noteClips.Length)
            {
                return instrumentData.noteClips[pitch];
            }
        }

        // PERFORMANCE FIX: Don't call LoadAudioFromAssets during gameplay!
        // This was causing 1+ second freezes due to synchronous Resources.Load calls.
        // If the clip is not preloaded, the note will simply be silent.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (showMissingClipWarnings)
        {
            int warnKey = ((int)instrument << 16) ^ pitch;
            if (missingClipWarnings.Add(warnKey))
            {
                Debug.LogWarning($"Audio clip not preloaded: {instrument} pitch {pitch}");
            }
        }
#endif
        return null;
    }

    /// <summary>
    /// Load audio clip directly from Resources folder with fallback paths.
    /// WARNING: This is BLOCKING I/O - only call during initialization, NOT during gameplay!
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

        return null;
    }

    private void BuildInstrumentLookup()
    {
        if (instrumentLookup == null)
        {
            instrumentLookup = new Dictionary<InstrumentType, InstrumentAudioData>();
        }

        instrumentLookup.Clear();
        if (instruments == null) return;

        foreach (var instrumentData in instruments)
        {
            if (instrumentData == null) continue;
            instrumentLookup[instrumentData.instrumentType] = instrumentData;
        }
    }

    private bool TryGetInstrumentData(InstrumentType instrument, out InstrumentAudioData instrumentData)
    {
        instrumentData = null;
        if (instrumentLookup != null && instrumentLookup.TryGetValue(instrument, out instrumentData) && instrumentData != null)
        {
            return true;
        }

        int instrumentId = (int)instrument;
        if (instruments != null && instrumentId >= 0 && instrumentId < instruments.Length)
        {
            instrumentData = instruments[instrumentId];
            return instrumentData != null;
        }

        return false;
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

                if (showDebugLogs) Debug.Log($"âœ… Loaded {clips.Length} clips for instrument '{instrumentName}' from '{path}'");
            }
            else
            {
                if (showDebugLogs) Debug.LogWarning($"âš ï¸ No audio clips found for instrument '{instrumentName}' at path '{path}'");
            }
        }

        instruments = loadedInstruments.ToArray();
        BuildInstrumentLookup();

        if (instruments.Length == 0)
        {
            Debug.LogError("âŒ FATAL: No instruments could be loaded at all! Check Resources/Audio folder structure. Notes will be silent.");
        }
    }

    /// <summary>
    /// Helper function to safely get the number of clips for an instrument.
    /// Used by external testing scripts.
    /// </summary>
    public int GetInstrumentClipCount(InstrumentType instrument)
    {
        if (TryGetInstrumentData(instrument, out var instrumentData) && instrumentData.noteClips != null)
        {
            return instrumentData.noteClips.Length;
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
