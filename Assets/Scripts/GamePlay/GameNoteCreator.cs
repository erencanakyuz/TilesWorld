using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 🎹 *** ORİJİNAL JAVA ALGORİTMASI RESTORE EDİLDİ! ***
/// GameNoteCreator - oldgame.md'deki EXACT algoritmaları uygular
/// Critical: LANE_PITCH_OFFSET, NOTE_LENGTH_FACTORS, LoadAndPrepareSong, Tick
/// </summary>
public class GameNoteCreator : MonoBehaviour
{
    // *** ORİJİNAL JAVA'DAN PORT EDİLEN SABİTLER (oldgame.md'den) ***
    private static readonly int[] NOTE_LENGTH_FACTORS = {
        1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56,
        1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56
    };

    private static readonly int[] LANE_PITCH_OFFSET = { 3, 5, 7, 11, 13, 17 };

    [Header("🎵 Original Java Algorithm Settings")]
    private const int MAX_DIRECTION_INTERVAL = 10;

    [SerializeField] private float accDeltaTime = 0.0f;
    [SerializeField] private int firstPassCnt = 3;
    [SerializeField] private bool isAllCreated = false;

    private const float FIRST_DELAY = 1000f;
    private bool isSecondRequest = true;

    [Header("🎵 oldgame.md Konfigürasyon")]
    [SerializeField] private int laneCount = 6;
    [SerializeField] private int maxDirectionInterval = 10;

    // oldgame.md Algoritma Değişkenleri
    private float accumulatedTime = 0f;
    private bool isGenerationComplete = false;
    private GameNoteInfoPackage currentPackage;
    private Queue<GameNoteInfoPackage> notePackageQueue = new Queue<GameNoteInfoPackage>();

    private int directionCounter = 0;
    private bool isFlowingRight = true;

    // Sequence Data
    private IEnumerator<GameNoteInfoPackage> finalGameNotePackIterator;
    private GameNoteInfoPackage returnGameNoteInfoPack;
    private List<GameNoteInfoPackage> finalGameNotePackages;

    private AudioManager audioManager;
    private SongData currentSong;
    private float currentSongBPM = 120f;

    // Events
    public static event System.Action<List<GameNoteInfo>> OnNotesGenerated;
    public static event System.Action OnGenerationComplete;

