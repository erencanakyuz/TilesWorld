using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text;

/// <summary>
/// GameNoteCreator - Oyunun Kalbi (Refaktör Edilmiş)
/// Orijinal Java oyununun dikey zaman dilimlemesi ve dinamik nota oluşturma mantığını uygular.
/// </summary>
public class GameNoteCreator : MonoBehaviour
{
    [Header("🎵 Konfigürasyon")]
    // PERFORMANCE: This is now calculated dynamically based on tempo.
    // [SerializeField] private float timingMultiplier = 4.0f;
    private float timingMultiplier;
    [SerializeField] private int laneCount = 6;
    [SerializeField] private int maxDirectionInterval = 10;

    [Header("🔧 Debugging")]
    [SerializeField] private bool showDebugLogs = false;

    [Header("🕒 Spawn Control (Testing)")]
    [Tooltip("If false, GameNoteCreator will NOT dequeue packages automatically. Use external scripts to pull them.")]
    public bool autoSpawnEnabled = true;

    // --- Orijinal Java'dan Port Edilen Sabitler (Unity için ayarlanmış) ---
    // ORIJINAL: {1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56, ...}
    // ARTIK ORİJİNAL DEĞERLERİ KULLANIYORUZ VE ÇARPANLA AYARLIYORUZ
    private static readonly int[] NOTE_LENGTH_FACTORS = {
        1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56
    };

    private static readonly int[] LANE_PITCH_OFFSET = { 3, 5, 7, 11, 13, 17 };
    private float firstDelayMs = 1500f; // Başlangıç gecikmesi (daha sonra NoteRenderer'dan gelen travel time ile ayarlanacak)

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
    public static event Action<List<GameNoteInfo>, double> OnNotesGenerated;
    public static event Action OnGenerationComplete;

    /// <summary>
    /// Sets the initial delay before the first note package is spawned.
    /// Should be set based on the note travel time from NoteRenderer.
    /// </summary>
    /// <param name="delayMs">The delay in milliseconds.</param>
    public void SetFirstDelay(float delayMs)
    {
        this.firstDelayMs = delayMs;
    }

    /// <summary>
    /// JSON'dan nota chart verilerini yükler
    /// </summary>
    private List<NoteChartSequence> LoadNoteChartFromJSON(string songKey)
    {
        try
        {
            string jsonPath = $"Song_Note_Jsons/Individual/{songKey}";
            TextAsset jsonFile = Resources.Load<TextAsset>(jsonPath);

            if (jsonFile == null)
            {
                Debug.LogError($"🎵 JSON nota dosyası bulunamadı: {jsonPath}");
                return new List<NoteChartSequence>();
            }

            JsonSequenceArray jsonData = JsonUtility.FromJson<JsonSequenceArray>(jsonFile.text);

            if (jsonData?.sequences == null)
            {
                Debug.LogError($"🎵 JSON formatı geçersiz: {jsonPath}");
                return new List<NoteChartSequence>();
            }

            List<NoteChartSequence> chartSequences = new List<NoteChartSequence>();

            foreach (var jsonSeq in jsonData.sequences)
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
#if UNITY_EDITOR
            if (showDebugLogs) Debug.Log($"🎵 JSON nota verileri yüklendi: {chartSequences.Count} sekans, dosya: {jsonPath}");
#endif
            return chartSequences;
        }
        catch (Exception e)
        {
            Debug.LogError($"🎵 JSON yükleme hatası: {e.Message}");
            return new List<NoteChartSequence>();
        }
    }

    /// <summary>
    /// Şarkıyı yükler, RefactorParse.md'ye uygun şekilde tüm nota verisini işler ve oynanmaya hazır hale getirir.
    /// </summary>
    public void LoadAndPrepareSong(List<NoteChartSequence> rawChart, int tempo)
    {
        ResetState();
#if UNITY_EDITOR
        if (showDebugLogs) Debug.Log($"🎵 Şarkı işleniyor: {rawChart.Count} sekans, tempo: {tempo} BPM");

        // DEBUG: Note factors'ı logla
        LogNoteFactorInfo();
#endif
        // 1. Ham veriyi, dikey dilimleme mantığıyla geçici bir formata dönüştür.
        List<TemporalNoteInfo> temporalNotes = ProcessChartWithVerticalSlicing(rawChart, tempo);

        // 2. Bu ara formattaki notaları, tüm kuralları uygulayarak nihai oyun paketlerine dönüştür.
        List<GameNoteInfoPackage> finalPackages = GenerateFinalPackagesFromTemporal(temporalNotes);

        // 3. Oynanacak paketleri sıraya (queue) al.
        foreach (var package in finalPackages)
        {
            notePackageQueue.Enqueue(package);
        }
#if UNITY_EDITOR
        if (showDebugLogs) Debug.Log($"🎵 Şarkı hazırlandı. Oynanacak {notePackageQueue.Count} nota paketi var.");
#endif
    }

