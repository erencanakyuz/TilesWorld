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

    [Header("📊 Debug & Analysis")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private int totalNotesGenerated = 0;
    [SerializeField] private int currentDirectionCnt = 0;
    [SerializeField] private bool isRightDirection = true;

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

    // Events for system integration
    public static event Action<List<GameNoteInfo>> OnNotesGenerated;
    public static event Action OnGenerationComplete;

    void Awake()
    {
        InitializeNoteCreator();
    }

    void Start()
    {
        if (showDebugInfo)
            Debug.Log("🎵 GameNoteCreator initialized with original algorithms");

        // Force enable debug for initial testing
        showDebugInfo = true;
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
        // First delay logic (original Java)
        if (firstPassCount > 0)
        {
            firstPassCount--;
            if (showDebugInfo)
                Debug.Log($"🎵 First pass delay: {firstPassCount} remaining");
            return null;
        }

        accumulatedDeltaTime += deltaTime;

        // Check if it's time to return notes (original timing logic)
        if (currentReturnPackage != null &&
            currentReturnPackage.oneNote <= accumulatedDeltaTime * 1000f)
        {
            var packageToProcess = currentReturnPackage;
            accumulatedDeltaTime = 0f;

            // Get next package ready
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
            OnNotesGenerated?.Invoke(notesToReturn);

            // Only log occasionally if debug enabled
            if (showDebugInfo && totalNotesGenerated % 5 == 0)
                Debug.Log($"🎵 Generated {notesToReturn.Count} notes at time: {(accumulatedDeltaTime + deltaTime) * 1000f:F1}ms");

            totalNotesGenerated += notesToReturn.Count;
            return notesToReturn;
        }

        return null;
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

                    if (showDebugInfo)
                        Debug.Log($"🎯 Applied spacing: Note moved from {notes[j].idx} to {note2.idx}");
                }

                // Check if notes are too close in time (using noteSpacingMinMs)
                float timeDifference = Mathf.Abs(note1.timeMs - note2.timeMs);
                if (timeDifference < noteSpacingMinMs)
                {
                    // Adjust timing to maintain minimum spacing
                    note2.timeMs = note1.timeMs + noteSpacingMinMs;

                    if (showDebugInfo)
                        Debug.Log($"🎯 Applied time spacing: {timeDifference:F1}ms → {noteSpacingMinMs}ms minimum");
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

            if (showDebugInfo && originalIdx != note.idx)
                Debug.Log($"🎯 Complex rule applied: Note {originalIdx} → {note.idx}, Direction: {(isRightDirection ? "→" : "←")}");
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

                if (showDebugInfo)
                    Debug.Log($"🎵 Merged chord: {timeDifference:F1}ms gap → {currentPackage.gameNoteInfos.Count} notes");
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

        Debug.Log($"🎵 Loading song: {songData.songName} - {songData.bpm} BPM");

        // Reset state for new song
        ResetGenerationState();

        // Load note chart (this would come from JSON or other data source)
        var rawNoteData = LoadNoteChartData(songData.noteChartPath);

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

        // Set first package as current
        if (finalGameNotePackages.Count > 0)
        {
            currentReturnPackage = finalGameNotePackages.Dequeue();
            Debug.Log($"🎵 First package ready: {currentReturnPackage.oneNote}ms with {currentReturnPackage.gameNoteInfos.Count} notes");
        }

        Debug.Log($"🎵 Song loaded: {notePackages.Count} note packages, {totalNotesInPackages} total notes in packages");
    }

    /// <summary>
    /// Load note chart data from file (placeholder for actual implementation)
    /// </summary>
    List<RawNoteData> LoadNoteChartData(string chartPath)
    {
        // This would load from JSON, binary, or other format
        // For now, return sample data with more notes for testing
        var sampleData = new List<RawNoteData>();

        Debug.Log($"🎵 Loading test note chart data from: {chartPath}");

        // Generate sample notes for testing - more frequent for visible gameplay
        for (int i = 0; i < 60; i++) // 60 notes over 30 seconds
        {
            sampleData.Add(new RawNoteData
            {
                timeMs = i * 500f, // Every 0.5 seconds
                lane = UnityEngine.Random.Range(0, laneCount),
                noteType = NoteType.Single
            });
        }

        Debug.Log($"🎵 Generated {sampleData.Count} test notes for gameplay");
        return sampleData;
    }

    /// <summary>
    /// Convert raw note data to game note packages with timing
    /// </summary>
    List<GameNoteInfoPackage> ConvertRawDataToPackages(List<RawNoteData> rawData, float bpm)
    {
        var packages = new List<GameNoteInfoPackage>();

        Debug.Log($"🎵 Converting {rawData.Count} raw notes to packages...");

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
                    pitch = 24 + rawNote.lane * 2 // Simple pitch mapping
                };

                package.gameNoteInfos.Add(gameNote);
            }

            packages.Add(package);

            if (showDebugInfo && packages.Count <= 5) // Show first 5 packages
            {
                Debug.Log($"🎵 Package {packages.Count}: Time {package.oneNote}ms, {package.gameNoteInfos.Count} notes");
            }
        }

        Debug.Log($"🎵 Converted to {packages.Count} packages");
        return packages.OrderBy(p => p.oneNote).ToList();
    }
    #endregion

    #region Public Interface

    public bool IsGenerationComplete() => isAllCreated;
    public int GetTotalNotesGenerated() => totalNotesGenerated;
    public float GetCurrentGenerationTime() => accumulatedDeltaTime;
    public bool IsRightDirection() => isRightDirection;
    public int GetCurrentDirectionCount() => currentDirectionCnt;

    /// <summary>
    /// Force generation completion (for testing)
    /// </summary>
    public void CompleteGeneration()
    {
        isAllCreated = true;
        OnGenerationComplete?.Invoke();
    }

    /// <summary>
    /// Get algorithm statistics for debugging
    /// </summary>
    public NoteGenerationStats GetGenerationStats()
    {
        return new NoteGenerationStats
        {
            totalNotesGenerated = totalNotesGenerated,
            currentDirectionCount = currentDirectionCnt,
            isRightDirection = isRightDirection,
            remainingPackages = finalGameNotePackages.Count,
            accumulatedTime = accumulatedDeltaTime
        };
    }
    #endregion

    #region Debug & Visualization

    void Update()
    {
        // Only log stats very rarely, not every 5 seconds
        if (showDebugInfo && Time.frameCount % 1800 == 0) // Every 30 seconds
        {
            Debug.Log($"🎵 Note Generator: {totalNotesGenerated} generated, Direction: {(isRightDirection ? "→" : "←")} ({currentDirectionCnt}), Remaining: {finalGameNotePackages.Count}");
        }
    }

    void OnDrawGizmos()
    {
        if (showDebugInfo && Application.isPlaying)
        {
            // Draw direction indicator
            Gizmos.color = isRightDirection ? Color.green : Color.red;
            Vector3 center = transform.position;
            Vector3 direction = isRightDirection ? Vector3.right : Vector3.left;

            Gizmos.DrawRay(center, direction * 2f);
            Gizmos.DrawSphere(center + direction * 2f, 0.2f);
        }
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
    public bool alreadyHit;           // Hit state tracking
}

[System.Serializable]
public class RawNoteData
{
    public float timeMs;
    public int lane;
    public NoteType noteType;
}

[System.Serializable]
public enum NoteType
{
    Single,
    Hold,
    Chord
}

[System.Serializable]
public struct NoteGenerationStats
{
    public int totalNotesGenerated;
    public int currentDirectionCount;
    public bool isRightDirection;
    public int remainingPackages;
    public float accumulatedTime;
}

#endregion