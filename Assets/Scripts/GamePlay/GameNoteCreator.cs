using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 🎹 ORIGINAL JAVA ALGORITHM RESTORED!
/// GameNoteCreator - Generates notes using sequence-based timing like original game
/// This is the heart of the note generation system
/// </summary>
public class GameNoteCreator : MonoBehaviour
{
    [Header("🎵 Original Java Algorithm Settings")]
    private const int FIRST_DELAY = 1000; // ms
    private const int MAX_DIRECTION_INTERVAL = 10;

    [SerializeField] private float accDeltaTime = 0.0f; // Accumulated delta time (CRITICAL!)
    [SerializeField] private int firstPassCnt = 3; // Skip first few frames
    [SerializeField] private bool isAllCreated = false;

    [Header("🎼 Sequence Data")]
    private IEnumerator<GameNoteInfoPackage> finalGameNotePackIterator;
    private GameNoteInfoPackage returnGameNoteInfoPack;
    private List<GameNoteInfoPackage> finalGameNotePackages;

    [Header("🎯 Audio Timing (CRITICAL!)")]
    private AudioSource audioSource;
    private float songStartTime;
    private bool usesAudioClock = true; // Use audio.time instead of deltaTime

    // Current song data
    private SongData currentSong;
    private float currentSongBPM = 120f;

    // Events for system integration (RESTORED from original)
    public static event System.Action<List<GameNoteInfo>> OnNotesGenerated;
    public static event System.Action OnGenerationComplete;

    void Start()
    {
        audioSource = FindFirstObjectByType<AudioSource>();
    }

    /// <summary>
    /// 🎹 ORIGINAL JAVA: getNote(float deltaTime)
    /// This is the EXACT algorithm from original GameNoteCreator!
    /// </summary>
    public List<GameNoteInfo> GetNote(float deltaTime)
    {
        // Handle first pass delay (original Java behavior)
        if (firstPassCnt > 0)
        {
            firstPassCnt--;
            return null;
        }

        // Check if all notes created
        if (isAllCreated)
        {
            return null;
        }

        // 🎵 CRITICAL: Use audio clock OR accumulated delta time
        float timeSource;
        if (usesAudioClock && audioSource != null && audioSource.clip != null)
        {
            // Use audio clock in milliseconds (only if clip is loaded)
            timeSource = audioSource.time * 1000f;
        }
        else
        {
            // Fallback to accumulated delta time
            accDeltaTime += deltaTime;
            timeSource = accDeltaTime * 1000f;
        }

        // 🎼 ORIGINAL ALGORITHM: Check if enough time passed for next sequence
        if (returnGameNoteInfoPack != null &&
            returnGameNoteInfoPack.oneNote <= timeSource)
        {
            if (usesAudioClock && audioSource != null && audioSource.clip != null)
            {
                // Reset based on audio time
                songStartTime = audioSource.time * 1000f;
            }
            else
            {
                accDeltaTime = 0.0f; // Reset accumulated time
            }

            // Get next sequence package
            if (finalGameNotePackIterator != null && finalGameNotePackIterator.MoveNext())
            {
                GameNoteInfoPackage gameNoteInfoPackage = finalGameNotePackIterator.Current;
                returnGameNoteInfoPack = gameNoteInfoPackage;

                Debug.Log($"🎵 Next sequence ready! oneNote: {gameNoteInfoPackage.oneNote}ms, notes: {gameNoteInfoPackage.gameNoteInfos.Count}");

                // Trigger event for system integration
                OnNotesGenerated?.Invoke(gameNoteInfoPackage.gameNoteInfos);

                return gameNoteInfoPackage.gameNoteInfos; // Return the notes from the sequence
            }
            else
            {
                isAllCreated = true;
                Debug.Log("🎵 All sequences created!");

                // Trigger completion event
                OnGenerationComplete?.Invoke();

                return null;
            }
        }

        return null; // Not time yet for next sequence
    }

    /// <summary>
    /// 🎵 Load song from individual JSON file (NEW SYSTEM!)
    /// Uses new Individual JSON files: song_artist.json format
    /// </summary>
    public void LoadSong(SongData song)
    {
        if (song == null)
        {
            Debug.LogError("❌ Song data is null!");
            return;
        }

        currentSong = song;
        currentSongBPM = song.bpm;

        Debug.Log($"🎵 Loading song: {song.songName} (BPM: {song.bpm})");

        // NEW: Get individual JSON file path
        string jsonFileName = GetIndividualJsonFileName(song);
        string jsonPath = $"Song_Note_Jsons/Individual/{jsonFileName}";

        Debug.Log($"🎵 Loading individual JSON: {jsonPath}");

        // Load individual JSON file
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonPath);
        if (jsonFile == null)
        {
            Debug.LogError($"❌ Could not load individual JSON: {jsonPath}");
            // Fallback to demo data
            var demoNotes = GenerateDemoNoteData();
            var demoPackages = ConvertRawDataToPackages(demoNotes, currentSongBPM);
            InitializePackageIterator(demoPackages);
            return;
        }

