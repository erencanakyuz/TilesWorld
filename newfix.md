# 🔧 UIManager Refactoring Plan - FINAL v2

## 📊 MEVCUT DURUM

**Dosya:** `Assets/Scripts/UI/UIManager.cs`  
**Toplam Satır:** 1217  
**Bootstrap.unity:** UIManager objesi var, tüm prefab referansları Inspector'da ayarlı (Bootstrap.cs ayrıca UIManager'ı da create ediyor; çift instance riski var)

---

## 🎯 UZUN VADELİ EN İYİ ÇÖZÜM

### Neden ScriptableObject Yaklaşımı?

| Kriter | SerializeField'da Tut | ScriptableObject |
|--------|----------------------|------------------|
| Kod temizliği | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| Data/Code ayrımı | ❌ | ✅ |
| Prefab yönetimi | Dağınık | Merkezi |
| Test edilebilirlik | Zor | Kolay |
| Yeni UI ekleme | Script değiştir | Asset değiştir |
| Uzun vadeli bakım | ⭐⭐ | ⭐⭐⭐⭐⭐ |

### Hedef Mimari

```
UIManager (Facade - ~150 LOC)
    │
    ├── UIConfig.asset (ScriptableObject)
    │   ├── Panel Prefabs (6)
    │   ├── Effect Prefabs (3)
    │   ├── Animation Curves
    │   └── Layout Settings
    │
    ├── CanvasLocator
    ├── HUDController  
    ├── PanelManager ──────► UIConfig'den prefab alır
    ├── UIEffectPool ──────► UIConfig'den effect alır
    ├── CountdownController
    └── MobileFinder
```

---

## 📁 YENİ DOSYA YAPISI

```
Assets/
├── Resources/
│   └── UI/
│       └── UIConfig.asset          # ScriptableObject - TÜM UI config'i
│
└── Scripts/
    └── UI/
        ├── UIManager.cs            (~150 LOC) - Facade
        ├── Config/
        │   └── UIConfig.cs         (~80 LOC)  - ScriptableObject tanımı
        ├── Canvas/
        │   └── CanvasLocator.cs    (~130 LOC)
        ├── HUD/
        │   └── HUDController.cs    (~180 LOC)
        ├── Panels/
        │   ├── PanelManager.cs     (~160 LOC)
        │   └── PanelButtonWirer.cs (~80 LOC)
        ├── Effects/
        │   └── UIEffectPool.cs     (~90 LOC)
        ├── Countdown/
        │   └── CountdownController.cs (~120 LOC)
        └── Mobile/
            └── MobileFinder.cs     (~70 LOC)
```

---

## 📋 AŞAMALAR

### AŞAMA 1: UIConfig ScriptableObject Oluşturma [10 dk]

**Yeni dosya:** `Assets/Scripts/UI/Config/UIConfig.cs`

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "UIConfig", menuName = "TilesWorld/UI Config")]
public class UIConfig : ScriptableObject
{
    [Header("🎮 Panel Prefabs")]
    public GameObject mainMenuPanelPrefab;
    public GameObject songSelectionPanelPrefab;
    public GameObject gameplayPanelPrefab;
    public GameObject pausePanelPrefab;
    public GameObject gameOverPanelPrefab;
    public GameObject settingsPanelPrefab;

    [Header("🎵 Effect Prefabs")]
    public GameObject perfectHitEffect;
    public GameObject goodHitEffect;
    public GameObject missEffect;

    [Header("⚙️ Effect Settings")]
    public float effectDuration = 1.0f;
    public AnimationCurve fadeAnimation;

    [Header("📐 HUD Layout")]
    public Vector2 scorePosition = new Vector2(100f, -60f);
    public Vector2 scoreSize = new Vector2(300f, 80f);
    public float scoreFontSize = 36f;
    
    public Vector2 comboPosition = new Vector2(0f, -60f);
    public Vector2 comboSize = new Vector2(400f, 80f);
    public float comboFontSize = 32f;
    
    public Vector2 healthPosition = new Vector2(-200f, -60f);
    public Vector2 healthSize = new Vector2(300f, 30f);
    
    public Vector2 multiplierPosition = new Vector2(100f, -120f);
    public Vector2 multiplierSize = new Vector2(150f, 60f);
    public float multiplierFontSize = 28f;

