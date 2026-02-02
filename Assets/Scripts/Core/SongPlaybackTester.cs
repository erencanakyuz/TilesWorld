using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable 0414 // Field is assigned but its value is never used (editor-only test tweaks)

/// <summary>
/// SONG PLAYBACK TESTER - Comprehensive Audio System Testing Tool
/// 
/// ğŸµ SONG SHORTCUTS:
/// - L: Turkish Delight (default test song)
/// - K: Cannon (moderate tempo)
/// - J: FÃ¼r Elise (classical)
/// - H: Moon Light (slow tempo)
/// - P: Sinfonia 40 (extreme tempo - hardest test)
/// - O: Turkish Delight (alternative shortcut)
/// 
/// ğŸ§ª AUDIO SYSTEM TEST CASES (Inspector controllable):
/// - 1: Machine Gun Prevention Test - Rapid fire blocking
/// - 2: Legitimate Fast Music Test - FÃ¼r Elise melody
/// - 3: Voice Stealing Test - 64+ polyphony stress test
/// - 4: Chord Progression Test - Harmonic sound test
/// 
/// ğŸ¯ HARMONIK SES TESTI:
/// - T: Lane 4 tap (InputManager â†’ HitZoneManager â†’ AudioManager)
/// - Q,W,E,R,T,Y: Lane 0-5 direct input (best harmony test)
/// 
/// Perfect Auto-Play toggle butonu - ekranÄ±n sol ortasÄ±nda, tÃ¼m notalarÄ± perfect zamanda otomatik Ã§alar
/// </summary>
public class SongPlaybackTester : MonoBehaviour
{
    [Header("Test AyarlarÄ±")]
    [Tooltip("Test iÃ§in kullanÄ±lacak ÅŸarkÄ±nÄ±n veritabanÄ±ndaki adÄ±.")]
    [SerializeField] private string testSongTitle = "Turkish Delight";
    [Tooltip("Testte kullanÄ±lacak enstrÃ¼man.")]
    [SerializeField] private InstrumentType testInstrument = InstrumentType.Piano;

    [Header("Tempo AyarÄ±")]
    [Tooltip(">1 = daha hÄ±zlÄ± Ã§alma, <1 = yavaÅŸlatÄ±r. 2 = iki kat hÄ±z.")]
    [SerializeField] private float speedMultiplier = 2f;

    [Header("ğŸ¯ Perfect Auto-Play")]
    [Tooltip("Perfect Auto-Play mode aktif olsun mu?")]
    [SerializeField] private bool isPerfectAutoPlayEnabled = false;
    [Tooltip("Auto-play butonunun konumu (ekran koordinatlarÄ±)")]
    [SerializeField] private Vector2 buttonPosition = new Vector2(100, Screen.height / 2);
    [Header("ğŸ§ª Auto Test (Performance)")]
    [Tooltip("Play'e girince otomatik performans testi baslasın mı?")]
    [SerializeField] private bool autoStartGameplayOnPlay = false; // DISABLED for normal gameplay
    [Tooltip("Otomatik testte kullanÄ±lacak ÅŸarkÄ± adÄ± (SongDatabase title)")]
    [SerializeField] private string autoStartSongTitle = "Moonlight Sonata";
    [Tooltip("Otomatik test baÅŸlamadan Ã¶nce bekleme (sn)")]
    [SerializeField] private float autoStartDelay = 0.5f;
    [Tooltip("Otomatik testte Perfect Auto-Play aÃ§Ä±lsÄ±n mÄ±?")]
    [SerializeField] private bool autoEnablePerfectAutoPlay = true;


