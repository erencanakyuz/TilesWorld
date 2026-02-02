using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

#pragma warning disable 0414 // Field is assigned but its value is never used (editor-only test tweaks)

/// <summary>
/// SONG PLAYBACK TESTER - Comprehensive Audio System Testing Tool
/// 
/// 🎵 SONG SHORTCUTS:
/// - L: Turkish Delight (default test song)
/// - K: Cannon (moderate tempo)
/// - J: Für Elise (classical)
/// - H: Moon Light (slow tempo)
/// - P: Sinfonia 40 (extreme tempo - hardest test)
/// - O: Turkish Delight (alternative shortcut)
/// 
/// 🧪 AUDIO SYSTEM TEST CASES (Inspector controllable):
/// - 1: Machine Gun Prevention Test - Rapid fire blocking
/// - 2: Legitimate Fast Music Test - Für Elise melody
/// - 3: Voice Stealing Test - 64+ polyphony stress test
/// - 4: Chord Progression Test - Harmonic sound test
/// 
/// 🎯 HARMONIK SES TESTI:
/// - T: Lane 4 tap (InputManager → HitZoneManager → AudioManager)
/// - Q,W,E,R,T,Y: Lane 0-5 direct input (best harmony test)
/// 
/// Perfect Auto-Play toggle butonu - ekranın sol ortasında, tüm notaları perfect zamanda otomatik çalar
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

    [Header("🧪 Inspector Test Controls")]
    [Tooltip("Test case'leri aktif olsun mu?")]
    [SerializeField] private bool enableTestCases = true;
    // [REMOVED] Machine Gun Prevention system was deleted
    [Tooltip("TEST: 2 tuşu - Legitimate fast music. BEKLENEN: 9 notanın hepsini duymalısın, smooth melody çalmalıdır.")]
    [SerializeField] private bool enableLegitimateRapidTest = true;
    // [REMOVED] Voice Stealing system was deleted
    [Tooltip("TEST: 3 tuşu - Chord progression harmony. BEKLENEN: 4 chord'u harmonic olarak duymalısın, 3 nota beraber çalmalıdır.")]
    [SerializeField] private bool enableChordProgressionTest = true;
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
    
    // UI Theme
    private UIConfig uiConfig;

    void Start()
    {
        uiConfig = Resources.Load<UIConfig>("UI/UIConfig");
        SetupHitZones();
        // NOTE: Don't call SetupAutoPlayUI here - it will be called when GameState changes to Playing
    }

    void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Playing && !isAutoPlayUISetup)
        {
            SetupAutoPlayUI();
        }
        else if (newState != GameState.Playing && autoPlayCanvas != null)
        {
            // Hide the auto-play button when not in gameplay
            autoPlayCanvas.gameObject.SetActive(false);
        }
        else if (newState == GameState.Playing && autoPlayCanvas != null)
        {
            // Show the auto-play button when returning to gameplay
            autoPlayCanvas.gameObject.SetActive(true);
        }
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

        // 🎯 AUDIO SYSTEM TEST CASES (Inspector Controllable)
        if (enableTestCases && Keyboard.current != null)
        {
            // Test Case 2: Legitimate Fast Music (2 key)
            if (enableLegitimateRapidTest && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                StartCoroutine(TestLegitimateRapidNotes());
            }
            
            // Test Case 3: Chord Progression Stress Test (3 key)
            if (enableChordProgressionTest && Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                StartCoroutine(TestChordProgression());
            }
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
            else if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                testSongTitle = "Sinfonia 40";
                StartTest();
            }
            else if (Keyboard.current.oKey.wasPressedThisFrame)
            {
                testSongTitle = "Turkish Delight";
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
                    // Use the new, centralized function for consistent sound with defensive programming
                    try
                    {
                        finalPitch = AudioConstants.GetFinalSoundIndex(testInstrument, note.line, note.pitch, maxIndex);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"🎵 SongPlaybackTester: AudioConstants.GetFinalSoundIndex failed: {ex.Message}. Using fallback.");
                        finalPitch = UnityEngine.Mathf.Clamp(note.pitch, 0, maxIndex);
                    }
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
        
        // NOTE: GameState check removed - now handled by HandleGameStateChanged event subscription

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
        buttonImage.color = isPerfectAutoPlayEnabled ? 
            (uiConfig != null ? uiConfig.successColor : Color.green) : 
            (uiConfig != null ? uiConfig.dangerColor : Color.red);

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
        buttonText.color = uiConfig != null ? uiConfig.textPrimaryColor : Color.white;
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
                autoPlayButton.GetComponent<Image>().color = isPerfectAutoPlayEnabled ? 
                    (uiConfig != null ? uiConfig.successColor : Color.green) : 
                    (uiConfig != null ? uiConfig.dangerColor : Color.red);
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
                    Debug.Log($"   🎼 Musical Integrity System: Active and optimized");

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

                    double timeDiff = System.Math.Abs(AudioSettings.dspTime - noteWrapper.dspHitTime);

                    if (timeDiff < smallestTimeDiff)
                    {
                        smallestTimeDiff = timeDiff;
                        closestNote = noteObj;
                        closestWrapper = noteWrapper;
                    }
                }

                // ENHANCED: Tüm perfect timing'deki notaları çal (sadece en yakını değil)
                double currentTime = AudioSettings.dspTime;
                float perfectWindow = hitZoneManager != null ? hitZoneManager.perfectWindowMs * 1.5f : 120f; // Biraz daha geniş window
                
                foreach (var noteObj in hitZone.insideNotes.ToList()) // ToList() to avoid modification during iteration
                {
                    if (noteObj == null) continue;
                    var noteWrapper = noteObj.GetComponent<NoteWrapper>();
                    if (noteWrapper == null) continue;

                    double timeDiffMs = System.Math.Abs(currentTime - noteWrapper.dspHitTime) * 1000.0;

                    // Perfect timing window içindeyse otomatik çal
                    if (timeDiffMs <= perfectWindow)
                    {
                        // Perfect hit simülasyonu
                        Debug.Log($"🎯 AUTO-PLAY: Found perfect note in lane {laneIndex}, pitch {noteWrapper.gameNoteInfo?.pitch}, timeDiff: {timeDiffMs:F1}ms");
                        AutoHitNote(laneIndex, noteObj, noteWrapper);
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

        // Ses çal (FIXED: Use same audio source as normal gameplay)
        if (noteWrapper.gameNoteInfo != null && AudioManager.Instance != null)
        {
            var noteInfo = noteWrapper.gameNoteInfo;
            // Use same parameters as normal gameplay in HitZoneManager
            var instrument = GameManager.Instance != null ? GameManager.Instance.GetSelectedInstrument() : InstrumentType.Piano;
            AudioManager.Instance.PlayNote(instrument, noteInfo.pitch, volume: 1.0f, useJavaMapping: true, line: noteInfo.line, noteDuration: noteInfo.duration);
            InteractiveMusicSystem.Instance?.ProcessChartNoteHit(noteInfo);
        }

        // Particle effect (perfect hit)
        SpawnAutoPerfectEffect(noteObj.transform.position);

        // Score güncelle
        GameManager.Instance?.UpdateScore(300); // Perfect hit = 300 puan

        Debug.Log($"🎯 AUTO-HIT: Lane {laneIndex} - Perfect hit! dspTime: {AudioSettings.dspTime:F3}");
    }

    void SpawnAutoPerfectEffect(Vector3 position)
    {
        // OPTIMIZED: Use public method instead of reflection for better performance
        if (hitZoneManager != null)
        {
            GameObject perfectEffectPrefab = hitZoneManager.GetParticlePrefabForAccuracy(HitAccuracy.Perfect);
            if (perfectEffectPrefab != null)
            {
                GameObject effect = Instantiate(perfectEffectPrefab, position, Quaternion.identity);
                Debug.Log($"✨ Auto-play perfect particle spawned at {position}");
            }
            else
            {
                Debug.LogWarning("⚠️ Perfect hit effect prefab not found in HitZoneManager");
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

    // ===== 🎯 AUDIO SYSTEM TEST CASES =====

    /// <summary>
    /// Test Case 1: Machine Gun Prevention - Rapid Button Mashing
    /// Tests minTimeBetweenNotes system with 10ms intervals
    /// </summary>
    // [REMOVED] TestMachineGunPrevention() - Machine Gun Prevention system was deleted

    /// <summary>
    /// Test Case 2: Legitimate Fast Music - Für Elise Fast Section
    /// Tests 200ms intervals (legitimate musical timing)
    /// </summary>
    IEnumerator TestLegitimateRapidNotes()
    {
        Debug.Log("🎯 === TEST CASE 2: LEGITIMATE FAST MUSIC ===");
        Debug.Log("🎵 Für Elise fast section (200ms intervals) - Should hear all 9 notes");
        
        // Für Elise melody using correct lane+pitch mapping
        var noteData = new[] {
            new { lane = 1, pitch = 8, name = "E" },   // Lane 1, Pitch 8
            new { lane = 1, pitch = 7, name = "D#" },  // Lane 1, Pitch 7  
            new { lane = 1, pitch = 8, name = "E" },   // Lane 1, Pitch 8
            new { lane = 1, pitch = 7, name = "D#" },  // Lane 1, Pitch 7
            new { lane = 1, pitch = 8, name = "E" },   // Lane 1, Pitch 8
            new { lane = 1, pitch = 3, name = "B" },   // Lane 1, Pitch 3
            new { lane = 1, pitch = 6, name = "D" },   // Lane 1, Pitch 6
            new { lane = 1, pitch = 5, name = "C" },   // Lane 1, Pitch 5
            new { lane = 1, pitch = 1, name = "A" }    // Lane 1, Pitch 1
        };
        
        for (int i = 0; i < noteData.Length; i++)
        {
            Debug.Log($"🎵 Playing note {i+1}/9: {noteData[i].name} (Lane {noteData[i].lane}, Pitch {noteData[i].pitch})");
            AudioManager.Instance?.PlayNote(InstrumentType.Piano, noteData[i].pitch, 1.0f, useJavaMapping: true, line: noteData[i].lane);
            yield return new WaitForSeconds(0.2f); // 200ms - legitimate musical timing
        }
        
        Debug.Log("🎯 Legitimate Fast Music test completed - Should hear smooth melody");
    }

    // [REMOVED] TestVoiceStealing() - Voice Stealing system was deleted

    /// <summary>
    /// Test Case 3: Chord Progression Stress Test - Harmony Test
    /// Tests multiple simultaneous notes (chords)
    /// </summary>
    IEnumerator TestChordProgression()
    {
        Debug.Log("🎯 === TEST CASE 4: CHORD PROGRESSION STRESS TEST ===");
        Debug.Log("🎹 Playing chord progression: C Major → F Major → G Major → A Minor");
        
        // Chord progression using correct lane+pitch mapping
        var chordData = new[] {
            new { 
                name = "C Major", 
                notes = new[] { 
                    new { lane = 2, pitch = 7, name = "C" }, 
                    new { lane = 1, pitch = 8, name = "E" }, 
                    new { lane = 1, pitch = 10, name = "G" } 
                } 
            },
            new { 
                name = "F Major", 
                notes = new[] { 
                    new { lane = 3, pitch = 5, name = "F" }, 
                    new { lane = 2, pitch = 5, name = "A" }, 
                    new { lane = 2, pitch = 7, name = "C" } 
                } 
            },
            new { 
                name = "G Major", 
                notes = new[] { 
                    new { lane = 3, pitch = 7, name = "G" }, 
                    new { lane = 2, pitch = 8, name = "B" }, 
                    new { lane = 2, pitch = 10, name = "D" } 
                } 
            },
            new { 
                name = "A Minor", 
                notes = new[] { 
                    new { lane = 3, pitch = 8, name = "A" }, 
                    new { lane = 2, pitch = 7, name = "C" }, 
                    new { lane = 1, pitch = 8, name = "E" } 
                } 
            }
        };
        
        for (int c = 0; c < chordData.Length; c++)
        {
            Debug.Log($"🎹 Playing chord {c+1}/4: {chordData[c].name}");
            
            // Play all notes of the chord simultaneously (no delay)
            foreach(var note in chordData[c].notes)
            {
                Debug.Log($"  🎵 Chord note: {note.name} (Lane {note.lane}, Pitch {note.pitch})");
                AudioManager.Instance?.PlayNote(InstrumentType.Piano, note.pitch, 1.0f, useJavaMapping: true, line: note.lane);
            }
            yield return new WaitForSeconds(1.0f); // 1 second per chord
        }
        
        Debug.Log("🎯 Chord Progression test completed - Should hear rich harmonic progression");
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