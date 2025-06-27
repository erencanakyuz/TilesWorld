using UnityEngine;
using System.Threading.Tasks;

// FMOD Integration için - Main.md Karar #2
// Hedef: <15ms audio latency
public class FMODAudioManager : MonoBehaviour
{
    [Header("FMOD Settings")]
    public int targetLatencyMs = 10;
    public int maxSimultaneousNotes = 20;

    [Header("Instrument Audio Events")]
    // FMOD Studio Event referansları buraya gelecek
    public string[] pianoNoteEvents;
    public string[] harpNoteEvents;
    public string[] guitarNoteEvents;

    [Header("Performance Monitoring")]
    public float currentLatency = 0f;
    public int activeVoices = 0;

    private static FMODAudioManager instance;
    public static FMODAudioManager Instance => instance;

    // Original Java Sounds.java mapping - OLD_GAME_TO_NEW_GAME_CODE.md
    private static readonly int[][] SOUND_RESOURCE_IDXS = {
        new int[] { 24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44 },
        new int[] { 19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39 },
        new int[] { 15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35 }
    };

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFMOD();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeFMOD()
    {
        Debug.Log("🎵 Initializing FMOD for low-latency audio...");

        // FMOD System ayarları - düşük gecikme için
        // Bu implementasyon FMOD import'undan sonra tamamlanacak

        Debug.Log($"🎯 Target latency: {targetLatencyMs}ms");
        Debug.Log("📋 FMOD configuration pending - import FMOD first");
    }

    // Original Java Sounds.playSound() karşılığı
    public async Task PlaySoundAsync(int line, int pitch, float velocity = 1.0f)
    {
        if (line >= 0 && line < SOUND_RESOURCE_IDXS.Length &&
            pitch >= 0 && pitch < SOUND_RESOURCE_IDXS[line].Length)
        {
            int noteIndex = SOUND_RESOURCE_IDXS[line][pitch];
            await PlayNoteAsync(InstrumentType.Piano, noteIndex, velocity);
        }
    }

    public async Task PlayNoteAsync(InstrumentType instrument, int noteIndex, float velocity)
    {
        // FMOD event triggering - ultra low latency
        // Implementation after FMOD import

        Debug.Log($"🎵 Playing {instrument} note {noteIndex} at velocity {velocity}");

        // Placeholder - gerçek FMOD kodu gelecek
        await Task.Delay(1); // Simulated minimal latency
    }

    public void UpdatePerformanceStats()
    {
        // FMOD performance monitoring
        // Real-time latency measurement
    }

    // Original GameNoteCreator'ın kullanabileceği static method
    public static int FindSoundIdx(int line, int pitch)
    {
        if (line >= 0 && line < SOUND_RESOURCE_IDXS.Length &&
            pitch >= 0 && pitch < SOUND_RESOURCE_IDXS[line].Length)
        {
            return SOUND_RESOURCE_IDXS[line][pitch];
        }
        return 0;
    }
}

public enum InstrumentType
{
    Piano,
    Harp,
    Guitar
}