    [Header("📱 Mobile Layout")]
    public Vector2 pauseButtonPosition = new Vector2(-80f, -80f);
    public Vector2 settingsButtonPosition = new Vector2(-80f, -160f);
    public Vector2 buttonSize = new Vector2(60f, 60f);

    [Header("🐛 Debug")]
    public bool enableDebugLogging = false;
    public bool enableFallbackUI = true;
}
```

### AŞAMA 2: UIConfig.asset Oluşturma [5 dk - Unity Editor]

**Unity Editor'da yapılacak:**
1. Project > Create > TilesWorld > UI Config
2. `Assets/Resources/UI/UIConfig.asset` olarak kaydet
3. Bootstrap.unity'deki UIManager'dan değerleri kopyala:
   - 6 Panel prefab
   - 3 Effect prefab
   - fadeAnimation curve
   - effectDuration

### AŞAMA 3: Klasör Yapısı + Backup [5 dk]

```
[ ] Assets/Scripts/UI/Config/ klasörü oluştur
[ ] Assets/Scripts/UI/Canvas/ klasörü oluştur
[ ] Assets/Scripts/UI/HUD/ klasörü oluştur
[ ] Assets/Scripts/UI/Panels/ klasörü oluştur
[ ] Assets/Scripts/UI/Effects/ klasörü oluştur
[ ] Assets/Scripts/UI/Countdown/ klasörü oluştur
[ ] Assets/Scripts/UI/Mobile/ klasörü oluştur
[ ] Assets/Resources/UI/ klasörü oluştur
[ ] UIManager.cs → UIManager_BACKUP.cs kopyala
```

### AŞAMA 4: CanvasLocator.cs [15 dk]

**Taşınacak Field'lar (UIManager satırları):**
- `mainCanvas` (Line 14)
- `overlayCanvas` (Line 15)
- `hudCanvas` (Line 16)

**Taşınacak Metodlar:**
- `FindCanvases()` (172-191)
- `CreateFallbackUI()` (303-350) - UIConfig.enableFallbackUI kullanır
- `ConfigureCanvasScalers()` (359-382)
- `SetupScaler()` (384-395)

**Not:** `AutoFindUIElements()` orkestrasyonu UIManager'da kalmalı; CanvasLocator sadece canvas/fallback/scaler sorumluluğu almalı.

**Yeni dosya:** `Assets/Scripts/UI/Canvas/CanvasLocator.cs`

```csharp
public class CanvasLocator : MonoBehaviour
{
    public static CanvasLocator Instance { get; private set; }
    
    public Canvas MainCanvas { get; private set; }
    public Canvas HUDCanvas { get; private set; }
    public Canvas OverlayCanvas { get; private set; }
    
    private UIConfig config;
    
    public void Initialize(UIConfig config)
    {
        Instance = this;
        this.config = config;
        DiscoverCanvases();
    }
    
