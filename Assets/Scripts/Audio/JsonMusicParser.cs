using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// JsonMusicParser - Converts JSON song data to game-ready note sequences
/// Parses all_songs_notes.json and cannon_notes.json into playable notes
/// </summary>
public class JsonMusicParser : MonoBehaviour
{
    [Header("🎵 JSON Data Files")]
    [SerializeField] private TextAsset allSongsJson;
    [SerializeField] private TextAsset cannonSongJson;

    [Header("📊 Parser Settings")]
    [SerializeField] private float defaultBPM = 120f;
    [SerializeField] private bool enableDebugLogging = true;

    // Static access for other systems
    public static JsonMusicParser Instance { get; private set; }

    // Cached parsed data
    private Dictionary<int, List<SongSequence>> songSequences;
    private Dictionary<int, List<ProcessedNote>> processedSongs;
    private Dictionary<int, SongMetadata> songMetadata;

    // Song database (generated from JSON)
    private List<SongInfo> availableSongs;

    void Awake()
    {
        Instance = this;
        InitializeParser();
    }

    void Start()
    {
        LoadAndProcessAllSongs();
    }

    void InitializeParser()
    {
        songSequences = new Dictionary<int, List<SongSequence>>();
        processedSongs = new Dictionary<int, List<ProcessedNote>>();
        songMetadata = new Dictionary<int, SongMetadata>();
        availableSongs = new List<SongInfo>();

        LogDebug("🎵 JsonMusicParser initialized");
    }

    #region JSON Loading & Parsing

    /// <summary>
    /// Load and process all songs from JSON files
    /// </summary>
    public void LoadAndProcessAllSongs()
    {
        LogDebug("📋 Loading JSON song data...");

        // Load main song collection
        if (allSongsJson != null)
        {
            LoadSongsFromTextAsset(allSongsJson, "all_songs_notes.json");
        }

        // Load cannon song separately
        if (cannonSongJson != null)
        {
            LoadSongsFromTextAsset(cannonSongJson, "cannon_notes.json");
        }

        // Process all loaded sequences
        ProcessAllSequences();
        GenerateSongDatabase();

        LogDebug($"🎵 Loaded {availableSongs.Count} songs with {GetTotalNoteCount()} total notes");
    }

