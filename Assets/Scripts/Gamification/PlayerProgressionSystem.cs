using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// PlayerProgressionSystem - XP, Level, Currency & Rank sistemi
/// Oyuncunun ilerlemesini, seviyesini, para birimini ve rütbesini yönetir.
/// Tüm gamification sistemlerinin temel taşı.
/// </summary>
public class PlayerProgressionSystem : MonoBehaviour
{
    public static PlayerProgressionSystem Instance { get; private set; }

    #region Events
    public static event Action<int> OnLevelUp;
    public static event Action<int> OnXPGained;
    public static event Action<int> OnCurrencyChanged;
    public static event Action<PlayerRank> OnRankChanged;
    public static event Action<string> OnTitleUnlocked;
    #endregion

    #region Serialized Data
    [Header("📊 Player Progress")]
    [SerializeField] private PlayerProfile profile = new PlayerProfile();

    [Header("⚙️ XP Configuration")]
    [SerializeField] private int baseXPPerLevel = 500;
    [SerializeField] private float xpScalingFactor = 1.15f;
    [SerializeField] private int maxLevel = 100;
    #endregion

    #region XP Reward Table
    // XP rewards for different actions
    private static readonly Dictionary<string, int> XP_REWARDS = new Dictionary<string, int>
    {
        { "perfect_hit", 15 },
        { "good_hit", 8 },
        { "okay_hit", 3 },
        { "song_complete", 100 },
        { "full_combo", 500 },
        { "no_miss", 250 },
        { "combo_10", 20 },
        { "combo_25", 50 },
        { "combo_50", 100 },
        { "combo_100", 250 },
        { "daily_login", 50 },
        { "daily_challenge_complete", 200 },
        { "world_tour_city_complete", 300 },
        { "artist_battle_win", 400 },
        { "achievement_unlock", 150 },
        { "first_perfect_song", 1000 },
        { "master_difficulty_clear", 500 },
    };