        // Parse individual JSON (same format, but filtered to one song)
        try
        {
            var sequences = ParseIndividualJson(jsonFile.text);
            Debug.Log($"🎵 Loaded {sequences.Count} sequences from individual JSON");

            if (sequences.Count == 0)
            {
                Debug.LogWarning("⚠️ No sequences found in individual JSON, using demo data");
                var demoNotes = GenerateDemoNoteData();
                var demoPackages = ConvertRawDataToPackages(demoNotes, currentSongBPM);
                InitializePackageIterator(demoPackages);
                return;
            }

            // Convert to raw notes
            var rawNotes = ConvertSequencesToRawNotes(sequences, currentSongBPM);
            Debug.Log($"🎯 Converted to {rawNotes.Count} raw notes");

            // Convert to packages and apply algorithms
            var packages = ConvertRawDataToPackages(rawNotes, currentSongBPM);
            var finalPackages = ApplyOriginalAlgorithms(packages);

            Debug.Log($"🎵 Song loaded: {finalPackages.Count} sequences generated");

            // Initialize iterator for note generation
            InitializePackageIterator(finalPackages);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error loading individual JSON: {e.Message}");
            // Fallback to demo data
            var demoNotes = GenerateDemoNoteData();
            var demoPackages = ConvertRawDataToPackages(demoNotes, currentSongBPM);
            InitializePackageIterator(demoPackages);
        }
    }

    /// <summary>
    /// 🎯 Get individual JSON filename from song data
    /// Format: song_artist.json (generated by SongJSONSplitter)
    /// </summary>
    string GetIndividualJsonFileName(SongData song)
    {
        if (song == null || string.IsNullOrEmpty(song.songName))
        {
            return "cannon_pachelbel.json"; // Default fallback
        }

        // Create filename based on song name and artist
        string songName = song.songName.ToLower()
            .Replace(" ", "_")
            .Replace("★", "")
            .Replace("☆", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("'", "")
            .Replace("`", "")
            .Replace("ü", "u")
            .Replace("é", "e")
            .Replace("ñ", "n");

        string artist = (!string.IsNullOrEmpty(song.artist) ? song.artist : "unknown").ToLower()
            .Replace(" ", "_")
            .Replace(".", "")
            .Replace(",", "");

        string filename = $"{songName}_{artist}.json";

        Debug.Log($"🎯 Generated filename: {filename} for song: {song.songName}");
        return filename;
    }

    /// <summary>
    /// 🎼 Parse individual JSON file (same format as all_songs but for one song)
    /// </summary>
    List<JsonSongSequence> ParseIndividualJson(string jsonText)
    {
        try
        {
            // Individual JSON format: {"sequences": [...]}
            JsonSequenceArray wrapper = JsonUtility.FromJson<JsonSequenceArray>("{\"sequences\":" + jsonText + "}");

            if (wrapper?.sequences == null)
            {
                // Try alternative parsing
                var root = JsonUtility.FromJson<JsonSequenceRoot>(jsonText);
                if (root?.sequences != null)
                {
                    return root.sequences.ToList();
                }

                Debug.LogError("❌ Failed to parse individual JSON - no sequences found");
                return new List<JsonSongSequence>();
            }

            return wrapper.sequences.ToList();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error parsing individual JSON: {e.Message}");
            return new List<JsonSongSequence>();
        }
    }

    /// <summary>
    /// 🎵 Convert sequences to raw note data (enhanced for individual JSONs)
    /// </summary>
    List<RawNoteData> ConvertSequencesToRawNotes(List<JsonSongSequence> sequences, float bpm)
    {
        var notesList = new List<RawNoteData>();

        if (sequences == null || sequences.Count == 0)
        {
            Debug.LogWarning("⚠️ No sequences to convert");
            return notesList;
        }

        float beatDuration = (60f / bpm) * 1000f; // ms per beat
        float currentTime = 0f;

        // Sort sequences by sequence number
        sequences.Sort((a, b) => a.seq.CompareTo(b.seq));

        foreach (var sequence in sequences)
        {
            float sequenceStartTime = currentTime;
            string[] lines = { sequence.line1, sequence.line2, sequence.line3, sequence.line4, sequence.line5, sequence.line6 };

            for (int lane = 0; lane < lines.Length; lane++)
            {
                if (string.IsNullOrEmpty(lines[lane])) continue;

                float lineTime = sequenceStartTime;
                var noteParts = lines[lane].Split('/');

                foreach (var noteData in noteParts)
                {
                    if (string.IsNullOrEmpty(noteData) || noteData.Trim() == "_,_")
                    {
                        lineTime += beatDuration / 8f; // 1/8 note step
                        continue;
                    }

                    // Parse "pitch,duration" format
                    var parts = noteData.Split(',');
                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0], out int pitch) &&
                            int.TryParse(parts[1], out int duration))
                        {
                            var note = new RawNoteData
                            {
                                timeMs = lineTime,
                                lane = lane, // 0-5 lanes
                                pitch = pitch,
                                duration = duration,
                                noteType = NoteType.Single
                            };

                            notesList.Add(note);
                            lineTime += (beatDuration * duration) / 8f; // Duration in 8th notes
                        }
                    }
                }
            }

            // Move to next sequence with gap
            currentTime += beatDuration * 2f; // 2 beats gap between sequences
        }

        Debug.Log($"🎼 Successfully converted {notesList.Count} notes from {sequences.Count} sequences");
        return notesList;
    }

    /// <summary>
    /// 🎯 Convert raw notes to GameNoteInfoPackages (sequences)
    /// This groups notes by timing windows, like original Java
    /// </summary>
    List<GameNoteInfoPackage> ConvertRawDataToPackages(List<RawNoteData> rawNotes, float bpm)
    {
        var packages = new List<GameNoteInfoPackage>();

        if (rawNotes == null || rawNotes.Count == 0)
        {
            Debug.LogWarning("⚠️ No raw notes to convert");
            return packages;
        }

        // Group notes by timing windows (original Java behavior)
        float sequenceWindow = (60f / bpm) * 250f; // Quarter note in ms
        var noteGroups = new Dictionary<int, List<RawNoteData>>();

        foreach (var note in rawNotes)
        {
            int timeSlot = Mathf.FloorToInt(note.timeMs / sequenceWindow);

            if (!noteGroups.ContainsKey(timeSlot))
                noteGroups[timeSlot] = new List<RawNoteData>();

            noteGroups[timeSlot].Add(note);
        }

        // Convert groups to packages
        var sortedTimeSlots = noteGroups.Keys.OrderBy(k => k).ToList();

        for (int i = 0; i < sortedTimeSlots.Count; i++)
        {
            int timeSlot = sortedTimeSlots[i];
            var notesInSlot = noteGroups[timeSlot];

            var package = new GameNoteInfoPackage();

            // Calculate timing to next package (original Java: oneNote)
            if (i < sortedTimeSlots.Count - 1)
            {
                int nextTimeSlot = sortedTimeSlots[i + 1];
                package.oneNote = (nextTimeSlot - timeSlot) * sequenceWindow;
            }
            else
            {
                package.oneNote = sequenceWindow; // Default gap for last package
            }

            // Add notes to package using proper list
            foreach (var rawNote in notesInSlot)
            {
                var gameNote = new GameNoteInfo
                {
                    idx = rawNote.lane,
                    pitch = rawNote.pitch,
                    duration = rawNote.duration,
                    timeMs = rawNote.timeMs,
                    instrumentType = InstrumentType.Piano
                };

                package.gameNoteInfos.Add(gameNote);
            }

            packages.Add(package);
        }

        Debug.Log($"🎯 Converted {rawNotes.Count} notes to {packages.Count} packages");
        return packages;
    }

    /// <summary>
    /// 🎼 Apply original Java algorithms: merging, spacing, direction rules
    /// Based on MD analysis: Anti-clustering, Direction balance, Chord merging
    /// </summary>
    List<GameNoteInfoPackage> ApplyOriginalAlgorithms(List<GameNoteInfoPackage> packages)
    {
        if (packages == null || packages.Count == 0)
        {
            return packages;
        }

        Debug.Log($"🎼 Applying original Java algorithms to {packages.Count} packages");

        // 1. Merge close packages into chords (original: mergeGameNoteInfoPackage)
        var mergedPackages = MergeGameNoteInfoPackage(packages);

        // 2. Apply direction balance and lane optimization (original: applyComplexRule)  
        var directionBalanced = ApplyComplexRule(mergedPackages);

        // 3. Apply spacing rules to prevent clustering (original: applySpace)
        var spacedPackages = ApplySpace(directionBalanced);

        Debug.Log($"🎼 Original algorithms applied: {packages.Count} → {spacedPackages.Count} packages");
        return spacedPackages;
    }

    /// <summary>
    /// 🎼 Original Java: mergeGameNoteInfoPackage()
    /// Merges packages that are very close in time to create chords
    /// </summary>
    List<GameNoteInfoPackage> MergeGameNoteInfoPackage(List<GameNoteInfoPackage> packages)
    {
        if (packages.Count < 2) return packages;

        var mergedPackages = new List<GameNoteInfoPackage>();
        var currentPackage = packages[0];
        float chordMergeThreshold = 100f; // ms - notes closer than this become chords

        for (int i = 1; i < packages.Count; i++)
        {
            var nextPackage = packages[i];

            // Calculate time difference between packages
            float timeDifference = Mathf.Abs(nextPackage.oneNote - currentPackage.oneNote);

            // If packages are close enough in time, merge them
            if (timeDifference <= chordMergeThreshold)
            {
                // Merge notes into chord
                foreach (var note in nextPackage.gameNoteInfos)
                {
                    currentPackage.gameNoteInfos.Add(note);
                }

                Debug.Log($"🎼 Merged chord: {currentPackage.gameNoteInfos.Count} notes, time diff: {timeDifference}ms");
            }
            else
            {
                // Time difference too large, finalize current package
                mergedPackages.Add(currentPackage);
                currentPackage = nextPackage;
            }
        }

        // Add the last package
        mergedPackages.Add(currentPackage);

        Debug.Log($"🎼 Chord merging complete: {packages.Count} → {mergedPackages.Count} packages");
        return mergedPackages;
    }

    /// <summary>
    /// 🎼 Original Java: applyComplexRule()
    /// Direction-based placement algorithm - ensures natural hand movement
    /// "The secret sauce" from MD analysis
    /// </summary>
    List<GameNoteInfoPackage> ApplyComplexRule(List<GameNoteInfoPackage> packages)
    {
        int currentDirectionCnt = 0;
        bool isRightDirection = true;
        GameNoteInfoPackage lastAppliedPackage = null;

        foreach (var package in packages)
        {
            // Direction management (original algorithm from MD)
            if (currentDirectionCnt >= MAX_DIRECTION_INTERVAL)
            {
                isRightDirection = false;
                currentDirectionCnt = MAX_DIRECTION_INTERVAL;
            }
            else if (currentDirectionCnt <= 0)
            {
                isRightDirection = true;
                currentDirectionCnt = 0;
            }

            // Apply directional bias to notes in this package
            foreach (var note in package.gameNoteInfos)
            {
                int originalIdx = note.idx;

                // Apply direction-based adjustment (original Java logic)
                if (isRightDirection)
                {
                    note.idx = Mathf.Min(note.idx + 1, 5); // Lane 0-5, move right
                    currentDirectionCnt++;
                }
                else
                {
                    note.idx = Mathf.Max(note.idx - 1, 0); // Lane 0-5, move left  
                    currentDirectionCnt--;
                }

                // Prevent repetition with last package (anti-repetition from MD)
                if (lastAppliedPackage != null)
                {
                    bool conflicts = CheckConflictWithLastPackage(note, lastAppliedPackage);
                    if (conflicts)
                    {
                        // Adjust to avoid repetition (original logic)
                        note.idx = (note.idx + 2) % 6; // Shift by 2 lanes
                        Debug.Log($"🎼 Resolved lane conflict: {originalIdx} → {note.idx}");
                    }
                }
            }

            lastAppliedPackage = package;
        }

        Debug.Log($"🎼 Direction balance applied: Right bias changes applied");
        return packages;
    }

    /// <summary>
    /// Check if note conflicts with previous package (anti-repetition)
    /// </summary>
    bool CheckConflictWithLastPackage(GameNoteInfo note, GameNoteInfoPackage lastPackage)
    {
        foreach (var lastNote in lastPackage.gameNoteInfos)
        {
            // Check if notes are in adjacent lanes (creates conflict)
            if (Mathf.Abs(lastNote.idx - note.idx) <= 1)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 🎼 Original Java: applySpace()
    /// Anti-clustering algorithm - prevents notes from overlapping
    /// </summary>
    List<GameNoteInfoPackage> ApplySpace(List<GameNoteInfoPackage> packages)
    {
        float noteSpacingMinMs = 150f; // Minimum time between notes

        foreach (var package in packages)
        {
            var notes = package.gameNoteInfos;

            for (int i = 0; i < notes.Count; i++)
            {
                for (int j = i + 1; j < notes.Count; j++)
                {
                    var note1 = notes[i];
                    var note2 = notes[j];

                    // Check if notes are too close in lane position
                    if (Mathf.Abs(note1.idx - note2.idx) <= 1)
                    {
                        // Adjust second note position (original logic)
                        note2.idx = (note2.idx + 1) % 6;
                        Debug.Log($"🎼 Lane spacing applied: moved note from lane {note1.idx} to {note2.idx}");
                    }

                    // Check if notes are too close in time  
                    float timeDifference = Mathf.Abs(note1.timeMs - note2.timeMs);
                    if (timeDifference < noteSpacingMinMs)
                    {
                        // Adjust timing to maintain minimum spacing
                        note2.timeMs = note1.timeMs + noteSpacingMinMs;
                        Debug.Log($"🎼 Time spacing applied: {timeDifference}ms → {noteSpacingMinMs}ms");
                    }
                }
            }
        }

        Debug.Log($"🎼 Anti-clustering applied: Notes spaced properly");
        return packages;
    }

    /// <summary>
    /// Generate demo note data for testing
    /// </summary>
    List<RawNoteData> GenerateDemoNoteData()
    {
        List<RawNoteData> demoNotes = new List<RawNoteData>();

        // Generate simple demo pattern with spaced timing
        for (int i = 0; i < 8; i++) // Reduced from 20 to 8 for better spacing
        {
            var note = new RawNoteData
            {
                timeMs = i * 1000f, // Every 1 second instead of 0.5
                lane = i % 6,
                noteType = NoteType.Single,
                pitch = i % 12, // Reduced pitch range for demo
                duration = 4
            };
            demoNotes.Add(note);
        }

        Debug.Log($"🎵 Generated {demoNotes.Count} demo notes with proper spacing");
        return demoNotes;
    }



    /// <summary>
    /// 🎯 Initialize package iterator for note generation
    /// </summary>
    void InitializePackageIterator(List<GameNoteInfoPackage> packages)
    {
        // Reset state for new song
        ResetGenerationState();

        // Store final packages
        finalGameNotePackages = packages;
        finalGameNotePackIterator = finalGameNotePackages.GetEnumerator();

        // Initialize first package
        if (finalGameNotePackIterator.MoveNext())
        {
            returnGameNoteInfoPack = finalGameNotePackIterator.Current;
            Debug.Log($"🎵 First sequence initialized: {returnGameNoteInfoPack.oneNote}ms delay");
        }
    }

    /// <summary>
    /// 🔄 Reset generation state for new song
    /// </summary>
    void ResetGenerationState()
    {
        accDeltaTime = 0.0f;
        firstPassCnt = 3;
        isAllCreated = false;
        songStartTime = 0f;

        // Clear existing data
        if (finalGameNotePackIterator != null)
        {
            finalGameNotePackIterator.Dispose();
            finalGameNotePackIterator = null;
        }

        returnGameNoteInfoPack = null;
        finalGameNotePackages?.Clear();
    }

    #region Public Interface

    public bool IsGenerationComplete() => isAllCreated;

    /// <summary>
    /// Get current song BPM for tempo calculations
    /// </summary>
    float GetCurrentSongBPM()
    {
        return currentSongBPM;
    }

    /// <summary>
    /// Force generation completion (for testing)
    /// </summary>
    public void CompleteGeneration()
    {
        isAllCreated = true;
    }

    public void SetDifficulty(DifficultyLevel difficulty)
    {
        // Adjust generation parameters based on difficulty
    }

    public void StopGeneration()
    {
        isAllCreated = true;
    }
    #endregion

    /// <summary>
    /// Helper classes for JSON parsing
    /// </summary>
}

#region Data Structures (Based on Original Java)

[System.Serializable]
public class GameNoteInfoPackage
{
    public float oneNote;                           // Timing in milliseconds
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

/// <summary>
/// Helper wrapper for JSON sequences array
/// </summary>
[System.Serializable]
public class JsonSequenceArray
{
    public JsonSongSequence[] sequences;
}

/// <summary>
/// Alternative JSON root structure
/// </summary>
[System.Serializable]
public class JsonSequenceRoot
{
    public JsonSongSequence[] sequences;
}

/// <summary>
/// Individual JSON sequence structure (same as original)
/// </summary>
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
public enum NoteType
{
    Single,
    Hold,
    Chord
}

#endregion