    [Header("Auto-Play Debug")]
    [SerializeField] private bool enableAutoHitPerfLogs = false;
    [SerializeField] private int autoHitLogEveryN = 120;
    [SerializeField] private float autoHitSlowThresholdMs = 2f;
    [SerializeField] private bool disableAutoHitAudio = false;     // ENABLED for Android test
    [SerializeField] private bool disableAutoHitParticles = false;  // ENABLED for Android test
    [SerializeField] private bool disableAutoHitIMS = false;        // ENABLED for Android test
    [Tooltip("Auto-play: HitZoneTrigger yerine aktif noteleri global listeden tarar.")]
    [SerializeField] private bool useGlobalNoteListForAutoHit = true;
    [Header("ğŸ§ª Inspector Test Controls")]
    [Tooltip("Test case'leri aktif olsun mu?")]
    [SerializeField] private bool enableTestCases = true;
    // [REMOVED] Machine Gun Prevention system was deleted
    [Tooltip("TEST: 2 tuÅŸu - Legitimate fast music. BEKLENEN: 9 notanÄ±n hepsini duymalÄ±sÄ±n, smooth melody Ã§almalÄ±dÄ±r.")]
    [SerializeField] private bool enableLegitimateRapidTest = true;
    // [REMOVED] Voice Stealing system was deleted
    [Tooltip("TEST: 3 tuÅŸu - Chord progression harmony. BEKLENEN: 4 chord'u harmonic olarak duymalÄ±sÄ±n, 3 nota beraber Ã§almalÄ±dÄ±r.")]
    [SerializeField] private bool enableChordProgressionTest = true;
    [Tooltip("Auto-play butonunun boyutu")]
    [SerializeField] private Vector2 buttonSize = new Vector2(120, 60);

#if UNITY_EDITOR
    [Header("Pitch Mapping AyarlarÄ± (Editor-Only)")]
    [Tooltip("Orijinal Java mapping'ini kullan (lane+pitch â†’ sound index). KapalÄ±ysa pitch'i direkt kullanÄ±r.")]
    [SerializeField] private bool useJavaMapping = true;

    [Tooltip("Custom mapping aktif olsun mu? Aktifse offset / factor uygulanÄ±r.")]
    [SerializeField] private bool enableCustomMapping = false;

    [Tooltip("Sound index'e eklenecek/Ã§Ä±karÄ±lacak offset. Ã–rnek: 3 â†’ +3 yarÄ±m ses, -2 â†’ -2 yarÄ±m ses.")]
    [SerializeField] private int pitchOffset = 0;

    [Tooltip("Sound index'i Ã§arpan deÄŸer. 1 = deÄŸiÅŸmez, 2 = iki kat, 0.5 = yarÄ±.")]
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
    // OPTIMIZATION: Increased from 20ms to 33ms (~30Hz) - still plenty accurate for 176 BPM songs
    private bool autoStartTriggered = false;
    private float autoStartRequestedAt = -1f;
    private static readonly WaitForSeconds autoPlayWait = new WaitForSeconds(0.033f);
    // NoteWrapper cache to eliminate GetComponent calls during heavy note density
    private readonly Dictionary<GameObject, NoteWrapper> noteWrapperCache = new Dictionary<GameObject, NoteWrapper>(64);

    private int autoHitLogCounter = 0;
    [SerializeField] private int autoHitCount = 0;
    // Mobile 3-finger touch detection
    private float threeFingerHoldTime = 0f;
    private const float requiredHoldDuration = 1f;

    // Auto-play iÃ§in hitzone izleme
    private HitZoneTrigger[] hitZones;
    
    // UI Theme
    private UIConfig uiConfig;

    void Start()
    {
        uiConfig = Resources.Load<UIConfig>("UI/UIConfig");
        SetupHitZones();
        // NOTE: Don't call SetupAutoPlayUI here - it will be called when GameState changes to Playing
        
        // FORCE DISABLE for mobile build testing - Inspector value override
        autoStartGameplayOnPlay = false;
        
        Debug.Log($"AutoStart init: enabled={autoStartGameplayOnPlay} delay={autoStartDelay:F2}s");

        if (autoStartGameplayOnPlay && autoStartRequestedAt < 0f)
        {
            autoStartRequestedAt = Time.unscaledTime;
        }
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

        if (autoStartGameplayOnPlay && !autoStartTriggered)
        {
            if (autoStartRequestedAt < 0f)
            {
                autoStartRequestedAt = Time.unscaledTime;
            }

            if (Time.unscaledTime - autoStartRequestedAt >= autoStartDelay)
            {
                if (TryAutoStartNow())
                {
                    autoStartTriggered = true;
                }
            }
        }
    }

    private bool TryAutoStartNow()
    {
        if (songDatabase == null) songDatabase = SongDatabase.Instance;
        var gameplayManager = FindFirstObjectByType<GameplayManager>();

        if (songDatabase == null || gameplayManager == null)
        {
            return false;
        }

        var song = songDatabase.GetSongByTitle(autoStartSongTitle);
        if (song == null)
        {
            Debug.LogWarning($"AutoStart failed: Song '{autoStartSongTitle}' not found in database.");
            return true;
        }

        Debug.Log($"AutoStart: Starting gameplay for \"{autoStartSongTitle}\".");
        gameplayManager.StartGameplay(song.musicId);

        if (autoEnablePerfectAutoPlay)
        {
            SetPerfectAutoPlay(true);
        }

        return true;
    }

