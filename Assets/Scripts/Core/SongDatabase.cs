using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// SongDatabase - Merkezi Şarkı Veritabanı Yöneticisi
/// songs_database.json dosyasını yükler ve runtime'da şarkı bilgilerini yönetir.
/// Singleton pattern ile erişim sağlar.
/// </summary>
public class SongDatabase : MonoBehaviour
{
    #region Singleton

    public static SongDatabase Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    [Header("🎵 Database Configuration")]
    [SerializeField] private string databasePath = "songs_database";
    [SerializeField] private bool enableDebugLogging = true;

    [Header("📊 Loaded Data")]
    [SerializeField] private List<SongDatabaseInfo> allSongs = new List<SongDatabaseInfo>();
    [SerializeField] private bool isLoaded = false;

    // Events
    public static System.Action OnDatabaseLoaded;
    public static System.Action<string> OnDatabaseError;

    #region Database Loading

    /// <summary>
    /// JSON veritabanını Resources klasöründen yükler
    /// </summary>
    void LoadDatabase()
    {
        try
        {
            // Resources/songs_database.json dosyasını yükle
            TextAsset jsonFile = Resources.Load<TextAsset>(databasePath);

            if (jsonFile == null)
            {
                LogError($"Database file not found: Resources/{databasePath}.json");
                OnDatabaseError?.Invoke($"Database file not found: {databasePath}");
                return;
            }

            // JSON'u parse et
            SongDatabaseListWrapper wrapper = JsonUtility.FromJson<SongDatabaseListWrapper>(jsonFile.text);

            if (wrapper == null || wrapper.Songs == null)
            {
                LogError("Failed to parse database JSON or Songs array is null");
                OnDatabaseError?.Invoke("Invalid database format");
                return;
            }

            // Şarkıları yükle
            allSongs = new List<SongDatabaseInfo>(wrapper.Songs);
            isLoaded = true;

            LogDebug($"✅ Database loaded successfully! {allSongs.Count} songs available.");

            // Şarkıları ID'ye göre sırala
            allSongs.Sort((a, b) => a.musicId.CompareTo(b.musicId));

            OnDatabaseLoaded?.Invoke();
        }
        catch (System.Exception e)
        {
            LogError($"Error loading database: {e.Message}\n{e.StackTrace}");
            OnDatabaseError?.Invoke($"Loading error: {e.Message}");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Tüm şarkıları döndürür
    /// </summary>
    public List<SongDatabaseInfo> GetAllSongs()
    {
        if (!isLoaded)
        {
            LogError("Database not loaded yet!");
            return new List<SongDatabaseInfo>();
        }

        return new List<SongDatabaseInfo>(allSongs);
    }

    /// <summary>
    /// Belirli bir ID'ye sahip şarkıyı döndürür
    /// </summary>
    public SongDatabaseInfo GetSongById(int musicId)
    {
        if (!isLoaded) return null;

        return allSongs.FirstOrDefault(song => song.musicId == musicId);
    }

    /// <summary>
    /// Şarkı adına göre arama yapar
    /// </summary>
    public SongDatabaseInfo GetSongByTitle(string title)
    {
        if (!isLoaded) return null;

        return allSongs.FirstOrDefault(song =>
            song.title.Equals(title, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Song key'e göre şarkı bulur
    /// </summary>
    public SongDatabaseInfo GetSongByKey(string songKey)
    {
        if (!isLoaded) return null;

        return allSongs.FirstOrDefault(song =>
            song.songKey.Equals(songKey, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Sanatçıya göre şarkıları filtreler
    /// </summary>
    public List<SongDatabaseInfo> GetSongsByArtist(string artist)
    {
        if (!isLoaded) return new List<SongDatabaseInfo>();

        return allSongs.Where(song =>
            song.artist.Equals(artist, System.StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Zorluk seviyesine göre şarkıları filtreler
    /// </summary>
    public List<SongDatabaseInfo> GetSongsByDifficulty(DifficultyLevel difficulty)
    {
        if (!isLoaded) return new List<SongDatabaseInfo>();

        return allSongs.Where(song => song.difficulty == difficulty).ToList();
    }

    /// <summary>
    /// Tempo aralığına göre şarkıları filtreler
    /// </summary>
    public List<SongDatabaseInfo> GetSongsByTempoRange(int minTempo, int maxTempo)
    {
        if (!isLoaded) return new List<SongDatabaseInfo>();

        return allSongs.Where(song => song.tempo >= minTempo && song.tempo <= maxTempo).ToList();
    }

    /// <summary>
    /// Veritabanının yüklenip yüklenmediğini kontrol eder
    /// </summary>
    public bool IsLoaded() => isLoaded;

    /// <summary>
    /// Toplam şarkı sayısını döndürür
    /// </summary>
    public int GetSongCount() => allSongs?.Count ?? 0;

    /// <summary>
    /// Difficulty dağılımını döndürür
    /// </summary>
    public Dictionary<DifficultyLevel, int> GetDifficultyDistribution()
    {
        if (!isLoaded) return new Dictionary<DifficultyLevel, int>();

        return allSongs.GroupBy(song => song.difficulty)
                      .ToDictionary(group => group.Key, group => group.Count());
    }

    /// <summary>
    /// Rastgele bir şarkı döndürür
    /// </summary>
    public SongDatabaseInfo GetRandomSong()
    {
        if (!isLoaded || allSongs.Count == 0) return null;

        int randomIndex = Random.Range(0, allSongs.Count);
        return allSongs[randomIndex];
    }

    /// <summary>
    /// Belirli zorlukta rastgele şarkı döndürür
    /// </summary>
    public SongDatabaseInfo GetRandomSongByDifficulty(DifficultyLevel difficulty)
    {
        List<SongDatabaseInfo> filteredSongs = GetSongsByDifficulty(difficulty);
        if (filteredSongs.Count == 0) return null;

        int randomIndex = Random.Range(0, filteredSongs.Count);
        return filteredSongs[randomIndex];
    }

    /// <summary>
    /// PLAN REQUIREMENT: Get tempo for a specific song by music ID
    /// </summary>
    public int GetTempoForSong(int musicId)
    {
        if (!isLoaded) return 120; // Default tempo

        var song = allSongs.FirstOrDefault(s => s.musicId == musicId);
        return song?.tempo ?? 120; // Return 120 BPM as default if not found
    }

    #endregion

    #region Debug & Logging

    void LogDebug(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[SongDatabase] {message}");
        }
    }

    void LogError(string message)
    {
        Debug.LogError($"[SongDatabase] {message}");
    }

    /// <summary>
    /// Veritabanı istatistiklerini console'a yazdırır
    /// </summary>
    [ContextMenu("Print Database Stats")]
    public void PrintDatabaseStats()
    {
        if (!isLoaded)
        {
            Debug.Log("📊 Database not loaded yet!");
            return;
        }

        Debug.Log("=== 🎵 SONG DATABASE STATS ===");
        Debug.Log($"📚 Total Songs: {allSongs.Count}");

        var difficultyStats = GetDifficultyDistribution();
        Debug.Log("🎯 Difficulty Distribution:");
        foreach (var kvp in difficultyStats)
        {
            Debug.Log($"   {kvp.Key}: {kvp.Value} songs");
        }

        var tempoStats = allSongs.GroupBy(s => s.tempo / 20 * 20) // Group by 20 BPM ranges
                               .OrderBy(g => g.Key)
                               .ToDictionary(g => g.Key, g => g.Count());

        Debug.Log("🎼 Tempo Distribution (by 20 BPM ranges):");
        foreach (var kvp in tempoStats)
        {
            Debug.Log($"   {kvp.Key}-{kvp.Key + 19} BPM: {kvp.Value} songs");
        }

        var artistStats = allSongs.GroupBy(s => s.artist)
                                .OrderByDescending(g => g.Count())
                                .Take(5)
                                .ToDictionary(g => g.Key, g => g.Count());

        Debug.Log("🎨 Top Artists:");
        foreach (var kvp in artistStats)
        {
            Debug.Log($"   {kvp.Key}: {kvp.Value} songs");
        }

        Debug.Log("===============================");
    }

    #endregion

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}