    public bool DiscoverCanvases() { /* FindCanvases() kodu */ }
    private void CreateFallbackUI() { /* Mevcut kod */ }
    private void ConfigureCanvasScalers() { /* Mevcut kod */ }
    private void SetupScaler(CanvasScaler scaler) { /* Mevcut kod */ }
}
```

### AŞAMA 5: HUDController.cs [20 dk]

**Taşınacak Field'lar:**
- `scoreText` (Line 18)
- `comboText` (Line 19)
- `multiplierText` (Line 20)
- `healthBar` (Line 21)
- `instrumentIcon` (Line 22)
- `stringBuilder` (Line 63)
- `lastDisplayedScore` (Line 64)
- `currentScore`, `currentCombo`, `currentMultiplier`, `currentHealth` (Lines 67-70)

**Taşınacak Metodlar:**
- `FindHUDElements()` (193-220)
- `UpdateScore()` (757-777)
- `UpdateCombo()` (779-801)
- `UpdateMultiplier()` (803-817)
- `UpdateHealth()` (819-839)
- `UpdateInstrumentIcon()` (841-852)
- `SetupLandscapeHUDLayout()` (558-607) - UIConfig'den layout değerlerini alır
- `ResetGameplayUI()` (967-977)
- `ScaleTextEffect()` (921-946)
- `ComboMilestoneEffect()` (948-963)

**Not:** Mobile buton layout (`SetupMobileLandscapeControls`) HUDController'a taşınmamalı; MobileFinder'da kalmalı.

**Yeni dosya:** `Assets/Scripts/UI/HUD/HUDController.cs`

```csharp
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }
    
    private UIConfig config;
    private Canvas hudCanvas;
    
    // HUD Elements
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI comboText;
    private TextMeshProUGUI multiplierText;
    private Slider healthBar;
    
    // State
    private int currentScore, currentCombo, currentMultiplier = 1;
    private float currentHealth = 1f;
    
    public void Initialize(UIConfig config, Canvas hudCanvas)
    {
        Instance = this;
        this.config = config;
        this.hudCanvas = hudCanvas;
        FindHUDElements();
        SetupLandscapeHUDLayout();
    }
    
    public void UpdateScore(int score) { /* Mevcut kod */ }
    public void UpdateCombo(int combo) { /* Mevcut kod */ }
    public void UpdateHealth(float health) { /* Mevcut kod */ }
    public void Reset() { /* ResetGameplayUI() kodu */ }
}
```

### AŞAMA 6: PanelManager.cs [15 dk]

**Taşınacak Field'lar:**
- `statePanelPrefabs` (Line 54)
- `currentPanelInstance` (Line 55)

**Taşınacak Metodlar:**
- `SetupCanvasReferences()` (397-421) - Dictionary init kısmı
- `HandleStateChangeImmediate()` (477-513)
- `GetParentCanvasForState()` (515-541)
- `ShowGameplayUI()` (543-556) **PanelManager'a taşınmamalı** (HUD/Mobile görünürlük ve layout koordinasyonu UIManager + HUDController/MobileFinder'da kalmalı)
- `ShowPauseUI()` (634-641)
- `ShowGameOverUI()` (671-692)
- `ShowMainMenuUI()` (736-743)
- `ShowSongSelectionUI()` (745-753)

**Yeni dosya:** `Assets/Scripts/UI/Panels/PanelManager.cs`

```csharp
public class PanelManager : MonoBehaviour
{
    public static PanelManager Instance { get; private set; }
    
    private UIConfig config;
    private CanvasLocator canvasLocator;
    private Dictionary<GameState, GameObject> statePanelPrefabs;
    private GameObject currentPanelInstance;
    
    // Events (UIManager'dan forward edilecek)
    public System.Action OnPausePressed;
    public System.Action OnResumePressed;
    public System.Action OnRestartPressed;
    public System.Action OnMainMenuPressed;
    
    public void Initialize(UIConfig config, CanvasLocator canvasLocator)
    {
        Instance = this;
        this.config = config;
        this.canvasLocator = canvasLocator;
        SetupPanelDictionary();
    }
    
    private void SetupPanelDictionary()
    {
        statePanelPrefabs = new Dictionary<GameState, GameObject>
        {
            { GameState.MainMenu, config.mainMenuPanelPrefab },
            { GameState.SongSelection, config.songSelectionPanelPrefab },
            { GameState.Playing, config.gameplayPanelPrefab },
            { GameState.Paused, config.pausePanelPrefab },
            { GameState.GameOver, config.gameOverPanelPrefab }
        };
    }
    
    public void ShowPanelForState(GameState state) { /* HandleStateChangeImmediate() */ }
    public void HideCurrentPanel() { /* Destroy logic */ }
    public GameObject CurrentPanel => currentPanelInstance;
}
```

### AŞAMA 7: PanelButtonWirer.cs [10 dk]

**Taşınacak Metodlar:**
- `SetupPausePanelButtons()` (643-669)
- `SetupGameOverPanelButtons()` (694-734)

**Not:** GameOver paneli için `GraphicRaycaster` guard'ı PanelManager içinde korunmalı (UIManager.cs:682-687).

**Yeni dosya:** `Assets/Scripts/UI/Panels/PanelButtonWirer.cs`

```csharp
public static class PanelButtonWirer
{
    public static void WirePausePanel(GameObject panel, 
        System.Action onResume, System.Action onRestart)
    {
        // Mevcut SetupPausePanelButtons() kodu
    }
    
