using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using System;

/// <summary>
/// GameNoteCreator - The Heart of the Game
/// Based on original Java algorithms with modern Unity optimizations
/// Implements: Anti-clustering, Direction-based placement, Note merging
/// </summary>
public class GameNoteCreator : MonoBehaviour
{
    [Header("🎵 Note Generation Configuration")]
    [SerializeField] private int laneCount = 6;
    [SerializeField] private int maxGameHeightLength = 6;

    [SerializeField] private int maxDirectionInterval = 10;

    [Header("🎯 Algorithm Parameters")]
    [SerializeField] private float noteSpacingMinMs = 150f;     // Minimum time between notes
    [SerializeField] private float chordMergeThresholdMs = 50f; // Notes closer than this become chords
    [SerializeField] private bool enableAntiClustering = true;
    [SerializeField] private bool enableDirectionBalance = true;

    private int totalNotesGenerated = 0;
    private int currentDirectionCnt = 0;
    private bool isRightDirection = true;

    // Core algorithm state (from original Java)
    private float accumulatedDeltaTime = 0f;
    private int firstPassCount = 3;
    private bool isAllCreated = false;

    // Note generation data
    private Queue<GameNoteInfoPackage> finalGameNotePackages;
    private GameNoteInfoPackage currentReturnPackage;
    private GameNoteInfoPackage lastAppliedGameNoteInfoPack;

    // Original algorithm arrays (from MD analysis)
    private static readonly int[] LINE_TO_MATCH = { 3, 5, 7, 11, 13, 17 };

    // Current song data
    private float currentSongBPM = 120f;

    // Events for system integration
    public static event Action<List<GameNoteInfo>> OnNotesGenerated;
    public static event Action OnGenerationComplete;

    void Awake()
    {
        InitializeNoteCreator();
    }

    void Start()
    {
    }

    void InitializeNoteCreator()
    {
        finalGameNotePackages = new Queue<GameNoteInfoPackage>();
        ResetGenerationState();
    }

    void ResetGenerationState()
    {
        accumulatedDeltaTime = 0f;
        firstPassCount = 3;
        isAllCreated = false;
        currentDirectionCnt = 0;
        isRightDirection = true;
        totalNotesGenerated = 0;
        lastAppliedGameNoteInfoPack = null;
    }

    #region Core Algorithm - Ported from Original Java

    /// <summary>
    /// Main note generation method - Original Java: getNote(float paramFloat)
    /// Returns notes when timing is right, null otherwise
    /// </summary>
    public List<GameNoteInfo> GetNote(float deltaTime)
    {
        if (isAllCreated || currentReturnPackage == null) return new List<GameNoteInfo>();

        // Original firstPassDelay logic
        if (firstPassCount > 0)
        {
            firstPassCount--;
            return new List<GameNoteInfo>();
        }

        accumulatedDeltaTime += deltaTime;

        if (currentReturnPackage != null &&
            currentReturnPackage.oneNote <= accumulatedDeltaTime * 1000f)
        {
            var packageToProcess = currentReturnPackage;
            accumulatedDeltaTime = 0f;

            if (finalGameNotePackages.Count > 0)
            {
                currentReturnPackage = finalGameNotePackages.Dequeue();
            }
            else
            {
                currentReturnPackage = null;
                isAllCreated = true;
                OnGenerationComplete?.Invoke();
            }

            var notesToReturn = ProcessNotePackage(packageToProcess);

            // OnNotesGenerated event working correctly

            OnNotesGenerated?.Invoke(notesToReturn);

            totalNotesGenerated += notesToReturn.Count;
            return notesToReturn;
        }

        return new List<GameNoteInfo>();
    }

    /// <summary>
    /// Process and apply original algorithms to note package
    /// </summary>
    List<GameNoteInfo> ProcessNotePackage(GameNoteInfoPackage package)
    {
        var notes = new List<GameNoteInfo>(package.gameNoteInfos);

        // Apply original algorithms in sequence
        if (enableAntiClustering)
            ApplySpacing(notes);

        if (enableDirectionBalance)
            ApplyComplexRule(notes);

        // Store for next iteration
        lastAppliedGameNoteInfoPack = package;

        return notes;
    }