    /// <summary>
    /// Load song sequences from a TextAsset
    /// </summary>
    void LoadSongsFromTextAsset(TextAsset jsonAsset, string fileName)
    {
        try
        {
            LogDebug($"📖 Processing {fileName}...");

            // Parse JSON array
            string jsonText = jsonAsset.text;

            // Fix JSON if needed (ensure it's a proper array)
            if (!jsonText.Trim().StartsWith("["))
            {
                jsonText = "[" + jsonText + "]";
            }

            // Parse sequences
            SongSequence[] sequences = JsonHelper.FromJson<SongSequence>(jsonText);

            LogDebug($"📊 Found {sequences.Length} sequences in {fileName}");

            // Group by music_id
            foreach (var sequence in sequences)
            {
                if (!songSequences.ContainsKey(sequence.music_id))
                {
                    songSequences[sequence.music_id] = new List<SongSequence>();
                }
                songSequences[sequence.music_id].Add(sequence);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to parse {fileName}: {e.Message}");
        }
    }

    /// <summary>
    /// Process all loaded sequences into playable notes
    /// </summary>
    void ProcessAllSequences()
    {
        LogDebug("🔄 Processing sequences into notes...");

        foreach (var songPair in songSequences)
        {
            int musicId = songPair.Key;
            List<SongSequence> sequences = songPair.Value;

            // Sort sequences by seq number
            sequences.Sort((a, b) => a.seq.CompareTo(b.seq));

            // Process sequences into notes
            List<ProcessedNote> songNotes = ProcessSongSequences(sequences);
            processedSongs[musicId] = songNotes;

            // Generate metadata
            SongMetadata metadata = GenerateSongMetadata(musicId, sequences, songNotes);
            songMetadata[musicId] = metadata;

            LogDebug($"🎵 Song {musicId}: {sequences.Count} sequences → {songNotes.Count} notes");
        }
    }

    /// <summary>
    /// Process song sequences into individual notes
    /// </summary>
    List<ProcessedNote> ProcessSongSequences(List<SongSequence> sequences)
    {
        List<ProcessedNote> allNotes = new List<ProcessedNote>();
        float currentTime = 0f;

        // Time per step (1/16 note at default BPM)
        float stepDuration = (60f / defaultBPM) / 4f;

        foreach (var sequence in sequences)
        {
            float sequenceStartTime = currentTime;

            // Process each lane (line1-line6)
            string[] lines = { sequence.line1, sequence.line2, sequence.line3, sequence.line4, sequence.line5, sequence.line6 };

            for (int lane = 0; lane < lines.Length; lane++)
            {
                if (string.IsNullOrEmpty(lines[lane])) continue;

                List<ProcessedNote> laneNotes = ParseNoteLine(lines[lane], lane, sequenceStartTime, stepDuration);
                allNotes.AddRange(laneNotes);
            }

            // Calculate sequence duration (longest lane)
            float sequenceDuration = CalculateSequenceDuration(lines, stepDuration);
            currentTime += sequenceDuration;
        }

        // Sort by time
        allNotes.Sort((a, b) => a.timeMs.CompareTo(b.timeMs));

        return allNotes;
    }

    /// <summary>
    /// Parse a single note line (e.g., "0,4/_,_/2,3/1,4/_,_/")
    /// </summary>
    List<ProcessedNote> ParseNoteLine(string noteLine, int lane, float startTime, float stepDuration)
    {
        List<ProcessedNote> notes = new List<ProcessedNote>();

        // Split by "/" to get individual steps
        string[] steps = noteLine.Split('/');

        for (int stepIndex = 0; stepIndex < steps.Length; stepIndex++)
        {
            string step = steps[stepIndex].Trim();
            if (string.IsNullOrEmpty(step)) continue;

            // Parse step (format: "pitch,duration" or "_,_")
            ProcessedNote note = ParseNoteStep(step, lane, startTime + (stepIndex * stepDuration));
            if (note != null)
            {
                notes.Add(note);
            }
        }

        return notes;
    }

    /// <summary>
    /// Parse a single note step (e.g., "0,4" or "_,18")
    /// </summary>
    ProcessedNote ParseNoteStep(string step, int lane, float timeMs)
    {
        if (string.IsNullOrEmpty(step) || step == "_,_") return null;

        try
        {
            string[] parts = step.Split(',');
            if (parts.Length != 2) return null;

            string pitchStr = parts[0].Trim();
            string durationStr = parts[1].Trim();

            // Handle special commands (e.g., "_,18", "_,19", "_,20")
            if (pitchStr == "_")
            {
                if (int.TryParse(durationStr, out int specialCode))
                {
                    return new ProcessedNote
                    {
                        lane = lane,
                        pitch = -1,
                        duration = specialCode,
                        timeMs = timeMs,
                        isSpecial = true,
                        specialType = GetSpecialType(specialCode)
                    };
                }
                return null;
            }

            // Parse normal note
            if (int.TryParse(pitchStr, out int pitch) && int.TryParse(durationStr, out int duration))
            {
                return new ProcessedNote
                {
                    lane = lane,
                    pitch = pitch,
                    duration = duration,
                    timeMs = timeMs,
                    isSpecial = false,
                    specialType = SpecialNoteType.None
                };
            }
        }
        catch (Exception e)
        {
            LogDebug($"⚠️ Failed to parse step '{step}': {e.Message}");
        }

        return null;
    }

    /// <summary>
    /// Determine special note type from code
    /// </summary>
    SpecialNoteType GetSpecialType(int code)
    {
        switch (code)
        {
            case 18: return SpecialNoteType.Pause;
            case 19: return SpecialNoteType.TempoChange;
            case 20: return SpecialNoteType.Effect;
            default: return SpecialNoteType.Unknown;
        }
    }

    /// <summary>
    /// Calculate duration of a sequence based on its longest lane
    /// </summary>
    float CalculateSequenceDuration(string[] lines, float stepDuration)
    {
        int maxSteps = 0;

        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            int steps = line.Split('/').Length;
            maxSteps = Mathf.Max(maxSteps, steps);
        }

        return maxSteps * stepDuration;
    }

