using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DataStructures - Merkezi Veri Yapıları
/// Tüm oyun veri yapılarını tek bir yerde toplar, duplicate tanımları önler
/// </summary>

#region Core Game Data Structures

// --- Nota Yapıları ---
public class GameNoteInfoPackage
{
    public float oneNote; // Bu paketten sonraki paketin ne kadar süre sonra geleceği (ms)
    public List<GameNoteInfo> gameNoteInfos = new List<GameNoteInfo>();
}

public class GameNoteInfo
{
    public int idx;           // Final lane indeksi (0-5)
    public int pitch;         // Orijinal pitch değeri (ses için)
    public int line;          // Orijinal line/lane (kural uygulamadan önce)
    public float duration = 1.0f;    // Note duration for InteractiveMusicSystem
    public InstrumentType instrumentType = InstrumentType.Piano; // Default to Piano
    public List<OneNote> noteInfoList = new List<OneNote>(); // Java uyumluluğu
}

public class OneNote
{
    public int line;
    public int flat;
    public int instrument;
}

public class NoteInfo
{
    public int line;
    public int flat;
    public int instrument;
    public int duration;
}

// --- Chart Data Structures ---
[System.Serializable]
public class NoteChartSequence
{
    public int music_id;
    public int seq;
    public string line1;
    public string line2;
    public string line3;
    public string line4;
    public string line5;
    public string line6;

    public string GetLineData(int lane)
    {
        return lane switch
        {
            0 => line1 ?? "",
            1 => line2 ?? "",
            2 => line3 ?? "",
            3 => line4 ?? "",
            4 => line5 ?? "",
            5 => line6 ?? "",
            _ => ""
        };
    }
}

[System.Serializable]
public class JsonSongSequence
{
    public int music_id;
    public int seq;
    public string line1;
    public string line2;
    public string line3;
    public string line4;
    public string line5;
    public string line6;
}

[System.Serializable]
public class JsonSequenceArray
{
    public JsonSongSequence[] sequences;
}

// --- Temporal Processing (Internal) ---
internal class TemporalNoteInfo
{
    public int[] pitches = { -1, -1, -1, -1, -1, -1 };
    public int[] originalLines = { -1, -1, -1, -1, -1, -1 };
    public int maxDurationType = -1;
    public float timingMs = 0f;
}

[System.Serializable]
public class RenderingNote
{
    public GameObject gameObject;
    public GameNoteInfo noteInfo;
    public Vector3 currentPosition;
    public float spawnTime;
    public Color baseColor;
}

#endregion

#region Enums

public enum NoteType
{
    Regular,
    Hold,
    Slide
}

public enum HitAccuracy
{
    Miss,
    Okay,
    Good,
    Perfect
}

public enum InstrumentType
{
    Piano,
    Guitar,
    Harp
}

public enum GameState
{
    MainMenu,
    SongSelection,
    Loading,
    Playing,
    Paused,
    GameOver,
    Settings,
    WorldTour,
    ArtistBattle,
    DailyChallenge,
    Profile,
    SongResult
}

public enum ChordType
{
    None,
    Unison,
    Major3rd,
    Minor3rd,
    Perfect5th,
    Major,
    Minor,
    Diminished,
    Augmented,
    Seventh
}

public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard,
    Expert,
    Master
}

public enum MusicalScale
{
    CMajor,
    AMajor,
    GMajor,
    Pentatonic,
    MinorPentatonic,
    Chromatic
}

#endregion

#region Game Constants

/// <summary>
/// Centralized game layout and timing constants
/// Eliminates magic numbers scattered across codebase
/// </summary>
public static class GameConstants
{
    // Lane Layout
    public const int LaneCount = 6;
    public const float LaneSpacing = 1.8f;
    
    /// <summary>
    /// Center offset for lane positioning, computed from LaneCount
    /// </summary>
    public static float LaneCenterOffset => (LaneCount - 1) / 2f;
    
    /// <summary>
    /// Calculate X position for a given lane index
    /// </summary>
    public static float GetLaneXPosition(int laneIndex)
    {
        return (laneIndex - LaneCenterOffset) * LaneSpacing;
    }
    
    // Duration Estimation (centralized to avoid duplicate code)
    
    /// <summary>
    /// Estimate song duration in seconds based on tempo (BPM)
    /// Slower songs tend to be longer. This is a heuristic until actual duration data is available.
    /// </summary>
    public static float EstimateDurationSeconds(int tempo)
    {
        if (tempo < 60) return 240f;      // Very slow: 4 minutes
        if (tempo < 80) return 210f;      // Slow: 3.5 minutes  
        if (tempo < 120) return 180f;     // Moderate: 3 minutes
        if (tempo < 140) return 150f;     // Fast: 2.5 minutes
        return 120f;                      // Very fast: 2 minutes
    }
    
    /// <summary>
    /// Format duration seconds as "M:SS" string (e.g., "3:45")
    /// </summary>
    public static string FormatDuration(float seconds)
    {
        int totalSeconds = UnityEngine.Mathf.RoundToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return $"{minutes}:{secs:D2}";
    }
    
    /// <summary>
    /// Parse "M:SS" duration string to seconds
    /// </summary>
    public static float ParseDuration(string duration, float fallback = 180f)
    {
        if (string.IsNullOrEmpty(duration)) return fallback;
        
        string[] parts = duration.Split(':');
        if (parts.Length == 2 && 
            int.TryParse(parts[0], out int minutes) && 
            int.TryParse(parts[1], out int seconds))
        {
            return minutes * 60 + seconds;
        }
        return fallback;
    }
}

#endregion

#region Song Database Structures

