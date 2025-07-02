using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 🎼 Musical Integrity System - The Ultimate Tempo Sync Solution
/// Manages all 5 interactive timing systems with musical realism
/// Ensures authentic musical experience across all tempos (45-250 BPM)
/// </summary>
public class MusicalIntegritySystem : MonoBehaviour
{
    public static MusicalIntegritySystem Instance { get; private set; }

    [Header("🎵 Musical Reality Configuration")]
    [SerializeField] private bool enableMusicalIntegrity = true;
    [SerializeField] private bool enableRealTimeValidation = true;
    [SerializeField] private bool enableAutoTesting = false;

    [Header("🎯 Sync System Configuration")]
    [SerializeField] private float baseMusicGameplayReferenceTemp = 120f;
    [SerializeField] private float databaseOptimalReference = 105f;
    [SerializeField] private float extremeTempoThreshold = 0.3f; // Musical realism threshold

    [Header("🔧 Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool enableMusicalWarnings = true;

    // Musical characteristics database
    private Dictionary<string, MusicalCharacteristics> songCharacteristics;
    private Dictionary<int, TempoClass> tempoClassification;

    // Real-time monitoring
    private float lastNoteSpawnTime = 0f;
    private float currentMusicalRealismScore = 1f;
    private List<float> recentNoteIntervals = new List<float>();

    // System sync state
    private int currentTempo = 120;
    private string currentSongKey = "";
    private MusicalCharacteristics currentSongCharacter;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMusicalIntegritySystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SetupSongCharacteristics();
        SetupTempoClassification();

        if (showDebugLogs)
            Debug.Log("🎼 Musical Integrity System initialized with database-driven characteristics");
    }

    void InitializeMusicalIntegritySystem()
    {
        songCharacteristics = new Dictionary<string, MusicalCharacteristics>();
        tempoClassification = new Dictionary<int, TempoClass>();
        recentNoteIntervals = new List<float>();
    }

    #region Musical Characteristics Database

    void SetupSongCharacteristics()
    {
        // 🎼 REAL MUSICAL CHARACTERISTICS FROM DATABASE
        songCharacteristics = new Dictionary<string, MusicalCharacteristics>
        {
            // VERY SLOW - Meditative & Emotional (45-60 BPM)
            ["cathedral_agustin_barrios"] = new MusicalCharacteristics
            {
                style = MusicalStyle.Meditative,
                emotionalTempo = 60f,           // Real feeling tempo vs BPM 45
                naturalNoteFlow = 0.4f,         // Sparse, contemplative
                gameSpeedMultiplier = 1.4f,     // Speed up for playability
                noteSpacingOverride = 0.8f,     // Custom spacing for flow
                requiresDelicateTouch = true,
                musicalIntensity = 0.3f
            },

            ["moon_light_beethoven"] = new MusicalCharacteristics
            {
                style = MusicalStyle.Romantic,
                emotionalTempo = 65f,           // BPM 50 → feels like 65
                naturalNoteFlow = 0.5f,
                gameSpeedMultiplier = 1.3f,
                noteSpacingOverride = 0.75f,
                requiresDelicateTouch = true,
                musicalIntensity = 0.4f
            },

            ["el_noi_de_la_mare_miguel_llobet"] = new MusicalCharacteristics
            {
                style = MusicalStyle.Folk,
                emotionalTempo = 70f,           // BPM 50 → feels like 70
                naturalNoteFlow = 0.6f,
                gameSpeedMultiplier = 1.4f,
                noteSpacingOverride = 0.7f,
                requiresDelicateTouch = true,
                musicalIntensity = 0.5f
            },

            // SLOW - Graceful (60-80 BPM)
            ["fur_elise_beethoven"] = new MusicalCharacteristics
            {
                style = MusicalStyle.Classical,
                emotionalTempo = 75f,           // BPM 62 → feels like 75
                naturalNoteFlow = 0.7f,
                gameSpeedMultiplier = 1.2f,
                noteSpacingOverride = -1f,      // Use auto
                requiresDelicateTouch = true,
                musicalIntensity = 0.6f
            },

            ["ciacona_s_l_weiss"] = new MusicalCharacteristics
            {
                style = MusicalStyle.Baroque,
                emotionalTempo = 85f,           // BPM 70 → feels like 85
                naturalNoteFlow = 0.8f,
                gameSpeedMultiplier = 1.15f,
                noteSpacingOverride = -1f,
                requiresDelicateTouch = false,
                musicalIntensity = 0.7f
            },

            // NORMAL - Balanced (80-130 BPM)  
            ["cannon_pachelbel"] = new MusicalCharacteristics
            {
                style = MusicalStyle.Baroque,
                emotionalTempo = 85f,           // BPM 77 → feels like 85
                naturalNoteFlow = 0.85f,
                gameSpeedMultiplier = 1.1f,
                noteSpacingOverride = -1f,
                requiresDelicateTouch = false,
                musicalIntensity = 0.8f
            },

            ["vidalita_traditional"] = new MusicalCharacteristics
            {
                style = MusicalStyle.Traditional,
                emotionalTempo = 120f,          // Perfect reference - no adjustment needed
                naturalNoteFlow = 1.0f,
                gameSpeedMultiplier = 1.0f,     // Baseline
                noteSpacingOverride = -1f,
                requiresDelicateTouch = false,
                musicalIntensity = 1.0f
            },

            ["turkish_delight_mozart"] = new MusicalCharacteristics
            {
                style = MusicalStyle.Classical,
                emotionalTempo = 135f,          // BPM 140 → feels like 135
                naturalNoteFlow = 1.1f,
                gameSpeedMultiplier = 0.95f,    // Slight slowdown for clarity
                noteSpacingOverride = -1f,
                requiresDelicateTouch = false,
                musicalIntensity = 1.2f
            },

            // FAST - Energetic (130-180 BPM)
            ["moonlight_sonata_op._27,_no._2._beethoven"] = new MusicalCharacteristics
            {
                style = MusicalStyle.Dramatic,
                emotionalTempo = 160f,          // BPM 176 → controlled at 160
                naturalNoteFlow = 1.3f,
                gameSpeedMultiplier = 0.9f,     // Controlled intensity
                noteSpacingOverride = 0.6f,     // Tighter spacing
                requiresDelicateTouch = false,
                allowsRapidFire = true,
                musicalIntensity = 1.5f
            },

            // EXTREME - Virtuosic (200+ BPM)
            ["sinfonia_40_mozart"] = new MusicalCharacteristics
            {
                style = MusicalStyle.Virtuosic,
                emotionalTempo = 200f,          // BPM 250 → controlled at 200
                naturalNoteFlow = 1.4f,
                gameSpeedMultiplier = 0.8f,     // Significant control to avoid machine gun
                noteSpacingOverride = 0.5f,     // Tight but musical spacing
                requiresDelicateTouch = false,
                allowsRapidFire = true,
                musicalIntensity = 2.0f
            }
        };

        if (showDebugLogs)
            Debug.Log($"🎼 Setup {songCharacteristics.Count} musical characteristics from database analysis");
    }

    void SetupTempoClassification()
    {
        // Database-driven tempo classification
        var tempoRanges = new[]
        {
            (45, 60, TempoClass.VerySlow),    // Cathedral, Moon Light, El Noi
            (61, 80, TempoClass.Slow),        // Fur Elise, Ciacona, Cannon
            (81, 130, TempoClass.Normal),     // Vidalita, Turkish Delight, most songs
            (131, 180, TempoClass.Fast),      // Moonlight Sonata
            (181, 220, TempoClass.VeryFast),  // (none in current DB)
            (221, 300, TempoClass.Extreme)    // Sinfonia 40
        };

        tempoClassification.Clear();
        for (int tempo = 40; tempo <= 300; tempo++)
        {
            var range = tempoRanges.FirstOrDefault(r => tempo >= r.Item1 && tempo <= r.Item2);
            tempoClassification[tempo] = range.Item3;
        }
    }

    #endregion

    #region Core Sync Calculations

    /// <summary>
    /// 🎯 MASTER SYNC CALCULATOR - All 5 systems sync through this
    /// </summary>
    public MusicalSyncData CalculateOptimalSync(string songKey, int tempo)
    {
        currentSongKey = songKey;
        currentTempo = tempo;
        currentSongCharacter = GetSongCharacteristics(songKey);

        var syncData = new MusicalSyncData();

        // 1. Calculate emotional/musical tempo (not raw BPM)
        syncData.emotionalTempo = CalculateEmotionalTempo(tempo, currentSongCharacter);

        // 2. Calculate optimal note spawn timing
        syncData.noteSpawnTimingMs = CalculateOptimalNoteSpacing(tempo, currentSongCharacter);

        // 3. Calculate visual speed multiplier
        syncData.visualSpeedMultiplier = CalculateOptimalVisualSpeed(tempo, currentSongCharacter);

        // 4. Calculate hit timing windows (based on musical style)
        syncData.hitTimingWindows = CalculateOptimalHitWindows(tempo, currentSongCharacter);

        // 5. Calculate animation durations
        syncData.animationDurations = CalculateOptimalAnimationTiming(tempo, currentSongCharacter);

        // 6. Musical realism score
        syncData.musicalRealismScore = CalculateMusicalRealismScore(syncData);

        if (showDebugLogs)
        {
            Debug.Log($"🎼 MUSICAL SYNC CALCULATED for {songKey}:");
            Debug.Log($"   📊 Raw BPM: {tempo} → Emotional Tempo: {syncData.emotionalTempo:F1}");
            Debug.Log($"   ⏱️ Note Spacing: {syncData.noteSpawnTimingMs:F1}ms");
            Debug.Log($"   🚀 Visual Speed: {syncData.visualSpeedMultiplier:F2}x");
            Debug.Log($"   🎯 Musical Realism: {syncData.musicalRealismScore:F2}/1.0");
        }

        return syncData;
    }

    float CalculateEmotionalTempo(int rawTempo, MusicalCharacteristics character)
    {
        if (character != null && character.emotionalTempo > 0)
            return character.emotionalTempo;

        // Fallback: Smart tempo adjustment for musical feel
        var tempoClass = GetTempoClass(rawTempo);

        return tempoClass switch
        {
            TempoClass.VerySlow => rawTempo * 1.3f,     // Feel faster than BPM
            TempoClass.Slow => rawTempo * 1.15f,        // Slight boost
            TempoClass.Normal => rawTempo,              // No adjustment
            TempoClass.Fast => rawTempo * 0.95f,        // Slight control
            TempoClass.VeryFast => rawTempo * 0.85f,    // More control
            TempoClass.Extreme => rawTempo * 0.8f,      // Heavy control
            _ => rawTempo
        };
    }

    float CalculateOptimalNoteSpacing(int tempo, MusicalCharacteristics character)
    {
        // Base timing calculation (musical quarter note)
        float baseTimingMs = (60000f / tempo) / 4f; // Quarter note duration

        // Apply musical character multiplier
        float characterMultiplier = 1.0f;
        if (character != null)
        {
            characterMultiplier = character.naturalNoteFlow;

            // Custom spacing override
            if (character.noteSpacingOverride > 0)
                return character.noteSpacingOverride * 1000f; // Convert to ms
        }

        // Apply tempo-class specific adjustments
        var tempoClass = GetTempoClass(tempo);
        float classMultiplier = tempoClass switch
        {
            TempoClass.VerySlow => 0.7f,     // Denser for playability
            TempoClass.Slow => 0.85f,        // Slightly denser
            TempoClass.Normal => 1.0f,       // Standard
            TempoClass.Fast => 1.1f,         // Slightly sparser
            TempoClass.VeryFast => 1.2f,     // Sparser for clarity
            TempoClass.Extreme => 1.3f,      // Much sparser
            _ => 1.0f
        };

        return baseTimingMs * characterMultiplier * classMultiplier;
    }

    float CalculateOptimalVisualSpeed(int tempo, MusicalCharacteristics character)
    {
        // Smart reference calculation (hybrid approach)
        float smartReference = Mathf.Lerp(databaseOptimalReference, baseMusicGameplayReferenceTemp, 0.6f);

        // Base speed calculation
        float baseSpeed = 12f; // Standard base speed
        float speedMultiplier = baseSpeed * (tempo / smartReference);

        // Apply character-specific adjustment
        if (character != null)
        {
            speedMultiplier *= character.gameSpeedMultiplier;
        }

        // Apply tempo-class specific bounds
        var tempoClass = GetTempoClass(tempo);
        var bounds = tempoClass switch
        {
            TempoClass.VerySlow => (min: 6f, max: 12f),     // Boosted minimum
            TempoClass.Slow => (min: 8f, max: 16f),         // Normal range
            TempoClass.Normal => (min: 10f, max: 20f),      // Standard range
            TempoClass.Fast => (min: 12f, max: 24f),        // Higher range
            TempoClass.VeryFast => (min: 15f, max: 25f),    // Controlled high
            TempoClass.Extreme => (min: 18f, max: 25f),     // Capped extreme
            _ => (min: 8f, max: 20f)
        };

        return Mathf.Clamp(speedMultiplier, bounds.min, bounds.max);
    }

    HitTimingWindows CalculateOptimalHitWindows(int tempo, MusicalCharacteristics character)
    {
        // Base windows (for 120 BPM reference)
        var baseWindows = new HitTimingWindows
        {
            perfectMs = 80f,
            goodMs = 160f,
            okayMs = 250f
        };

        // Adjust based on tempo and musical style
        var tempoClass = GetTempoClass(tempo);
        float difficultyMultiplier = tempoClass switch
        {
            TempoClass.VerySlow => 1.5f,     // Easier timing (longer windows)
            TempoClass.Slow => 1.3f,         // Slightly easier
            TempoClass.Normal => 1.0f,       // Standard
            TempoClass.Fast => 0.85f,        // Tighter windows
            TempoClass.VeryFast => 0.7f,     // Much tighter
            TempoClass.Extreme => 0.6f,      // Extremely tight
            _ => 1.0f
        };

        // Character-specific adjustments
        if (character != null)
        {
            if (character.requiresDelicateTouch)
                difficultyMultiplier *= 1.2f; // More forgiving for delicate pieces
            if (character.allowsRapidFire)
                difficultyMultiplier *= 0.9f; // Tighter for rapid pieces
        }

        return new HitTimingWindows
        {
            perfectMs = baseWindows.perfectMs * difficultyMultiplier,
            goodMs = baseWindows.goodMs * difficultyMultiplier,
            okayMs = baseWindows.okayMs * difficultyMultiplier
        };
    }

    AnimationDurations CalculateOptimalAnimationTiming(int tempo, MusicalCharacteristics character)
    {
        // Base animation durations
        float baseTravelTime = 1.5f; // seconds

        // Adjust based on tempo
        var tempoClass = GetTempoClass(tempo);
        float tempoMultiplier = tempoClass switch
        {
            TempoClass.VerySlow => 1.8f,     // Slower animations
            TempoClass.Slow => 1.4f,         // Slightly slower
            TempoClass.Normal => 1.0f,       // Standard
            TempoClass.Fast => 0.8f,         // Faster animations
            TempoClass.VeryFast => 0.6f,     // Much faster
            TempoClass.Extreme => 0.5f,      // Very fast
            _ => 1.0f
        };

        return new AnimationDurations
        {
            noteSpawnToHit = baseTravelTime * tempoMultiplier,
            hitAnimation = 0.3f / tempoMultiplier,
            missAnimation = 0.5f / tempoMultiplier
        };
    }

    float CalculateMusicalRealismScore(MusicalSyncData syncData)
    {
        float score = 1f;

        // Penalty for extreme deviations from musical norms
        float tempoDeviation = Mathf.Abs(syncData.emotionalTempo - currentTempo) / currentTempo;
        if (tempoDeviation > extremeTempoThreshold)
            score -= (tempoDeviation - extremeTempoThreshold) * 0.5f;

        // Bonus for character-aware adjustments
        if (currentSongCharacter != null)
            score += 0.1f; // Character-specific bonus

        return Mathf.Clamp01(score);
    }

    #endregion

    #region Real-Time Validation

    void Update()
    {
        if (!enableRealTimeValidation) return;

        MonitorMusicalIntegrity();
    }

    void MonitorMusicalIntegrity()
    {
        // Track note spawn intervals
        if (Time.time - lastNoteSpawnTime > 0.1f) // Avoid spam
        {
            float interval = Time.time - lastNoteSpawnTime;
            recentNoteIntervals.Add(interval);

            // Keep only recent data
            if (recentNoteIntervals.Count > 10)
                recentNoteIntervals.RemoveAt(0);

            // Calculate flow smoothness
            if (recentNoteIntervals.Count >= 5)
            {
                float averageInterval = recentNoteIntervals.Average();
                float variability = recentNoteIntervals.Select(x => Mathf.Abs(x - averageInterval)).Average();
                float flowSmoothness = 1f - Mathf.Clamp01(variability / averageInterval);

                // Warning for choppy flow
                if (flowSmoothness < 0.7f && enableMusicalWarnings)
                {
                    Debug.LogWarning($"🎵 MUSICAL FLOW WARNING: Choppy audio detected! " +
                                   $"Flow smoothness: {flowSmoothness:F2}, Song: {currentSongKey}");
                }
            }
        }
    }

    public void OnNoteSpawned()
    {
        lastNoteSpawnTime = Time.time;
    }

    #endregion

    #region Public API

    public MusicalCharacteristics GetSongCharacteristics(string songKey)
    {
        return songCharacteristics.GetValueOrDefault(songKey, GetDefaultCharacteristics());
    }

    public TempoClass GetTempoClass(int tempo)
    {
        return tempoClassification.GetValueOrDefault(tempo, TempoClass.Normal);
    }

    public float GetCurrentMusicalRealismScore()
    {
        return currentMusicalRealismScore;
    }

    MusicalCharacteristics GetDefaultCharacteristics()
    {
        return new MusicalCharacteristics
        {
            style = MusicalStyle.Classical,
            emotionalTempo = currentTempo,
            naturalNoteFlow = 1.0f,
            gameSpeedMultiplier = 1.0f,
            noteSpacingOverride = -1f,
            requiresDelicateTouch = false,
            allowsRapidFire = false,
            musicalIntensity = 1.0f
        };
    }

    #endregion

    #region Testing & Debug

    [ContextMenu("🧪 Test All Database Songs")]
    public void TestAllDatabaseSongs()
    {
        if (!enableAutoTesting) return;

        Debug.Log("🧪 TESTING ALL DATABASE SONGS FOR MUSICAL INTEGRITY:");

        var testSongs = new[]
        {
            ("cathedral_agustin_barrios", 45),
            ("moon_light_beethoven", 50),
            ("turkish_delight_mozart", 140),
            ("sinfonia_40_mozart", 250),
            ("vidalita_traditional", 120)
        };

        foreach (var (songKey, tempo) in testSongs)
        {
            var syncData = CalculateOptimalSync(songKey, tempo);

            string evaluation = syncData.musicalRealismScore switch
            {
                >= 0.9f => "✅ EXCELLENT",
                >= 0.8f => "✅ GOOD",
                >= 0.7f => "⚠️ ACCEPTABLE",
                >= 0.6f => "⚠️ NEEDS WORK",
                _ => "❌ POOR"
            };

            Debug.Log($"🎼 {songKey} ({tempo} BPM): {evaluation} " +
                     $"(Realism: {syncData.musicalRealismScore:F2}, " +
                     $"Emotional: {syncData.emotionalTempo:F0} BPM)");
        }
    }

    [ContextMenu("🎯 Test Current Song")]
    public void TestCurrentSong()
    {
        if (string.IsNullOrEmpty(currentSongKey))
        {
            Debug.LogWarning("🎼 No current song to test!");
            return;
        }

        var syncData = CalculateOptimalSync(currentSongKey, currentTempo);
        Debug.Log($"🎼 CURRENT SONG TEST COMPLETE - Realism Score: {syncData.musicalRealismScore:F2}");
    }

    #endregion
}

#region Data Structures

[System.Serializable]
public class MusicalCharacteristics
{
    [Header("🎼 Musical Identity")]
    public MusicalStyle style = MusicalStyle.Classical;
    public float emotionalTempo = 120f;        // How the song "feels" tempo-wise
    public float naturalNoteFlow = 1.0f;       // Natural density of notes (0.2-2.0)
    public float musicalIntensity = 1.0f;      // Emotional intensity factor

