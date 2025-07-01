using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Linq;

#pragma warning disable 0414 // Field is assigned but its value is never used (editor-only test tweaks)

/// <summary>
/// 'L' tuşuna basıldığında Turkish Delight şarkısını çalarak ses sistemini test eder.
/// Bu scripti sahnedeki bir GameObject'e ekleyip public alanları Inspector'dan atamalısın.
/// </summary>
public class SongPlaybackTester : MonoBehaviour
{
    [Header("Test Ayarları")]
    [Tooltip("Test için kullanılacak şarkının veritabanındaki adı.")]
    [SerializeField] private string testSongTitle = "Turkish Delight";
    [Tooltip("Testte kullanılacak enstrüman.")]
    [SerializeField] private InstrumentType testInstrument = InstrumentType.Piano;

    [Header("Tempo Ayarı")]
    [Tooltip(">1 = daha hızlı çalma, <1 = yavaşlatır. 2 = iki kat hız.")]
    [SerializeField] private float speedMultiplier = 2f;

#if UNITY_EDITOR
    [Header("Pitch Mapping Ayarları (Editor-Only)")]
    [Tooltip("Orijinal Java mapping'ini kullan (lane+pitch → sound index). Kapalıysa pitch'i direkt kullanır.")]
    [SerializeField] private bool useJavaMapping = true;

    [Tooltip("Custom mapping aktif olsun mu? Aktifse offset / factor uygulanır.")]
    [SerializeField] private bool enableCustomMapping = false;

    [Tooltip("Sound index'e eklenecek/çıkarılacak offset. Örnek: 3 → +3 yarım ses, -2 → -2 yarım ses.")]
    [SerializeField] private int pitchOffset = 0;

    [Tooltip("Sound index'i çarpan değer. 1 = değişmez, 2 = iki kat, 0.5 = yarı.")]
    [SerializeField] private float pitchFactor = 1f;
#else
    private bool useJavaMapping = true; // Must exist in builds
    private bool enableCustomMapping = false;
    private int pitchOffset = 0;
    private float pitchFactor = 1f;
#endif

#pragma warning restore 0414

    [Header("Gerekli Komponentler")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private GameNoteCreator gameNoteCreator;
    [SerializeField] private SongDatabase songDatabase;

    private bool isTestRunning = false;
    private Coroutine testCoroutine;

    // Mobile 3-finger touch detection
    private float threeFingerHoldTime = 0f;
    private const float requiredHoldDuration = 1f;

    void Update()
    {
        // Handle keyboard input (existing L key functionality)
        HandleKeyboardInput();

        // Handle mobile 3-finger touch input
        HandleMobileInput();
    }

    void HandleKeyboardInput()
    {
        // 'L' tuşuna her basıldığında testi başlat veya yeniden başlat
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            StartTest();
        }

        // Additional keyboard shortcuts for different songs
        if (Keyboard.current != null)
        {
            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                testSongTitle = "Cannon";
                StartTest();
            }
            else if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                testSongTitle = "Fur Elise";
                StartTest();
            }
            else if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                testSongTitle = "Moon Light";
                StartTest();
            }
        }
    }

    void HandleMobileInput()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (UnityEngine.InputSystem.Touchscreen.current != null)
        {
            var touchscreen = UnityEngine.InputSystem.Touchscreen.current;

            // Count active touches
            int activeTouchCount = 0;
            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                if (touchscreen.touches[i].isInProgress)
                {
                    activeTouchCount++;
                }
            }

            // Check for 3-finger hold
            if (activeTouchCount >= 3)
            {
                threeFingerHoldTime += Time.deltaTime;

                if (threeFingerHoldTime >= requiredHoldDuration && !isTestRunning)
                {
                    Debug.Log("📱 3-finger touch detected! Starting mobile test...");
                    StartTest();
                    threeFingerHoldTime = 0f; // Reset to prevent multiple triggers
                }
            }
            else
            {
                threeFingerHoldTime = 0f; // Reset if not holding 3 fingers
            }
        }
