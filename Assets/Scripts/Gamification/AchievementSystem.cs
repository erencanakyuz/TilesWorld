using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AchievementSystem - Başarım & Rozet Sistemi
/// 50+ başarım ile oyuncuya sürekli hedefler sunar.
/// Kategoriler: Performans, Koleksiyon, Sosyal, Gizli
/// </summary>
public class AchievementSystem : MonoBehaviour
{
    public static AchievementSystem Instance { get; private set; }

    #region Events
    public static event Action<Achievement> OnAchievementUnlocked;
    public static event Action<Achievement, int, int> OnAchievementProgress; // achievement, current, target
    #endregion

    [Header("🏅 Achievement Data")]
    [SerializeField] private AchievementSaveData saveData;

    private List<Achievement> allAchievements;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);

        InitializeAchievements();
        LoadProgress();
    }

    void OnEnable()
    {
        // Subscribe to game events for automatic achievement tracking
        PlayerProgressionSystem.OnLevelUp += OnLevelUp;
        PlayerProgressionSystem.OnXPGained += OnXPGainedHandler;
        WorldTourSystem.OnCityCompleted += OnCityCompletedHandler;
        ArtistBattleSystem.OnArtistDefeated += OnArtistDefeatedHandler;
        ArtistBattleSystem.OnArtistMastered += OnArtistMasteredHandler;
        GameplayManager.OnGameplayEnded += CheckAllAchievements;
    }

    void OnDisable()
    {
        PlayerProgressionSystem.OnLevelUp -= OnLevelUp;
        PlayerProgressionSystem.OnXPGained -= OnXPGainedHandler;
        WorldTourSystem.OnCityCompleted -= OnCityCompletedHandler;
        ArtistBattleSystem.OnArtistDefeated -= OnArtistDefeatedHandler;
        ArtistBattleSystem.OnArtistMastered -= OnArtistMasteredHandler;
        GameplayManager.OnGameplayEnded -= CheckAllAchievements;
    }

    // Named handlers for proper unsubscription
    private void OnXPGainedHandler(int _) => CheckAllAchievements();
    private void OnCityCompletedHandler(TourCity _) => CheckAllAchievements();
    private void OnArtistDefeatedHandler(ArtistProfile _) => CheckAllAchievements();
    private void OnArtistMasteredHandler(ArtistProfile _) => CheckAllAchievements();

    private void OnLevelUp(int level) => CheckAllAchievements();

    #region Achievement Definitions

    private void InitializeAchievements()
    {
        allAchievements = new List<Achievement>
        {
            // ===== 🎵 PERFORMANCE ACHIEVEMENTS =====
            new Achievement("first_note", "İlk Nota", "İlk notanı çal", "🎵", AchievementCategory.Performance, 1, 10, 5),
            new Achievement("combo_10", "Combo Başlangıcı", "10'luk bir combo yap", "🔗", AchievementCategory.Performance, 10, 20, 10),
            new Achievement("combo_25", "Combo Ustası", "25'lik bir combo yap", "⛓️", AchievementCategory.Performance, 25, 50, 25),
            new Achievement("combo_50", "Combo Canavarı", "50'lik bir combo yap", "💥", AchievementCategory.Performance, 50, 100, 50),
            new Achievement("combo_100", "Combo Tanrısı", "100'lük bir combo yap", "👑", AchievementCategory.Performance, 100, 300, 100),
            new Achievement("combo_200", "Efsanevi Zincir", "200'lük bir combo yap", "🌟", AchievementCategory.Performance, 200, 500, 200),
            
            new Achievement("accuracy_80", "Hassas Parmaklar", "%80 doğruluk oranına ulaş", "🎯", AchievementCategory.Performance, 80, 30, 15),
            new Achievement("accuracy_90", "Lazer Hassasiyeti", "%90 doğruluk oranına ulaş", "💎", AchievementCategory.Performance, 90, 75, 35),
            new Achievement("accuracy_95", "Neredeyse Mükemmel", "%95 doğruluk oranına ulaş", "✨", AchievementCategory.Performance, 95, 150, 75),
            new Achievement("accuracy_100", "Kusursuz!", "%100 doğruluk oranına ulaş", "🏆", AchievementCategory.Performance, 100, 500, 200),

            new Achievement("perfect_100", "Yüz Perfect", "100 perfect hit yap (toplam)", "💯", AchievementCategory.Performance, 100, 50, 25),
            new Achievement("perfect_500", "Beşyüz Perfect", "500 perfect hit yap (toplam)", "⭐", AchievementCategory.Performance, 500, 100, 50),
            new Achievement("perfect_1000", "Bin Perfect", "1000 perfect hit yap (toplam)", "🌟", AchievementCategory.Performance, 1000, 250, 100),
            new Achievement("perfect_5000", "Altın Parmak", "5000 perfect hit yap (toplam)", "🥇", AchievementCategory.Performance, 5000, 500, 250),

            new Achievement("full_combo_1", "İlk Full Combo", "Bir şarkıyı sıfır miss ile bitir", "🎖️", AchievementCategory.Performance, 1, 100, 50),
            new Achievement("full_combo_5", "Full Combo Uzmanı", "5 şarkıyı full combo ile bitir", "🏅", AchievementCategory.Performance, 5, 200, 100),
            new Achievement("full_combo_10", "Demir İrade", "10 şarkıyı full combo ile bitir", "🎗️", AchievementCategory.Performance, 10, 400, 200),

            // ===== 📚 COLLECTION ACHIEVEMENTS =====
            new Achievement("songs_5", "Müzik Sever", "5 farklı şarkı çal", "📀", AchievementCategory.Collection, 5, 30, 15),
            new Achievement("songs_10", "Plak Koleksiyoncusu", "10 farklı şarkı çal", "💿", AchievementCategory.Collection, 10, 75, 35),
            new Achievement("songs_all", "Tam Koleksiyon", "Tüm şarkıları çal", "📚", AchievementCategory.Collection, 22, 300, 150),
            
            new Achievement("stars_25", "Yıldız Toplayıcı", "Toplam 25 yıldız kazan", "⭐", AchievementCategory.Collection, 25, 50, 25),
            new Achievement("stars_50", "Yıldız Avcısı", "Toplam 50 yıldız kazan", "🌟", AchievementCategory.Collection, 50, 100, 50),
            new Achievement("stars_100", "Galaksi", "Toplam 100 yıldız kazan", "🌌", AchievementCategory.Collection, 100, 250, 125),

            new Achievement("5star_1", "Beş Yıldız", "Bir şarkıdan 5 yıldız al", "⭐", AchievementCategory.Collection, 1, 75, 35),
            new Achievement("5star_5", "Beş Yıldız Uzmanı", "5 şarkıdan 5 yıldız al", "🌟", AchievementCategory.Collection, 5, 200, 100),
            new Achievement("5star_all", "Mükemmeliyetçi", "Tüm şarkılardan 5 yıldız al", "👑", AchievementCategory.Collection, 22, 1000, 500),

            // ===== 🌍 JOURNEY ACHIEVEMENTS =====
            new Achievement("city_1", "İlk Konser", "World Tour'da bir şehri tamamla", "🏙️", AchievementCategory.Journey, 1, 100, 50),
            new Achievement("city_3", "Avrupa Turu", "3 şehri tamamla", "🌍", AchievementCategory.Journey, 3, 200, 100),
            new Achievement("city_5", "Dünya Gezgini", "5 şehri tamamla", "🗺️", AchievementCategory.Journey, 5, 400, 200),
            new Achievement("city_all", "Dünya Turu Şampiyonu", "Tüm şehirleri tamamla", "🏆", AchievementCategory.Journey, 10, 1000, 500),

            new Achievement("battle_1", "İlk Düello", "Bir besteciye karşı kazan", "⚔️", AchievementCategory.Journey, 1, 100, 50),
            new Achievement("battle_5", "Düello Uzmanı", "5 besteciye karşı kazan", "🗡️", AchievementCategory.Journey, 5, 300, 150),
            new Achievement("battle_all", "Efsane Katili", "Tüm bestecileri yen", "👑", AchievementCategory.Journey, 9, 1000, 500),
            new Achievement("master_1", "İlk Ustalaşma", "Bir besteciye ustalaş", "🎓", AchievementCategory.Journey, 1, 200, 100),
            new Achievement("master_all", "Grand Master", "Tüm bestecilere ustalaş", "🏛️", AchievementCategory.Journey, 9, 2000, 1000),

            // ===== 📈 PROGRESSION ACHIEVEMENTS =====
            new Achievement("level_5", "Gelişen Piyanist", "Seviye 5'e ulaş", "📈", AchievementCategory.Progression, 5, 30, 15),
            new Achievement("level_10", "Yükselen Yıldız", "Seviye 10'a ulaş", "🌱", AchievementCategory.Progression, 10, 75, 35),
            new Achievement("level_25", "Deneyimli Müzisyen", "Seviye 25'e ulaş", "🎭", AchievementCategory.Progression, 25, 150, 75),
            new Achievement("level_50", "Usta Piyanist", "Seviye 50'ye ulaş", "🎹", AchievementCategory.Progression, 50, 400, 200),
            new Achievement("level_100", "Efsane", "Seviye 100'e ulaş", "🏆", AchievementCategory.Progression, 100, 1000, 500),

            new Achievement("streak_3", "Üç Gün Üst Üste", "3 gün üst üste giriş yap", "📅", AchievementCategory.Progression, 3, 25, 10),
            new Achievement("streak_7", "Haftalık Alışkanlık", "7 gün üst üste giriş yap", "📆", AchievementCategory.Progression, 7, 75, 35),
            new Achievement("streak_30", "Ay Boyunca", "30 gün üst üste giriş yap", "🗓️", AchievementCategory.Progression, 30, 300, 150),
            new Achievement("streak_100", "Vazgeçilmez", "100 gün üst üste giriş yap", "🔥", AchievementCategory.Progression, 100, 1000, 500),

            new Achievement("play_100", "Yüz Şarkı", "Toplam 100 şarkı çal", "🎶", AchievementCategory.Progression, 100, 150, 75),
            new Achievement("play_500", "Beş Yüz Şarkı", "Toplam 500 şarkı çal", "🎵", AchievementCategory.Progression, 500, 500, 250),

            // ===== 🔮 SECRET ACHIEVEMENTS =====
            new Achievement("secret_rio", "Karnaval Dansçısı", "Gizli Rio de Janeiro sahnesini aç", "🎭", AchievementCategory.Secret, 1, 300, 150, true),
            new Achievement("secret_liszt", "Şeytan Avcısı", "Boss Liszt'i yen", "😈", AchievementCategory.Secret, 1, 500, 250, true),
            new Achievement("secret_all_master", "Tanrısal Dokunuş", "Tüm bestecilere ve tüm şehirlere ustalaş", "✝️", AchievementCategory.Secret, 1, 2000, 1000, true),
            new Achievement("secret_speed", "Speed Runner", "Bir şarkıyı 30 saniyenin altında full combo yap", "⚡", AchievementCategory.Secret, 1, 300, 150, true),
            new Achievement("secret_perfect_streak", "Mükemmellik Serisi", "Ardışık 5 şarkıda %95+ doğruluk", "💫", AchievementCategory.Secret, 5, 400, 200, true),
        };
    }

    #endregion

    #region Achievement Checking

    /// <summary>
    /// Tüm başarımları kontrol eder - event'ler tetiklendiğinde çağrılır
    /// </summary>
    public void CheckAllAchievements()
    {
        var profile = PlayerProgressionSystem.Instance?.GetProfile();
        if (profile == null) return;

        foreach (var achievement in allAchievements)
        {
            if (IsUnlocked(achievement.id)) continue;

            int currentProgress = GetAchievementProgress(achievement, profile);
            
            // Update progress
            if (currentProgress > 0)
            {
                int oldProgress = saveData.achievementProgress.GetValueOrDefault(achievement.id, 0);
                if (currentProgress != oldProgress)
                {
                    saveData.achievementProgress[achievement.id] = currentProgress;
                    OnAchievementProgress?.Invoke(achievement, currentProgress, achievement.targetValue);
                }
            }

            // Check if completed
            if (currentProgress >= achievement.targetValue)
            {
                UnlockAchievement(achievement);
            }
        }
    }

    /// <summary>
    /// Tek bir şarkı sonucunu kontrol eder (GameplayManager'dan çağrılır)
    /// </summary>
    public void CheckSongAchievements(GameplayStats stats)
    {
        if (stats == null) return;

        // Combo achievements
        CheckProgressAchievement("combo_10", stats.maxCombo);
        CheckProgressAchievement("combo_25", stats.maxCombo);
        CheckProgressAchievement("combo_50", stats.maxCombo);
        CheckProgressAchievement("combo_100", stats.maxCombo);
        CheckProgressAchievement("combo_200", stats.maxCombo);

        // Accuracy achievements
        CheckProgressAchievement("accuracy_80", Mathf.RoundToInt(stats.accuracy));
        CheckProgressAchievement("accuracy_90", Mathf.RoundToInt(stats.accuracy));
        CheckProgressAchievement("accuracy_95", Mathf.RoundToInt(stats.accuracy));
        CheckProgressAchievement("accuracy_100", Mathf.RoundToInt(stats.accuracy));

        // Full combo
        if (stats.missedNotes == 0 && stats.totalNotesHit > 0)
        {
            int currentFullCombos = saveData.achievementProgress.GetValueOrDefault("full_combo_count", 0) + 1;
            saveData.achievementProgress["full_combo_count"] = currentFullCombos;
            CheckProgressAchievement("full_combo_1", currentFullCombos);
            CheckProgressAchievement("full_combo_5", currentFullCombos);
            CheckProgressAchievement("full_combo_10", currentFullCombos);
        }

        // First note
        if (stats.totalNotesHit > 0)
        {
            CheckProgressAchievement("first_note", 1);
        }

        // Track perfect streak
        if (stats.accuracy >= 95f)
        {
            int streak = saveData.achievementProgress.GetValueOrDefault("perfect_streak_current", 0) + 1;
            saveData.achievementProgress["perfect_streak_current"] = streak;
            CheckProgressAchievement("secret_perfect_streak", streak);
        }
        else
        {
            saveData.achievementProgress["perfect_streak_current"] = 0;
        }

        SaveProgress();
        CheckAllAchievements();
    }

    private int GetAchievementProgress(Achievement achievement, PlayerProfile profile)
    {
        return achievement.id switch
        {
            // Performance
            "first_note" => profile.totalNotesHit > 0 ? 1 : 0,
            "perfect_100" => profile.totalPerfects,
            "perfect_500" => profile.totalPerfects,
            "perfect_1000" => profile.totalPerfects,
            "perfect_5000" => profile.totalPerfects,

            // Collection
            "songs_5" or "songs_10" or "songs_all" => profile.songBestStars?.Count ?? 0,
            "stars_25" or "stars_50" or "stars_100" => PlayerProgressionSystem.Instance?.GetTotalStars() ?? 0,
            "5star_1" or "5star_5" or "5star_all" => profile.songBestStars?.Count(kvp => kvp.Value >= 5) ?? 0,

            // Journey
            "city_1" or "city_3" or "city_5" or "city_all" =>
                WorldTourSystem.Instance != null ? WorldTourSystem.Instance.GetAllCities().Count(c => WorldTourSystem.Instance.IsCityCompleted(c.cityId)) : 0,
            "battle_1" or "battle_5" or "battle_all" =>
                ArtistBattleSystem.Instance != null ? ArtistBattleSystem.Instance.GetAllArtists().Count(a => ArtistBattleSystem.Instance.IsArtistDefeated(a.artistId)) : 0,
            "master_1" or "master_all" =>
                ArtistBattleSystem.Instance != null ? ArtistBattleSystem.Instance.GetAllArtists().Count(a => ArtistBattleSystem.Instance.IsArtistMastered(a.artistId)) : 0,

            // Progression
            "level_5" or "level_10" or "level_25" or "level_50" or "level_100" => profile.level,
            "streak_3" or "streak_7" or "streak_30" or "streak_100" => profile.loginStreak,
            "play_100" or "play_500" => profile.totalSongsPlayed,

            // Secret
            "secret_liszt" => ArtistBattleSystem.Instance != null && ArtistBattleSystem.Instance.IsArtistDefeated("liszt") ? 1 : 0,
            "secret_rio" => WorldTourSystem.Instance != null && WorldTourSystem.Instance.IsCityCompleted("rio") ? 1 : 0,

            // Others tracked by saveData
            _ => saveData.achievementProgress.GetValueOrDefault(achievement.id, 0)
        };
    }

    private void CheckProgressAchievement(string id, int currentValue)
    {
        if (IsUnlocked(id)) return;

        var achievement = allAchievements.FirstOrDefault(a => a.id == id);
        if (achievement == null) return;

        saveData.achievementProgress[id] = Mathf.Max(
            currentValue,
            saveData.achievementProgress.GetValueOrDefault(id, 0)
        );

        if (currentValue >= achievement.targetValue)
        {
            UnlockAchievement(achievement);
        }
    }

    private void UnlockAchievement(Achievement achievement)
    {
        if (saveData.unlockedAchievements.Contains(achievement.id)) return;

        saveData.unlockedAchievements.Add(achievement.id);
        saveData.achievementProgress[achievement.id] = achievement.targetValue;

        // Award rewards
        PlayerProgressionSystem.Instance?.GainXPDirect(achievement.rewardXP);
        PlayerProgressionSystem.Instance?.GainCurrencyDirect(achievement.rewardCurrency);
        PlayerProgressionSystem.Instance?.GainXP("achievement_unlock");

        OnAchievementUnlocked?.Invoke(achievement);
        Debug.Log($"🏅 Achievement Unlocked: {achievement.icon} {achievement.title} - {achievement.description}");

        SaveProgress();
    }

    #endregion

    #region Public API

    public List<Achievement> GetAllAchievements() => allAchievements;

    public List<Achievement> GetAchievementsByCategory(AchievementCategory category)
    {
        return allAchievements.Where(a => a.category == category && (!a.isSecret || IsUnlocked(a.id))).ToList();
    }

    public bool IsUnlocked(string achievementId) => saveData.unlockedAchievements.Contains(achievementId);

    public int GetUnlockedCount() => saveData.unlockedAchievements.Count;

    public int GetTotalCount() => allAchievements.Count;

    public float GetCompletionPercentage()
    {
        return allAchievements.Count > 0 ? (float)saveData.unlockedAchievements.Count / allAchievements.Count * 100f : 0f;
    }

    public (int current, int target) GetProgress(string achievementId)
    {
        var achievement = allAchievements.FirstOrDefault(a => a.id == achievementId);
        if (achievement == null) return (0, 1);

        int current = saveData.achievementProgress.GetValueOrDefault(achievementId, 0);
        return (Mathf.Min(current, achievement.targetValue), achievement.targetValue);
    }

    public List<Achievement> GetRecentlyUnlocked(int count = 5)
    {
        // Return last N unlocked achievements
        var recentIds = saveData.unlockedAchievements.TakeLast(count);
        return allAchievements.Where(a => recentIds.Contains(a.id)).Reverse().ToList();
    }

    #endregion

    #region Persistence

    private void LoadProgress()
    {
        string json = PlayerPrefs.GetString("AchievementProgress", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                saveData = JsonUtility.FromJson<AchievementSaveData>(json);
                saveData.RestoreDictionaries();
            }
            catch
            {
                saveData = new AchievementSaveData();
            }
        }
        else
        {
            saveData = new AchievementSaveData();
        }
    }

    private void SaveProgress()
    {
        saveData.PrepareSerialization();
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString("AchievementProgress", json);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SaveProgress();
            PlayerPrefs.Save();
            Instance = null;
        }
    }

    #endregion
}