    /// <summary>
    /// Generate metadata for a song
    /// </summary>
    SongMetadata GenerateSongMetadata(int musicId, List<SongSequence> sequences, List<ProcessedNote> notes)
    {
        // Calculate difficulty based on note density and patterns
        DifficultyLevel difficulty = CalculateDifficulty(notes);

        // Get song name from database or use default
        string songName = GetSongNameFromId(musicId);

        return new SongMetadata
        {
            musicId = musicId,
            songName = songName,
            totalSequences = sequences.Count,
            totalNotes = notes.Count,
            duration = notes.Count > 0 ? notes.Last().timeMs / 1000f : 0f,
            difficulty = difficulty,
            bpm = defaultBPM,
            hasSpecialNotes = notes.Any(n => n.isSpecial)
        };
    }

    /// <summary>
    /// Calculate song difficulty based on note patterns
    /// </summary>
    DifficultyLevel CalculateDifficulty(List<ProcessedNote> notes)
    {
        if (notes.Count == 0) return DifficultyLevel.Easy;

        // Calculate notes per second
        float duration = notes.Last().timeMs / 1000f;
        float notesPerSecond = notes.Count / duration;

        // Calculate lane spread (how many different lanes are used)
        int uniqueLanes = notes.Select(n => n.lane).Distinct().Count();

        // Calculate chord frequency (simultaneous notes)
        int simultaneousNotes = CountSimultaneousNotes(notes);

        // Difficulty scoring
        int difficultyScore = 0;

        if (notesPerSecond > 3f) difficultyScore += 10;
        if (notesPerSecond > 5f) difficultyScore += 10;
        if (notesPerSecond > 8f) difficultyScore += 20;

        if (uniqueLanes >= 4) difficultyScore += 10;
        if (uniqueLanes >= 6) difficultyScore += 10;

        if (simultaneousNotes > notes.Count * 0.1f) difficultyScore += 15;
        if (simultaneousNotes > notes.Count * 0.2f) difficultyScore += 25;

        // Determine difficulty level
        if (difficultyScore < 20) return DifficultyLevel.Easy;
        if (difficultyScore < 40) return DifficultyLevel.Medium;
        if (difficultyScore < 60) return DifficultyLevel.Hard;
        return DifficultyLevel.Expert;
    }

    /// <summary>
    /// Count notes that play simultaneously
    /// </summary>
    int CountSimultaneousNotes(List<ProcessedNote> notes)
    {
        Dictionary<float, int> timeGroups = new Dictionary<float, int>();

        foreach (var note in notes)
        {
            if (!note.isSpecial)
            {
                // Group by rounded time (to handle floating point precision)
                float roundedTime = Mathf.Round(note.timeMs / 10f) * 10f;
                timeGroups[roundedTime] = timeGroups.GetValueOrDefault(roundedTime, 0) + 1;
            }
        }

        return timeGroups.Values.Where(count => count > 1).Sum(count => count - 1);
    }

    /// <summary>
    /// Get song name from music ID
    /// </summary>
    string GetSongNameFromId(int musicId)
    {
        // Default song names based on JSON analysis
        switch (musicId)
        {
            case 1: return "Cannon's Song";
            case 2: return "Piano Dreams";
            case 3: return "Harp Fantasy";
            case 4: return "Guitar Hero";
            case 5: return "Jazz Fusion";
            case 6: return "Classical";
            case 7: return "Epic Theme";
            default: return $"Unknown Song {musicId}";
        }
    }

    #endregion

    #region Song Database Generation

