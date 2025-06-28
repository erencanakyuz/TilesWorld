# Oyun Mimarisi ve Canvas Yönetim Sistemi

Bu doküman, "TilesWorld" projesinin sağlam, ölçeklenebilir ve bakımı kolay bir yapıya kavuşması için önerilen temel mimariyi ve UI yönetim felsefesini açıklamaktadır.

## Temel Felsefe

1.  **Sahnelerden Bağımsız Yöneticiler:** `GameManager` gibi temel sistemler, hangi sahnenin yüklü olduğunu bilmemeli veya ona bağımlı olmamalıdır. Onlar oyunun "beynidir" ve her zaman var olmalıdırlar.
2.  **Prefab Tabanlı UI:** Kullanıcı arayüzü (UI) panelleri, sahnelerin içine gömülü statik objeler olmamalıdır. Bunun yerine, ihtiyaç duyulduğunda kod tarafından yaratılıp (instantiate) yok edilen (destroy) **Prefab**'lar olmalıdır.
3.  **Global ve Yerel Ayrımı:** Bazı sistemler tüm oyun boyunca yaşarken (Global), bazıları sadece belirli sahnelerde (örn. oynanış sahnesi) var olmalıdır (Yerel).

## Önerilen Mimari: İki Katmanlı Sistem

Projemiz iki ana katmandan oluşacaktır:

### Katman 1: Global Yöneticiler (Bootstrap Sahnesi)

Bu, projenin temelini oluşturan katmandır.

-   **Yapı:** Projede `Bootstrap` veya `Initializer` adında **yeni bir sahne** oluşturulur. Bu sahne, Build Settings'de **en üstte (index 0)** yer alır ve oyun her açıldığında ilk olarak bu sahne yüklenir.
-   **İçerik:** Bu sahnede **sadece ve sadece** global yönetici objeleri bulunur.
    -   `GameManager`
    -   `AudioManager`
    -   `InputManager`
    -   `UIManager`
-   **İşleyiş:** `Bootstrap` sahnesi yüklendiğinde, içindeki tüm yöneticiler `Awake()` metodunda kendilerini `DontDestroyOnLoad` olarak işaretler. Ardından, bir `Bootstrap` script'i, oyunun asıl ilk sahnesini (örn. `MainMenu` sahnesi) yükler.
-   **Faydaları:**
    -   **Singleton Sorunu Çözülür:** "Manager objeleri kayboluyor" dediğiniz kendini yok etme mantığına artık gerek kalmaz. Çünkü bu sahne bir daha asla yüklenmeyeceği için yönetici kopyaları oluşmaz. Kod temizlenir.
    -   **Temiz Başlangıç:** Oyunun başlaması için gerekli her şeyin tek bir yerde ve tek bir seferde yüklendiğinden emin oluruz.

### Katman 2: Yerel Yöneticiler (Oyun Sahneleri)

Bunlar, `MainMenu`, `Level_01`, `Shop` gibi oyununuzun asıl sahneleridir.

-   **Yapı:** Her sahne, sadece o sahneye özgü mantığı içeren objeleri barındırır.
-   **İçerik:**
    -   Oynanış sahnesinde (`MainScene`): `GameplayManager`, `NoteRenderer`, `NoteContainer` gibi sadece oynanış sırasında gereken yöneticiler bulunur.
    -   Ana Menü sahnesinde: Sadece menü butonlarını yöneten bir `MainMenuManager` bulunabilir.
-   **İşleyiş:** Yerel yöneticiler, Global yöneticilere `GameManager.Instance.PauseGame()` gibi statik `Instance` referansları üzerinden erişir. Bir oyun sahnesi kapatıldığında, içindeki tüm yerel yöneticiler de onunla birlikte yok olur.

## UI Yönetimi: Prefab Tabanlı Sistem

Önceki tartışmamızı burada resmileştiriyoruz:

