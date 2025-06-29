using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("🎵 Audio Configuration")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private int audioSourcePoolSize = 200;

    [Header("🎼 Instrument Audio Clips")]
    [SerializeField] private InstrumentAudioData[] instruments;

    [Header("🎚️ Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.9f;
    [SerializeField] private bool enableNoteFadeOut = true;
    [SerializeField] private float noteFadeDuration = 3.0f; // TEST: Değeri geçici olarak 3 saniyeye çıkardık.

    [Header("🔧 Debugging")]
    [SerializeField] private bool showDebugLogs = false;

    [Header("📊 Performance Monitoring")]
    [SerializeField] private bool enableLatencyMonitoring = true;
    [SerializeField] private float averageLatency = 0f;

    // === ESKİ JAVA OYUNUNDAN: SOUND_RESOURCE_IDXS MAPPING SİSTEMİ ===
    // Bu sistem artık DataStructures.cs/AudioConstants'da merkezi olarak tanımlı

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
        catch (System.Exception e)
        {
            Debug.LogWarning($"📱 Could not apply mobile audio optimizations: {e.Message}");
        }
    }

    #region Note Playing (Simplified and Enhanced)
    public void PlayNote(InstrumentType instrument, int pitch, float volume = 1.0f, bool useJavaMapping = false, int line = 0)
    {
        int finalPitch = pitch;

        if (useJavaMapping)
        {
            finalPitch = AudioConstants.GetSoundIndex(line, pitch);
            if (showDebugLogs)
            {
                Debug.Log($"🎵 JAVA MAPPING: Line={line}, Pitch={pitch} → RealSoundIndex={finalPitch} ({instrument})");
            }
        }

        int instrumentId = (int)instrument;
        if (instrumentId < 0 || instrumentId >= instruments.Length || instruments[instrumentId].noteClips == null || instruments[instrumentId].noteClips.Length == 0)
        {
            if (showDebugLogs) Debug.LogWarning($"🎵 AudioManager: Instrument '{instrument}' is not configured or has no audio clips. Aborting PlayNote.");
            return;
        }

        int maxIdx = instruments[instrumentId].noteClips.Length - 1;
        finalPitch = GetInstrumentAdjustedIndex(instrument, finalPitch, maxIdx);

        /*
        if (showDebugLogs)
        {
            Debug.Log($"🎵 AudioManager: Attempting to play note. Instrument='{instrument}', FinalPitch={finalPitch}, Volume={volume:F2}");
        }
        */

        AudioSource audioSource = GetAvailableAudioSource();
        if (audioSource == null) return;

        AudioClip clip = GetNoteClip(instrument, finalPitch);
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
            // This source's lifecycle is now managed by the coroutine.
            // Remove it from the main recycling list to prevent conflicts.
            activeAudioSources.Remove(audioSource);
            StartCoroutine(FadeOutAndRecycle(audioSource, noteFadeDuration));
        }
        // If not fading, the source remains in activeAudioSources and will be 
        // recycled by the main Update loop when it finishes playing.
    }

    [System.Obsolete("Use PlayNote with useJavaMapping=true instead")]
    public void PlayNoteFromChart(int line, int pitch, InstrumentType instrument, float volume = 1.0f)
    {
        PlayNote(instrument, pitch, volume, useJavaMapping: true, line: line);
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

    IEnumerator MonitorMusicPlayback()
    {
        while (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            OnMusicTimeUpdate?.Invoke(musicAudioSource.time);
            yield return null;
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
        RecycleFinishedAudioSources();

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

    void OnDestroy()
    {
        PlayerPrefs.Save();
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

    AudioClip LoadAudioFromAssets(InstrumentType instrument, int pitch)
    {
        string instrumentFolder = instrument.ToString();
        string fileName = "";

        switch (instrument)
        {
            case InstrumentType.Piano:
                fileName = $"piano_snd{pitch:D3}";
                break;
            case InstrumentType.Harp:
                fileName = $"harp_snd{pitch:D3}";
                break;
            case InstrumentType.Guitar:
                fileName = $"acustic_guitar_snd{pitch:D3}";
                break;
        }

        string[] possiblePaths = {
            $"Audio/{instrumentFolder}/{fileName}",
            $"{instrumentFolder}/{fileName}",
            fileName
        };

        foreach (string path in possiblePaths)
        {
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip != null)
            {
                return clip;
            }
        }

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
                if (clip != null) return clip;
            }
        }

        Debug.LogWarning($"🎵 Could not load audio for {instrument} pitch {pitch}. Make sure files are in a Resources folder!");
        return null;
    }

    private int GetInstrumentAdjustedIndex(InstrumentType instrument, int baseIndex, int maxIndex)
    {
        int adjusted = baseIndex;
        switch (instrument)
        {
            case InstrumentType.Guitar:
                adjusted = baseIndex - 4;
                break;
            case InstrumentType.Harp:
                adjusted = baseIndex + 2;
                break;
        }
        return Mathf.Clamp(adjusted, 0, maxIndex);
    }

    IEnumerator FadeOutAndRecycle(AudioSource source, float fadeTime)
    {
        if (source == null || source.clip == null) yield break;

        float waitTime = Mathf.Max(0f, source.clip.length - fadeTime);
        yield return new WaitForSeconds(waitTime);

        if (showDebugLogs)
        {
            Debug.Log($"🎵 FADE START: '{source.clip.name}' klibi {fadeTime} saniye içinde sönümleniyor. (Beklenen süre: {waitTime:F2}s)");
        }

        float startVol = source.volume;
        float t = 0f;
        while (t < fadeTime && source != null)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        if (source != null)
        {
            source.Stop();
            source.volume = startVol;
            // The coroutine now exclusively manages this source,
            // so it returns it directly to the pool.
            audioSourcePool.Enqueue(source);
        }
    }
}

[System.Serializable]
public class InstrumentAudioData
{
    public InstrumentType instrumentType;
    public AudioClip[] noteClips;
    public string instrumentName;
}