#region Achievement Data Classes

[Serializable]
public class Achievement
{
    public string id;
    public string title;
    public string description;
    public string icon;
    public AchievementCategory category;
    public int targetValue;
    public int rewardXP;
    public int rewardCurrency;
    public bool isSecret;

    public Achievement(string id, string title, string description, string icon,
        AchievementCategory category, int targetValue, int rewardXP, int rewardCurrency, bool isSecret = false)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.icon = icon;
        this.category = category;
        this.targetValue = targetValue;
        this.rewardXP = rewardXP;
        this.rewardCurrency = rewardCurrency;
        this.isSecret = isSecret;
    }
}

public enum AchievementCategory
{
    Performance,
    Collection,
    Journey,
    Progression,
    Secret
}

[Serializable]
public class AchievementSaveData
{
    public List<string> unlockedAchievements = new List<string>();

    // Dictionary serialization
    [NonSerialized] public Dictionary<string, int> achievementProgress = new Dictionary<string, int>();
    public List<string> progressKeys = new List<string>();
    public List<int> progressValues = new List<int>();

    public void PrepareSerialization()
    {
        progressKeys.Clear();
        progressValues.Clear();
        if (achievementProgress != null)
        {
            foreach (var kvp in achievementProgress)
            {
                progressKeys.Add(kvp.Key);
                progressValues.Add(kvp.Value);
            }
        }
    }

    public void RestoreDictionaries()
    {
        achievementProgress = new Dictionary<string, int>();
        if (progressKeys != null && progressValues != null)
        {
            for (int i = 0; i < Mathf.Min(progressKeys.Count, progressValues.Count); i++)
            {
                achievementProgress[progressKeys[i]] = progressValues[i];
            }
        }
    }
}

#endregion
