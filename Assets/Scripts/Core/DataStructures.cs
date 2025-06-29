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
    public int durationType = -1;
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