    /// <summary>
    /// Oyun döngüsünde sürekli çağrılır. Doğru zamanda nota spawn olayını tetikler.
    /// </summary>
    public void Tick(float deltaTime, double dspTime)
    {
        if (!autoSpawnEnabled || isGenerationComplete) return;

        accumulatedTime += deltaTime * 1000f; // Zamanı milisaniye olarak biriktir

        if (!firstDelayCompleted)
        {
            if (accumulatedTime >= firstDelayMs)
            {
                firstDelayCompleted = true;
                accumulatedTime = 0; // Sayacı sıfırla
#if UNITY_EDITOR
                if (showDebugLogs) Debug.Log($"🎵 FIRST_DELAY tamamlandı ({firstDelayMs}ms). İlk nota spawn ediliyor...");
#endif
                TrySpawnNextPackage(dspTime);
            }
            return;
        }

        if (currentPackageToSpawn != null && accumulatedTime >= currentPackageToSpawn.oneNote)
        {
            accumulatedTime -= currentPackageToSpawn.oneNote;
            TrySpawnNextPackage(dspTime);
        }
    }

    private void TrySpawnNextPackage(double dspTime)
    {
        if (notePackageQueue.Count > 0)
        {
            currentPackageToSpawn = notePackageQueue.Dequeue();

#if UNITY_EDITOR
            // DEBUG: Spawn timing bilgisi
            if (showDebugLogs) Debug.Log($"🎵 SPAWN: {currentPackageToSpawn.gameNoteInfos.Count} nota, nextTiming: {currentPackageToSpawn.oneNote:F1}ms, queueLeft: {notePackageQueue.Count}");
#endif

            OnNotesGenerated?.Invoke(currentPackageToSpawn.gameNoteInfos, dspTime);
        }
        else
        {
            isGenerationComplete = true;
            OnGenerationComplete?.Invoke();
#if UNITY_EDITOR
            if (showDebugLogs) Debug.Log("🎵 Tüm nota paketleri oluşturuldu. Şarkı bitti.");
#endif
        }
    }

    #region Refactored Parsing Logic (RefactorParse.md)