    // Currency rewards
    private static readonly Dictionary<string, int> CURRENCY_REWARDS = new Dictionary<string, int>
    {
        { "song_complete", 25 },
        { "full_combo", 100 },
        { "daily_challenge_complete", 75 },
        { "world_tour_city_complete", 150 },
        { "artist_battle_win", 200 },
        { "level_up", 50 },
        { "achievement_unlock", 30 },
        { "daily_login", 10 },
        { "star_3", 50 },
        { "star_5", 100 },
    };
    #endregion

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Note: DontDestroyOnLoad handled by parent GamificationManager
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
        LoadProfile();
    }

    #region XP & Leveling

    /// <summary>
    /// XP kazandırır ve seviye atlama kontrolü yapar
    /// </summary>
    public void GainXP(string action)
    {
        if (!XP_REWARDS.TryGetValue(action, out int xpAmount)) return;
        GainXPDirect(xpAmount);
    }

    public void GainXPDirect(int amount)
    {
        if (amount <= 0) return;

        profile.totalXP += amount;
        profile.currentXP += amount;
        OnXPGained?.Invoke(amount);

        // Check for level up(s)
        while (profile.currentXP >= GetXPForNextLevel() && profile.level < maxLevel)
        {
            profile.currentXP -= GetXPForNextLevel();
            profile.level++;

            // Level up rewards
            GainCurrencyDirect(CURRENCY_REWARDS.GetValueOrDefault("level_up", 50));
            CheckRankPromotion();
            OnLevelUp?.Invoke(profile.level);

            Debug.Log($"🎉 LEVEL UP! Now Level {profile.level}");
        }

        SaveProfile();
    }

    /// <summary>
    /// Bir sonraki seviye için gereken XP miktarı
    /// </summary>
    public int GetXPForNextLevel()
    {
        return Mathf.RoundToInt(baseXPPerLevel * Mathf.Pow(xpScalingFactor, profile.level - 1));
    }

    /// <summary>
    /// Mevcut seviyedeki ilerleme yüzdesi (0-1)
    /// </summary>
    public float GetLevelProgress()
    {
        int needed = GetXPForNextLevel();
        return needed > 0 ? (float)profile.currentXP / needed : 0f;
    }

    #endregion

    #region Currency

    public void GainCurrency(string action)
    {
        if (!CURRENCY_REWARDS.TryGetValue(action, out int amount)) return;
        GainCurrencyDirect(amount);
    }

    public void GainCurrencyDirect(int amount)
    {
        if (amount <= 0) return;
        profile.currency += amount;
        profile.totalCurrencyEarned += amount;
        OnCurrencyChanged?.Invoke(profile.currency);
        SaveProfile();
    }

    public bool SpendCurrency(int amount)
    {
        if (amount <= 0 || profile.currency < amount) return false;
        profile.currency -= amount;
        OnCurrencyChanged?.Invoke(profile.currency);
        SaveProfile();
        return true;
    }

    #endregion

    #region Rank System

    private void CheckRankPromotion()
    {
        PlayerRank newRank = CalculateRank(profile.level);
        if (newRank != profile.rank)
        {
            PlayerRank oldRank = profile.rank;
            profile.rank = newRank;
            OnRankChanged?.Invoke(newRank);
            Debug.Log($"🏆 RANK UP! {oldRank} → {newRank}");

            // Unlock title for new rank
            string rankTitle = GetRankTitle(newRank);
            if (!profile.unlockedTitles.Contains(rankTitle))
            {
                profile.unlockedTitles.Add(rankTitle);
                OnTitleUnlocked?.Invoke(rankTitle);
            }
        }
    }

    private PlayerRank CalculateRank(int level)
    {
        if (level >= 90) return PlayerRank.Virtuoso;
        if (level >= 75) return PlayerRank.Maestro;
        if (level >= 60) return PlayerRank.Concertmaster;
        if (level >= 45) return PlayerRank.Soloist;
        if (level >= 35) return PlayerRank.Performer;
        if (level >= 25) return PlayerRank.Musician;
        if (level >= 15) return PlayerRank.Student;
        if (level >= 8) return PlayerRank.Apprentice;
        if (level >= 3) return PlayerRank.Beginner;
        return PlayerRank.Novice;
    }

    private string GetRankTitle(PlayerRank rank)
    {
        return rank switch
        {
            PlayerRank.Novice => "Yeni Başlayan",
            PlayerRank.Beginner => "Acemi Piyanist",
            PlayerRank.Apprentice => "Çırak",
            PlayerRank.Student => "Müzik Öğrencisi",
            PlayerRank.Musician => "Müzisyen",
            PlayerRank.Performer => "Sahne Sanatçısı",
            PlayerRank.Soloist => "Solist",
            PlayerRank.Concertmaster => "Konser Şefi",
            PlayerRank.Maestro => "Maestro",
            PlayerRank.Virtuoso => "Virtüöz",
            _ => "Bilinmeyen"
        };
    }

    #endregion

    #region Song Results Processing

    /// <summary>
    /// Şarkı bittiğinde çağrılır - tüm XP ve ödülleri hesaplar
    /// </summary>
    public SongResultReward ProcessSongResult(GameplayStats stats, DifficultyLevel difficulty)
    {
        var reward = new SongResultReward();

        // Base XP from hits
        reward.xpFromPerfects = stats.perfectHits * XP_REWARDS["perfect_hit"];
        reward.xpFromGoods = stats.goodHits * XP_REWARDS["good_hit"];
        reward.xpFromOkays = stats.okayHits * XP_REWARDS["okay_hit"];

        // Song completion bonus
        reward.xpFromCompletion = XP_REWARDS["song_complete"];

        // Full combo bonus
        if (stats.missedNotes == 0 && stats.totalNotesHit > 0)
        {
            reward.xpFromFullCombo = XP_REWARDS["full_combo"];
            reward.currencyBonus += CURRENCY_REWARDS["full_combo"];
            reward.isFullCombo = true;
        }

        // No miss bonus (not same as full combo, this means you hit every note but not necessarily perfectly)
        if (stats.missedNotes == 0)
        {
            reward.xpFromNoMiss = XP_REWARDS["no_miss"];
        }

        // Difficulty multiplier
        reward.difficultyMultiplier = GetDifficultyMultiplier(difficulty);

        // Calculate stars
        reward.starsEarned = CalculateStars(stats.accuracy, stats.missedNotes == 0);

        // Currency from stars
        if (reward.starsEarned >= 5) reward.currencyBonus += CURRENCY_REWARDS["star_5"];
        else if (reward.starsEarned >= 3) reward.currencyBonus += CURRENCY_REWARDS["star_3"];

        // Base currency
        reward.currencyBonus += CURRENCY_REWARDS["song_complete"];

        // Calculate totals
        int rawXP = reward.xpFromPerfects + reward.xpFromGoods + reward.xpFromOkays +
                    reward.xpFromCompletion + reward.xpFromFullCombo + reward.xpFromNoMiss;
        reward.totalXP = Mathf.RoundToInt(rawXP * reward.difficultyMultiplier);
        reward.totalCurrency = reward.currencyBonus;

        // Apply rewards
        GainXPDirect(reward.totalXP);
        GainCurrencyDirect(reward.totalCurrency);

        // Update stats
        profile.totalSongsPlayed++;
        profile.totalNotesHit += stats.totalNotesHit;
        profile.totalPerfects += stats.perfectHits;
        if (stats.maxCombo > profile.bestCombo) profile.bestCombo = stats.maxCombo;
        if (stats.accuracy > profile.bestAccuracy) profile.bestAccuracy = stats.accuracy;

        // Track best score per song (use songKey for consistency, fallback to songName)
        string songKey = !string.IsNullOrEmpty(stats.songKey) ? stats.songKey : (stats.songName ?? "unknown");
        if (!profile.songBestStars.ContainsKey(songKey) || profile.songBestStars[songKey] < reward.starsEarned)
        {
            profile.songBestStars[songKey] = reward.starsEarned;
        }

        SaveProfile();
        return reward;
    }

    private float GetDifficultyMultiplier(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => 1.0f,
            DifficultyLevel.Medium => 1.25f,
            DifficultyLevel.Hard => 1.5f,
            DifficultyLevel.Expert => 2.0f,
            DifficultyLevel.Master => 3.0f,
            _ => 1.0f
        };
    }

    private int CalculateStars(float accuracy, bool noMiss)
    {
        if (accuracy >= 98f && noMiss) return 5;
        if (accuracy >= 95f) return 5;
        if (accuracy >= 85f) return 4;
        if (accuracy >= 70f) return 3;
        if (accuracy >= 50f) return 2;
        if (accuracy >= 30f) return 1;
        return 0;
    }

    #endregion

    #region Profile Persistence

    private void LoadProfile()
    {
        string json = PlayerPrefs.GetString("PlayerProfile", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                profile = JsonUtility.FromJson<PlayerProfile>(json);
                // Restore dictionaries from serializable lists
                profile.RestoreDictionaries();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load profile: {e.Message}. Creating new profile.");
                profile = new PlayerProfile();
            }
        }

        // Check daily login
        CheckDailyLogin();
    }

    public void SaveProfile()
    {
        profile.PrepareSerialization();
        string json = JsonUtility.ToJson(profile);
        PlayerPrefs.SetString("PlayerProfile", json);
    }

    public void SaveToDisk()
    {
        SaveProfile();
        PlayerPrefs.Save();
    }

    private void CheckDailyLogin()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        if (profile.lastLoginDate != today)
        {
            // Check streak
            if (!string.IsNullOrEmpty(profile.lastLoginDate))
            {
                DateTime lastLogin = DateTime.Parse(profile.lastLoginDate);
                if ((DateTime.Now - lastLogin).TotalDays <= 1.5)
                {
                    profile.loginStreak++;
                }
                else
                {
                    profile.loginStreak = 1;
                }
            }
            else
            {
                profile.loginStreak = 1;
            }

            profile.lastLoginDate = today;
            profile.totalDaysPlayed++;

            // Daily login rewards (streak bonus!)
            int loginXP = XP_REWARDS["daily_login"] * Mathf.Min(profile.loginStreak, 7);
            int loginCurrency = CURRENCY_REWARDS["daily_login"] * Mathf.Min(profile.loginStreak, 7);
            GainXPDirect(loginXP);
            GainCurrencyDirect(loginCurrency);

            Debug.Log($"📅 Daily Login! Streak: {profile.loginStreak} days. Bonus XP: {loginXP}");
            SaveProfile();
        }
    }

    #endregion

    #region Public Getters

    public PlayerProfile GetProfile() => profile;
    public int GetLevel() => profile.level;
    public int GetCurrentXP() => profile.currentXP;
    public int GetTotalXP() => profile.totalXP;
    public int GetCurrency() => profile.currency;
    public PlayerRank GetRank() => profile.rank;
    public int GetLoginStreak() => profile.loginStreak;
    public int GetTotalSongsPlayed() => profile.totalSongsPlayed;
    public float GetBestAccuracy() => profile.bestAccuracy;
    public int GetBestCombo() => profile.bestCombo;
    public int GetStarsForSong(string songKey) => profile.songBestStars.GetValueOrDefault(songKey, 0);
    public int GetTotalStars()
    {
        int total = 0;
        foreach (var kvp in profile.songBestStars) total += kvp.Value;
        return total;
    }

    #endregion

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveToDisk();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SaveToDisk();
            Instance = null;
        }
    }
}

