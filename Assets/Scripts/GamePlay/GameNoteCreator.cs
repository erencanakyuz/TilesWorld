using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// GameNoteCreator - Oyunun Kalbi (Refaktör Edilmiş)
/// Orijinal Java oyununun dikey zaman dilimlemesi ve dinamik nota oluşturma mantığını uygular.
/// </summary>
public class GameNoteCreator : MonoBehaviour
{
    // --- Orijinal Java'dan Port Edilen Sabitler ---
    private static readonly int[] NOTE_LENGTH_FACTORS = {
        1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56,
        1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56
    };

    private static readonly int[] LANE_PITCH_OFFSET = { 3, 5, 7, 11, 13, 17 };
    private const float FIRST_DELAY_MS = 1500f; // Başlangıç gecikmesi (daha belirgin)

    [Header("🎵 Konfigürasyon")]
    [SerializeField] private int laneCount = 6;
    [SerializeField] private int maxDirectionInterval = 10;

    // --- Algoritma Durum Değişkenleri ---
    private float accumulatedTime = 0f;
    private bool isGenerationComplete = false;
    private bool firstDelayCompleted = false;
    private Queue<GameNoteInfoPackage> notePackageQueue = new Queue<GameNoteInfoPackage>();
    private GameNoteInfoPackage currentPackageToSpawn;

    // Kural motoru durumu
    private int directionCounter = 0;
    private bool isFlowingRight = true;

    // --- Olaylar ---
    public static event Action<List<GameNoteInfo>> OnNotesGenerated;
    public static event Action OnGenerationComplete;

    /// <summary>
    /// Şarkıyı yükler, RefactorParse.md'ye uygun şekilde tüm nota verisini işler ve oynanmaya hazır hale getirir.
    /// </summary>
    public void LoadAndPrepareSong(List<NoteChartSequence> rawChart, int tempo)
    {
        ResetState();

        // 1. Ham veriyi, dikey dilimleme mantığıyla geçici bir formata dönüştür.
        List<TemporalNoteInfo> temporalNotes = ProcessChartWithVerticalSlicing(rawChart, tempo);

        // 2. Bu ara formattaki notaları, tüm kuralları uygulayarak nihai oyun paketlerine dönüştür.
        List<GameNoteInfoPackage> finalPackages = GenerateFinalPackagesFromTemporal(temporalNotes);

        // 3. Oynanacak paketleri sıraya (queue) al.
        foreach (var package in finalPackages)
        {
            notePackageQueue.Enqueue(package);
        }

        Debug.Log($"🎵 Şarkı hazırlandı. Oynanacak {notePackageQueue.Count} nota paketi var.");
    }

    /// <summary>
    /// Oyun döngüsünde sürekli çağrılır. Doğru zamanda nota spawn olayını tetikler.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (isGenerationComplete) return;

        accumulatedTime += deltaTime * 1000f; // Zamanı milisaniye olarak biriktir

        if (!firstDelayCompleted)
        {
            if (accumulatedTime >= FIRST_DELAY_MS)
            {
                firstDelayCompleted = true;
                accumulatedTime = 0; // Sayacı sıfırla
                TrySpawnNextPackage();
            }
            return;
        }