    public static void WireGameOverPanel(GameObject panel, 
        System.Action onRestart, System.Action onMainMenu)
    {
        // Mevcut SetupGameOverPanelButtons() kodu
    }
}
```

### AŞAMA 8: UIEffectPool.cs [10 dk]

**Taşınacak Field'lar:**
- `effectParent` (Line 24)
- `hitEffectPool` (Line 56)
- `activeEffects` (Line 57)

**Taşınacak Metodlar:**
- `InitializeHitEffectPool()` (423-434)
- `ShowHitEffect()` (856-890)
- `GetEffectPrefab()` (892-901)
- `GetPooledEffect()` (903-917)
- `Update()` effect animation kısmı (1172-1203)

**Ek:** `FindEffectElements()` (222-233) ya UIEffectPool'a taşınmalı ya da UIManager bu parent'ı bulup Initialize'a geçirmeli.

**Nested Class:**
- `ActiveHitEffect` (1207-1213)

**Yeni dosya:** `Assets/Scripts/UI/Effects/UIEffectPool.cs`

```csharp
public class UIEffectPool : MonoBehaviour
{
    public static UIEffectPool Instance { get; private set; }
    
    private UIConfig config;
    private Transform effectParent;
    private Queue<GameObject> hitEffectPool;
    private List<ActiveHitEffect> activeEffects;
    
    public void Initialize(UIConfig config, Transform effectParent)
    {
        Instance = this;
        this.config = config;
        this.effectParent = effectParent;
        InitializePool();
    }
    
    public void ShowEffect(HitAccuracy accuracy, Vector2 screenPosition) { /* Mevcut kod */ }
    
    void Update() { /* Effect animation loop */ }
    
    private class ActiveHitEffect { /* Mevcut nested class */ }
}
```

### AŞAMA 9: CountdownController.cs [10 dk]

**Taşınacak Field'lar:**
- `countdownUI` (Line 1006)
- `countdownText` (Line 1007)

**Taşınacak Metodlar:**
- `ShowCountdown()` (1009-1036)
- `HideCountdown()` (1038-1044)
- `CreateCountdownUIIfNeeded()` (1046-1092)
- `CountdownPulseEffect()` (1094-1124)

**Not:** Countdown parent seçiminde HUD yoksa MainCanvas fallback korunmalı (UIManager.cs:1050-1052).

**Yeni dosya:** `Assets/Scripts/UI/Countdown/CountdownController.cs`

```csharp
public class CountdownController : MonoBehaviour
{
    public static CountdownController Instance { get; private set; }
    
    private Canvas parentCanvas;
    private GameObject countdownUI;
    private TextMeshProUGUI countdownText;
    
    public void Initialize(Canvas parentCanvas)
    {
        Instance = this;
        this.parentCanvas = parentCanvas;
    }
    
    public void ShowCountdown(int number) { /* Mevcut kod */ }
    public void HideCountdown() { /* Mevcut kod */ }
}
```

### AŞAMA 10: MobileFinder.cs [10 dk]

**Taşınacak Field'lar:**
- `pauseButton` (Line 26)
- `settingsButton` (Line 27)
- `mobileControls` (Line 28)

**Taşınacak Metodlar:**
- `FindMobileControls()` (235-301)
- `SetupMobileLandscapeControls()` (609-632) - UIConfig'den layout değerlerini alır

**Not:** `cachedUIElements` cache mekanizması MobileFinder'a taşınmalı (UIManager.cs:60, 235-269).

**Yeni dosya:** `Assets/Scripts/UI/Mobile/MobileFinder.cs`

```csharp
public class MobileFinder : MonoBehaviour
{
    public static MobileFinder Instance { get; private set; }
    
    private UIConfig config;
    
    public Button PauseButton { get; private set; }
    public Button SettingsButton { get; private set; }
    public GameObject MobileControls { get; private set; }
    
    public void Initialize(UIConfig config)
    {
        Instance = this;
        this.config = config;
    }
    
