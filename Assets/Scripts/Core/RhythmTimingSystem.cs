using UnityEngine;

/// <summary>
/// RhythmTimingSystem - Central timing utility for rhythm game.
/// Uses AudioSettings.dspTime for sample-accurate timing.
/// 
/// Usage:
/// 1. Call StartSong() when audio begins (or schedule with leadIn)
/// 2. Use GetSongTime() or InputToDspTime() for judgment
/// 3. Supports user calibration offset
/// </summary>
public class RhythmTimingSystem : MonoBehaviour
{
    public static RhythmTimingSystem Instance { get; private set; }

    [Header("Calibration")]
    [Tooltip("User calibration offset in seconds (positive = notes hit early)")]
    [SerializeField] private float userOffsetSeconds = 0f;
    
    [Header("Debug")]
    [SerializeField] private bool logTimingInfo = false;

    // DSP timing state
    private double dspStartTime = 0;
    private bool isSongPlaying = false;
    
    // Frame sync cache (updated once per frame for consistency)
    private double cachedDspTime;
    private double cachedRealtimeSinceStartup;
    private bool frameCacheValid = false;

    #region Unity Lifecycle

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        // Invalidate cache each frame - will be recalculated on first access
        frameCacheValid = false;
    }

    #endregion

    #region Public API - Song Control

    /// <summary>
    /// Call when song starts playing (after AudioSource.Play or PlayScheduled).
    /// </summary>
    public void StartSong()
    {
        UpdateFrameCache();
        dspStartTime = cachedDspTime;
        isSongPlaying = true;
        
        if (logTimingInfo)
        {
            Debug.Log($"[TIMING] Song started at DSP time: {dspStartTime:F4}");
        }
    }

    /// <summary>
    /// Call when using PlayScheduled - pass the scheduled DSP time.
    /// </summary>
    public void StartSongScheduled(double scheduledDspTime)
    {
        dspStartTime = scheduledDspTime;
        isSongPlaying = true;
        
        if (logTimingInfo)
        {
            Debug.Log($"[TIMING] Song scheduled at DSP time: {dspStartTime:F4}");
        }
    }

    /// <summary>
    /// Call when song ends or is stopped.
    /// </summary>
    public void StopSong()
    {
        isSongPlaying = false;
        
        if (logTimingInfo)
        {
            Debug.Log("[TIMING] Song stopped");
        }
    }

    /// <summary>
    /// Check if a song is currently playing.
    /// </summary>
    public bool IsSongPlaying => isSongPlaying;

    #endregion

    #region Public API - Time Queries

    /// <summary>
    /// Get current song time in seconds (time since song started).
    /// This is the primary method for note spawning and judgment.
    /// </summary>
    public double GetSongTime()
    {
        if (!isSongPlaying) return 0;
        
        UpdateFrameCache();
        return cachedDspTime - dspStartTime + userOffsetSeconds;
    }

    /// <summary>
    /// Get current song time as float (for legacy compatibility).
    /// </summary>
    public float GetSongTimeFloat()
    {
        return (float)GetSongTime();
    }

    /// <summary>
    /// Convert an input timestamp (from touch.time) to song time.
    /// Use this for accurate judgment of when the user actually tapped.
    /// </summary>
    public double InputTimeToSongTime(double inputTime)
    {
        if (!isSongPlaying) return 0;
        
        UpdateFrameCache();
        
        // Calculate how long ago the input occurred
        double timeSinceInput = cachedRealtimeSinceStartup - inputTime;
        
        // Estimate what DSP time the input occurred at
        double inputDspTime = cachedDspTime - timeSinceInput;
        
        // Convert to song time
        return inputDspTime - dspStartTime + userOffsetSeconds;
    }

    /// <summary>
    /// Get raw DSP time (for advanced usage).
    /// </summary>
    public double GetDspTime()
    {
        UpdateFrameCache();
        return cachedDspTime;
    }

    /// <summary>
    /// Get the DSP time when the song started.
    /// </summary>
    public double GetSongStartDspTime()
    {
        return dspStartTime;
    }

    #endregion

    #region Public API - Calibration

    /// <summary>
    /// Set user calibration offset (in seconds).
    /// Positive = user hits early, Negative = user hits late.
    /// </summary>
    public void SetUserOffset(float offsetSeconds)
    {
        userOffsetSeconds = offsetSeconds;
        PlayerPrefs.SetFloat("RhythmUserOffset", offsetSeconds);
        
        if (logTimingInfo)
        {
            Debug.Log($"[TIMING] User offset set to: {offsetSeconds * 1000:F1}ms");
        }
    }

    /// <summary>
    /// Get current user calibration offset in seconds.
    /// </summary>
    public float GetUserOffset()
    {
        return userOffsetSeconds;
    }

    /// <summary>
    /// Get current user calibration offset in milliseconds.
    /// </summary>
    public float GetUserOffsetMs()
    {
        return userOffsetSeconds * 1000f;
    }

    /// <summary>
    /// Load saved user offset from PlayerPrefs.
    /// </summary>
    public void LoadUserOffset()
    {
        userOffsetSeconds = PlayerPrefs.GetFloat("RhythmUserOffset", 0f);
        
        if (logTimingInfo)
        {
            Debug.Log($"[TIMING] Loaded user offset: {userOffsetSeconds * 1000:F1}ms");
        }
    }

    #endregion

    #region Internal

    private void UpdateFrameCache()
    {
        if (frameCacheValid) return;
        
        cachedDspTime = AudioSettings.dspTime;
        cachedRealtimeSinceStartup = Time.realtimeSinceStartupAsDouble;
        frameCacheValid = true;
    }

    #endregion

    #region Debug

    void OnGUI()
    {
        if (!logTimingInfo || !isSongPlaying) return;

        GUI.Box(new Rect(10, 100, 300, 60), "Timing System");
        GUI.Label(new Rect(20, 125, 280, 20), $"Song Time: {GetSongTime():F3}s");
        GUI.Label(new Rect(20, 145, 280, 20), $"User Offset: {userOffsetSeconds * 1000:F1}ms");
    }

    #endregion
}
