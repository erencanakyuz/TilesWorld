using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System.Reflection;

#pragma warning disable 0414 // Field is assigned but its value is never used (editor-only test tweaks)

/// <summary>
/// 'L' tuşuna basıldığında Turkish Delight şarkısını çalarak ses sistemini test eder.
/// Bu scripti sahnedeki bir GameObject'e ekleyip public alanları Inspector'dan atamalısın.
/// YENI: Perfect Auto-Play toggle butonu - ekranın sol ortasında, tüm notaları perfect zamanda otomatik çalar
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

    [Header("🎯 Perfect Auto-Play")]
    [Tooltip("Perfect Auto-Play mode aktif olsun mu?")]
    [SerializeField] private bool isPerfectAutoPlayEnabled = false;
    [Tooltip("Auto-play butonunun konumu (ekran koordinatları)")]
    [SerializeField] private Vector2 buttonPosition = new Vector2(100, Screen.height / 2);
    [Tooltip("Auto-play butonunun boyutu")]
    [SerializeField] private Vector2 buttonSize = new Vector2(120, 60);

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
    [SerializeField] private HitZoneManager hitZoneManager;

    // Perfect Auto-Play UI
    private Button autoPlayButton;
    private Canvas autoPlayCanvas;
    private bool isAutoPlayUISetup = false;

    private bool isTestRunning = false;
    private Coroutine testCoroutine;
    private Coroutine autoPlayCoroutine;

    // Mobile 3-finger touch detection
    private float threeFingerHoldTime = 0f;
    private const float requiredHoldDuration = 1f;

    // Auto-play için hitzone izleme
    private HitZoneTrigger[] hitZones;

    void Start()
    {
        SetupAutoPlayUI();
        SetupHitZones();
    }

    void Update()
    {
        // Handle keyboard input (existing L key functionality)
        HandleKeyboardInput();

        // Handle mobile 3-finger touch input
        HandleMobileInput();

        // Handle Perfect Auto-Play if enabled
        if (isPerfectAutoPlayEnabled)
        {
            HandlePerfectAutoPlay();
        }
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

    void SetupAutoPlayUI()
    {
        if (isAutoPlayUISetup) return;

        // Canvas oluştur
        GameObject canvasObj = new GameObject("AutoPlayCanvas");
        autoPlayCanvas = canvasObj.AddComponent<Canvas>();
        autoPlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        autoPlayCanvas.sortingOrder = 1000; // En üstte görünmesi için

        // CanvasScaler ekle
        CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        // GraphicRaycaster ekle
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem varsa kullan, yoksa oluştur
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Button oluştur
        GameObject buttonObj = new GameObject("PerfectAutoPlayButton");
        buttonObj.transform.SetParent(autoPlayCanvas.transform, false);

        // RectTransform ayarla - Sol orta
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(0, 0.5f);
        rectTransform.pivot = new Vector2(0, 0.5f);
        rectTransform.anchoredPosition = new Vector2(10, 0); // Sol kenardan 10 pixel
        rectTransform.sizeDelta = buttonSize;

        // Button component
        autoPlayButton = buttonObj.AddComponent<Button>();

        // Background image (opsiyonel - basit renkli arkaplan)
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = isPerfectAutoPlayEnabled ? Color.green : Color.red;

        // Text ekle
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = isPerfectAutoPlayEnabled ? "AUTO\nON" : "AUTO\nOFF";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;

        // Button click event
        autoPlayButton.onClick.AddListener(TogglePerfectAutoPlay);

        isAutoPlayUISetup = true;
        Debug.Log("🎯 Perfect Auto-Play UI kuruldu!");
    }

    void SetupHitZones()
    {
        // HitZone referanslarını al
        hitZones = FindObjectsByType<HitZoneTrigger>(FindObjectsSortMode.None);
        System.Array.Sort(hitZones, (a, b) => a.laneIndex.CompareTo(b.laneIndex));

        // HitZoneManager'ı otomatik bul
        if (hitZoneManager == null)
            hitZoneManager = FindFirstObjectByType<HitZoneManager>();

        Debug.Log($"🎯 {hitZones.Length} hit zone bulundu, HitZoneManager: {hitZoneManager != null}");
    }

    void TogglePerfectAutoPlay()
    {
        isPerfectAutoPlayEnabled = !isPerfectAutoPlayEnabled;

        if (autoPlayButton != null)
        {
            var buttonText = autoPlayButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = isPerfectAutoPlayEnabled ? "AUTO ON" : "AUTO OFF";
                autoPlayButton.GetComponent<Image>().color = isPerfectAutoPlayEnabled ? Color.green : Color.red;
            }
        }

        Debug.Log($"🎯 Perfect Auto-Play: {(isPerfectAutoPlayEnabled ? "AÇIK" : "KAPALI")}");

        if (isPerfectAutoPlayEnabled)
        {
            // 🎵 TEMPO SYNC DEBUG: Check if timing is properly synchronized
            var noteRenderer = FindFirstObjectByType<NoteRenderer>();
            var gameplayManager = FindFirstObjectByType<GameplayManager>();
            if (noteRenderer != null && gameplayManager != null)
            {
                Debug.Log($"🎼 MUSICAL INTEGRITY AUTO-PLAY DEBUG:");
                Debug.Log($"   🚀 Current Speed Multiplier: {noteRenderer.GetSpeedMultiplier():F2}");
                Debug.Log($"   ⏱️ Note Travel Time: {noteRenderer.GetNoteTravelTime():F2}s");

                // Musical Integrity System durumunu kontrol et
                if (MusicalIntegritySystem.Instance != null)
                {
                    var realmScore = MusicalIntegritySystem.Instance.GetCurrentMusicalRealismScore();
                    Debug.Log($"   🎼 Musical Realism Score: {realmScore:F2}/1.0");

                    // Test mode'u aç
                    MusicalIntegritySystem.Instance.TestCurrentSong();
                }
                else
                {
                    Debug.LogWarning("⚠️ Musical Integrity System not available during auto-play!");
                }
            }

            StartCoroutine(PerfectAutoPlayLoop());
        }
        else
        {
            StopCoroutine(PerfectAutoPlayLoop());
        }
    }

    void HandlePerfectAutoPlay()
    {
        // Bu metod Update'de çağrılır ve hiçbir şey yapmaz
        // Asıl iş PerfectAutoPlayLoop coroutine'inde yapılır
    }

    IEnumerator PerfectAutoPlayLoop()
    {
        Debug.Log("🎯 Perfect Auto-Play başlatıldı!");

        while (isPerfectAutoPlayEnabled)
        {
            // Tüm hit zone'ları kontrol et
            for (int laneIndex = 0; laneIndex < hitZones.Length; laneIndex++)
            {
                var hitZone = hitZones[laneIndex];
                if (hitZone == null || hitZone.insideNotes.Count == 0) continue;

                // Bu lane'deki en yakın notayı bul
                GameObject closestNote = null;
                NoteWrapper closestWrapper = null;
                double smallestTimeDiff = double.MaxValue;

                foreach (var noteObj in hitZone.insideNotes)
                {
                    if (noteObj == null) continue;
                    var noteWrapper = noteObj.GetComponent<NoteWrapper>();
                    if (noteWrapper == null) continue;

                    double currentTime = AudioSettings.dspTime;
                    double timeDiff = System.Math.Abs(currentTime - noteWrapper.dspHitTime);

                    if (timeDiff < smallestTimeDiff)
                    {
                        smallestTimeDiff = timeDiff;
                        closestNote = noteObj;
                        closestWrapper = noteWrapper;
                    }
                }

                // Eğer perfect zamanda olan bir nota varsa onu çal
                if (closestNote != null && closestWrapper != null)
                {
                    double currentTime = AudioSettings.dspTime;
                    double timeDiffMs = System.Math.Abs(currentTime - closestWrapper.dspHitTime) * 1000.0;

                    // Perfect timing window içindeyse otomatik çal
                    float perfectWindow = hitZoneManager != null ? hitZoneManager.perfectWindowMs : 80f;

                    if (timeDiffMs <= perfectWindow)
                    {
                        // Perfect hit simülasyonu
                        AutoHitNote(laneIndex, closestNote, closestWrapper);
                    }
                }
            }

            yield return new WaitForSeconds(0.01f); // 10ms'de bir kontrol et (yüksek hassasiyet)
        }

        Debug.Log("🎯 Perfect Auto-Play durduruldu!");
    }

    void AutoHitNote(int laneIndex, GameObject noteObj, NoteWrapper noteWrapper)
    {
        if (hitZoneManager == null || noteObj == null || noteWrapper == null) return;

        // Manuel hit zone işlemi simüle et
        var hitZone = hitZones[laneIndex];
        if (hitZone == null) return;

        // Notayı hit zone'dan çıkar (ÇİFT VURMA ÖNLEMİ)
        hitZone.RemoveNote(noteObj);

        // Note animator ile hit animasyonu
        var animator = noteObj.GetComponent<NoteAnimator>();
        if (animator != null)
        {
            animator.AnimateHit(HitAccuracy.Perfect);
        }
        else
        {
            Destroy(noteObj);
        }

        // Ses çal (InteractiveMusicSystem)
        if (noteWrapper.gameNoteInfo != null)
        {
            InteractiveMusicSystem.Instance?.PlayNoteFromChart(noteWrapper.gameNoteInfo);
        }

        // Particle effect (perfect hit)
        SpawnAutoPerfectEffect(noteObj.transform.position);

        // Score güncelle
        GameManager.Instance?.UpdateScore(300); // Perfect hit = 300 puan

        Debug.Log($"🎯 AUTO-HIT: Lane {laneIndex} - Perfect hit! dspTime: {AudioSettings.dspTime:F3}");
    }

    void SpawnAutoPerfectEffect(Vector3 position)
    {
        // HitZoneManager'dan perfect effect prefab'ını kullan
        if (hitZoneManager != null)
        {
            // Reflection ile private perfect effect prefab'ına eriş
            var effectField = typeof(HitZoneManager).GetField("perfectHitEffectPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (effectField != null)
            {
                GameObject perfectEffectPrefab = effectField.GetValue(hitZoneManager) as GameObject;
                if (perfectEffectPrefab != null)
                {
                    GameObject effect = Instantiate(perfectEffectPrefab, position, Quaternion.identity);
                    Debug.Log($"✨ Auto-play perfect particle spawned at {position}");
                }
            }
        }
    }

    void OnDestroy()
    {
        // Auto-play UI temizle
        if (autoPlayCanvas != null)
        {
            Destroy(autoPlayCanvas.gameObject);
        }

        // Coroutine'leri durdur
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
        }
    }

    /// <summary>
    /// 🎵 DATABASE TEMPO TEST: Tests extreme tempos from songs_database.json
    /// </summary>
    [ContextMenu("Test Database Tempos")]
    public void TestDatabaseTempos()
    {
        Debug.Log("🎵 === DATABASE TEMPO TEST ===");

        // Extreme tempos from database
        var testCases = new[]
        {
            new { song = "Cathedral", tempo = 45, expected = "Very Slow" },
            new { song = "Moon Light", tempo = 50, expected = "Very Slow" },
            new { song = "Fur Elise", tempo = 62, expected = "Slow" },
            new { song = "Cannon", tempo = 77, expected = "Moderate" },
            new { song = "Vidalita", tempo = 120, expected = "Normal (Reference)" },
            new { song = "Turkish Delight", tempo = 140, expected = "Fast" },
            new { song = "Moonlight Sonata", tempo = 176, expected = "Very Fast" },
            new { song = "Sinfonia 40", tempo = 250, expected = "EXTREME!" }
        };

        var noteRenderer = FindFirstObjectByType<NoteRenderer>();
        if (noteRenderer == null)
        {
            Debug.LogError("NoteRenderer not found!");
            return;
        }

        float baseSpeed = 12f;
        float spawnZ = 25f;
        float hitZoneZ = 0f;

        foreach (var test in testCases)
        {
            float calculatedSpeed = baseSpeed * ((float)test.tempo / 120f);
            calculatedSpeed = Mathf.Clamp(calculatedSpeed, 3f, 30f); // Same clamp as system

            float travelTime = Mathf.Abs(spawnZ - hitZoneZ) / calculatedSpeed;

            string evaluation = "";
            if (travelTime > 4f) evaluation = "⚠️ TOO SLOW?";
            else if (travelTime < 0.8f) evaluation = "⚠️ TOO FAST?";
            else evaluation = "✅ GOOD";

            Debug.Log($"🎵 {test.song} ({test.tempo} BPM): Speed={calculatedSpeed:F1}, Travel={travelTime:F2}s [{test.expected}] {evaluation}");
        }

        Debug.Log("🎵 === RECOMMENDATIONS ===");
        Debug.Log("⭐ Travel times between 1.0-3.0 seconds are ideal for most players");
        Debug.Log("⭐ Cathedral (45 BPM) might need special handling - very meditative");
        Debug.Log("⭐ Sinfonia 40 (250 BPM) will be INTENSE - expert level only!");
    }
}