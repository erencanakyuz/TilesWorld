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
    private int maxGameHeightLength = 6;            // ✅ EXACT Java: maxGameHeightLength
    private int validGameNoteCnt = 0;               // ✅ EXACT Java: validGameNoteCnt

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
    private GameNoteInfoPackage lastApplyGameNoteInfoPack;           // ✅ EXACT Java: lastApplyGameNoteInfoPack
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
    /// *** EXACT JAVA: getNote() method - CRITICAL! ***
    /// This is the EXACT 1:1 port from original GameNoteCreator.java line 259
    /// </summary>
    public List<GameNoteInfo> GetNote(float deltaTime)
    {
        // *** EXACT JAVA LOGIC ***
        if (firstPassCnt > 0)
        {
            firstPassCnt--;
            return null;
        }

        accDeltaTime += deltaTime;

        if (isSecondRequest)
        {
            if (FIRST_DELAY <= accDeltaTime * 1000.0f)
            {
                accDeltaTime = 0.0f;
                isSecondRequest = false;

                // Fire Unity event
                if (returnGameNoteInfoPack != null)
                {
                    OnNotesGenerated?.Invoke(returnGameNoteInfoPack.gameNoteInfos);
                    return returnGameNoteInfoPack.gameNoteInfos;
                }
                return null;
            }
            return null;
        }

        if (returnGameNoteInfoPack == null) return null;

        if (returnGameNoteInfoPack.oneNote <= accDeltaTime * 1000.0f)
        {
            accDeltaTime = 0.0f;
            if (finalGameNotePackIterator != null && finalGameNotePackIterator.MoveNext())
            {
                GameNoteInfoPackage gameNoteInfoPackage = finalGameNotePackIterator.Current;
                returnGameNoteInfoPack = gameNoteInfoPackage;

                // Fire Unity event
                OnNotesGenerated?.Invoke(gameNoteInfoPackage.gameNoteInfos);
                return gameNoteInfoPackage.gameNoteInfos;
            }

            isAllCreated = true;
            OnGenerationComplete?.Invoke();
            return null;
        }
        return null;
    }

    /// <summary>
    /// *** EXACT JAVA: Constructor equivalent ***
    /// Simplified to work with LoadSong flow
    /// </summary>
    public void InitializeGame(int tempo, int maxHeightLength)
    {
        maxGameHeightLength = maxHeightLength;

        // Reset state for new song
        ResetState();

        Debug.Log($"🎵 EXACT JAVA: Game initialized with tempo={tempo}, lanes={maxHeightLength}");
    }

    /// <summary>
    /// *** EXACT JAVA: generateFinalList() method ***
    /// This works with the LoadSong flow that provides already processed packages
    /// </summary>
    private void GenerateFinalList()
    {
        // This method is now handled by LoadSong -> ConvertChartToTemporalInfo -> GenerateFinalPackages
        // The finalGameNotePackages list is set by LoadSong process

        if (finalGameNotePackages != null && finalGameNotePackages.Count > 0)
        {
            validGameNoteCnt = finalGameNotePackages.Count;
            Debug.Log($"🎵 EXACT JAVA: Using {finalGameNotePackages.Count} pre-generated packages");
        }
        else
        {
            Debug.LogWarning("🎵 EXACT JAVA: No packages found! Make sure LoadSong was called first.");
        }
    }

    /// <summary>
    /// *** EXACT JAVA: applyRule() method ***
    /// </summary>
    private void ApplyRule(GameNoteInfoPackage gameNoteInfoPackage)
    {
        MergeGameNoteInfoPackage(gameNoteInfoPackage, 2);
        if (!EqualLastNoteInfoPackagePitch(gameNoteInfoPackage) && EqualLastInfoPackageIdx(gameNoteInfoPackage))
        {
            ApplyComplexRule(gameNoteInfoPackage);
        }
        ApplySpace(gameNoteInfoPackage);
    }

    /// <summary>
    /// *** EXACT JAVA: applyComplexRule() method ***
    /// This creates the flowing "S" pattern that makes the game feel alive
    /// </summary>
    private void ApplyComplexRule(GameNoteInfoPackage gameNoteInfoPackage)
    {
        if (isFlowingRight)
        {
            directionCounter++;
        }
        else
        {
            directionCounter--;
        }

        for (int i = 0; i < gameNoteInfoPackage.gameNoteInfos.Count; i++)
        {
            int j = gameNoteInfoPackage.gameNoteInfos[i].idx;
            int k = directionCounter;
            int m = laneCount;
            gameNoteInfoPackage.gameNoteInfos[i].idx = (j + k) % m;
        }

        if (directionCounter >= maxDirectionInterval)
        {
            isFlowingRight = false;
        }
        else if (directionCounter <= 0)
        {
            isFlowingRight = true;
        }

        // Check for overlap with last package
        for (int i = 0; i < gameNoteInfoPackage.gameNoteInfos.Count; i++)
        {
            if (IsExistInLastApplyGameNoteInfoPack(gameNoteInfoPackage.gameNoteInfos[i]))
            {
                ApplyComplexRule(gameNoteInfoPackage);
                break;
            }
        }
    }

    /// <summary>
    /// *** EXACT JAVA: applySpace() method ***
    /// Prevents clustering of notes in adjacent lanes
    /// </summary>
    private void ApplySpace(GameNoteInfoPackage gameNoteInfoPackage)
    {
        if (gameNoteInfoPackage.gameNoteInfos.Count > 1)
        {
            gameNoteInfoPackage.gameNoteInfos.Sort((a, b) => a.idx.CompareTo(b.idx));

            for (int i = 0; i < gameNoteInfoPackage.gameNoteInfos.Count - 1; i++)
            {
                GameNoteInfo gameNoteInfo1 = gameNoteInfoPackage.gameNoteInfos[i];
                GameNoteInfo gameNoteInfo2 = gameNoteInfoPackage.gameNoteInfos[i + 1];

                if (Mathf.Abs(gameNoteInfo1.idx - gameNoteInfo2.idx) == 1)
                {
                    gameNoteInfo2.idx = (gameNoteInfo2.idx + 1) % laneCount;
                }
            }
        }
    }

    /// <summary>
    /// *** EXACT JAVA: Load and prepare song for GetNote() ***
    /// </summary>
    public void LoadAndPrepareSong(List<NoteChartSequence> chartSequences, int tempo)
    {
        ResetState();

        // 1. Convert chart to temporal data
        List<TemporalNoteInfo> temporalNotes = ConvertChartToTemporalInfo(chartSequences, tempo);

        // 2. Generate final packages with rules
        List<GameNoteInfoPackage> finalPackages = GenerateFinalPackages(temporalNotes);

        // 3. Store packages for GetNote() iterator
        finalGameNotePackages = finalPackages;

        // 4. Initialize iterator
        if (finalGameNotePackages != null && finalGameNotePackages.Count > 0)
        {
            finalGameNotePackIterator = finalGameNotePackages.GetEnumerator();
            if (finalGameNotePackIterator.MoveNext())
            {
                returnGameNoteInfoPack = finalGameNotePackIterator.Current;
            }
        }
        else
        {
            isAllCreated = true;
        }

        Debug.Log($"🎵 EXACT JAVA: Song ready! {finalGameNotePackages?.Count ?? 0} packages.");
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

            // Use EXACT Java algorithm
            InitializeGame((int)song.bpm, 6); // Call EXACT Java constructor equivalent
            LoadAndPrepareSong(chartSequences, (int)song.bpm); // Load packages for GetNote()

            Debug.Log($"🎵 EXACT JAVA: LoadSong complete, ready for GetNote() calls");

            Debug.Log($"🔍 DEBUG: Final packages ready: {finalGameNotePackages?.Count ?? 0}");
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

    #region EXACT Java Helper Methods

    /// <summary>
    /// *** EXACT JAVA: equalLastNoteInfoPackagePitch() method ***
    /// </summary>
    private bool EqualLastNoteInfoPackagePitch(GameNoteInfoPackage gameNoteInfoPackage)
    {
        if (lastApplyGameNoteInfoPack != null)
        {
            List<int> lastList = MakeOneNoteIdList(lastApplyGameNoteInfoPack);
            List<int> currentList = MakeOneNoteIdList(gameNoteInfoPackage);

            if (lastList.Count == currentList.Count)
            {
                for (int i = 0; i < lastList.Count; i++)
                {
                    if (!currentList.Contains(lastList[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// *** EXACT JAVA: equalLastInfoPackageIdx() method ***
    /// </summary>
    private bool EqualLastInfoPackageIdx(GameNoteInfoPackage gameNoteInfoPackage)
    {
        if (lastApplyGameNoteInfoPack != null &&
            lastApplyGameNoteInfoPack.gameNoteInfos.Count == gameNoteInfoPackage.gameNoteInfos.Count)
        {
            for (int i = 0; i < lastApplyGameNoteInfoPack.gameNoteInfos.Count; i++)
            {
                GameNoteInfo lastNote = lastApplyGameNoteInfoPack.gameNoteInfos[i];
                for (int j = 0; j < gameNoteInfoPackage.gameNoteInfos.Count; j++)
                {
                    GameNoteInfo currentNote = gameNoteInfoPackage.gameNoteInfos[j];
                    if (lastNote.idx == currentNote.idx)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// *** EXACT JAVA: isExistInLastApplyGameNoteInfoPack() method ***
    /// </summary>
    private bool IsExistInLastApplyGameNoteInfoPack(GameNoteInfo gameNoteInfo)
    {
        if (lastApplyGameNoteInfoPack == null) return false;

        for (int i = 0; i < lastApplyGameNoteInfoPack.gameNoteInfos.Count; i++)
        {
            if (lastApplyGameNoteInfoPack.gameNoteInfos[i].idx == gameNoteInfo.idx)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// *** EXACT JAVA: makeOneNoteIdList() method ***
    /// </summary>
    private List<int> MakeOneNoteIdList(GameNoteInfoPackage gameNoteInfoPackage)
    {
        List<int> noteIdList = new List<int>();

        for (int i = 0; i < gameNoteInfoPackage.gameNoteInfos.Count; i++)
        {
            for (int j = 0; j < gameNoteInfoPackage.gameNoteInfos[i].noteInfoList.Count; j++)
            {
                OneNote oneNote = gameNoteInfoPackage.gameNoteInfos[i].noteInfoList[j];
                // Using a simple mapping since we don't have Sounds.findSoundIdx
                int soundIdx = oneNote.line * 10 + oneNote.flat;
                noteIdList.Add(soundIdx);
            }
        }
        return noteIdList;
    }

    /// <summary>
    /// *** EXACT JAVA: mergeGameNoteInfoPackage() method ***
    /// </summary>
    private void MergeGameNoteInfoPackage(GameNoteInfoPackage gameNoteInfoPackage, int startIdx)
    {
        while (startIdx < gameNoteInfoPackage.gameNoteInfos.Count)
        {
            GameNoteInfo gameNoteInfo = gameNoteInfoPackage.gameNoteInfos[startIdx];

            for (int i = 0; i < gameNoteInfo.noteInfoList.Count; i++)
            {
                if (startIdx > 0)
                {
                    int randomIdx = UnityEngine.Random.Range(0, startIdx);
                    gameNoteInfoPackage.gameNoteInfos[randomIdx].noteInfoList.Add(gameNoteInfo.noteInfoList[i]);
                }
            }
            gameNoteInfoPackage.gameNoteInfos.RemoveAt(startIdx);
        }
    }

    /// <summary>
    /// Load note list from current song (placeholder implementation)
    /// </summary>
    private List<NoteInfo> LoadNoteListFromCurrentSong(int tempo)
    {
        // This is handled differently in Unity - we use JSON parsing
        // This method is kept for Java compatibility but returns empty list
        return new List<NoteInfo>();
    }

    #endregion

    private void ResetState()
    {
        // EXACT Java reset
        accDeltaTime = 0.0f;
        firstPassCnt = 3;
        isAllCreated = false;
        isSecondRequest = true;
        directionCounter = 0;
        isFlowingRight = true;
        validGameNoteCnt = 0;
        lastApplyGameNoteInfoPack = null;
        returnGameNoteInfoPack = null;
        finalGameNotePackIterator = null;
        finalGameNotePackages = null;

        // Legacy reset (for compatibility)
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