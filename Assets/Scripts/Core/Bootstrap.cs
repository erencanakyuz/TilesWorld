using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    // Bu sahne, Build Settings'de MainScene'in bir üstündeki sahne olmalıdır.
    private const string TargetSceneName = "MainScene";

    [Header("🎵 Core Systems")]
    [SerializeField] private GameObject songDatabasePrefab;

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        // Ana sahnenin zaten yüklü olup olmadığını kontrol et.
        // Bu, geliştirme sırasında Bootstrap sahnesinden değil de doğrudan başka bir sahneden başlarsak diye bir önlem.
        if (SceneManager.GetSceneByName(TargetSceneName).isLoaded)
        {
            Debug.LogWarning($"'{TargetSceneName}' sahnesi zaten yüklü. Bootstrap işlemi atlanıyor.");

            // CRITICAL FIX: Sahne zaten yüklü olsa bile core sistemleri başlat!
            InitializeCoreSystemsFirst();

            // İsteğe bağlı olarak, bu objeyi yok edebiliriz çünkü görevi tamamlandı.
            Destroy(gameObject);
            return;
        }

        // Core sistemleri başlat
        InitializeCoreSystemsFirst();

        // Ana sahneyi yükle
        LoadTargetScene();
    }

    /// <summary>
    /// Sahne yüklenmeden önce temel sistemleri başlatır
    /// </summary>
    void InitializeCoreSystemsFirst()
    {
        // SongDatabase singleton'u oluştur
        InitializeSongDatabase();

        Debug.Log("🚀 Core systems initialized during bootstrap");
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

    private void LoadTargetScene()
    {
        // Debug.Log($"'{TargetSceneName}' sahnesi yükleniyor...");
        // Sahneyi asenkron olarak yükle, böylece oyun donmaz.
        SceneManager.LoadSceneAsync(TargetSceneName, LoadSceneMode.Additive);
    }
}