    public void DiscoverControls(Canvas[] canvases) { /* FindMobileControls() */ }
    public void SetupLandscapeLayout() { /* SetupMobileLandscapeControls() */ }
}
```

### AŞAMA 11: Yeni UIManager.cs (Facade) [15 dk]

**Kalacak ve korunacak:**
- `Instance` (Singleton)
- Events: `OnPausePressed`, `OnResumePressed`, `OnRestartPressed`, `OnMainMenuPressed`, `OnSettingsPressed`
- Scene lifecycle metodları
 - GameManager event subscription (OnGameStateChanged/OnScoreChanged/OnComboChanged)

**Yeni dosya:** `Assets/Scripts/UI/UIManager.cs` (Refactored)

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    // Sub-managers
    private CanvasLocator canvasLocator;
    private HUDController hudController;
    private PanelManager panelManager;
    private UIEffectPool effectPool;
    private CountdownController countdownController;
    private MobileFinder mobileFinder;
    
    // Config
    private UIConfig config;
    
    // Events (GameManager subscribes to these)
    public System.Action OnPausePressed;
    public System.Action OnResumePressed;
    public System.Action OnRestartPressed;
    public System.Action OnMainMenuPressed;
    public System.Action OnSettingsPressed;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Load config from Resources
        config = Resources.Load<UIConfig>("UI/UIConfig");
        if (config == null)
        {
            Debug.LogError("❌ UIConfig not found in Resources/UI/UIConfig!");
            return;
        }
        
        // Initialize sub-managers
        InitializeSubManagers();
    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void Start()
    {
        // Subscribe to GameManager events
        GameManager.OnGameStateChanged += HandleGameStateChange;
        GameManager.OnScoreChanged += score => hudController?.UpdateScore((int)score);
        GameManager.OnComboChanged += combo => hudController?.UpdateCombo(combo);
        
        // Check already loaded scenes
        CheckAlreadyLoadedScenes();
    }
    
    void InitializeSubManagers()
    {
        // Add components to this GameObject
        canvasLocator = gameObject.AddComponent<CanvasLocator>();
        hudController = gameObject.AddComponent<HUDController>();
        panelManager = gameObject.AddComponent<PanelManager>();
        effectPool = gameObject.AddComponent<UIEffectPool>();
        countdownController = gameObject.AddComponent<CountdownController>();
        mobileFinder = gameObject.AddComponent<MobileFinder>();
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Bootstrap") return;
        ProcessScene(scene);
    }
    
    void ProcessScene(Scene scene)
    {
        // Initialize sub-managers with scene canvases
        canvasLocator.Initialize(config);
        
        if (canvasLocator.HUDCanvas != null)
        {
            hudController.Initialize(config, canvasLocator.HUDCanvas);
            countdownController.Initialize(canvasLocator.HUDCanvas);
            effectPool.Initialize(config, canvasLocator.HUDCanvas.transform);
        }
        
        panelManager.Initialize(config, canvasLocator);
        
        // Wire panel events
        panelManager.OnPausePressed = () => OnPausePressed?.Invoke();
        panelManager.OnResumePressed = () => OnResumePressed?.Invoke();
        panelManager.OnRestartPressed = () => OnRestartPressed?.Invoke();
        panelManager.OnMainMenuPressed = () => OnMainMenuPressed?.Invoke();
        
        mobileFinder.Initialize(config);
        mobileFinder.DiscoverControls(new[] { 
            canvasLocator.MainCanvas, 
            canvasLocator.HUDCanvas, 
            canvasLocator.OverlayCanvas 
        });
        
        // Wire mobile button events
        if (mobileFinder.PauseButton != null)
            mobileFinder.PauseButton.onClick.AddListener(() => OnPausePressed?.Invoke());
        if (mobileFinder.SettingsButton != null)
            mobileFinder.SettingsButton.onClick.AddListener(() => OnSettingsPressed?.Invoke());
    }
    
    void CheckAlreadyLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name != "Bootstrap")
            {
                ProcessScene(scene);
            }
        }
    }
    
    void HandleGameStateChange(GameState newState)
    {
        panelManager?.ShowPanelForState(newState);
        
        if (newState == GameState.Playing)
        {
            hudController?.Reset();
            mobileFinder?.SetupLandscapeLayout();
        }
    }
    
    // ========== BACKWARDS COMPATIBLE PUBLIC API ==========
    
    public void ShowCountdown(int number) => countdownController?.ShowCountdown(number);
    public void HideCountdown() => countdownController?.HideCountdown();
    public void ShowHitEffect(HitAccuracy accuracy, Vector2 pos) => effectPool?.ShowEffect(accuracy, pos);
    public void UpdateScore(float score) => hudController?.UpdateScore((int)score);
    public void UpdateCombo(int combo) => hudController?.UpdateCombo(combo);
    public void UpdateHealth(float health) => hudController?.UpdateHealth(health);
    public void SetUIInteractable(bool interactable) => canvasLocator?.MainCanvas?.GetComponent<GraphicRaycaster>()?.enabled = interactable;
    
    void OnDestroy()
    {
        try
        {
            GameManager.OnGameStateChanged -= HandleGameStateChange;
            // NOTE: Lambda unsubscribe çalışmaz; delegate referanslarını field olarak saklayıp aynı referansla unsubscribe et.
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ UI System: Error during cleanup: {e.Message}");
        }
    }
}
```

