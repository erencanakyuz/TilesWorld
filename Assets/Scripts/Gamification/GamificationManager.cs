using UnityEngine;
using System;

/// <summary>
/// GamificationManager - Tüm Gamification Sistemlerinin Orkestratörü
/// Bootstrap'ta oluşturulur, tüm alt sistemleri yönetir ve koordine eder.
/// GameplayManager'dan gelen sonuçları ilgili sistemlere dağıtır.
/// </summary>
public class GamificationManager : MonoBehaviour
{
    public static GamificationManager Instance { get; private set; }

    /// <summary>Last processed song result (available for late subscribers)</summary>
    public SongResultPackage LastSongResult { get; private set; }

    #region Events
    /// <summary>Şarkı sonucu işlendikten sonra tetiklenir (UI gösteriminde kullanılır)</summary>
    public static event Action<SongResultPackage> OnSongResultProcessed;
    /// <summary>Bildirim gösterilmesi gerektiğinde tetiklenir</summary>
    public static event Action<GamificationNotification> OnNotification;
    #endregion

    [Header("🎮 Sub-System References")]
    [SerializeField] private PlayerProgressionSystem progressionSystem;
    [SerializeField] private WorldTourSystem worldTourSystem;
    [SerializeField] private ArtistBattleSystem artistBattleSystem;
    [SerializeField] private AchievementSystem achievementSystem;
    [SerializeField] private DailyChallengeSystem dailyChallengeSystem;

    [Header("⚔️ Active Battle State")]
    [SerializeField] private bool isInBattleMode = false;
    [SerializeField] private string activeBattleArtistId = "";