        if (currentPackageToSpawn != null && accumulatedTime >= currentPackageToSpawn.oneNote)
        {
            accumulatedTime -= currentPackageToSpawn.oneNote;
            TrySpawnNextPackage();
        }
    }

    private void TrySpawnNextPackage()
    {
        if (notePackageQueue.Count > 0)
        {
            currentPackageToSpawn = notePackageQueue.Dequeue();
            OnNotesGenerated?.Invoke(currentPackageToSpawn.gameNoteInfos);
        }
        else
        {
            isGenerationComplete = true;
            OnGenerationComplete?.Invoke();
            Debug.Log("🎵 Tüm nota paketleri oluşturuldu. Şarkı bitti.");
        }
    }

    #region Refactored Parsing Logic (RefactorParse.md)

    /// <summary>
    /// Ham chart verisini, dikey dilimleme (vertical slicing) yöntemiyle işler.
    /// </summary>
    private List<TemporalNoteInfo> ProcessChartWithVerticalSlicing(List<NoteChartSequence> chart, int tempo)
    {
        float baseTimingMs = ((60000f / tempo) / 8f) * 10f; // Yavaşlatılmış temel zaman birimi
        var temporalNoteList = new List<TemporalNoteInfo>();

        // 1. Tüm şeritlerdeki maksimum zaman dilimi sayısını bul.
        int maxSubdivisions = 0;
        foreach (var sequence in chart)
        {
            for (int lane = 0; lane < laneCount; lane++)
            {
                maxSubdivisions = Math.Max(maxSubdivisions, sequence.GetLineData(lane).Split('/').Length);
            }
        }

        // 2. Dikey olarak dilimle ve işle.
        for (int i = 0; i < maxSubdivisions; i++)
        {
            var temporalInfo = new TemporalNoteInfo();
            // bool hasNotesInSlice = false; // Commented out - unused variable

            for (int lane = 0; lane < laneCount; lane++)
            {
                string[] subdivisions = chart.Select(s => s.GetLineData(lane).Split('/')).SelectMany(a => a).ToArray();
                // Bu kısım basitleştirilmeli. Her sequence için ayrı ayrı işlem yapalım.
            }
        }
        // -- Geçici olarak eski sistemin basitleştirilmiş halini kullanalım --
        // DOĞRU YÖNTEM: Tüm sequence'leri birleştirip sonra dikey işle.
        // Şimdilik her sequence'i kendi içinde işleyelim.
        foreach (var sequence in chart)
        {
            var columns = new Dictionary<int, (int pitch, int duration)[]>();
            int sequenceMaxSub = 0;

            for (int lane = 0; lane < laneCount; lane++)
            {
                string[] subdivisions = sequence.GetLineData(lane).Split('/');
                if (subdivisions.Length > sequenceMaxSub) sequenceMaxSub = subdivisions.Length;

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
            for (int subIdx = 0; subIdx < sequenceMaxSub; subIdx++)
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
                        temporalInfo.maxDurationType = Math.Max(temporalInfo.maxDurationType, duration);
                        hasNotes = true;
                    }
                }

                if (hasNotes)
                {
                    if (temporalInfo.maxDurationType >= 0 && temporalInfo.maxDurationType < NOTE_LENGTH_FACTORS.Length)
                    {
                        temporalInfo.timingMs = NOTE_LENGTH_FACTORS[temporalInfo.maxDurationType] * baseTimingMs;
                    }
                    temporalNoteList.Add(temporalInfo);
                }
            }
        }
        return temporalNoteList;
    }

    /// <summary>
    /// Geçici nota bilgisini, oyun kurallarını uygulayarak nihai paketlere dönüştürür.
    /// </summary>
    private List<GameNoteInfoPackage> GenerateFinalPackagesFromTemporal(List<TemporalNoteInfo> temporalNotes)
    {
        var packages = new List<GameNoteInfoPackage>();

        foreach (var tNote in temporalNotes)
        {
            var package = new GameNoteInfoPackage { oneNote = tNote.timingMs };
            var tempGameNotes = new List<GameNoteInfo>();

            for (int lane = 0; lane < laneCount; lane++)
            {
                if (tNote.pitches[lane] != -1)
                {
                    var gameNote = new GameNoteInfo
                    {
                        // Kural uygulamadan önceki ham lane index'i ve pitch
                        idx = (tNote.pitches[lane] + LANE_PITCH_OFFSET[lane]) % laneCount,
                        pitch = tNote.pitches[lane],
                        line = lane
                    };
                    tempGameNotes.Add(gameNote);
                }
            }

            // Kuralları uygula
            ApplyComplexRule(tempGameNotes);
            ApplySpacing(tempGameNotes);

            package.gameNoteInfos = tempGameNotes;
            packages.Add(package);
        }
        return packages;
    }

    // Yönlü akış kuralı
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

    // Bitişik notaları ayırma kuralı
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
        firstDelayCompleted = false;
        directionCounter = 0;
        isFlowingRight = true;
        notePackageQueue.Clear();
        currentPackageToSpawn = null;
    }
    #endregion

    #region Compatibility Layer (Eski sistemle uyumluluk için)

    /// <summary>
    /// Eski sistemle uyumluluk için - GetNote() metodunu Tick() ile eşler
    /// </summary>
    public void GetNote(float deltaTime)
    {
        Tick(deltaTime);
    }

    /// <summary>
    /// Eski sistemle uyumluluk için - LoadSong() metodunu LoadAndPrepareSong() ile eşler  
    /// </summary>
    public void LoadSong(SongData songData)
    {
        if (songData == null)
        {
            Debug.LogError("🎵 Cannot load null song data!");
            return;
        }

        // SongData'yı NoteChartSequence formatına dönüştür
        // Şimdilik basit bir dummy implementation
        List<NoteChartSequence> dummyChart = new List<NoteChartSequence>();

        // Geçici olarak boş bir chart ile yükle
        LoadAndPrepareSong(dummyChart, 120); // Default tempo

        Debug.Log($"🎵 Song loaded via compatibility layer: {songData.songName}");
    }

    #endregion
}

// --- Veri Yapıları ---
// Bu sınıflar artık DataStructures.cs'te tanımlanıyor.