    /// <summary>
    /// RefactorParse.md'ye göre: Ham chart verisini, dikey dilimleme (vertical slicing) yöntemiyle işler.
    /// Orijinal Java oyununun PlayData.getTabList mantığını uygular.
    /// </summary>
    private List<TemporalNoteInfo> ProcessChartWithVerticalSlicing(List<NoteChartSequence> chart, int tempo)
    {
        // DYNAMIC TIMING MULTIPLIER (from performance.md)
        // This value is calculated to maintain the "feel" of the original hardcoded value of 4.0 at 100 BPM.
        // The magic number 400f can be adjusted later to change the overall note density.
        // Formula: 4.0 (original multiplier) * 100 (reference BPM) = 400.
        this.timingMultiplier = 400f / tempo;

#if UNITY_EDITOR
        if (showDebugLogs) Debug.Log($"🎵 Dikey zaman dilimlemesi başlıyor: {chart.Count} sekans, tempo: {tempo}, Dinamik Çarpan: {this.timingMultiplier:F2}");
#endif

        // A. Temel Zaman Biriminin Hesaplanması (RefactorParse.md - Adım 2.2.A)
        float baseTimingMs = (60000f / tempo) / 8f; // 32'lik nota süresi (ms)
#if UNITY_EDITOR
        if (showDebugLogs) Debug.Log($"🎵 Temel zaman birimi: {baseTimingMs:F2} ms (32'lik nota)");
#endif
        var temporalNoteList = new List<TemporalNoteInfo>();

        // B. Tüm Veriyi Topla ve Maksimum Dilim Sayısını Bul (RefactorParse.md - Adım 3.2.1-2)
        var allLineData = new Dictionary<int, string[]>(); // lane -> all subdivisions
        int maxSubdivisions = 0;

        // Tüm sekansları birleştir ve her lane için subdivision array'i oluştur
        for (int lane = 0; lane < laneCount; lane++)
        {
            var allSubdivisions = new List<string>();

            foreach (var sequence in chart)
            {
                string lineData = sequence.GetLineData(lane);
                if (!string.IsNullOrEmpty(lineData))
                {
                    string[] subdivisions = lineData.Split('/');
                    allSubdivisions.AddRange(subdivisions);
                }
            }

            allLineData[lane] = allSubdivisions.ToArray();
            maxSubdivisions = Math.Max(maxSubdivisions, allSubdivisions.Count);
        }
#if UNITY_EDITOR
        if (showDebugLogs) Debug.Log($"🎵 Maksimum zaman dilimi sayısı: {maxSubdivisions}");
#endif

        // C. Dikey Dilimleme ve Haritala (RefactorParse.md - Adım 3.2.3-4)
        var sliceMap = new Dictionary<int, List<(int lane, int pitch, int duration)>>();

        // Her zaman dilimi için tüm lane'leri dikey olarak tara
        for (int sliceIndex = 0; sliceIndex < maxSubdivisions; sliceIndex++)
        {
            sliceMap[sliceIndex] = new List<(int lane, int pitch, int duration)>();

            for (int lane = 0; lane < laneCount; lane++)
            {
                // Bu zaman diliminde bu lane'de ne var?
                if (sliceIndex < allLineData[lane].Length)
                {
                    string subdivision = allLineData[lane][sliceIndex];

                    if (!string.IsNullOrEmpty(subdivision) && subdivision != "_,_")
                    {
                        string[] parts = subdivision.Split(',');
                        if (parts.Length == 2)
                        {
                            if (int.TryParse(parts[0], out int pitch) &&
                                int.TryParse(parts[1], out int duration) &&
                                pitch > 0) // "_" = -1 veya parse hatası, geçerli nota değil
                            {
                                sliceMap[sliceIndex].Add((lane, pitch, duration));
                            }
                        }
                    }
                }
            }
        }

        // D. Anlamlı Paketlere Dönüştür ve oneTempo Hesapla (RefactorParse.md - Adım 3.2.5-6)
        for (int sliceIndex = 0; sliceIndex < maxSubdivisions; sliceIndex++)
        {
            if (!sliceMap.ContainsKey(sliceIndex) || sliceMap[sliceIndex].Count == 0)
                continue; // Bu zaman diliminde nota yok, atla

            var temporalInfo = new TemporalNoteInfo();
            int maxDuration = 0;

            // Bu zaman dilimindeki tüm notaları işle
            foreach (var (lane, pitch, duration) in sliceMap[sliceIndex])
            {
                temporalInfo.pitches[lane] = pitch;
                temporalInfo.originalLines[lane] = lane;
                maxDuration = Math.Max(maxDuration, duration);
            }

            // oneTempo Hesaplama (RefactorParse.md - Adım 2.2.C)
            temporalInfo.maxDurationType = maxDuration;

            if (maxDuration >= 0 && maxDuration < NOTE_LENGTH_FACTORS.Length)
            {
                // YENİ SİSTEM: Orijinal faktörü, ayarlanabilir çarpanımızla çarpıyoruz.
                float calculatedTiming = (float)(NOTE_LENGTH_FACTORS[maxDuration] * timingMultiplier * baseTimingMs);
                temporalInfo.timingMs = calculatedTiming;
#if UNITY_EDITOR
                // DEBUG: Timing hesaplama detayları (sadece ilk 5 paket için)
                if (sliceIndex < 5)
                {
                    if (showDebugLogs) Debug.Log($"🎵 TIMING CAL [{sliceIndex}]: maxDuration={maxDuration}, factor={NOTE_LENGTH_FACTORS[maxDuration]}, multiplier={timingMultiplier}, baseMs={baseTimingMs:F1}, result={calculatedTiming:F1}ms");
                }
#endif
            }
            else
            {
                temporalInfo.timingMs = baseTimingMs; // Fallback
#if UNITY_EDITOR
                if (sliceIndex < 5)
                {
                    if (showDebugLogs) Debug.Log($"🎵 TIMING FALLBACK [{sliceIndex}]: maxDuration={maxDuration} out of range, using baseMs={baseTimingMs:F1}ms");
                }
#endif
            }

            temporalNoteList.Add(temporalInfo);
#if UNITY_EDITOR
            // Debug için - çok fazla spam önlemek için sadece ilk 10'unu göster
            if (sliceIndex < 10)
            {
                var activeNotes = temporalInfo.pitches.Select((pitch, idx) => pitch != -1 ? $"L{idx}:P{pitch}" : null)
                                                       .Where(x => x != null).ToArray();
                if (showDebugLogs) Debug.Log($"🎵 Zaman dilimi {sliceIndex}: {string.Join(", ", activeNotes)}, maxDuration={maxDuration}, timingMs={temporalInfo.timingMs:F1}");
            }
#endif
        }
#if UNITY_EDITOR
        if (showDebugLogs) Debug.Log($"🎵 Dikey dilimleme tamamlandı: {temporalNoteList.Count} temporal note package oluşturuldu");
#endif
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
                        line = tNote.originalLines[lane]
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
            if (Math.Abs(notes[i].idx - notes[i + 1].idx) <= 1)
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

    // DEBUG: Timing faktörlerini kontrol etmek için
    private void LogNoteFactorInfo()
    {
        if (NOTE_LENGTH_FACTORS == null || NOTE_LENGTH_FACTORS.Length == 0) return;
#if UNITY_EDITOR
        if (showDebugLogs) Debug.Log("🎵 NOTE_LENGTH_FACTORS dizisi:");
        for (int i = 0; i < Math.Min(10, NOTE_LENGTH_FACTORS.Length); i++)
        {
            if (showDebugLogs) Debug.Log($"  Factor[{i}] = {NOTE_LENGTH_FACTORS[i]}");
        }
#endif
    }
    #endregion

    #region Compatibility Layer (Eski sistemle uyumluluk için)

    /// <summary>
    /// Eski sistemle uyumluluk için - GetNote() metodunu Tick() ile eşler
    /// </summary>
    public void GetNote(float deltaTime, double dspTime)
    {
        Tick(deltaTime, dspTime);
    }

    /// <summary>
    /// Eski sistemle uyumluluk için - LoadSong() metodunu LoadAndPrepareSong() ile eşler  
    /// Şimdi gerçek JSON nota verilerini yükler!
    /// </summary>
    public void LoadSong(SongData songData)
    {
        if (songData == null)
        {
            Debug.LogError("🎵 Cannot load null song data!");
            return;
        }
#if UNITY_EDITOR
        if (showDebugLogs) Debug.Log($"🎵 Loading song: {songData.songName} with key: {songData.songKey}");
#endif
        // Gerçek JSON nota verilerini yükle
        List<NoteChartSequence> chartData = LoadNoteChartFromJSON(songData.songKey);

        if (chartData.Count == 0)
        {
            Debug.LogError($"🎵 No note data loaded for song: {songData.songKey}");
            // Boş liste ile devam et, error handling
            chartData = new List<NoteChartSequence>();
        }

        // Gerçek tempo ile yükle
        int tempo = songData.bpm > 0 ? (int)songData.bpm : 120; // Fallback tempo
        LoadAndPrepareSong(chartData, tempo);
#if UNITY_EDITOR
        if (showDebugLogs) Debug.Log($"🎵 Song loaded successfully: {songData.songName} (BPM: {tempo})");
#endif
    }

    #endregion

    // ========================
    // Testing Helper Methods
    // ========================
    /// <summary>
    /// Returns the number of prepared note packages currently waiting in the internal queue.
    /// </summary>
    public int GetQueueCount()
    {
        return notePackageQueue.Count;
    }

    /// <summary>
    /// Dequeues and returns the next <see cref="GameNoteInfoPackage"/> from the internal queue.
    /// Intended for use by isolated playback tests such as <c>SongPlaybackTester</c>.
    /// Returns <c>null</c> when the queue is empty.
    /// </summary>
    public GameNoteInfoPackage GetNextTestPackage()
    {
        if (notePackageQueue.Count > 0)
        {
            return notePackageQueue.Dequeue();
        }
        return null;
    }
}

// --- Veri Yapıları ---
// Bu sınıflar artık DataStructures.cs'te tanımlanıyor.