    [Header("🎮 Game Adaptation")]
    public float gameSpeedMultiplier = 1.0f;   // Speed adjustment for playability
    public float noteSpacingOverride = -1f;    // Custom note spacing (-1 = auto)

    [Header("🎯 Playing Style")]
    public bool requiresDelicateTouch = false; // Needs gentle, precise timing
    public bool allowsRapidFire = false;       // Can handle rapid successive notes
}

[System.Serializable]
public class MusicalSyncData
{
    public float emotionalTempo;               // Musically-adjusted tempo
    public float noteSpawnTimingMs;            // Optimal note spawn interval
    public float visualSpeedMultiplier;        // Visual speed for note movement
    public HitTimingWindows hitTimingWindows;  // Hit detection windows
    public AnimationDurations animationDurations; // Animation timing
    public float musicalRealismScore;          // 0-1 realism score
}

[System.Serializable]
public class HitTimingWindows
{
    public float perfectMs = 80f;
    public float goodMs = 160f;
    public float okayMs = 250f;
}

[System.Serializable]
public class AnimationDurations
{
    public float noteSpawnToHit = 1.5f;        // Travel time from spawn to hit
    public float hitAnimation = 0.3f;          // Hit effect duration
    public float missAnimation = 0.5f;         // Miss effect duration
}

public enum MusicalStyle
{
    Classical, Baroque, Romantic, Modern, Traditional, Folk,
    Meditative, Dramatic, Virtuosic
}

public enum TempoClass
{
    VerySlow,    // 45-60 BPM
    Slow,        // 61-80 BPM  
    Normal,      // 81-130 BPM
    Fast,        // 131-180 BPM
    VeryFast,    // 181-220 BPM
    Extreme      // 221+ BPM
}

#endregion