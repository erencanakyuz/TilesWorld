using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DataStructures - Merkezi Veri Yapıları
/// Tüm oyun veri yapılarını tek bir yerde toplar, duplicate tanımları önler
/// </summary>

#region Core Game Data Structures

// --- Nota Yapıları ---
[System.Serializable]
public class GameNoteInfoPackage
{
    public float oneNote; // Bu paketten sonraki paketin ne kadar süre sonra geleceği (ms)
    public List<GameNoteInfo> gameNoteInfos = new List<GameNoteInfo>();
}

[System.Serializable]
public class GameNoteInfo
{
    public int idx;           // Final lane indeksi (0-5)
    public int pitch;         // Orijinal pitch değeri (ses için)
    public int line;          // Orijinal line/lane (kural uygulamadan önce)
    public float duration = 1.0f;    // Note duration for InteractiveMusicSystem
    public InstrumentType instrumentType = InstrumentType.Piano; // Default to Piano
    public List<OneNote> noteInfoList = new List<OneNote>(); // Java uyumluluğu
}

[System.Serializable]
public class OneNote
{
    public int line;
    public int flat;
    public int instrument;
}

[System.Serializable]
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
    public int maxDurationType = -1;
    public float timingMs = 0f;
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
    Settings
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

#region Song Database Structures

[System.Serializable]
public class SongDatabaseInfo
{
    public int musicId;
    public string title;
    public string artist;
    public int tempo;
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

[System.Serializable]
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

[System.Serializable]
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

[System.Serializable]
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

    public override string ToString()
    {
        return $"Stats: {totalNotesHit} notes, {accuracy:F1}% accuracy, {maxCombo} max combo";
    }
}

[System.Serializable]
public class PlayingNote
{
    public MusicalNoteInfo noteInfo;
    public float startTime;
    public bool isStillPlaying;
}

[System.Serializable]
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
            UnityEngine.Debug.LogWarning($"⚠️ AudioConstants: Geçersiz pitch değeri! Lane: {lane}, Pitch: {pitch}. Varsayılan olarak 0 kullanılıyor.");
            pitch = UnityEngine.Mathf.Clamp(pitch, 0, SOUND_RESOURCE_IDXS[safeLane].Length - 1);
        }

        return SOUND_RESOURCE_IDXS[safeLane][pitch];
    }
}

#endregion