using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ArtistBattleSystem - Ünlü Bestecilere Karşı Düello
/// Oyuncu, Beethoven, Mozart, Bach gibi ünlü bestecilere karşı yarışır.
/// Her bestecinin kendi zorluk seviyesi, şarkıları ve özel yetenekleri var.
/// Bestecileri yenmek özel ödüller ve ünvanlar kazandırır.
/// </summary>
public class ArtistBattleSystem : MonoBehaviour
{
    public static ArtistBattleSystem Instance { get; private set; }

    #region Events
    public static event Action<ArtistProfile> OnBattleStarted;
    public static event Action<BattleResult> OnBattleCompleted;
    public static event Action<ArtistProfile> OnArtistDefeated;
    public static event Action<ArtistProfile> OnArtistMastered;
    public static event Action<string> OnSpecialTitleUnlocked;
    #endregion

    [Header("⚔️ Battle State")]
    [SerializeField] private ArtistBattleSaveData saveData;

    private List<ArtistProfile> allArtists;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);

        InitializeArtists();
        LoadProgress();
    }

    #region Artist Definitions

    private void InitializeArtists()
    {
        allArtists = new List<ArtistProfile>
        {
            // ===== TIER 1: Apprentice Level =====
            new ArtistProfile
            {
                artistId = "bach",
                artistName = "Johann Sebastian Bach",
                nickname = "Polifoni Ustası",
                era = "Barok (1685-1750)",
                description = "Karmaşık füg ve kontrapunk yapılarının tartışmasız ustası. Müzikal matematiğin babası.",
                portraitEmoji = "🎼",
                difficulty = ArtistDifficulty.Apprentice,
                requiredLevel = 3,
                specialAbility = "Kontrapunk Kalkanı",
                specialAbilityDesc = "Çok sesli bölümlerde ekstra puan kazanırsın",
                battleSongs = new List<string> { "minuet_bach", "air_on_a_g_string_bach", "noel_bach", "toccata_and_fugue_bach" },
                signatureSong = "toccata_and_fugue_bach",
                targetAccuracy = 60f,
                targetScore = 5000,
                defeatRewardXP = 400,
                defeatRewardCurrency = 150,
                defeatTitle = "Bach'ın Öğrencisi",
                masterTitle = "Barok Virtüözü",
                masterRequiresPerfect = true,
                funFact = "Bach, hayatında 1000'den fazla eser besteledi!"
            },

            new ArtistProfile
            {
                artistId = "pachelbel",
                artistName = "Johann Pachelbel",
                nickname = "Kanon Kralı",
                era = "Barok (1653-1706)",
                description = "Meşhur Canon in D ile tarihe geçen barok dönem bestecisi.",
                portraitEmoji = "🎵",
                difficulty = ArtistDifficulty.Apprentice,
                requiredLevel = 1,
                specialAbility = "Tekrar Ustası",
                specialAbilityDesc = "Tekrarlayan bölümlerde combo bonusu 2x",
                battleSongs = new List<string> { "cannon_pachelbel" },
                signatureSong = "cannon_pachelbel",
                targetAccuracy = 50f,
                targetScore = 3000,
                defeatRewardXP = 300,
                defeatRewardCurrency = 100,
                defeatTitle = "Kanon Çırak",
                masterTitle = "Kanon Ustası",
                masterRequiresPerfect = false,
                funFact = "Canon in D, dünyanın en çok çalınan düğün şarkısıdır!"
            },

            // ===== TIER 2: Student Level =====
            new ArtistProfile
            {
                artistId = "mozart",
                artistName = "Wolfgang Amadeus Mozart",
                nickname = "Müziğin Harika Çocuğu",
                era = "Klasik (1756-1791)",
                description = "5 yaşında beste yapan dahi. Zarif melodileri ve muazzam üretkenliği ile tanınır.",
                portraitEmoji = "👑",
                difficulty = ArtistDifficulty.Student,
                requiredLevel = 10,
                specialAbility = "Prodigy Hızı",
                specialAbilityDesc = "Hızlı bölümlerde nota hızı %15 yavaşlar",
                battleSongs = new List<string> { "turkish_delight_mozart", "sinfonia_40_mozart" },
                signatureSong = "sinfonia_40_mozart",
                targetAccuracy = 70f,
                targetScore = 8000,
                defeatRewardXP = 600,
                defeatRewardCurrency = 250,
                defeatTitle = "Mozart'ın Rakibi",
                masterTitle = "Klasik Dahi",
                masterRequiresPerfect = true,
                funFact = "Mozart, 35 yıllık kısa hayatında 600'den fazla eser besteledi!"
            },

            new ArtistProfile
            {
                artistId = "satie",
                artistName = "Erik Satie",
                nickname = "Sessizliğin Şairi",
                era = "Modern (1866-1925)",
                description = "Minimalizmin öncüsü. Basit ama derinden etkileyen melodiler.",
                portraitEmoji = "🌙",
                difficulty = ArtistDifficulty.Student,
                requiredLevel = 8,
                specialAbility = "Zen Modu",
                specialAbilityDesc = "Yavaş şarkılarda zamanlama penceresi genişler",
                battleSongs = new List<string> { "gymnopedie_no_1_erik_satie" },
                signatureSong = "gymnopedie_no_1_erik_satie",
                targetAccuracy = 75f,
                targetScore = 6000,
                defeatRewardXP = 500,
                defeatRewardCurrency = 200,
                defeatTitle = "Satie'nin Yoldaşı",
                masterTitle = "Minimalist Usta",
                masterRequiresPerfect = true,
                funFact = "Satie, aynı gömleğin 12 aynısından oluşan bir gardırobuna sahipti!"
            },

            // ===== TIER 3: Performer Level =====
            new ArtistProfile
            {
                artistId = "beethoven",
                artistName = "Ludwig van Beethoven",
                nickname = "Fırtınanın Efendisi",
                era = "Klasik-Romantik (1770-1827)",
                description = "Sağırlığına rağmen tarihin en güçlü müziğini yaratan titan. Duygusal yoğunluğun simgesi.",
                portraitEmoji = "⚡",
                difficulty = ArtistDifficulty.Performer,
                requiredLevel = 20,
                specialAbility = "İç Duyuş",
                specialAbilityDesc = "Miss'lerde combo sıfırlanmaz, sadece yarıya düşer",
                battleSongs = new List<string> { "fur_elise_beethoven", "moon_light_beethoven", "moonlight_sonata_op_27_no_2_beethoven" },
                signatureSong = "moonlight_sonata_op_27_no_2_beethoven",
                targetAccuracy = 80f,
                targetScore = 12000,
                defeatRewardXP = 800,
                defeatRewardCurrency = 350,
                defeatTitle = "Beethoven'a Meydan Okuyan",
                masterTitle = "Kader Senfonisinin Efendisi",
                masterRequiresPerfect = true,
                funFact = "Beethoven, son 10 yılını tamamen sağır olarak geçirdi ama en büyük eserlerini bu dönemde yazdı!"
            },

            new ArtistProfile
            {
                artistId = "brahms",
                artistName = "Johannes Brahms",
                nickname = "Akademik Romantik",
                era = "Romantik (1833-1897)",
                description = "Klasik yapıyı romantik tutkuyla birleştiren dev. Macar Dansları ile meşhur.",
                portraitEmoji = "🔥",
                difficulty = ArtistDifficulty.Performer,
                requiredLevel = 25,
                specialAbility = "Çift Vuruş",
                specialAbilityDesc = "Akor bölümlerinde puanlar 1.5x",
                battleSongs = new List<string> { "hungarian_danse_no_5_brahms" },
                signatureSong = "hungarian_danse_no_5_brahms",
                targetAccuracy = 75f,
                targetScore = 10000,
                defeatRewardXP = 700,
                defeatRewardCurrency = 300,
                defeatTitle = "Macar Dansçısı",
                masterTitle = "Romantik Titan",
                masterRequiresPerfect = true,
                funFact = "Brahms, ilk senfonisini yazmak için 21 yıl bekledi - mükemmeliyetçiliği efsaneydi!"
            },

            // ===== TIER 4: Virtuoso Level =====
            new ArtistProfile
            {
                artistId = "barrios",
                artistName = "Agustín Barrios",
                nickname = "Gitarın Paganini'si",
                era = "Modern (1885-1944)",
                description = "Paraguay'ın gitar dehası. Teknik ustalık ve derin duyguyu birleştiren efsane.",
                portraitEmoji = "🎸",
                difficulty = ArtistDifficulty.Virtuoso,
                requiredLevel = 35,
                specialAbility = "Parmak Fırtınası",
                specialAbilityDesc = "Süper hızlı bölümlerde zamanlama %20 esner",
                battleSongs = new List<string> { "cathedral_agustin_barrios", "the_bees_agustin_barrios" },
                signatureSong = "the_bees_agustin_barrios",
                targetAccuracy = 85f,
                targetScore = 15000,
                defeatRewardXP = 1000,
                defeatRewardCurrency = 500,
                defeatTitle = "Barrios'un Varisi",
                masterTitle = "Gitar Tanrısı",
                masterRequiresPerfect = true,
                funFact = "Barrios, konserlerinde yerlilerin geleneksel kıyafetlerini giyerek sahneye çıkardı!"
            },

            new ArtistProfile
            {
                artistId = "albeniz",
                artistName = "Isaac Albéniz",
                nickname = "İspanya'nın Sesi",
                era = "Romantik (1860-1909)",
                description = "İspanyol müziğini dünyaya tanıtan piyanist-besteci. Asturias ile ölümsüzleşti.",
                portraitEmoji = "🇪🇸",
                difficulty = ArtistDifficulty.Virtuoso,
                requiredLevel = 30,
                specialAbility = "Flamenco Ruhu",
                specialAbilityDesc = "Ritmik bölümlerde combo çarpanı ekstra 1x artar",
                battleSongs = new List<string> { "asturias_isaac_albeniz" },
                signatureSong = "asturias_isaac_albeniz",
                targetAccuracy = 80f,
                targetScore = 13000,
                defeatRewardXP = 900,
                defeatRewardCurrency = 400,
                defeatTitle = "Asturias Fatihi",
                masterTitle = "İspanyol Usta",
                masterRequiresPerfect = true,
                funFact = "Albéniz, 4 yaşında ilk konserini verdi!"
            },

            // ===== BOSS: The Ultimate Challenge =====
            new ArtistProfile
            {
                artistId = "liszt",
                artistName = "Franz Liszt",
                nickname = "Piyano Şeytanı",
                era = "Romantik (1811-1886)",
                description = "🔥 BOSS BATTLE! Tarihin en teknik piyanisti. Onu yenmek imkansız gibi görünüyor...",
                portraitEmoji = "😈",
                difficulty = ArtistDifficulty.Legendary,
                requiredLevel = 45,
                isBoss = true,
                specialAbility = "Şeytanın Trili",
                specialAbilityDesc = "Tüm zamanlama pencereleri %30 daralır!",
                battleSongs = new List<string> { "moonlight_sonata_op_27_no_2_beethoven", "sinfonia_40_mozart", "hungarian_danse_no_5_brahms" },
                signatureSong = "moonlight_sonata_op_27_no_2_beethoven",
                targetAccuracy = 90f,
                targetScore = 20000,
                defeatRewardXP = 2000,
                defeatRewardCurrency = 1000,
                defeatTitle = "Liszt'e Kafa Tutan",
                masterTitle = "Efsanevi Piyanist",
                masterRequiresPerfect = true,
                funFact = "Liszt o kadar popülerdi ki hayranları konserlerinde bayılırdı - tarihte ilk 'Beatlemania'!"
            }
        };
    }

    #endregion

    #region Battle System

    /// <summary>
    /// Tüm sanatçıları döndürür (UI için)
    /// </summary>
    public List<ArtistProfile> GetAllArtists() => allArtists;

    /// <summary>
    /// Sanatçının kilidinin açık olup olmadığını kontrol eder
    /// </summary>
    public bool IsArtistUnlocked(string artistId)
    {
        var artist = allArtists.FirstOrDefault(a => a.artistId == artistId);
        if (artist == null) return false;

        // Boss requires defeating all non-boss artists
        if (artist.isBoss)
        {
            return allArtists.Where(a => !a.isBoss).All(a => IsArtistDefeated(a.artistId));
        }

        int playerLevel = PlayerProgressionSystem.Instance?.GetLevel() ?? 1;
        return playerLevel >= artist.requiredLevel;
    }

    /// <summary>
    /// Sanatçının yenilip yenilmediğini kontrol eder
    /// </summary>
    public bool IsArtistDefeated(string artistId)
    {
        return saveData.defeatedArtists.Contains(artistId);
    }

    /// <summary>
    /// Sanatçıya ustalaşılıp ustalaşılmadığını kontrol eder (perfect beat)
    /// </summary>
    public bool IsArtistMastered(string artistId)
    {
        return saveData.masteredArtists.Contains(artistId);
    }

    /// <summary>
    /// Bir düello başlatır - sanatçının şarkılarından birini seçer
    /// </summary>
    public BattleSetup StartBattle(string artistId)
    {
        var artist = allArtists.FirstOrDefault(a => a.artistId == artistId);
        if (artist == null) return null;

        // Pick the next unbeaten song, or random if all beaten
        string selectedSong = artist.signatureSong;
        foreach (var song in artist.battleSongs)
        {
            string key = $"{artistId}_{song}";
            if (!saveData.completedBattleSongs.Contains(key))
            {
                selectedSong = song;
                break;
            }
        }

        var setup = new BattleSetup
        {
            artist = artist,
            songKey = selectedSong,
            targetAccuracy = artist.targetAccuracy,
            targetScore = artist.targetScore,
            specialAbilityActive = true
        };

        OnBattleStarted?.Invoke(artist);
        return setup;
    }

    /// <summary>
    /// Düello sonucunu değerlendirir
    /// </summary>
    public BattleResult EvaluateBattle(string artistId, GameplayStats stats)
    {
        var artist = allArtists.FirstOrDefault(a => a.artistId == artistId);
        if (artist == null) return null;

        var result = new BattleResult
        {
            artist = artist,
            playerAccuracy = stats.accuracy,
            playerScore = stats.totalScore,
            playerMaxCombo = stats.maxCombo,
            targetAccuracy = artist.targetAccuracy,
            targetScore = artist.targetScore
        };

        // Did the player win?
        result.isVictory = stats.accuracy >= artist.targetAccuracy && stats.totalScore >= artist.targetScore;

        // Check for mastery (perfect performance)
        result.isMastery = stats.accuracy >= 95f && stats.missedNotes == 0;

        if (result.isVictory)
        {
            result.xpReward = artist.defeatRewardXP;
            result.currencyReward = artist.defeatRewardCurrency;
            result.unlockedTitle = artist.defeatTitle;

            if (!saveData.defeatedArtists.Contains(artistId))
            {
                saveData.defeatedArtists.Add(artistId);
                saveData.totalBattleWins++;

                PlayerProgressionSystem.Instance?.GainXPDirect(result.xpReward);
                PlayerProgressionSystem.Instance?.GainCurrencyDirect(result.currencyReward);
                PlayerProgressionSystem.Instance?.GainXP("artist_battle_win");

                OnArtistDefeated?.Invoke(artist);
            }

            // Mark song as completed
            string songKey = $"{artistId}_{stats.songName}";
            if (!saveData.completedBattleSongs.Contains(songKey))
            {
                saveData.completedBattleSongs.Add(songKey);
            }

            // Check mastery
            if (result.isMastery && !saveData.masteredArtists.Contains(artistId))
            {
                saveData.masteredArtists.Add(artistId);
                result.unlockedTitle = artist.masterTitle;

                // Extra mastery rewards
                result.xpReward += artist.defeatRewardXP; // Double XP for mastery
                PlayerProgressionSystem.Instance?.GainXPDirect(artist.defeatRewardXP);

                OnArtistMastered?.Invoke(artist);
                OnSpecialTitleUnlocked?.Invoke(artist.masterTitle);
            }
        }
        else
        {
            saveData.totalBattleLosses++;
            result.xpReward = Mathf.RoundToInt(artist.defeatRewardXP * 0.1f); // Consolation XP
            PlayerProgressionSystem.Instance?.GainXPDirect(result.xpReward);
        }

        // Generate tips based on performance
        result.performanceTips = GeneratePerformanceTips(stats, artist);

        SaveProgress();
        OnBattleCompleted?.Invoke(result);
        return result;
    }

    private List<string> GeneratePerformanceTips(GameplayStats stats, ArtistProfile artist)
    {
        var tips = new List<string>();

        if (stats.accuracy < artist.targetAccuracy)
        {
            float gap = artist.targetAccuracy - stats.accuracy;
            if (gap > 20f)
                tips.Add($"Hedef doğruluk: %{artist.targetAccuracy:F0}. Daha fazla pratik yap!");
            else
                tips.Add($"Sadece %{gap:F0} daha fazla doğruluk gerekiyor. Neredeyse oradasın!");
        }

        if (stats.missedNotes > stats.totalNotesHit * 0.3f)
            tips.Add("Çok fazla nota kaçırıyorsun. Ritmi takip etmeye odaklan.");

        if (stats.maxCombo < 20)
            tips.Add("Combo zincirlerin kısa. Art arda isabetlere odaklan.");

        if (stats.perfectHits < stats.goodHits)
            tips.Add("İyi vuruşların fazla ama perfect'leri artırmaya çalış!");

        if (tips.Count == 0)
            tips.Add($"{artist.artistName} seni tebrik ediyor! Harika performans!");

        return tips;
    }

    /// <summary>
    /// Belirli bir besteciye karşı kazanma sayısı
    /// </summary>
    public int GetBattleWinsAgainst(string artistId)
    {
        return saveData.artistBattleHistory.GetValueOrDefault(artistId, 0);
    }

    /// <summary>
    /// Toplam düello istatistikleri
    /// </summary>
    public (int wins, int losses) GetTotalBattleStats()
    {
        return (saveData.totalBattleWins, saveData.totalBattleLosses);
    }

    #endregion

    #region Persistence

    private void LoadProgress()
    {
        string json = PlayerPrefs.GetString("ArtistBattleProgress", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                saveData = JsonUtility.FromJson<ArtistBattleSaveData>(json);
                saveData.RestoreDictionaries();
            }
            catch
            {
                saveData = new ArtistBattleSaveData();
            }
        }
        else
        {
            saveData = new ArtistBattleSaveData();
        }
    }

    private void SaveProgress()
    {
        saveData.PrepareSerialization();
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString("ArtistBattleProgress", json);
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

#region Artist Battle Data Classes

[Serializable]
public class ArtistProfile
{
    public string artistId;
    public string artistName;
    public string nickname;
    public string era;
    public string description;
    public string portraitEmoji;
    public ArtistDifficulty difficulty;
    public int requiredLevel;
    public bool isBoss = false;

    // Abilities
    public string specialAbility;
    public string specialAbilityDesc;

    // Songs
    public List<string> battleSongs = new List<string>();
    public string signatureSong;

    // Battle requirements
    public float targetAccuracy;
    public int targetScore;

    // Rewards
    public int defeatRewardXP;
    public int defeatRewardCurrency;
    public string defeatTitle;
    public string masterTitle;
    public bool masterRequiresPerfect;

    // Fun
    public string funFact;
}

public enum ArtistDifficulty
{
    Apprentice,     // Easy
    Student,        // Medium
    Performer,      // Hard
    Virtuoso,       // Very Hard
    Legendary       // Boss
}

[Serializable]
public class BattleSetup
{
    public ArtistProfile artist;
    public string songKey;
    public float targetAccuracy;
    public int targetScore;
    public bool specialAbilityActive;
}

[Serializable]
public class BattleResult
{
    public ArtistProfile artist;
    public float playerAccuracy;
    public int playerScore;
    public int playerMaxCombo;
    public float targetAccuracy;
    public int targetScore;

    public bool isVictory;
    public bool isMastery;
    public int xpReward;
    public int currencyReward;
    public string unlockedTitle;
    public List<string> performanceTips;
}

[Serializable]
public class ArtistBattleSaveData
{
    public List<string> defeatedArtists = new List<string>();
    public List<string> masteredArtists = new List<string>();
    public List<string> completedBattleSongs = new List<string>();
    public int totalBattleWins = 0;
    public int totalBattleLosses = 0;

    // Dictionary serialization
    [NonSerialized] public Dictionary<string, int> artistBattleHistory = new Dictionary<string, int>();
    public List<string> historyKeys = new List<string>();
    public List<int> historyValues = new List<int>();

    public void PrepareSerialization()
    {
        historyKeys.Clear();
        historyValues.Clear();
        if (artistBattleHistory != null)
        {
            foreach (var kvp in artistBattleHistory)
            {
                historyKeys.Add(kvp.Key);
                historyValues.Add(kvp.Value);
            }
        }
    }

    public void RestoreDictionaries()
    {
        artistBattleHistory = new Dictionary<string, int>();
        if (historyKeys != null && historyValues != null)
        {
            for (int i = 0; i < Mathf.Min(historyKeys.Count, historyValues.Count); i++)
            {
                artistBattleHistory[historyKeys[i]] = historyValues[i];
            }
        }
    }
}

#endregion
