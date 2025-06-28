using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 🎹 Game Data Structures
/// oldgame.md'deki orijinal Java algoritmaları için veri yapıları
/// </summary>

#region Core Game Data Structures

[System.Serializable]
public class GameNoteInfoPackage
{
    public float oneNote;                           // Timing in milliseconds (oldgame.md'den)
    public List<GameNoteInfo> gameNoteInfos;       // Notes in this package

    public GameNoteInfoPackage()
    {
        gameNoteInfos = new List<GameNoteInfo>();
    }
}

[System.Serializable]
public class GameNoteInfo
{
    public int idx;                    // Lane index (0-5)
    public float timeMs;               // Time in milliseconds
    public NoteType noteType;          // Single, Hold, etc.
    public InstrumentType instrumentType; // Piano, Harp, Guitar
    public int pitch;                  // Musical pitch (for audio)
    public int duration;               // Note duration from JSON
    public bool alreadyHit;           // Hit state tracking
    public int line;                  // Original line/lane (oldgame.md'den)
}

[System.Serializable]
public class RawNoteData
{
    public float timeMs;
    public int lane;
    public NoteType noteType;
    public int pitch;        // Musical pitch (0-26)
    public int duration;     // Note duration (1-9)
}

// Ham veriyi geçici olarak tutmak için (oldgame.md'den)
internal class TemporalNoteInfo
{
    public int[] pitches = { -1, -1, -1, -1, -1, -1 };
    public int durationType = -1;
    public float timingMs = 0f;
}

#endregion

#region JSON Data Structures

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
}

[System.Serializable]
public class JsonSequenceArray
{
    public JsonSongSequence[] sequences;
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

#endregion

#region Enums

[System.Serializable]
public enum NoteType
{
    Single,
    Hold,
    Chord
}

[System.Serializable]
public enum HitAccuracy
{
    Miss,
    Good,
    Perfect
}

[System.Serializable]
public enum InstrumentType
{
    Piano,
    Harp,
    Guitar
}

[System.Serializable]
public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard,
    Expert,
    Master
}

[System.Serializable]
public enum GameState
{
    MainMenu,
    SongSelection,
    Playing,
    Paused,
    GameOver,
    Settings,
    Loading
}

[System.Serializable]
public enum MusicalScale
{
    CMajor,
    AMajor,
    GMajor,
    Pentatonic,
    MinorPentatonic,
    Chromatic
}

[System.Serializable]
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
    Augmented
}

#endregion

#region Game State Structures

[System.Serializable]
public struct GameplayStats
{
    public int totalNotesHit;
    public int perfectHits;
    public int goodHits;
    public int missedNotes;
    public int maxCombo;
    public float accuracy;
    public string songName;

    public override string ToString()
    {
        return $"Stats: {totalNotesHit} notes, {accuracy:F1}% accuracy, {maxCombo} max combo";
    }
}

[System.Serializable]
public class MusicalEvent
{
    public float timestamp;
    public string eventType;
    public int pitch;
    public float duration;
    public InstrumentType instrument;

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
    public int lane;                   // Lane index
    public float velocity;             // Note velocity (0-1)
    public InstrumentType instrument;  // Piano, Harp, Guitar

    // InteractiveMusicSystem için gerekli ek field'lar
    public int midiNote;              // MIDI note number (0-127)
    public int soundIndex;            // Sound resource index
    public string noteName;           // Note name (C4, D#5, etc.)
    public InstrumentType instrumentType; // Alternative name for consistency
    public bool isValid;              // Whether this note info is valid
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