#endif
    }

    void StartTest()
    {
        if (isTestRunning)
        {
            // Mevcut testi iptal et
            if (testCoroutine != null)
            {
                StopCoroutine(testCoroutine);
            }

            // Aktif sesleri durdurmak istiyorsanız AudioManager'da global bir StopAll metodu ekleyebilirsiniz
        }

        // Yeniden başlat
        testCoroutine = StartCoroutine(PlaySongTest());
    }

    private IEnumerator PlaySongTest()
    {
        isTestRunning = true;
        Debug.Log($"--- BAŞLATILIYOR: '{testSongTitle}' çalma testi ---");

        // 1. Auto-find components if not assigned
        if (audioManager == null) audioManager = AudioManager.Instance;
        if (gameNoteCreator == null) gameNoteCreator = FindFirstObjectByType<GameNoteCreator>();
        if (songDatabase == null) songDatabase = SongDatabase.Instance;

        if (audioManager == null || gameNoteCreator == null || songDatabase == null)
        {
            Debug.LogError("Test başarısız: Gerekli komponentler bulunamadı. AudioManager, GameNoteCreator, SongDatabase singleton'ları kontrol edin.");
            isTestRunning = false;
            yield break;
        }

        // 2. Şarkı verisini veritabanından al
        SongDatabaseInfo songToTest = songDatabase.GetSongByTitle(testSongTitle);
        if (songToTest == null)
        {
            Debug.LogError($"Test başarısız: '{testSongTitle}' adlı şarkı veritabanında bulunamadı.");
            isTestRunning = false;
            yield break;
        }

        // 3. SongDatabaseInfo -> SongData dönüştür ve GameNoteCreator'a yükle
        SongData tempSongData = ScriptableObject.CreateInstance<SongData>();
        tempSongData.songName = songToTest.title;
        tempSongData.artist = songToTest.artist;
        tempSongData.bpm = songToTest.tempo;
        tempSongData.duration = EstimateDuration(songToTest.tempo); // Kabaca tahmin
        tempSongData.audioFilePath = $"Music/{songToTest.songKey}";
        tempSongData.noteChartPath = $"Song_Note_Jsons/Individual/{songToTest.songKey}";
        tempSongData.songKey = songToTest.songKey;

        // GameNoteCreator'ın otomatik spawn özelliğini kapat
        gameNoteCreator.autoSpawnEnabled = false;

        // Song'u yükle ve paketleri oluştur
        gameNoteCreator.LoadSong(tempSongData);

        // Paketlerin hazırlanmasını bekle
        yield return new WaitUntil(() => gameNoteCreator.GetQueueCount() > 0);
        Debug.Log("Şarkı verisi GameNoteCreator tarafından işlendi. Çalmaya hazırlanılıyor...");

        // Oyuncunun hazırlanması için kısa bir başlangıç gecikmesi
        yield return new WaitForSeconds(1.5f);
        Debug.Log("Çalmaya Başla!");

        // 4. Hazırlanmış nota paketlerini sırayla çal
        int pkgCounter = 0;
        while (true)
        {
            GameNoteInfoPackage package = gameNoteCreator.GetNextTestPackage();

            // Çalınacak paket kalmadıysa testi bitir
            if (package == null)
            {
                Debug.Log("--- BİTTİ: Şarkı çalma testi tamamlandı. ---");
                break;
            }

            // Debug paketi özetle
            string noteSummary = string.Join(", ", package.gameNoteInfos.Select(n => $"L{n.line}:P{n.pitch}"));
            Debug.Log($"PKG {pkgCounter}: {noteSummary} → wait {package.oneNote / speedMultiplier:F1}ms");

            foreach (var note in package.gameNoteInfos)
            {
                int finalPitch;
                int maxIndex = audioManager.GetInstrumentClipCount(testInstrument) - 1;

                if (useJavaMapping)
                {
                    // Use the new, centralized function for consistent sound.
                    finalPitch = AudioConstants.GetFinalSoundIndex(testInstrument, note.line, note.pitch, maxIndex);
                }
                else
                {
                    // If not using Java mapping, just use the raw pitch, but still clamp it.
                    finalPitch = Mathf.Clamp(note.pitch, 0, maxIndex);
                }

#if UNITY_EDITOR
                if (enableCustomMapping)
                {
                    // Apply test-specific custom mapping
                    finalPitch = Mathf.RoundToInt(finalPitch * pitchFactor) + pitchOffset;
                }
#endif

                // Detaylı log (opsiyonel)
                Debug.Log($"🎵 TEST PLAY: Lane={note.line}, BasePitch={note.pitch} → FinalIdx={finalPitch}");

                // useJavaMapping should be false here because we have already calculated the final index.
                audioManager.PlayNote(testInstrument, finalPitch, 1.0f, false, note.line);
            }

            pkgCounter++;

            // Bir sonraki pakete geçmeden önce zamanlaması kadar bekle
            float waitTime = (package.oneNote / speedMultiplier) / 1000f;
            yield return new WaitForSeconds(waitTime);
        }

        isTestRunning = false;
    }

    /// <summary>
    /// Tempo değerinden kabaca şarkı süresi tahmini yapar (GameplayManager'daki ile aynı mantık).
    /// </summary>
    private float EstimateDuration(int tempo)
    {
        if (tempo < 60) return 240f;
        if (tempo < 80) return 210f;
        if (tempo < 120) return 180f;
        if (tempo < 140) return 150f;
        return 120f;
    }
}