    void HandleKeyboardInput()
    {
        // 'L' tuÅŸuna her basÄ±ldÄ±ÄŸÄ±nda testi baÅŸlat veya yeniden baÅŸlat
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            StartTest();
        }

        // ğŸ¯ AUDIO SYSTEM TEST CASES (Inspector Controllable)
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
                    Debug.Log("ğŸ“± 3-finger touch detected! Starting mobile test...");
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

            // Aktif sesleri durdurmak istiyorsanÄ±z AudioManager'da global bir StopAll metodu ekleyebilirsiniz
        }

        // Yeniden baÅŸlat
        testCoroutine = StartCoroutine(PlaySongTest());
    }

    private IEnumerator PlaySongTest()
    {
        isTestRunning = true;
        Debug.Log($"--- BAÅLATILIYOR: '{testSongTitle}' Ã§alma testi ---");

        // 1. Auto-find components if not assigned
        if (audioManager == null) audioManager = AudioManager.Instance;
        if (gameNoteCreator == null) gameNoteCreator = FindFirstObjectByType<GameNoteCreator>();
        if (songDatabase == null) songDatabase = SongDatabase.Instance;

        if (audioManager == null || gameNoteCreator == null || songDatabase == null)
        {
            Debug.LogError("Test baÅŸarÄ±sÄ±z: Gerekli komponentler bulunamadÄ±. AudioManager, GameNoteCreator, SongDatabase singleton'larÄ± kontrol edin.");
            isTestRunning = false;
            yield break;
        }

        // 2. ÅarkÄ± verisini veritabanÄ±ndan al
        SongDatabaseInfo songToTest = songDatabase.GetSongByTitle(testSongTitle);
        if (songToTest == null)
        {
            Debug.LogError($"Test baÅŸarÄ±sÄ±z: '{testSongTitle}' adlÄ± ÅŸarkÄ± veritabanÄ±nda bulunamadÄ±.");
            isTestRunning = false;
            yield break;
        }

        // 3. SongDatabaseInfo -> SongData dÃ¶nÃ¼ÅŸtÃ¼r ve GameNoteCreator'a yÃ¼kle
        SongData tempSongData = ScriptableObject.CreateInstance<SongData>();
        tempSongData.songName = songToTest.title;
        tempSongData.artist = songToTest.artist;
        tempSongData.bpm = songToTest.tempo;
        tempSongData.duration = EstimateDuration(songToTest.tempo); // Kabaca tahmin
        tempSongData.audioFilePath = $"Music/{songToTest.songKey}";
        tempSongData.noteChartPath = $"Song_Note_Jsons/Individual/{songToTest.songKey}";
        tempSongData.songKey = songToTest.songKey;

        // GameNoteCreator'Ä±n otomatik spawn Ã¶zelliÄŸini kapat
        gameNoteCreator.autoSpawnEnabled = false;

        // Song'u yÃ¼kle ve paketleri oluÅŸtur
        gameNoteCreator.LoadSong(tempSongData);

        // Paketlerin hazÄ±rlanmasÄ±nÄ± bekle
        yield return new WaitUntil(() => gameNoteCreator.GetQueueCount() > 0);
        Debug.Log("ÅarkÄ± verisi GameNoteCreator tarafÄ±ndan iÅŸlendi. Ã‡almaya hazÄ±rlanÄ±lÄ±yor...");

        // Oyuncunun hazÄ±rlanmasÄ± iÃ§in kÄ±sa bir baÅŸlangÄ±Ã§ gecikmesi
        yield return new WaitForSeconds(1.5f);
        Debug.Log("Ã‡almaya BaÅŸla!");

        // 4. HazÄ±rlanmÄ±ÅŸ nota paketlerini sÄ±rayla Ã§al
        int pkgCounter = 0;
        while (true)
        {
            GameNoteInfoPackage package = gameNoteCreator.GetNextTestPackage();

            // Ã‡alÄ±nacak paket kalmadÄ±ysa testi bitir
            if (package == null)
            {
                Debug.Log("--- BÄ°TTÄ°: ÅarkÄ± Ã§alma testi tamamlandÄ±. ---");
                break;
            }

            // Debug paketi Ã¶zetle
            string noteSummary = string.Join(", ", package.gameNoteInfos.Select(n => $"L{n.line}:P{n.pitch}"));
            Debug.Log($"PKG {pkgCounter}: {noteSummary} â†’ wait {package.oneNote / speedMultiplier:F1}ms");

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
                        Debug.LogError($"ğŸµ SongPlaybackTester: AudioConstants.GetFinalSoundIndex failed: {ex.Message}. Using fallback.");
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

                // DetaylÄ± log (opsiyonel)
                Debug.Log($"ğŸµ TEST PLAY: Lane={note.line}, BasePitch={note.pitch} â†’ FinalIdx={finalPitch}");

                // useJavaMapping should be false here because we have already calculated the final index.
                audioManager.PlayNote(testInstrument, finalPitch, 1.0f, false, note.line);
            }

            pkgCounter++;

            // Bir sonraki pakete geÃ§meden Ã¶nce zamanlamasÄ± kadar bekle
            float waitTime = (package.oneNote / speedMultiplier) / 1000f;
            yield return new WaitForSeconds(waitTime);
        }

        isTestRunning = false;
    }

    /// <summary>
    /// Tempo deÄŸerinden kabaca ÅŸarkÄ± sÃ¼resi tahmini yapar (GameplayManager'daki ile aynÄ± mantÄ±k).
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

        // Canvas oluÅŸtur
        GameObject canvasObj = new GameObject("AutoPlayCanvas");
        autoPlayCanvas = canvasObj.AddComponent<Canvas>();
        autoPlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        autoPlayCanvas.sortingOrder = 1000; // En Ã¼stte gÃ¶rÃ¼nmesi iÃ§in

        // CanvasScaler ekle
        CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        // GraphicRaycaster ekle
        canvasObj.AddComponent<GraphicRaycaster>();

        // DISABLED: MainScene already has EventSystem, don't create duplicate
        // var existingEventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        // if (existingEventSystem == null)
        // {
        //     GameObject eventSystemObj = new GameObject("EventSystem");
        //     eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        //     eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        // }

        // Button oluÅŸtur
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
        Debug.Log("ğŸ¯ Perfect Auto-Play UI kuruldu!");
    }

    void SetupHitZones()
    {
        // HitZone referanslarÄ±nÄ± al
        hitZones = FindObjectsByType<HitZoneTrigger>(FindObjectsSortMode.None);
        System.Array.Sort(hitZones, (a, b) => a.laneIndex.CompareTo(b.laneIndex));

        // HitZoneManager'Ä± otomatik bul
        if (hitZoneManager == null)
            hitZoneManager = FindFirstObjectByType<HitZoneManager>();

        Debug.Log($"ğŸ¯ {hitZones.Length} hit zone bulundu, HitZoneManager: {hitZoneManager != null}");
    }

    void TogglePerfectAutoPlay()
    {
        SetPerfectAutoPlay(!isPerfectAutoPlayEnabled);
    }

    void HandlePerfectAutoPlay()
    {
        // Bu metod Update'de Ã§aÄŸrÄ±lÄ±r ve hiÃ§bir ÅŸey yapmaz
        // AsÄ±l iÅŸ PerfectAutoPlayLoop coroutine'inde yapÄ±lÄ±r
    }

    IEnumerator PerfectAutoPlayLoop()
    {
        Debug.Log("Perfect Auto-Play baslatilaadi!");
        
        var audioMgr = AudioManager.Instance;
        if (audioMgr != null)
        {
            var selectedInstrument = GameManager.Instance != null ? GameManager.Instance.GetSelectedInstrument() : InstrumentType.Piano;
            while (!audioMgr.IsInstrumentPrewarmed(selectedInstrument))
            {
                yield return null;
            }
        }

        // OPTIMIZATION: Cache list to avoid GC allocations every frame
        var notesToHit = new System.Collections.Generic.List<(int lane, GameObject noteObj, NoteWrapper wrapper)>(32);
        
        // Cache perfect window value
        float perfectWindow = hitZoneManager != null ? hitZoneManager.perfectWindowMs * 1.5f : 120f;
        
        // OPTIMIZATION: Clear cache periodically to prevent memory bloat
        int frameCounter = 0;

        while (isPerfectAutoPlayEnabled)
        {
            notesToHit.Clear();
            double currentTime = AudioSettings.dspTime;
            
            // OPTIMIZATION: Clean cache every 300 frames (~10 seconds at 30Hz poll rate)
            frameCounter++;
            if (frameCounter >= 300)
            {
                frameCounter = 0;
                noteWrapperCache.Clear();
            }
            
            // Collect only one note per lane to minimize scan cost
            for (int laneIndex = 0; laneIndex < hitZones.Length; laneIndex++)
            {
                var hitZone = hitZones[laneIndex];
                if (hitZone == null || hitZone.GetNoteCount() == 0) continue;

                var noteObj = hitZone.PeekEarliestNote();
                if (noteObj == null) continue;

                if (!noteWrapperCache.TryGetValue(noteObj, out var noteWrapper))
                {
                    noteWrapper = noteObj.GetComponent<NoteWrapper>();
                    if (noteWrapper != null)
                    {
                        noteWrapperCache[noteObj] = noteWrapper;
                    }
                }

                if (noteWrapper == null) continue;

                double timeDiffMs = System.Math.Abs(currentTime - noteWrapper.dspHitTime) * 1000.0;
                if (timeDiffMs <= perfectWindow)
                {
                    notesToHit.Add((laneIndex, noteObj, noteWrapper));
                }
            }
            
            // Now hit all collected notes
            foreach (var (lane, noteObj, wrapper) in notesToHit)
            {
                if (noteObj != null && wrapper != null)
                {
                    AutoHitNote(lane, noteObj, wrapper);
                    // Remove from cache after hit to prevent stale references
                    noteWrapperCache.Remove(noteObj);
                }
            }

            // OPTIMIZATION: Poll at 30Hz - still plenty accurate for 176 BPM songs
            yield return autoPlayWait;
        }

        Debug.Log("Perfect Auto-Play durdurulaadi!");
        noteWrapperCache.Clear(); // Clean up on stop
    }

    void AutoHitNote(int laneIndex, GameObject noteObj, NoteWrapper noteWrapper)
    {
        if (hitZoneManager == null || noteObj == null || noteWrapper == null) return;

        autoHitCount++;

        bool logPerf = enableAutoHitPerfLogs;
        float t0 = 0f;
        float t1 = 0f;
        float t2 = 0f;
        float t3 = 0f;
        float t4 = 0f;
        float t5 = 0f;

        if (logPerf)
        {
            t0 = Time.realtimeSinceStartup;
        }

        // Manuel hit zone islemi simule et
        var hitZone = hitZones[laneIndex];
        if (hitZone == null) return;

        // Notayi hit zone'dan cikar (CIFT VURMA ONLEMI)
        hitZone.RemoveNote(noteObj);

        if (logPerf)
        {
            t1 = Time.realtimeSinceStartup;
        }

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

        if (logPerf)
        {
            t2 = Time.realtimeSinceStartup;
        }

        if (noteWrapper.gameNoteInfo != null)
        {
            var noteInfo = noteWrapper.gameNoteInfo;
            var instrument = GameManager.Instance != null ? GameManager.Instance.GetSelectedInstrument() : InstrumentType.Piano;

            if (!disableAutoHitAudio && AudioManager.Instance != null)
            {
                float calculatedVolume = AudioManager.Instance.CalculateNoteVolume(noteInfo.duration);
                AudioManager.Instance.PlayNote(instrument, noteInfo.pitch, volume: calculatedVolume, useJavaMapping: true, line: noteInfo.line, noteDuration: noteInfo.duration);
            }

            if (!disableAutoHitIMS)
            {
                InteractiveMusicSystem.Instance?.ProcessChartNoteHit(noteInfo);
            }
        }

        if (logPerf)
        {
            t3 = Time.realtimeSinceStartup;
        }

        if (!disableAutoHitParticles)
        {
            SpawnAutoPerfectEffect(noteObj.transform.position);
        }

        if (logPerf)
        {
            t4 = Time.realtimeSinceStartup;
        }

        // Score guncelle
        GameManager.Instance?.UpdateScore(300); // Perfect hit = 300 puan

        if (logPerf)
        {
            t5 = Time.realtimeSinceStartup;
            autoHitLogCounter++;

            float totalMs = (t5 - t0) * 1000f;
            float removeMs = (t1 - t0) * 1000f;
            float animMs = (t2 - t1) * 1000f;
            float audioMs = (t3 - t2) * 1000f;
            float particleMs = (t4 - t3) * 1000f;
            float scoreMs = (t5 - t4) * 1000f;

            bool logNow = totalMs >= autoHitSlowThresholdMs;
            if (!logNow && autoHitLogEveryN > 0 && (autoHitLogCounter % autoHitLogEveryN) == 0)
            {
                logNow = true;
            }

            if (logNow)
            {
                int queueCount = gameNoteCreator != null ? gameNoteCreator.GetQueueCount() : -1;
                int insideCount = hitZone != null ? hitZone.GetNoteCount() : -1;
                int activeAudio = AudioManager.Instance != null ? AudioManager.Instance.GetActiveSourceCount() : -1;
                int pooledAudio = AudioManager.Instance != null ? AudioManager.Instance.GetPooledSourceCount() : -1;

                int dropNoClip = 0;
                int dropNotLoaded = 0;
                int dropNoVoice = 0;
                int stolen = 0;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.GetDropStats(out dropNoClip, out dropNotLoaded, out dropNoVoice, out stolen);
                }

                Debug.Log($"AutoHitPerf lane={laneIndex} totalMs={totalMs:F2} removeMs={removeMs:F2} animMs={animMs:F2} audioMs={audioMs:F2} particleMs={particleMs:F2} scoreMs={scoreMs:F2} queue={queueCount} inside={insideCount} audio={activeAudio}/{pooledAudio} drops={dropNoClip}/{dropNotLoaded}/{dropNoVoice}|{stolen}");
            }
        }
    }

    void SpawnAutoPerfectEffect(Vector3 position)
    {
        if (hitZoneManager != null)
        {
            hitZoneManager.SpawnHitEffect(position, HitAccuracy.Perfect);
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
            autoPlayCoroutine = null;
        }
    }

    private void SetPerfectAutoPlay(bool enabled)
    {
        isPerfectAutoPlayEnabled = enabled;

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

        Debug.Log($"ğŸ¯ Perfect Auto-Play: {(isPerfectAutoPlayEnabled ? "AÃ‡IK" : "KAPALI")}");

        if (isPerfectAutoPlayEnabled)
        {
            // ğŸµ TEMPO SYNC DEBUG: Check if timing is properly synchronized
            var noteRenderer = FindFirstObjectByType<NoteRenderer>();
            var gameplayManager = FindFirstObjectByType<GameplayManager>();
            if (noteRenderer != null && gameplayManager != null)
            {
                Debug.Log($"ğŸ¼ MUSICAL INTEGRITY AUTO-PLAY DEBUG:");
                Debug.Log($"   ğŸš€ Current Speed Multiplier: {noteRenderer.GetSpeedMultiplier():F2}");
                Debug.Log($"   â±ï¸ Note Travel Time: {noteRenderer.GetNoteTravelTime():F2}s");

                // Musical Integrity System durumunu kontrol et
                if (MusicalIntegritySystem.Instance != null)
                {
                    Debug.Log($"   ğŸ¼ Musical Integrity System: Active and optimized");

                    // Test mode'u aÃ§
                    MusicalIntegritySystem.Instance.TestCurrentSong();
                }
                else
                {
                    Debug.LogWarning("âš ï¸ Musical Integrity System not available during auto-play!");
                }
            }

            if (autoPlayCoroutine == null)
            {
                autoPlayCoroutine = StartCoroutine(PerfectAutoPlayLoop());
            }
        }
        else
        {
            if (autoPlayCoroutine != null)
            {
                StopCoroutine(autoPlayCoroutine);
                autoPlayCoroutine = null;
            }
        }
    }

    private IEnumerator AutoStartGameplayRoutine()
    {
        Debug.Log("AutoStart: coroutine started.");
        float delayEnd = Time.realtimeSinceStartup + autoStartDelay;
        while (Time.realtimeSinceStartup < delayEnd)
        {
            yield return null;
        }
        Debug.Log("AutoStart: delay complete.");

        GameplayManager gameplayManager = null;
        float waitStart = Time.realtimeSinceStartup;
        const float maxWaitSeconds = 5f;
        while (Time.realtimeSinceStartup - waitStart < maxWaitSeconds)
        {
            if (songDatabase == null) songDatabase = SongDatabase.Instance;
            if (gameplayManager == null) gameplayManager = FindFirstObjectByType<GameplayManager>();
            if (songDatabase != null && gameplayManager != null) break;
            yield return null;
        }

        if (songDatabase == null || gameplayManager == null)
        {
            Debug.LogWarning("AutoStart failed: SongDatabase or GameplayManager not found.");
            yield break;
        }

        var song = songDatabase.GetSongByTitle(autoStartSongTitle);
        if (song == null)
        {
            Debug.LogWarning($"AutoStart failed: Song '{autoStartSongTitle}' not found in database.");
            yield break;
        }

        Debug.Log($"AutoStart: Starting gameplay for \"{autoStartSongTitle}\".");
        gameplayManager.StartGameplay(song.musicId);

        if (autoEnablePerfectAutoPlay)
        {
            SetPerfectAutoPlay(true);
        }
    }

    // ===== ğŸ¯ AUDIO SYSTEM TEST CASES =====

    /// <summary>
    /// Test Case 1: Machine Gun Prevention - Rapid Button Mashing
    /// Tests minTimeBetweenNotes system with 10ms intervals
    /// </summary>
    // [REMOVED] TestMachineGunPrevention() - Machine Gun Prevention system was deleted

    /// <summary>
    /// Test Case 2: Legitimate Fast Music - FÃ¼r Elise Fast Section
    /// Tests 200ms intervals (legitimate musical timing)
    /// </summary>
    IEnumerator TestLegitimateRapidNotes()
    {
        Debug.Log("ğŸ¯ === TEST CASE 2: LEGITIMATE FAST MUSIC ===");
        Debug.Log("ğŸµ FÃ¼r Elise fast section (200ms intervals) - Should hear all 9 notes");
        
        // FÃ¼r Elise melody using correct lane+pitch mapping
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
            Debug.Log($"ğŸµ Playing note {i+1}/9: {noteData[i].name} (Lane {noteData[i].lane}, Pitch {noteData[i].pitch})");
            AudioManager.Instance?.PlayNote(InstrumentType.Piano, noteData[i].pitch, 1.0f, useJavaMapping: true, line: noteData[i].lane);
            yield return new WaitForSeconds(0.2f); // 200ms - legitimate musical timing
        }
        
        Debug.Log("ğŸ¯ Legitimate Fast Music test completed - Should hear smooth melody");
    }

    // [REMOVED] TestVoiceStealing() - Voice Stealing system was deleted

    /// <summary>
    /// Test Case 3: Chord Progression Stress Test - Harmony Test
    /// Tests multiple simultaneous notes (chords)
    /// </summary>
    IEnumerator TestChordProgression()
    {
        Debug.Log("ğŸ¯ === TEST CASE 4: CHORD PROGRESSION STRESS TEST ===");
        Debug.Log("ğŸ¹ Playing chord progression: C Major â†’ F Major â†’ G Major â†’ A Minor");
        
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
            Debug.Log($"ğŸ¹ Playing chord {c+1}/4: {chordData[c].name}");
            
            // Play all notes of the chord simultaneously (no delay)
            foreach(var note in chordData[c].notes)
            {
                Debug.Log($"  ğŸµ Chord note: {note.name} (Lane {note.lane}, Pitch {note.pitch})");
                AudioManager.Instance?.PlayNote(InstrumentType.Piano, note.pitch, 1.0f, useJavaMapping: true, line: note.lane);
            }
            yield return new WaitForSeconds(1.0f); // 1 second per chord
        }
        
        Debug.Log("ğŸ¯ Chord Progression test completed - Should hear rich harmonic progression");
    }

    /// <summary>
    /// ğŸµ DATABASE TEMPO TEST: Tests extreme tempos from songs_database.json
    /// </summary>
    [ContextMenu("Test Database Tempos")]
    public void TestDatabaseTempos()
    {
        Debug.Log("ğŸµ === DATABASE TEMPO TEST ===");

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
            if (travelTime > 4f) evaluation = "âš ï¸ TOO SLOW?";
            else if (travelTime < 0.8f) evaluation = "âš ï¸ TOO FAST?";
            else evaluation = "âœ… GOOD";

            Debug.Log($"ğŸµ {test.song} ({test.tempo} BPM): Speed={calculatedSpeed:F1}, Travel={travelTime:F2}s [{test.expected}] {evaluation}");
        }

        Debug.Log("ğŸµ === RECOMMENDATIONS ===");
        Debug.Log("â­ Travel times between 1.0-3.0 seconds are ideal for most players");
        Debug.Log("â­ Cathedral (45 BPM) might need special handling - very meditative");
        Debug.Log("â­ Sinfonia 40 (250 BPM) will be INTENSE - expert level only!");
    }
}
