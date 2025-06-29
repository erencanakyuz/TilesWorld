using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// GameNoteCreator - True Dynamic System (oldgame.md'ye uygun)
/// Ham veriyi saklar, her tick'te anlık olarak nota paketi oluşturur ve kuralları uygular.
/// </summary>
public class GameNoteCreator : MonoBehaviour
{
    // --- Orijinal Java'dan Port Edilen Sabitler ---
    private static readonly int[] NOTE_LENGTH_FACTORS = {
        1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56,
        1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56
    };

    private static readonly int[] LANE_PITCH_OFFSET = { 3, 5, 7, 11, 13, 17 };
    private const float FIRST_DELAY_MS = 1000f; // oldgame.md'den

    [Header("🎵 Konfigürasyon")]
    [SerializeField] private int laneCount = 6;
    [SerializeField] private int maxDirectionInterval = 10;

    // --- HAM VERİ (Dinamik İçin) ---
    private List<NoteChartSequence> rawChartData;
    private float baseTimingMs;
    private int currentSequenceIndex = 0;
    private int currentSubdivisionIndex = 0;

    // --- DİNAMİK DURUM ---
    private float accumulatedTime = 0f;
    private bool isAllCreated = false;
    private bool firstDelayCompleted = false;

    // --- KURAL MOTORU (O anki durum) ---
    private int directionCounter = 0;
    private bool isFlowingRight = true;

    // --- Olaylar ---
    public static event Action<List<GameNoteInfo>> OnNotesGenerated;
    public static event Action OnGenerationComplete;

    // --- DİNAMİK DURUM (Yeni) ---
    private GameNoteInfoPackage currentNotePackage;
    private float nextNoteTime = 0f;

    /// <summary>
    /// Ham veriyi yükler ama hiçbir şey hesaplamaz - sadece saklar
    /// </summary>
    public void LoadSong(List<NoteChartSequence> rawChart, int tempo)
    {
        ResetState();

        rawChartData = rawChart;
        baseTimingMs = (60000f / tempo) / 8f;

        Debug.Log($"🎵 Raw chart loaded with {rawChartData.Count} sequences. Base timing: {baseTimingMs:F2}ms");
    }

    /// <summary>
    /// Backward compatibility - SongData'dan JSON yükleyip raw chart'a çevir
    /// </summary>
    public void LoadSong(SongData song)
    {
        if (song == null)
        {
            Debug.LogError("🚨 Song is null!");
            return;
        }

        // JSON dosyasını yükle
        string jsonFileName = GetJsonFileName(song);
        string resourcePath = $"Song_Note_Jsons/Individual/{jsonFileName}".Replace(".json", "");

        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);
        if (jsonFile == null)
        {
            Debug.LogError($"❌ Could not load JSON from: {resourcePath}");
            // Alternative path denemesi kaldırıldı, ana yola odaklanalım
            return;
        }