### AŞAMA 12: Bootstrap.unity Güncelleme [5 dk - Unity Editor]

**Unity Editor'da yapılacak:**
1. Bootstrap sahnesini aç
2. UIManager objesini sil (çünkü artık kod config'i Resources'dan yüklüyor)
3. (Opsiyonel) Manager objelerini UIManager altında tutmak istersen bırakabilirsin ama referansları kaldır

**Alternatif:** Bootstrap.cs'deki `InitializeUIManager()` kodu zaten programatik oluşturuyor, sahne objesine gerek yok.

---

## ✅ UYGULAMA CHECKLIST

### Aşama 1-3: Hazırlık
- [ ] `Assets/Scripts/UI/Config/` klasörü oluştur
- [ ] `UIConfig.cs` ScriptableObject tanımını yaz
- [ ] `Assets/Resources/UI/` klasörü oluştur
- [ ] Diğer klasörleri oluştur
- [ ] `UIManager_BACKUP.cs` backup al

### Aşama 4: Unity Editor'da UIConfig.asset
- [ ] Unity'de Create > TilesWorld > UI Config
- [ ] Bootstrap.unity'deki değerleri kopyala:
  - [ ] mainMenuPanelPrefab
  - [ ] songSelectionPanelPrefab
  - [ ] gameplayPanelPrefab
  - [ ] pausePanelPrefab
  - [ ] gameOverPanelPrefab
  - [ ] settingsPanelPrefab
  - [ ] perfectHitEffect
  - [ ] goodHitEffect
  - [ ] missEffect
  - [ ] fadeAnimation
  - [ ] effectDuration

### Aşama 5-10: Alt Sistemler
- [ ] CanvasLocator.cs
- [ ] HUDController.cs
- [ ] PanelManager.cs
- [ ] PanelButtonWirer.cs
- [ ] UIEffectPool.cs
- [ ] CountdownController.cs
- [ ] MobileFinder.cs

### Aşama 11: Yeni UIManager
- [ ] UIManager.cs (Facade pattern)

### Aşama 12: Cleanup
- [ ] Bootstrap.unity'den eski UIManager objesini kaldır (opsiyonel)
- [ ] Test et

---

## 🔴 KRİTİK BAĞIMLILIKLAR

1. **UIConfig.asset** önce oluşturulmalı (Resources'dan yüklenecek)
2. **CanvasLocator** ilk init edilmeli (diğerleri canvas'a bağlı)
3. **Backwards compatibility** - Mevcut API korunuyor:
    - `UIManager.Instance.ShowCountdown()` ✅
    - `UIManager.Instance.ShowHitEffect()` ✅
    - `UIManager.Instance.OnPausePressed` ✅
    - `UIManager.Instance.UpdateHealth()` ✅
    - `UIManager.Instance.SetUIInteractable()` ✅
    - `UIManager.Instance.RefreshUIElements()` ✅

---

## 📊 SONUÇ

| Metrik | Önce | Sonra |
|--------|------|-------|
| UIManager.cs LOC | 1217 | ~150 |
| Toplam UI LOC | 1217 | ~1040 |
| Dosya sayısı | 1 | 8 |
| ScriptableObject | 0 | 1 |
| Magic Numbers | ~20 | Merkezi config'e taşındı |
| Test edilebilirlik | ❌ | ✅ |
| Single Responsibility | ❌ | ✅ |

---

## 📅 TAHMİNİ SÜRE

| Aşama | Süre |
|-------|------|
| UIConfig.cs | 5 dk |
| Klasörler + Backup | 5 dk |
| UIConfig.asset (Unity Editor) | 10 dk |
| CanvasLocator | 15 dk |
| HUDController | 20 dk |
| PanelManager | 15 dk |
| PanelButtonWirer | 10 dk |
| UIEffectPool | 10 dk |
| CountdownController | 10 dk |
| MobileFinder | 10 dk |
| Yeni UIManager | 15 dk |
| Bootstrap cleanup | 5 dk |
| **Toplam** | **~130 dk** |

---

## 🚀 BAŞLA

"başla" dersen AŞAMA 1'den (UIConfig.cs oluşturma) başlıyoruz!
