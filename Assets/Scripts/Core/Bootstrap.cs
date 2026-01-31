using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class Bootstrap : MonoBehaviour
{
    // Bu sahne, Build Settings'de MainScene'in bir üstündeki sahne olmalıdır.
    private const string TargetSceneName = "MainScene";

    [Header("🎵 Core Systems")]
    [SerializeField] private GameObject songDatabasePrefab;

    private static bool s_isInitialized = false;

    private void Awake()
    {
        if (s_isInitialized)
        {
            Destroy(gameObject);
            return;
        }

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0; // Mobile: VSync off for targetFrameRate to work

        // Bu objeyi sahneler arasında koru
        DontDestroyOnLoad(gameObject);
        s_isInitialized = true;

        // Core sistemleri başlat
        InitializeCoreSystemsFirst();

        // Ana sahnenin zaten yüklü olup olmadığını kontrol et.
        if (!SceneManager.GetSceneByName(TargetSceneName).isLoaded)
        {
            // Ana sahneyi yükle
            LoadTargetScene();
        }
    }

    void Start()
    {
        // Artık Awake'de yapıldığı için bu fonksiyon boş kalabilir
    }

    /// <summary>
    /// Sahne yüklenmeden önce temel sistemleri başlatır
    /// </summary>
    void InitializeCoreSystemsFirst()
    {
        // 1. Görsel ve Animasyon Altyapısı (En başta olmalı)
        InitializeDOTweenManager();

        // 2. Veri ve Ayarlar
        InitializeSongDatabase();

        // 3. Çekirdek Servisler (Veri'den sonra)
        InitializeAudioManager();
        InitializeInputManager();

        // 4. Müzik ve Oynanış Mantığı Sistemleri (Çekirdek servislerden sonra)
        InitializeMusicalIntegritySystem();
        InitializeInteractiveMusicSystem();

        // 5. UI Yöneticisi (Genellikle diğer yöneticilere bağlıdır)
        InitializeUIManager();

        // 6. UI Etkileşimi için EventSystem (UI'dan sonra, GameManager'dan önce)
        InitializeEventSystem();

        // 7. Ana Oyun Yöneticisi (TÜM diğer sistemlerden sonra!)
        InitializeGameManager();

        Debug.Log("🚀 Core systems initialized during bootstrap in a controlled order.");
    }

    /// <summary>
    /// SongDatabase singleton'unu başlatır
    /// </summary>
    void InitializeSongDatabase()
    {
        // Eğer zaten var olan bir SongDatabase instance'ı yoksa oluştur
        if (SongDatabase.Instance == null)
        {
            GameObject songDbObject = new GameObject("SongDatabase");
            songDbObject.AddComponent<SongDatabase>();

            Debug.Log("🎵 SongDatabase singleton created during bootstrap");
        }
        else
        {
            Debug.Log("🎵 SongDatabase already exists, skipping initialization");
        }
    }

    /// <summary>
    /// InputManager singleton'unu başlatır - CRITICAL for mobile touch!
    /// </summary>
    void InitializeInputManager()
    {
        // Eğer zaten var olan bir InputManager instance'ı yoksa oluştur
        if (InputManager.Instance == null)
        {
            GameObject inputMgrObject = new GameObject("InputManager");
            inputMgrObject.AddComponent<InputManager>();

            Debug.Log("🎮 InputManager singleton created during bootstrap");
        }
        else
        {
            Debug.Log("🎮 InputManager already exists, skipping initialization");
        }
    }

    /// <summary>
    /// AudioManager singleton'unu başlatır
    /// </summary>
    void InitializeAudioManager()
    {
        // Eğer zaten var olan bir AudioManager instance'ı yoksa oluştur
        if (AudioManager.Instance == null)
        {
            GameObject audioMgrObject = new GameObject("AudioManager");
            audioMgrObject.AddComponent<AudioManager>();

            Debug.Log("🔊 AudioManager singleton created during bootstrap");
        }
        else
        {
            Debug.Log("🔊 AudioManager already exists, skipping initialization");
        }
    }

    /// <summary>
    /// UIManager singleton'unu başlatır (GameManager'dan önce!)
    /// </summary>
    void InitializeUIManager()
    {
        // If an existing UIManager is already present in the scene hierarchy, don't create another.
        if (UIManager.Instance == null && FindFirstObjectByType<UIManager>() == null)
        {
            GameObject uiMgrObject = new GameObject("UIManager");
            uiMgrObject.AddComponent<UIManager>();

            Debug.Log("🎨 UIManager singleton created during bootstrap");
        }
        else
        {
            Debug.Log("🎨 UIManager already exists, skipping initialization");
        }
    }

    /// <summary>
    /// EventSystem oluştur (UI tıklamaları için CRITICAL!)
    /// </summary>
    void InitializeEventSystem()
    {
        // Eğer zaten var olan bir EventSystem instance'ı yoksa oluştur
        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // New Input System – required for mobile UI interaction
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            // Legacy Input Module (mouse/keyboard)
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif

            Debug.Log("🎮 EventSystem singleton created during bootstrap");
        }
        else
        {
            Debug.Log("🎮 EventSystem already exists, skipping initialization");
        }
    }

    /// <summary>
    /// DOTweenEnhancementManager singleton'unu başlatır (EN BAŞTA OLMALI!)
    /// </summary>
    void InitializeDOTweenManager()
    {
        if (DOTweenEnhancementManager.Instance == null)
        {
            GameObject dotweenMgrObject = new GameObject("DOTweenEnhancementManager");
            dotweenMgrObject.AddComponent<DOTweenEnhancementManager>();
            Debug.Log("🎨 DOTweenEnhancementManager singleton created during bootstrap");
        }
        else
        {
            Debug.Log("🎨 DOTweenEnhancementManager already exists, skipping initialization");
        }
    }

    /// <summary>
    /// MusicalIntegritySystem singleton'unu başlatır
    /// </summary>
    void InitializeMusicalIntegritySystem()
    {
        if (MusicalIntegritySystem.Instance == null)
        {
            GameObject musicalMgrObject = new GameObject("MusicalIntegritySystem");
            musicalMgrObject.AddComponent<MusicalIntegritySystem>();
            Debug.Log("🎼 MusicalIntegritySystem singleton created during bootstrap");
        }
        else
        {
            Debug.Log("🎼 MusicalIntegritySystem already exists, skipping initialization");
        }
    }

    /// <summary>
    /// InteractiveMusicSystem singleton'unu başlatır
    /// </summary>
    void InitializeInteractiveMusicSystem()
    {
        if (InteractiveMusicSystem.Instance == null)
        {
            GameObject interactiveMusicObject = new GameObject("InteractiveMusicSystem");
            interactiveMusicObject.AddComponent<InteractiveMusicSystem>();
            Debug.Log("🎵 InteractiveMusicSystem singleton created during bootstrap");
        }
        else
        {
            Debug.Log("🎵 InteractiveMusicSystem already exists, skipping initialization");
        }
    }

    /// <summary>
    /// GameManager singleton'unu başlatır (en son - tüm dependency'lerden sonra!)
    /// </summary>
    void InitializeGameManager()
    {
        // Eğer zaten var olan bir GameManager instance'ı yoksa oluştur
        if (GameManager.Instance == null)
        {
            GameObject gameMgrObject = new GameObject("GameManager");
            gameMgrObject.AddComponent<GameManager>();

            Debug.Log("🎮 GameManager singleton created during bootstrap");
        }
        else
        {
            Debug.Log("🎮 GameManager already exists, skipping initialization");
        }
    }

    private void LoadTargetScene()
    {
        // Debug.Log($"'{TargetSceneName}' sahnesi yükleniyor...");
        // Sahneyi asenkron olarak yükle, böylece oyun donmaz.
        SceneManager.LoadSceneAsync(TargetSceneName, LoadSceneMode.Additive);
    }
}