    /// <summary>
    /// Generate song database for game use
    /// </summary>
    void GenerateSongDatabase()
    {
        availableSongs.Clear();

        foreach (var metadataPair in songMetadata)
        {
            SongMetadata metadata = metadataPair.Value;

            SongInfo songInfo = new SongInfo
            {
                musicId = metadata.musicId,
                songName = metadata.songName,
                artist = "TilesWorld Original",
                duration = metadata.duration,
                bpm = metadata.bpm,
                difficulty = metadata.difficulty,
                totalNotes = metadata.totalNotes,
                isAvailable = true
            };

            availableSongs.Add(songInfo);
        }

        // Sort by music ID
        availableSongs.Sort((a, b) => a.musicId.CompareTo(b.musicId));

        LogDebug($"🎵 Generated song database with {availableSongs.Count} songs");
    }

    #endregion

    #region Public Interface

    /// <summary>
    /// Get all available songs
    /// </summary>
    public List<SongInfo> GetAvailableSongs()
    {
        return new List<SongInfo>(availableSongs);
    }

    /// <summary>
    /// Get processed notes for a specific song
    /// </summary>
    public List<ProcessedNote> GetSongNotes(int musicId)
    {
        if (processedSongs.ContainsKey(musicId))
        {
            return new List<ProcessedNote>(processedSongs[musicId]);
        }

        LogDebug($"⚠️ Song {musicId} not found in processed songs");
        return new List<ProcessedNote>();
    }

    /// <summary>
    /// Get song metadata
    /// </summary>
    public SongMetadata GetSongMetadata(int musicId)
    {
        if (songMetadata.ContainsKey(musicId))
        {
            return songMetadata[musicId];
        }

        LogDebug($"⚠️ Metadata for song {musicId} not found");
        return new SongMetadata { musicId = musicId, songName = $"Unknown Song {musicId}" };
    }

    /// <summary>
    /// Get song info by music ID
    /// </summary>
    public SongInfo GetSongInfo(int musicId)
    {
        return availableSongs.FirstOrDefault(s => s.musicId == musicId);
    }

    /// <summary>
    /// Get total number of notes across all songs
    /// </summary>
    public int GetTotalNoteCount()
    {
        return processedSongs.Values.Sum(notes => notes.Count);
    }

    /// <summary>
    /// Check if a song is loaded
    /// </summary>
    public bool IsSongLoaded(int musicId)
    {
        return processedSongs.ContainsKey(musicId);
    }

    #endregion

    #region Utility Methods

    void LogDebug(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[JsonMusicParser] {message}");
        }
    }

    #endregion
}

#region Data Structures

/// <summary>
/// JSON sequence structure (matches JSON format)
/// </summary>
[System.Serializable]
public class SongSequence
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

/// <summary>
/// Processed note data for gameplay
/// </summary>
[System.Serializable]
public class ProcessedNote
{
    public int lane;                    // 0-5 (line1-line6)
    public int pitch;                   // 0-26 (instrument note) or -1 for special
    public float duration;              // 1-9 (note length) or special code
    public float timeMs;                // When to play (milliseconds)
    public bool isSpecial;              // Special command note
    public SpecialNoteType specialType; // Type of special note
}

/// <summary>
/// Song metadata generated from JSON
/// </summary>
[System.Serializable]
public class SongMetadata
{
    public int musicId;
    public string songName;
    public int totalSequences;
    public int totalNotes;
    public float duration;
    public DifficultyLevel difficulty;
    public float bpm;
    public bool hasSpecialNotes;
}

/// <summary>
/// Song information for UI
/// </summary>
[System.Serializable]
public class SongInfo
{
    public int musicId;
    public string songName;
    public string artist;
    public float duration;
    public float bpm;
    public DifficultyLevel difficulty;
    public int totalNotes;
    public bool isAvailable;
}

/// <summary>
/// Special note types
/// </summary>
public enum SpecialNoteType
{
    None,
    Pause,
    TempoChange,
    Effect,
    Unknown
}

#endregion

#region JSON Helper (for array parsing)

/// <summary>
/// Helper class for parsing JSON arrays
/// </summary>
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}

#endregion