    /// <summary>
    /// Original Java: applySpace() - Anti-clustering algorithm
    /// Prevents notes from overlapping or clustering too closely
    /// </summary>
    void ApplySpacing(List<GameNoteInfo> notes)
    {
        for (int i = 0; i < notes.Count; i++)
        {
            for (int j = i + 1; j < notes.Count; j++)
            {
                GameNoteInfo note1 = notes[i];
                GameNoteInfo note2 = notes[j];

                // Check if notes are too close in lane position (original algorithm)
                if (Mathf.Abs(note1.idx - note2.idx) == 1)
                {
                    // Adjust second note position (original logic)
                    note2.idx = (note2.idx + 1) % maxGameHeightLength;
                }

                // Check if notes are too close in time (using noteSpacingMinMs)
                float timeDifference = Mathf.Abs(note1.timeMs - note2.timeMs);
                if (timeDifference < noteSpacingMinMs)
                {
                    // Adjust timing to maintain minimum spacing
                    note2.timeMs = note1.timeMs + noteSpacingMinMs;
                }
            }
        }
    }

    /// <summary>
    /// Original Java: applyComplexRule() - Direction-based placement algorithm
    /// The "secret sauce" that makes gameplay feel natural and flowing
    /// </summary>
    void ApplyComplexRule(List<GameNoteInfo> notes)
    {
        // Direction management (original algorithm)
        if (currentDirectionCnt >= maxDirectionInterval)
        {
            isRightDirection = false;
            currentDirectionCnt = maxDirectionInterval;
        }
        else if (currentDirectionCnt <= 0)
        {
            isRightDirection = true;
            currentDirectionCnt = 0;
        }

        // Apply directional bias to notes
        foreach (var note in notes)
        {
            int originalIdx = note.idx;

            // Apply direction-based adjustment
            if (isRightDirection)
            {
                note.idx = Mathf.Min(note.idx + 1, laneCount - 1);
                currentDirectionCnt++;
            }
            else
            {
                note.idx = Mathf.Max(note.idx - 1, 0);
                currentDirectionCnt--;
            }

            // Prevent repetition with last package (original anti-repetition logic)
            if (lastAppliedGameNoteInfoPack != null)
            {
                bool conflicts = CheckConflictWithLastPackage(note);
                if (conflicts)
                {
                    // Adjust to avoid repetition
                    note.idx = (note.idx + 2) % laneCount;
                }
            }
        }
    }