#region Data Classes

[Serializable]
public class PlayerProfile
{
    // Identity
    public string playerName = "Pianist";
    public PlayerRank rank = PlayerRank.Novice;
    public string activeTitle = "Yeni Başlayan";
    public List<string> unlockedTitles = new List<string> { "Yeni Başlayan" };

    // Leveling
    public int level = 1;
    public int currentXP = 0;
    public int totalXP = 0;

    // Currency
    public int currency = 0;
    public int totalCurrencyEarned = 0;

    // Statistics
    public int totalSongsPlayed = 0;
    public int totalNotesHit = 0;
    public int totalPerfects = 0;
    public int bestCombo = 0;
    public float bestAccuracy = 0f;
    public int totalDaysPlayed = 0;

    // Login tracking
    public string lastLoginDate = "";
    public int loginStreak = 0;

    // Song progress (song key -> best stars)
    [NonSerialized] public Dictionary<string, int> songBestStars = new Dictionary<string, int>();

    // Serializable version of dictionaries
    public List<string> songBestStarsKeys = new List<string>();
    public List<int> songBestStarsValues = new List<int>();

    // World Tour progress
    public int worldTourCurrentCity = 0;
    public List<string> completedCities = new List<string>();

    // Artist Battle progress
    public List<string> defeatedArtists = new List<string>();
    public int artistBattleWins = 0;
    public int artistBattleLosses = 0;