[System.Serializable]
public class SongDatabaseInfo
{
    public int musicId;
    public string title;
    public string artist;
    public int tempo;
    public float duration; // Duration of the song in seconds
    public DifficultyLevel difficulty;
    public string songKey; // JSON dosya adı için
}

[System.Serializable]
public class SongDatabaseListWrapper
{
    public List<SongDatabaseInfo> Songs;
}

#endregion

#region Musical Structures

public class MusicalEvent
{
    public float timestamp;
    public ChordType detectedChord;
    public List<int> activePitches = new List<int>();
    public float harmonyScore;
    public string musicKey;

    // InteractiveMusicSystem için gerekli ek field'lar
    public int lane;                      // Lane index where note was played
    public MusicalNoteInfo noteInfo;      // Complete note information
    public float velocity;                // Note velocity/volume
    public bool isPlayerTriggered;        // Whether triggered by player input
}

public class MusicalNoteInfo
{
    public int pitch;                  // Musical pitch (0-26)
    public int velocity;               // Note velocity (0-1)
    public float duration;             // Note duration
    public InstrumentType instrument;  // Piano, Harp, Guitar

    // InteractiveMusicSystem için gerekli ek field'lar
    public int midiNote;              // MIDI note number (0-127)
    public int soundIndex;            // Sound resource index
    public string noteName;           // Note name (C4, D#5, etc.)
    public InstrumentType instrumentType; // Alternative name for consistency
    public bool isValid;              // Whether this note info is valid
}

public class GameplayStats
{
    public int totalNotes;
    public int totalNotesHit;     // Alias for consistency
    public int perfectHits;
    public int goodHits;
    public int okayHits;
    public int missedNotes;
    public int maxCombo;
    public float accuracy;
    public int totalScore;
    public string songName;       // Song name for this stats
    public string songKey;        // Song key for progression tracking
    public string artist;         // Artist name for daily challenges
    public DifficultyLevel difficulty; // Difficulty for reward calculation
    public float songDuration;    // Duration of the song

    public override string ToString()
    {
        return $"Stats: {totalNotesHit} notes, {accuracy:F1}% accuracy, {maxCombo} max combo";
    }
}

public class PlayingNote
{
    public MusicalNoteInfo noteInfo;
    public float startTime;
    public bool isStillPlaying;
}

public struct MusicalSessionStats
{
    public int notesPlayed;
    public int chordsPlayed;
    public float melodyComplexity;
    public InstrumentType currentInstrument;
    public MusicalScale currentScale;
}

#endregion

#region Audio Constants (Centralized)

/// <summary>
/// Merkezi Audio Sabitleri - Eski Java oyunundan SOUND_RESOURCE_IDXS mapping sistemi
/// Bu sistem hangi line+pitch kombinasyonunun hangi ses dosyasını kullanacağını belirler
/// </summary>
public static class AudioConstants
{
    /// <summary>
    /// Orijinal Java oyunundan: SOUND_RESOURCE_IDXS
    /// Her lane için hangi ses indekslerinin kullanılacağını belirler
    /// [lane][pitch] → actual sound file index
    /// </summary>
    public static readonly int[][] SOUND_RESOURCE_IDXS = {
        // Lane 0: Piano yüksek oktav (24-44)
        new int[] { 24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44 },
        // Lane 1: Piano orta-yüksek (19-39)
        new int[] { 19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39 },
        // Lane 2: Piano orta (15-35)
        new int[] { 15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35 },
        // Lane 3: Piano alçak-orta (10-30)
        new int[] { 10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30 },
        // Lane 4: Piano alçak (5-25)
        new int[] { 5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25 },
        // Lane 5: Piano en alçak (1-21)
        new int[] { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21 }
    };

    /// <summary>
    /// Güvenli lane access için helper metod
    /// </summary>
    public static int GetSoundIndex(int lane, int pitch)
    {
        int safeLane = UnityEngine.Mathf.Clamp(lane, 0, SOUND_RESOURCE_IDXS.Length - 1);

        if (pitch < 0 || pitch >= SOUND_RESOURCE_IDXS[safeLane].Length)
        {
            // PERFORMANCE: Removed Debug.LogWarning - was causing massive console spam during dense sections
            // Invalid pitches are silently clamped to valid range
            pitch = UnityEngine.Mathf.Clamp(pitch, 0, SOUND_RESOURCE_IDXS[safeLane].Length - 1);
        }

        return SOUND_RESOURCE_IDXS[safeLane][pitch];
    }

    /// <summary>
    /// Calculates the final, playable sound index for a given note, 
    /// applying both the Java-style lane/pitch mapping and any instrument-specific offsets.
    /// This is the authoritative, centralized function for determining which audio clip to play.
    /// </summary>
    /// <param name="instrument">The instrument being played, which may have a pitch offset.</param>
    /// <param name="line">The original line (0-5) of the note from the chart.</param>
    /// <param name="pitch">The pitch value (0-20) of the note from the chart.</param>
    /// <param name="maxIndex">The maximum valid index for the given instrument's audio clips, to prevent errors.</param>
    /// <returns>The final, clamped audio clip index to be played.</returns>
    public static int GetFinalSoundIndex(InstrumentType instrument, int line, int pitch, int maxIndex)
    {
        // 1. Apply the base Java mapping
        int baseIndex = GetSoundIndex(line, pitch);

        // 2. Apply instrument-specific adjustments
        int adjustedIndex = baseIndex;
        switch (instrument)
        {
            case InstrumentType.Guitar:
                adjustedIndex = baseIndex - 4;
                break;
            case InstrumentType.Harp:
                adjustedIndex = baseIndex + 2;
                break;
        }

        // 3. Clamp the result to the valid range of the instrument's clips
        return Mathf.Clamp(adjustedIndex, 0, maxIndex);
    }
}

#endregion