    /// <summary>
    /// Check if note conflicts with previous package (anti-repetition)
    /// </summary>
    bool CheckConflictWithLastPackage(GameNoteInfo note)
    {
        if (lastAppliedGameNoteInfoPack == null) return false;

        foreach (var lastNote in lastAppliedGameNoteInfoPack.gameNoteInfos)
        {
            if (Mathf.Abs(lastNote.idx - note.idx) <= 1)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Original Java: mergeGameNoteInfoPackage() - Chord creation algorithm
    /// Merges notes that are very close in time to create chords
    /// </summary>
    void MergeGameNoteInfoPackage(List<GameNoteInfoPackage> packages)
    {
        if (packages.Count < 2) return;

        var mergedPackages = new List<GameNoteInfoPackage>();
        var currentPackage = packages[0];

        for (int i = 1; i < packages.Count; i++)
        {
            var nextPackage = packages[i];
            float timeDifference = nextPackage.oneNote - currentPackage.oneNote;

            // If notes are close enough, merge them (original threshold logic)
            if (timeDifference <= chordMergeThresholdMs)
            {
                // Merge notes into chord
                currentPackage.gameNoteInfos.AddRange(nextPackage.gameNoteInfos);
            }
            else
            {
                mergedPackages.Add(currentPackage);
                currentPackage = nextPackage;
            }
        }

        mergedPackages.Add(currentPackage);
        packages.Clear();
        packages.AddRange(mergedPackages);
    }
    #endregion

    #region Song Data Loading & Processing

    /// <summary>
    /// Load song data and prepare note packages
    /// Modern Unity approach for data loading
    /// </summary>
    public void LoadSongData(SongData songData)
    {
        if (songData == null)
        {
            Debug.LogError("🎵 Cannot load null song data");
            return;
        }

        // Reset state for new song
        ResetGenerationState();

        // Store song BPM for tempo calculations
        currentSongBPM = songData.bpm;
        Debug.Log($"🎵 Song BPM set to: {currentSongBPM}");

        // Load note chart using songKey instead of noteChartPath
        var rawNoteData = LoadNotesFromJSON(songData.songKey);

        // Convert raw data to note packages using original algorithms
        var notePackages = ConvertRawDataToPackages(rawNoteData, songData.bpm);

        // Apply original merging algorithm
        MergeGameNoteInfoPackage(notePackages);

        // Count total notes in packages for debugging
        int totalNotesInPackages = 0;
        foreach (var package in notePackages)
        {
            totalNotesInPackages += package.gameNoteInfos.Count;
            finalGameNotePackages.Enqueue(package);
        }

        Debug.Log($"🎵 Created {notePackages.Count} note packages with {totalNotesInPackages} total notes");

        // Set first package as current
        if (finalGameNotePackages.Count > 0)
        {
            currentReturnPackage = finalGameNotePackages.Dequeue();
        }
    }

    /// <summary>
    /// Load note chart data from file - supports JSON, binary, or other formats
    /// </summary>
    List<RawNoteData> LoadNoteChartData(string chartPath)
    {
        // Try to load actual chart data from Resources
        TextAsset chartAsset = Resources.Load<TextAsset>(chartPath);
        if (chartAsset != null)
        {
            Debug.Log($"🎵 Chart file found: {chartPath}, loading real data!");
            float songBPM = GetCurrentSongBPM(); // Get BPM from current song
            return ParseJsonChartData(chartAsset.text, songBPM);
        }

        // Try alternative paths for known songs
        string[] alternatePaths = {
            "Song_Note_Jsons/cannon_notes",
            "Song_Note_Jsons/all_songs_notes"
        };

        foreach (string altPath in alternatePaths)
        {
            chartAsset = Resources.Load<TextAsset>(altPath);
            if (chartAsset != null)
            {
                Debug.Log($"🎵 Chart file found at alternate path: {altPath}");
                return ParseJsonChartData(chartAsset.text);
            }
        }

        // Generate demo data for testing
        Debug.LogWarning($"🎵 Chart file not found: {chartPath}, generating demo data");
        return GenerateDemoNoteData();
    }

    /// <summary>
    /// Parse JSON chart data into raw note data
    /// </summary>
    List<RawNoteData> ParseJsonChartData(string jsonText, float songBPM = 120f)
    {
        List<RawNoteData> notes = new List<RawNoteData>();

        try
        {
            // Simple JSON parsing for song sequences
            JsonSequenceArray wrapper = JsonUtility.FromJson<JsonSequenceArray>(
                "{\"sequences\":" + jsonText + "}"
            );
            JsonSongSequence[] sequences = wrapper.sequences;

            float currentTime = 0f;
            float stepDuration = (60f / songBPM) / 4f; // Use actual song BPM!

            foreach (var sequence in sequences) // Load all sequences for full song
            {
                float sequenceStartTime = currentTime;

                // Process each lane
                string[] lines = { sequence.line1, sequence.line2, sequence.line3,
                                  sequence.line4, sequence.line5, sequence.line6 };

                for (int lane = 0; lane < lines.Length; lane++)
                {
                    if (string.IsNullOrEmpty(lines[lane])) continue;

                    var laneNotes = ParseNoteLine(lines[lane], lane, sequenceStartTime, stepDuration);
                    notes.AddRange(laneNotes);
                }

                // Estimate sequence duration
                currentTime += stepDuration * 60; // Roughly 60 steps per sequence
            }

            Debug.Log($"🎵 Parsed {notes.Count} notes from JSON data");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🎵 JSON parsing failed: {e.Message}");
            return GenerateDemoNoteData();
        }

        return notes;
    }

    /// <summary>
    /// Parse a single note line from JSON
    /// </summary>
    List<RawNoteData> ParseNoteLine(string noteLine, int lane, float startTime, float stepDuration)
    {
        List<RawNoteData> notes = new List<RawNoteData>();

        string[] steps = noteLine.Split('/');

        for (int stepIndex = 0; stepIndex < steps.Length; stepIndex++)
        {
            string step = steps[stepIndex].Trim();
            if (string.IsNullOrEmpty(step) || step == "_,_") continue;

            // Parse "pitch,duration" format
            string[] parts = step.Split(',');
            if (parts.Length == 2 && parts[0] != "_")
            {
                if (int.TryParse(parts[0], out int pitch) && int.TryParse(parts[1], out int duration))
                {
                    // Validate pitch range (AudioManager supports 0-44)
                    if (pitch >= 0 && pitch <= 44)
                    {
                        var noteData = new RawNoteData
                        {
                            timeMs = startTime + (stepIndex * stepDuration * 1000f),
                            lane = lane,
                            noteType = NoteType.Single,
                            pitch = pitch,
                            duration = duration
                        };
                        notes.Add(noteData);
                    }
                    else
                    {
                        Debug.LogWarning($"🎵 Pitch {pitch} out of range (0-44), skipping note");
                    }
                }
            }
        }

        return notes;
    }

    /// <summary>
    /// Generate demo note data for testing
    /// </summary>
    List<RawNoteData> GenerateDemoNoteData()
    {
        List<RawNoteData> demoNotes = new List<RawNoteData>();

        // Generate simple demo pattern with full pitch range
        for (int i = 0; i < 20; i++)
        {
            var note = new RawNoteData
            {
                timeMs = i * 500f, // Every 0.5 seconds
                lane = i % laneCount,
                noteType = NoteType.Single,
                pitch = UnityEngine.Random.Range(0, 45), // Full range (0-44) to use all available audio files
                duration = 4
            };
            demoNotes.Add(note);
        }

        Debug.Log($"🎵 Generated {demoNotes.Count} demo notes with full pitch range (0-44)");
        return demoNotes;
    }

    /// <summary>
    /// Convert raw note data to game note packages with timing
    /// </summary>
    List<GameNoteInfoPackage> ConvertRawDataToPackages(List<RawNoteData> rawData, float bpm)
    {
        var packages = new List<GameNoteInfoPackage>();

        // Group notes by time
        var groupedNotes = rawData.GroupBy(note => Mathf.RoundToInt(note.timeMs / 50f) * 50f);

        foreach (var group in groupedNotes)
        {
            var package = new GameNoteInfoPackage
            {
                oneNote = group.Key,
                gameNoteInfos = new List<GameNoteInfo>()
            };

            foreach (var rawNote in group)
            {
                var gameNote = new GameNoteInfo
                {
                    idx = rawNote.lane,
                    timeMs = rawNote.timeMs,
                    noteType = rawNote.noteType,
                    instrumentType = GameManager.Instance?.GetSelectedInstrument() ?? InstrumentType.Piano,
                    pitch = rawNote.pitch >= 0 ? rawNote.pitch : (24 + rawNote.lane * 2), // Use JSON pitch or fallback
                    duration = rawNote.duration
                };

                package.gameNoteInfos.Add(gameNote);
            }

            packages.Add(package);
        }

        return packages.OrderBy(p => p.oneNote).ToList();
    }

    /// <summary>
    /// Load note data from JSON file based on song key
    /// </summary>
    List<RawNoteData> LoadNotesFromJSON(string songKey)
    {
        try
        {
            // Validate songKey
            if (string.IsNullOrEmpty(songKey))
            {
                Debug.LogError("❌ songKey is null or empty! Using demo notes.");
                return GenerateDemoNoteData();
            }

            // Map song key to correct JSON file
            string jsonPath = GetJSONPathForSong(songKey);

            Debug.Log($"🎼 Loading notes from: {jsonPath} (songKey: '{songKey}')");

            TextAsset jsonFile = Resources.Load<TextAsset>(jsonPath);
            if (jsonFile == null)
            {
                Debug.LogWarning($"⚠️ JSON file not found: {jsonPath}, using demo notes");
                return GenerateDemoNoteData();
            }

            // Use existing ParseJsonChartData method with current song BPM
            List<RawNoteData> notes = ParseJsonChartData(jsonFile.text, currentSongBPM);

            Debug.Log($"🎵 Loaded {notes.Count} notes from {jsonPath}");
            return notes;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error loading JSON for '{songKey}': {e.Message}");
            return GenerateDemoNoteData();
        }
    }

    /// <summary>
    /// Map song key to correct JSON file path
    /// </summary>
    string GetJSONPathForSong(string songKey)
    {
        // songKey format: "music_1", "music_2", etc.
        if (songKey.StartsWith("music_"))
        {
            string musicId = songKey.Substring(6); // Remove "music_" prefix

            // Check for specific named files first
            switch (musicId)
            {
                case "1":
                    return "Song_Note_Jsons/cannon_notes"; // Pachelbel's Cannon
                case "4":
                    return "Song_Note_Jsons/vidalita_notes"; // Vidalita (if exists)
                case "7":
                    return "Song_Note_Jsons/toccata_notes"; // Toccata and Fugue (if exists)
                default:
                    // Try generic format first
                    return $"Song_Note_Jsons/music_{musicId}_notes";
            }
        }

        // Fallback to old format for backwards compatibility
        return $"Song_Note_Jsons/{songKey}_notes";
    }
    #endregion

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
        OnGenerationComplete?.Invoke();
    }
    #endregion
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

[System.Serializable]
public enum NoteType
{
    Single,
    Hold,
    Chord
}

#endregion