    public void PrepareSerialization()
    {
        songBestStarsKeys.Clear();
        songBestStarsValues.Clear();
        if (songBestStars != null)
        {
            foreach (var kvp in songBestStars)
            {
                songBestStarsKeys.Add(kvp.Key);
                songBestStarsValues.Add(kvp.Value);
            }
        }
    }

    public void RestoreDictionaries()
    {
        songBestStars = new Dictionary<string, int>();
        if (songBestStarsKeys != null && songBestStarsValues != null)
        {
            for (int i = 0; i < Mathf.Min(songBestStarsKeys.Count, songBestStarsValues.Count); i++)
            {
                songBestStars[songBestStarsKeys[i]] = songBestStarsValues[i];
            }
        }
    }
}

[Serializable]
public class SongResultReward
{
    public int xpFromPerfects;
    public int xpFromGoods;
    public int xpFromOkays;
    public int xpFromCompletion;
    public int xpFromFullCombo;
    public int xpFromNoMiss;
    public float difficultyMultiplier = 1f;
    public int totalXP;

    public int currencyBonus;
    public int totalCurrency;

    public int starsEarned;
    public bool isFullCombo;
    public bool isNewBestScore;

    public override string ToString()
    {
        return $"🎵 Result: {totalXP} XP, {totalCurrency} 💰, {starsEarned}⭐ {(isFullCombo ? "FULL COMBO!" : "")}";
    }
}

public enum PlayerRank
{
    Novice,         // Level 1-2
    Beginner,       // Level 3-7
    Apprentice,     // Level 8-14
    Student,        // Level 15-24
    Musician,       // Level 25-34
    Performer,      // Level 35-44
    Soloist,        // Level 45-59
    Concertmaster,  // Level 60-74
    Maestro,        // Level 75-89
    Virtuoso        // Level 90+
}

#endregion