1.  **Tüm Paneller Prefab Olmalı:** `MainMenuPanel`, `PausePanel`, `SettingsPanel`, `GameOverPanel` gibi tüm tam ekran paneller, Hiyerarşi'den Project klasörüne sürüklenerek birer **Prefab** haline getirilmelidir.
2.  **Sahne Temizlenmeli:** Prefab yapıldıktan sonra bu paneller sahneden silinmelidir.
3.  **Referanslar Prefab'lara:** `UIManager`, sahnedeki objelere değil, bu **Prefab**'lara referans tutmalıdır.
4.  **`UIManager` Yaratır ve Yok Eder:** `UIManager`, `GameManager`'dan gelen `OnGameStateChanged` event'ini dinler. Yeni duruma göre, bir önceki paneli `Destroy()` ile yok eder ve yeni durumun panelini prefab'dan `Instantiate()` ile yaratır.
    -   **Geleceğe Hazırlık ("Farklı HUD'lar"):** Bu sistem sayesinde, farklı bölümler için farklı HUD prefab'ları oluşturabilir ve `UIManager`'ın o bölüme özel HUD'ı yaratmasını sağlayabilirsiniz. Mevcut yapı bunu mükemmel şekilde destekler.

---

## Mevcut `UIManager`'ın Detaylı Analizi

Yeni mimariye geçmeden önce, şu anki `UIManager.cs` script'inin nasıl çalıştığını, referanslarını ve potansiyel zayıflıklarını tam olarak anlayalım.

### 1. Singleton ve `DontDestroyOnLoad` Mekanizması