        try
        {
            var sequences = ParseIndividualJson(jsonFile.text);
            var chartSequences = ConvertToChartSequences(sequences);
            LoadSong(chartSequences, (int)song.bpm);
            Debug.Log($"🎵 DYNAMIC SYSTEM: {song.songName} loaded via compatibility layer!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Load error: {e.Message}");
            Debug.LogError($"❌ Stack trace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// Oyun döngüsünde sürekli çağrılır. oldgame.md'deki oneNote timing sistemini uygular.
    /// Her nota paketi bir sonraki paketin ne kadar süre sonra geleceğini belirler.
    /// </summary>
    public void GetNote(float deltaTime)
    {
        if (isAllCreated) return;

        accumulatedTime += deltaTime * 1000f; // milisaniye'ye çevir

        // PHASE 1: FIRST_DELAY (1000ms başlangıç gecikmesi)
        if (!firstDelayCompleted)
        {
            if (accumulatedTime >= FIRST_DELAY_MS)
            {
                //Debug.Log($"✅ FIRST_DELAY completed! Starting dynamic generation...");
                firstDelayCompleted = true;

                // İlk paketi oluştur
                currentNotePackage = CreatePackageAtCurrentMoment();
                if (currentNotePackage != null)
                {
                    //Debug.Log($"🎯 FIRST package: {currentNotePackage.gameNoteInfos.Count} notes, oneNote: {currentNotePackage.oneNote:F1}ms");
                    OnNotesGenerated?.Invoke(currentNotePackage.gameNoteInfos);

                    // *** FIX: Timing'i doğru reset et ***
                    accumulatedTime = 0f; // Reset accumulated time for clean start
                    nextNoteTime = currentNotePackage.oneNote; // Set next package time
                }
                else
                {
                    isAllCreated = true;
                    OnGenerationComplete?.Invoke();
                }
                return; // *** FIX: Prevent double spawn in same frame ***
            }
            return;
        }

        // PHASE 2: oneNote timing system (asıl dinamik sistem)
        if (currentNotePackage != null && accumulatedTime >= nextNoteTime)
        {
            accumulatedTime -= nextNoteTime; // Kalan süreyi koru (precision için)

            // Bir sonraki paketi oluştur
            currentNotePackage = CreatePackageAtCurrentMoment();
            if (currentNotePackage != null)
            {
                //Debug.Log($"🎯 Next package: {currentNotePackage.gameNoteInfos.Count} notes, oneNote: {currentNotePackage.oneNote:F1}ms, dir: {directionCounter}");

                // *** DEBUG: Event'i tetiklemeden önce subscriber sayısını kontrol et ***
                int subscriberCount = OnNotesGenerated?.GetInvocationList()?.Length ?? 0;
                //Debug.Log($"🔥 EVENT DEBUG: OnNotesGenerated has {subscriberCount} subscribers");

                OnNotesGenerated?.Invoke(currentNotePackage.gameNoteInfos);

                // Bir sonraki paketin zamanını ayarla
                nextNoteTime = currentNotePackage.oneNote;
            }
            else
            {
                // Artık nota kalmadı
                isAllCreated = true;
                OnGenerationComplete?.Invoke();
                //Debug.Log("🏁 Generation complete!");
            }
        }
    }

    #region True Dynamic Generation (Her Call'da Anlık)

    /// <summary>
    /// O ANKİ ANDA sıradaki package'ı bul, oluştur ve kuralları uygula
    /// </summary>
    private GameNoteInfoPackage CreatePackageAtCurrentMoment()
    {
        var templatePackage = FindNextValidPackage();
        if (templatePackage == null) return null;

        // Sıradaki subdivision'a geç
        AdvanceToNextSubdivision();

        // Template'den yeni bir package oluştur ve kuralları uygula
        var finalNotes = new List<GameNoteInfo>();

        // Template'den notaları oluştur
        foreach (var templateNote in templatePackage.gameNoteInfos)
        {
            var gameNote = new GameNoteInfo
            {
                idx = (templateNote.pitch + LANE_PITCH_OFFSET[templateNote.line]) % laneCount,
                pitch = templateNote.pitch,
                line = templateNote.line
            };
            finalNotes.Add(gameNote);
        }

        // *** O ANKİ DURUMA GÖRE KURALLAR UYGULA ***
        ApplyComplexRuleAtThisMoment(finalNotes);
        ApplySpacingAtThisMoment(finalNotes);

        // Final package oluştur
        var finalPackage = new GameNoteInfoPackage
        {
            oneNote = templatePackage.oneNote,
            gameNoteInfos = finalNotes
        };

        return finalPackage;
    }

    /// <summary>
    /// Sıradaki geçerli paketi bul (henüz oluşturmaz!)
    /// </summary>
    private GameNoteInfoPackage FindNextValidPackage()
    {
        if (rawChartData == null)
        {
            Debug.LogError("🚨 rawChartData is null! Song was not loaded properly.");
            return null;
        }

        while (currentSequenceIndex < rawChartData.Count)
        {
            var sequence = rawChartData[currentSequenceIndex];
            var packageData = GetSubdivisionData(sequence, currentSubdivisionIndex);

            if (packageData != null)
            {
                return packageData;
            }

            // Bu sequence bitti, sıradakine geç
            currentSequenceIndex++;
            currentSubdivisionIndex = 0;
        }

        return null; // Hiç nota kalmadı
    }

    /// <summary>
    /// Belirli bir subdivision'dan geçici package oluştur
    /// </summary>
    private GameNoteInfoPackage GetSubdivisionData(NoteChartSequence sequence, int subdivisionIndex)
    {
        var package = new GameNoteInfoPackage();
        var tempNotes = new List<GameNoteInfo>();
        bool hasAnyNote = false;
        int maxDurationType = 0;

        for (int lane = 0; lane < laneCount; lane++)
        {
            string[] subdivisions = sequence.GetLineData(lane).Split('/');
            if (subdivisionIndex >= subdivisions.Length) continue;

            string[] parts = subdivisions[subdivisionIndex].Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out int pitch) && int.TryParse(parts[1], out int duration))
            {
                if (pitch != -1) // Valid note
                {
                    var note = new GameNoteInfo
                    {
                        pitch = pitch,
                        line = lane,
                        idx = -1 // Henüz hesaplanmadı
                    };
                    tempNotes.Add(note);
                    maxDurationType = Mathf.Max(maxDurationType, duration);
                    hasAnyNote = true;
                }
            }
        }

        if (!hasAnyNote) return null;

        // Timing hesapla
        if (maxDurationType < NOTE_LENGTH_FACTORS.Length)
        {
            package.oneNote = NOTE_LENGTH_FACTORS[maxDurationType] * baseTimingMs;
        }
        else
        {
            package.oneNote = baseTimingMs; // Fallback
        }

        package.gameNoteInfos = tempNotes;
        return package;
    }

    private void AdvanceToNextSubdivision()
    {
        currentSubdivisionIndex++;
    }

    /// <summary>
    /// O ANKİ direction counter'a göre akış kuralı uygula
    /// </summary>
    private void ApplyComplexRuleAtThisMoment(List<GameNoteInfo> notes)
    {
        // Direction counter'ı güncelle
        if (isFlowingRight) directionCounter++;
        else directionCounter--;

        if (directionCounter >= maxDirectionInterval) isFlowingRight = false;
        else if (directionCounter <= 0) isFlowingRight = true;

        // O ANKİ counter değeriyle notaları kaydır
        foreach (var note in notes)
        {
            note.idx = (note.idx + directionCounter + laneCount) % laneCount;
        }
    }

    /// <summary>
    /// O ANKİ durumda spacing kuralı uygula
    /// </summary>
    private void ApplySpacingAtThisMoment(List<GameNoteInfo> notes)
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
        isAllCreated = false;
        firstDelayCompleted = false;
        currentSequenceIndex = 0;
        currentSubdivisionIndex = 0;
        directionCounter = 0;
        isFlowingRight = true;
        rawChartData = null;
    }
    #endregion

    #region Helper Methods for Compatibility

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

    private string GetJsonFileName(SongData song)
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
            JsonSequenceArray wrapper = JsonUtility.FromJson<JsonSequenceArray>(jsonText);
            return wrapper?.sequences?.ToList() ?? new List<JsonSongSequence>();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ JSON parse error: {e.Message}");
            return new List<JsonSongSequence>();
        }
    }

    #endregion
}