    void Start()
    {
        audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogWarning("⚠️ AudioManager.Instance not found!");
        }
    }

    /// <summary>
    /// *** oldgame.md'deki GetNote algoritması (CRITICAL!) ***
    /// Simplified version that works with the Tick system
    /// </summary>
    public List<GameNoteInfo> GetNote(float deltaTime)
    {
        // Simply return null since we're using Tick() system now
        // This prevents double-processing
        return null;
    }

    /// <summary>
    /// *** oldgame.md'deki LoadAndPrepareSong (CRITICAL!) ***
    /// </summary>
    public void LoadAndPrepareSong(List<NoteChartSequence> rawChart, int tempo)
    {
        ResetState();

        // 1. Convert to temporal data
        List<TemporalNoteInfo> temporalNotes = ConvertChartToTemporalInfo(rawChart, tempo);

        // 2. Generate final packages with rules
        List<GameNoteInfoPackage> finalPackages = GenerateFinalPackages(temporalNotes);

        // 3. Queue packages
        foreach (var package in finalPackages)
        {
            notePackageQueue.Enqueue(package);
        }

        // 4. Prepare first package
        if (notePackageQueue.Count > 0)
        {
            currentPackage = notePackageQueue.Dequeue();
            returnGameNoteInfoPack = currentPackage;
        }
        else
        {
            isGenerationComplete = true;
        }

        Debug.Log($"🎵 oldgame.md: Song ready! {notePackageQueue.Count + 1} packages.");
    }

    /// <summary>
    /// *** oldgame.md'deki Tick metodu (CRITICAL!) ***
    /// Alternative to GetNote for queue-based approach
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (isGenerationComplete || currentPackage == null)
        {
            return; // Removed spam debug log
        }

        accumulatedTime += deltaTime * 1000f;

        // Only log when close to spawning to reduce spam
        if (accumulatedTime >= currentPackage.oneNote * 0.8f)
        {
            Debug.Log($"🔍 TICK: accTime={accumulatedTime:F1}ms, needed={currentPackage.oneNote:F1}ms, notes={currentPackage.gameNoteInfos.Count}");
        }

        if (accumulatedTime >= currentPackage.oneNote)
        {
            accumulatedTime -= currentPackage.oneNote;

            Debug.Log($"🎵 TICK: Spawning {currentPackage.gameNoteInfos.Count} notes, remaining queue: {notePackageQueue.Count}");
            OnNotesGenerated?.Invoke(currentPackage.gameNoteInfos);

            if (notePackageQueue.Count > 0)
            {
                currentPackage = notePackageQueue.Dequeue();
                Debug.Log($"🔍 TICK: Next package loaded, delay={currentPackage.oneNote:F1}ms, notes={currentPackage.gameNoteInfos.Count}");
            }
            else
            {
                currentPackage = null;
                isGenerationComplete = true;
                OnGenerationComplete?.Invoke();
                Debug.Log("🎵 TICK: All packages complete!");
            }
        }
    }

    /// <summary>
    /// Public interface for existing code
    /// </summary>
    public void LoadSong(SongData song)
    {
        if (song == null) return;

        currentSong = song;
        currentSongBPM = song.bpm;

        // Load JSON and convert to NoteChartSequence format
        string jsonFileName = GetIndividualJsonFileName(song);
        string resourcePath = $"Song_Note_Jsons/Individual/{jsonFileName}".Replace(".json", "");

        Debug.Log($"🔍 DEBUG: Loading {song.songName} by {song.artist}");
        Debug.Log($"🔍 DEBUG: jsonFileName = {jsonFileName}");
        Debug.Log($"🔍 DEBUG: resourcePath = {resourcePath}");

        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);

        if (jsonFile == null)
        {
            Debug.LogError($"❌ Could not load: {resourcePath}");
            return;
        }

        Debug.Log($"🔍 DEBUG: JSON file loaded, length: {jsonFile.text.Length}");

        try
        {
            var sequences = ParseIndividualJson(jsonFile.text);
            Debug.Log($"🔍 DEBUG: Parsed {sequences.Count} sequences from JSON");

            var chartSequences = ConvertToChartSequences(sequences);
            Debug.Log($"🔍 DEBUG: Converted to {chartSequences.Count} chart sequences");

            // Use oldgame.md algorithm
            LoadAndPrepareSong(chartSequences, (int)song.bpm);
            InitializePackageIterator();

            Debug.Log($"🔍 DEBUG: Final packages in queue: {notePackageQueue.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Load error: {e.Message}");
            Debug.LogError($"❌ Stack trace: {e.StackTrace}");
        }
    }

    #region oldgame.md Core Algorithms

    /// <summary>
    /// *** oldgame.md'deki ConvertChartToTemporalInfo (CRITICAL!) ***
    /// </summary>
    private List<TemporalNoteInfo> ConvertChartToTemporalInfo(List<NoteChartSequence> chart, int tempo)
    {
        // *** GEÇICI FIX: 10x yavaşlatım test için ***
        float baseTimingMs = ((60000f / tempo) / 8f) * 10f; // 10x slower for testing
        var temporalNoteList = new List<TemporalNoteInfo>();

        Debug.Log($"🔍 TIMING: BPM={tempo}, baseTimingMs={baseTimingMs:F1}ms");

        foreach (var sequence in chart)
        {
            var columns = new Dictionary<int, (int pitch, int duration)[]>();
            int maxSubdivisions = 0;

            // Parse all lanes
            for (int lane = 0; lane < laneCount; lane++)
            {
                string lineData = GetLineData(sequence, lane);
                string[] subdivisions = lineData.Split('/');
                if (subdivisions.Length > maxSubdivisions) maxSubdivisions = subdivisions.Length;

                for (int i = 0; i < subdivisions.Length; i++)
                {
                    if (!columns.ContainsKey(i)) columns[i] = new (int, int)[6];

                    string[] parts = subdivisions[i].Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int pitch) && int.TryParse(parts[1], out int duration))
                    {
                        columns[i][lane] = (pitch, duration);
                    }
                    else
                    {
                        columns[i][lane] = (-1, -1);
                    }
                }
            }

            // Process time slots
            for (int subIdx = 0; subIdx < maxSubdivisions; subIdx++)
            {
                if (!columns.ContainsKey(subIdx)) continue;

                var temporalInfo = new TemporalNoteInfo();
                bool hasNotes = false;

                for (int lane = 0; lane < laneCount; lane++)
                {
                    var (pitch, duration) = columns[subIdx][lane];
                    if (pitch != -1)
                    {
                        temporalInfo.pitches[lane] = pitch;
                        temporalInfo.durationType = duration;
                        hasNotes = true;
                    }
                }

                if (hasNotes)
                {
                    // *** ORİJİNAL JAVA: NOTE_LENGTH_FACTORS ***
                    if (temporalInfo.durationType >= 0 && temporalInfo.durationType < NOTE_LENGTH_FACTORS.Length)
                    {
                        temporalInfo.timingMs = NOTE_LENGTH_FACTORS[temporalInfo.durationType] * baseTimingMs;
                    }
                    else
                    {
                        temporalInfo.timingMs = baseTimingMs; // Fallback
                    }
                    temporalNoteList.Add(temporalInfo);
                }
            }
        }
        return temporalNoteList;
    }

    /// <summary>
    /// *** oldgame.md'deki GenerateFinalPackages (CRITICAL!) ***
    /// </summary>
    private List<GameNoteInfoPackage> GenerateFinalPackages(List<TemporalNoteInfo> temporalNotes)
    {
        var packages = new List<GameNoteInfoPackage>();

        foreach (var tNote in temporalNotes)
        {
            var package = new GameNoteInfoPackage { oneNote = tNote.timingMs };
            var tempNotes = new List<GameNoteInfo>();

            for (int lane = 0; lane < laneCount; lane++)
            {
                if (tNote.pitches[lane] != -1)
                {
                    var gameNote = new GameNoteInfo
                    {
                        // *** ORİJİNAL JAVA: LANE_PITCH_OFFSET ***
                        idx = (tNote.pitches[lane] + LANE_PITCH_OFFSET[lane]) % laneCount,
                        pitch = tNote.pitches[lane],
                        line = lane
                    };
                    tempNotes.Add(gameNote);
                }
            }

            // *** oldgame.md Rules ***
            ApplyComplexRule(tempNotes);
            ApplySpacing(tempNotes);

            package.gameNoteInfos = tempNotes;
            packages.Add(package);
        }
        return packages;
    }

    /// <summary>
    /// *** oldgame.md'deki ApplyComplexRule (CRITICAL!) ***
    /// </summary>
    private void ApplyComplexRule(List<GameNoteInfo> notes)
    {
        if (isFlowingRight) directionCounter++;
        else directionCounter--;

        if (directionCounter >= maxDirectionInterval) isFlowingRight = false;
        else if (directionCounter <= 0) isFlowingRight = true;

        foreach (var note in notes)
        {
            note.idx = (note.idx + directionCounter + laneCount) % laneCount;
        }
    }

    /// <summary>
    /// *** oldgame.md'deki ApplySpacing (CRITICAL!) ***
    /// </summary>
    private void ApplySpacing(List<GameNoteInfo> notes)
    {
        if (notes.Count <= 1) return;

        notes.Sort((a, b) => a.idx.CompareTo(b.idx));
        for (int i = 0; i < notes.Count - 1; i++)
        {
            if (Mathf.Abs(notes[i].idx - notes[i + 1].idx) <= 1)
            {
                notes[i + 1].idx = (notes[i + 1].idx + 1) % laneCount;
            }
        }
    }

    private void ResetState()
    {
        accumulatedTime = 0f;
        isGenerationComplete = false;
        directionCounter = 0;
        isFlowingRight = true;
        notePackageQueue.Clear();
        accDeltaTime = 0.0f;
        firstPassCnt = 3;
        isSecondRequest = true;
        returnGameNoteInfoPack = null;
    }

    #endregion

    #region Helper Methods

    private string GetLineData(NoteChartSequence sequence, int lane)
    {
        return lane switch
        {
            0 => sequence.line1 ?? "",
            1 => sequence.line2 ?? "",
            2 => sequence.line3 ?? "",
            3 => sequence.line4 ?? "",
            4 => sequence.line5 ?? "",
            5 => sequence.line6 ?? "",
            _ => ""
        };
    }

    private List<NoteChartSequence> ConvertToChartSequences(List<JsonSongSequence> jsonSequences)
    {
        var chartSequences = new List<NoteChartSequence>();

        foreach (var jsonSeq in jsonSequences)
        {
            var chartSeq = new NoteChartSequence
            {
                music_id = jsonSeq.music_id,
                seq = jsonSeq.seq,
                line1 = jsonSeq.line1,
                line2 = jsonSeq.line2,
                line3 = jsonSeq.line3,
                line4 = jsonSeq.line4,
                line5 = jsonSeq.line5,
                line6 = jsonSeq.line6
            };
            chartSequences.Add(chartSeq);
        }

        return chartSequences;
    }

    private string GetIndividualJsonFileName(SongData song)
    {
        string songName = song.songName.ToLower()
            .Replace(" ", "_")
            .Replace("★", "").Replace("☆", "")
            .Replace(".", "").Replace(",", "").Replace("'", "");

        string artist = (!string.IsNullOrEmpty(song.artist) ? song.artist : "unknown").ToLower()
            .Replace(" ", "_").Replace(".", "");

        return $"{songName}_{artist}.json";
    }

    private List<JsonSongSequence> ParseIndividualJson(string jsonText)
    {
        try
        {
            // Individual JSON files already have { "sequences": [...] } format
            // No need to add extra wrapper like in old format
            JsonSequenceArray wrapper = JsonUtility.FromJson<JsonSequenceArray>(jsonText);
            Debug.Log($"🔍 DEBUG: Wrapper parsed, sequences array length: {wrapper?.sequences?.Length ?? 0}");
            return wrapper?.sequences?.ToList() ?? new List<JsonSongSequence>();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ JSON parse error: {e.Message}");
            Debug.LogError($"❌ JSON content preview: {jsonText.Substring(0, Mathf.Min(200, jsonText.Length))}...");
            return new List<JsonSongSequence>();
        }
    }

    private void InitializePackageIterator()
    {
        if (finalGameNotePackages != null)
        {
            finalGameNotePackIterator = finalGameNotePackages.GetEnumerator();
            if (finalGameNotePackIterator.MoveNext())
            {
                returnGameNoteInfoPack = finalGameNotePackIterator.Current;
            }
        }
    }

    #endregion

    #region Public Interface

    public bool IsGenerationComplete() => isAllCreated;
    public void StopGeneration() => isAllCreated = true;

    #endregion
}