-   **Kod (`Awake` metodu):**
    ```csharp
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    ```
-   **Anlamı:** Bu kod, `UIManager` objesinin oyun boyunca **tekil** kalmasını ve sahne değişse bile **yok olmamasını** sağlar. Bu, global yöneticiler için istenen bir davranıştır.
-   **Kritik Zayıflık:** `UIManager` objesi hayatta kalırken, ona Inspector üzerinden atadığınız **sahnedeki diğer tüm objeler** (paneller, butonlar, text'ler) yeni bir sahne yüklendiğinde yok olur. Bu, `UIManager`'ın referanslarının "boşa düşmesine" (Missing Reference) ve `NullReferenceException` hatalarına yol açar. **Mevcut sistemin en temel sorunu budur.**

### 2. Serialize Edilmiş Alanlar (`[SerializeField]`)

Bu alanlar, `UIManager`'ın çalışmak için ihtiyaç duyduğu ve Inspector üzerinden **manuel olarak atanması gereken** parçalardır.

-   **`UI References` (Canvaslar):**
    -   `mainCanvas`, `overlayCanvas`, `hudCanvas`: Bunlar, UI sisteminin temelini oluşturan üç ana tuvaldir. Tüm diğer UI elemanları bu tuvallerin altında yer alır. `OverlayCanvas` şu an boş, ama olması gerektiği gibi tanımlanmış.

-   **`HUD Elements` (Oyun İçi Arayüz):**
    -   `scoreText`, `comboText`, `multiplierText`, `healthBar`: Bunlar, oyun sırasında oyuncunun gördüğü, sürekli güncellenen elemanlardır. Referansları olmadan `UIManager` skoru veya canı güncelleyemez.

-   **`Game State Panels` (Oyun Durumu Panelleri):**
    -   `mainMenuPanel`, `songSelectionPanel`, `gameplayPanel`, `pausePanel`, `gameOverPanel`, `settingsPanel`: Bunlar oyunun farklı durumlarında gösterilen ana panellerdir. `UIManager` bu referansları, `HandleGameStateChange` metodunda doğru paneli açıp kapatmak için kullanır. **Mevcut sistemde bu referanslar, sahne değiştiğinde kaybolacaktır.**

-   **`Audio Feedback UI` (Görsel Efektler):**
    -   `perfectHitEffect`, `goodHitEffect`, `missEffect`: Bunlar, sahnedeki objeler değil, Project klasöründeki **Prefab**'lar olmalıdır. `UIManager` bu prefab'ları kullanarak vuruş efektleri yaratır.
    -   `effectParent`: Yaratılan bu efektlerin hiyerarşide nerede duracağını belirleyen bir `Transform` referansıdır. Genellikle bu `OverlayCanvas` altında bir obje olur.

-   **`Mobile UI`:**
    -   `pauseButton`, `settingsButton`, `mobileControls`: Mobil cihaza özel kontroller için referanslar.

### 3. Çalışma Mantığı

-   **`Start()`:** Kod, Canvas'ları mobil için yapılandırır ve `GameManager` gibi diğer sistemlerden gelen olayları (`OnGameStateChanged`) dinlemeye başlar.
-   **`HandleGameStateChange(GameState newState)`:** `GameManager` oyun durumunu değiştirdiğinde bu metod tetiklenir. Metod, `statePanels` sözlüğünü kullanarak yeni duruma uygun paneli bulur ve `SetActive(true)` ile görünür hale getirir. Diğer panelleri de gizler.
-   **Zayıflık:** Bu mantık, **sadece tek bir sahne içinde** çalışır. `MainMenu`'den `MainScene`'e geçtiğiniz anda, `mainMenuPanel` referansı kaybolur ve bir daha geri dönemezsiniz veya `pausePanel` referansı hiç bulunamaz.

**Özetle:** Mevcut sistem, tek bir sahnede test amaçlı olarak işlev görebilir ancak çok sahneli, tam bir oyun için kesinlikle uygun değildir. Referansların sahne geçişlerinde kaybolması kaçınılmazdır. Bu yüzden **Prefab Tabanlı UI Sistemi** ve **Bootstrap Sahnesi** yaklaşımları bu sorunları kökünden çözmek için önerilmektedir.

---
Benim Teklif planım:

## Projenin Mevcut Durumu - Gerçek Analiz

### 🎯 Sahne İçeriği (MainScene.unity)
Mevcut `MainScene.unity` incelendiğinde şunlar görülüyor:

**✅ İyi Olan Yanlar:**
- **Oynanış Elemanları:** Lane_2, Lane_5 gibi oyun için gerekli 3D objeler sahne içinde doğru şekilde konumlandırılmış
- **UI Elemanları:** ComboText, ScoreText gibi temel UI elemanları mevcut ve çalışır durumda
- **Hiyerarşi Organizasyonu:** Objeler mantıklı şekilde organize edilmiş

**❌ Problemli Olan Yanlar:**
- **UI Referans Bağımlılığı:** Tüm UI panelleri ve elemanları doğrudan sahne içinde tanımlanmış
- **Manager Bağımlılığı:** UIManager bu sahnedeki objelere sıkı sıkıya bağlı

### 🎮 Manager Sistemleri - Detaylı Analiz

#### **GameManager.cs - Kapsamlı Analiz**
```csharp
// Mevcut Singleton Yapısı
if (Instance == null)
{
    Instance = this;
    DontDestroyOnLoad(gameObject);
    InitializeGameManager();
}
else
{
    Destroy(gameObject); // ← Bu satır, önerdiğimiz Bootstrap sisteminde gereksiz hale gelecek
}
```

**✅ Güçlü Yanları:**
- **Event Sistemi:** `OnGameStateChanged`, `OnScoreChanged`, `OnComboChanged` event'leri çok iyi tasarlanmış
- **State Management:** GameState enum'u ve değişim mantığı solid
- **Player Data:** PlayerPrefs ile veri kaydetme sistemi var ve çalışıyor
- **Cross-Manager Communication:** Diğer manager'lara temiz referanslar

**❌ İyileştirme Gereken Yanları:**
- **Fazla Sorumluluk:** Scene loading, player data, game session, core systems - çok fazla iş yapıyor
- **Inspector Bağımlılığı:** AudioManager, InputManager, UIManager referansları Inspector'dan atanması gerekiyor

#### **UIManager.cs - Kritik Sorunlar**
```csharp
[Header("🎮 Game State Panels")]
[SerializeField] private GameObject mainMenuPanel;      // ← Sahne objesi referansı
[SerializeField] private GameObject songSelectionPanel; // ← Sahne objesi referansı
[SerializeField] private GameObject gameplayPanel;      // ← Sahne objesi referansı
[SerializeField] private GameObject pausePanel;         // ← Sahne objesi referansı
[SerializeField] private GameObject gameOverPanel;      // ← Sahne objesi referansı
[SerializeField] private GameObject settingsPanel;      // ← Sahne objesi referansı
```

**🚨 Ana Problem:** Bu referanslar, MainScene'den başka bir sahne yüklendiğinde (`SceneManager.LoadScene()`) otomatik olarak `null` olacak ve UIManager çalışmayı durduracak.

**✅ İyi Tasarlanmış Kısımlar:**
- **Mobile UI Layout:** `SetupLandscapeHUDLayout()` ve `SetupMobileLandscapeControls()` methodları mobil için optimize edilmiş
- **Effect Pool System:** Hit effect'leri için object pooling sistemi var
- **Event Integration:** GameManager event'lerini dinliyor
- **Canvas Scaler Optimization:** Farklı cihazlar için responsive tasarım

#### **AudioManager.cs - En İyi Tasarlanmış Manager**
**✅ Mükemmel Özellikler:**
- **Object Pooling:** 100 AudioSource pool'u ile performans optimizasyonu
- **Mobile Optimizations:** Android/iOS için özel buffer ayarları
- **Resource Loading:** Dinamik audio dosyası yükleme sistemi
- **Latency Monitoring:** Gerçek zamanlı performans takibi

### 🎯 Mimari Değerlendirmesi

#### **Şu Anki Durumun Analizi:**
1. **Tek Sahne Testi:** Mevcut sistem MainScene içinde mükemmel çalışıyor
2. **Multi-Scene Problemi:** İkinci bir sahne (MainMenu) eklense, UIManager referansları kaybolacak
3. **Bootstrap İhtiyacı:** Editörde farklı sahnelerden test etmek şu an imkansız
4. **Manager Dependencies:** Manager'lar birbirine Inspector üzerinden bağlı

#### **Geçiş Planı Öncelik Sırası:**
1. **Bootstrap Sahnesi:** Global manager'lar için merkezi başlatma noktası
2. **UI Prefab Sistemi:** Panel'ları sahne bağımlılığından kurtarma
3. **Manager Sorumluluk Dağılımı:** GameManager'ın yükünü azaltma
4. **Reference Automation:** Inspector bağımlılığını kod ile otomatikleştirme

---

## İlk Tartışma Soruları (Güncel Bilgiler Işığında)

Bu detaylı analizden sonra, tekrar soruyorum:

1.  **Bootstrap Sahnesi Yaklaşımı:** Sadece global yöneticileri barındıran ve oyunun başlangıç noktası olan ayrı bir `Bootstrap` sahnesi oluşturma fikrini onaylıyor musunuz?
2.  **Prefab Tabanlı UI Sistemi:** `UIManager`'ın referanslarının sahne geçişlerinde kaybolması sorununu çözmek için, tüm UI panellerini prefab'a dönüştürüp, `UIManager`'ın bunları dinamik olarak yaratıp yok etmesi sistemine geçelim mi?
3.  **`GameManager`'ın Rolü:** `GameManager` bir "süper yönetici" mi olmalı, yoksa sadece oyun durumunu (GameState) ve oyuncu verilerini yöneten daha odaklı bir rolde mi kalmalı? (Örn: `noteSpeed` gibi ayarlar `GameplayManager`'da mı kalmalı, yoksa `GameManager`'a mı taşınmalı?)
4.  **Gelecek Planları:** "Farklı HUD'lar" gibi gelecekteki esneklik ihtiyaçları için, `UIManager`'ın farklı sahnelerde veya oyun modlarında farklı UI prefab setlerini (örn. `Level1_UI_Prefabs`, `BossFight_UI_Prefabs`) yönetebilmesini ister misiniz? 