    [Header("🌍 Active Tour State")]
    [SerializeField] private bool isInTourMode = false;
    [SerializeField] private string activeTourCityId = "";
    [SerializeField] private int activeTourConcertIndex = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        EnsureSubSystemsExist();
        SubscribeToEvents();
    }

    /// <summary>
    /// Tüm alt sistemlerin mevcut olduğundan emin ol (Bootstrap sırasında çağrılır)
    /// </summary>
    public void EnsureSubSystemsExist()
    {
        if (progressionSystem == null)
            progressionSystem = FindOrCreate<PlayerProgressionSystem>("PlayerProgressionSystem");

        if (worldTourSystem == null)
            worldTourSystem = FindOrCreate<WorldTourSystem>("WorldTourSystem");

        if (artistBattleSystem == null)
            artistBattleSystem = FindOrCreate<ArtistBattleSystem>("ArtistBattleSystem");

        if (achievementSystem == null)
            achievementSystem = FindOrCreate<AchievementSystem>("AchievementSystem");

        if (dailyChallengeSystem == null)
            dailyChallengeSystem = FindOrCreate<DailyChallengeSystem>("DailyChallengeSystem");
    }

    private T FindOrCreate<T>(string name) where T : MonoBehaviour
    {
        T existing = FindFirstObjectByType<T>();
        if (existing != null) return existing;

        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        return go.AddComponent<T>();
    }

    private void SubscribeToEvents()
    {
        // Achievement notifications
        AchievementSystem.OnAchievementUnlocked += OnAchievementUnlocked;

        // Level up notifications
        PlayerProgressionSystem.OnLevelUp += OnPlayerLevelUp;
        PlayerProgressionSystem.OnRankChanged += OnPlayerRankChanged;

        // World tour notifications
        WorldTourSystem.OnCityCompleted += OnCityCompleted;
        WorldTourSystem.OnCityUnlocked += OnCityUnlocked;

        // Battle notifications
        ArtistBattleSystem.OnArtistDefeated += OnArtistDefeated;

        // Daily challenge notifications
        DailyChallengeSystem.OnChallengeCompleted += OnDailyChallengeCompleted;
        DailyChallengeSystem.OnWeeklyBonusClaimed += OnWeeklyBonus;
    }

    #region Song Result Processing

    /// <summary>
    /// Ana giriş noktası - Şarkı bittiğinde GameplayManager buraya çağrı yapar.
    /// Tüm alt sistemlere sonucu dağıtır ve toplam ödül paketini döndürür.
    /// </summary>
    public SongResultPackage ProcessSongEnd(GameplayStats stats, DifficultyLevel difficulty)
    {
        var package = new SongResultPackage();
        package.stats = stats;
        package.difficulty = difficulty;

        // 1. Process through PlayerProgressionSystem (XP, Currency, Stars)
        if (PlayerProgressionSystem.Instance != null)
        {
            package.reward = PlayerProgressionSystem.Instance.ProcessSongResult(stats, difficulty);
            package.newLevel = PlayerProgressionSystem.Instance.GetLevel();
            package.newXP = PlayerProgressionSystem.Instance.GetCurrentXP();
            package.newCurrency = PlayerProgressionSystem.Instance.GetCurrency();
            package.xpProgress = PlayerProgressionSystem.Instance.GetLevelProgress();
            package.rank = PlayerProgressionSystem.Instance.GetRank();
        }

        // 2. Check achievements
        if (AchievementSystem.Instance != null)
        {
            AchievementSystem.Instance.CheckSongAchievements(stats);
        }

        // 3. Report to daily challenges
        if (DailyChallengeSystem.Instance != null)
        {
            int starsEarned = package.reward?.starsEarned ?? 0;
            string artist = stats.artist ?? "Unknown";
            DailyChallengeSystem.Instance.ReportSongResult(stats, difficulty, starsEarned, artist);
        }

        // 4. If in World Tour mode, report to World Tour
        if (isInTourMode && WorldTourSystem.Instance != null)
        {
            int starsEarned = package.reward?.starsEarned ?? 0;
            WorldTourSystem.Instance.CompleteConcert(activeTourCityId, activeTourConcertIndex, starsEarned);
            package.tourCityId = activeTourCityId;
            package.tourConcertIndex = activeTourConcertIndex;
            package.isTourMode = true;
        }

        // 5. If in Artist Battle mode, evaluate battle
        if (isInBattleMode && ArtistBattleSystem.Instance != null)
        {
            package.battleResult = ArtistBattleSystem.Instance.EvaluateBattle(activeBattleArtistId, stats);
            package.isBattleMode = true;
        }

        // Reset active mode states
        isInBattleMode = false;
        isInTourMode = false;
        activeBattleArtistId = "";
        activeTourCityId = "";
        activeTourConcertIndex = -1;

        // Store and fire event for UI to display
        LastSongResult = package;
        OnSongResultProcessed?.Invoke(package);

        return package;
    }

    #endregion

    #region Mode Setters (Called before starting gameplay)

    /// <summary>
    /// World Tour modunda bir konser başlatmadan önce çağrılır
    /// </summary>
    public void SetTourMode(string cityId, int concertIndex)
    {
        isInTourMode = true;
        activeTourCityId = cityId;
        activeTourConcertIndex = concertIndex;
        isInBattleMode = false;
        Debug.Log($"🌍 Tour Mode Set: {cityId}, Concert #{concertIndex}");
    }

    /// <summary>
    /// Artist Battle modunda bir düello başlatmadan önce çağrılır
    /// </summary>
    public void SetBattleMode(string artistId)
    {
        isInBattleMode = true;
        activeBattleArtistId = artistId;
        isInTourMode = false;
        Debug.Log($"⚔️ Battle Mode Set: {artistId}");
    }

    /// <summary>
    /// Normal (freeplay) mod
    /// </summary>
    public void SetFreePlayMode()
    {
        isInBattleMode = false;
        isInTourMode = false;
        activeBattleArtistId = "";
        activeTourCityId = "";
        activeTourConcertIndex = -1;
    }

    public bool IsInBattleMode => isInBattleMode;
    public bool IsInTourMode => isInTourMode;
    public string ActiveBattleArtistId => activeBattleArtistId;

    #endregion

    #region Notification Handlers

    private void OnAchievementUnlocked(Achievement achievement)
    {
        OnNotification?.Invoke(new GamificationNotification
        {
            type = NotificationType.Achievement,
            title = "Başarım Açıldı!",
            message = $"{achievement.icon} {achievement.title}",
            subMessage = achievement.description,
            icon = achievement.icon
        });
    }

    private void OnPlayerLevelUp(int newLevel)
    {
        OnNotification?.Invoke(new GamificationNotification
        {
            type = NotificationType.LevelUp,
            title = "SEVİYE ATLADIN!",
            message = $"Seviye {newLevel}",
            subMessage = $"+50 💰 Ödül",
            icon = "⬆️"
        });
    }

    private void OnPlayerRankChanged(PlayerRank newRank)
    {
        OnNotification?.Invoke(new GamificationNotification
        {
            type = NotificationType.RankUp,
            title = "YENİ RÜTBE!",
            message = $"🏆 {newRank}",
            subMessage = "Yeni ünvan açıldı!",
            icon = "🏆"
        });
    }

    private void OnCityCompleted(TourCity city)
    {
        OnNotification?.Invoke(new GamificationNotification
        {
            type = NotificationType.CityCompleted,
            title = "ŞEHİR TAMAMLANDI!",
            message = $"{city.iconEmoji} {city.cityName}",
            subMessage = $"+{city.cityRewardXP} XP, +{city.cityRewardCurrency} 💰",
            icon = city.iconEmoji
        });
    }

    private void OnCityUnlocked(TourCity city)
    {
        OnNotification?.Invoke(new GamificationNotification
        {
            type = NotificationType.CityUnlocked,
            title = "YENİ ŞEHİR!",
            message = $"{city.iconEmoji} {city.cityName} açıldı!",
            subMessage = city.description,
            icon = city.iconEmoji
        });
    }

    private void OnArtistDefeated(ArtistProfile artist)
    {
        OnNotification?.Invoke(new GamificationNotification
        {
            type = NotificationType.ArtistDefeated,
            title = "DÜELLO KAZANILDI!",
            message = $"{artist.portraitEmoji} {artist.artistName}",
            subMessage = $"Ünvan: {artist.defeatTitle}",
            icon = "⚔️"
        });
    }

    private void OnDailyChallengeCompleted(DailyChallenge challenge)
    {
        OnNotification?.Invoke(new GamificationNotification
        {
            type = NotificationType.DailyChallenge,
            title = "GÖREV TAMAMLANDI!",
            message = $"{challenge.icon} {challenge.title}",
            subMessage = $"+{challenge.rewardXP} XP, +{challenge.rewardCurrency} 💰",
            icon = challenge.icon
        });
    }

    private void OnWeeklyBonus()
    {
        OnNotification?.Invoke(new GamificationNotification
        {
            type = NotificationType.WeeklyBonus,
            title = "HAFTALIK BONUS!",
            message = "7 gün boyunca tüm görevleri tamamladın!",
            subMessage = "+500 XP, +200 💰",
            icon = "🎉"
        });
    }

    #endregion

    #region Quick Access API

    /// <summary>Oyuncu profili bilgileri</summary>
    public PlayerProfile GetPlayerProfile() => PlayerProgressionSystem.Instance?.GetProfile();

    /// <summary>Oyuncu seviyesi</summary>
    public int GetPlayerLevel() => PlayerProgressionSystem.Instance?.GetLevel() ?? 1;

    /// <summary>Oyuncu parası</summary>
    public int GetPlayerCurrency() => PlayerProgressionSystem.Instance?.GetCurrency() ?? 0;

    /// <summary>Bugünkü görevler</summary>
    public System.Collections.Generic.List<DailyChallenge> GetDailyChallenges() =>
        DailyChallengeSystem.Instance?.GetTodayChallenges();

    /// <summary>Başarım tamamlanma yüzdesi</summary>
    public float GetAchievementCompletion() =>
        AchievementSystem.Instance?.GetCompletionPercentage() ?? 0f;

    /// <summary>Dünya turu ilerleme yüzdesi</summary>
    public float GetWorldTourProgress() =>
        WorldTourSystem.Instance?.GetTourProgress() ?? 0f;

    #endregion

    void OnDestroy()
    {
        if (Instance == this)
        {
            // Unsubscribe from events
            AchievementSystem.OnAchievementUnlocked -= OnAchievementUnlocked;
            PlayerProgressionSystem.OnLevelUp -= OnPlayerLevelUp;
            PlayerProgressionSystem.OnRankChanged -= OnPlayerRankChanged;
            WorldTourSystem.OnCityCompleted -= OnCityCompleted;
            WorldTourSystem.OnCityUnlocked -= OnCityUnlocked;
            ArtistBattleSystem.OnArtistDefeated -= OnArtistDefeated;
            DailyChallengeSystem.OnChallengeCompleted -= OnDailyChallengeCompleted;
            DailyChallengeSystem.OnWeeklyBonusClaimed -= OnWeeklyBonus;

            Instance = null;
        }
    }
}

#region Result & Notification Data

/// <summary>
/// Şarkı sonucunun tüm bilgilerini içeren paket.
/// UI tarafından gösterilmek üzere kullanılır.
/// </summary>
[System.Serializable]
public class SongResultPackage
{
    // Input
    public GameplayStats stats;
    public DifficultyLevel difficulty;

    // Progression rewards
    public SongResultReward reward;
    public int newLevel;
    public int newXP;
    public int newCurrency;
    public float xpProgress;
    public PlayerRank rank;

    // World Tour
    public bool isTourMode;
    public string tourCityId;
    public int tourConcertIndex;

    // Artist Battle
    public bool isBattleMode;
    public BattleResult battleResult;
}

/// <summary>
/// Oyun içi bildirimler için veri yapısı
/// </summary>
[System.Serializable]
public class GamificationNotification
{
    public NotificationType type;
    public string title;
    public string message;
    public string subMessage;
    public string icon;
}

public enum NotificationType
{
    Achievement,
    LevelUp,
    RankUp,
    CityCompleted,
    CityUnlocked,
    ArtistDefeated,
    DailyChallenge,
    WeeklyBonus
}

#endregion
