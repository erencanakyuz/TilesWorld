using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// DailyChallengeSystem - Günlük Görevler & Meydan Okumalar
/// Her gün 3 yeni görev oluşturur. Tamamlandığında XP ve para ödülü verir.
/// Haftada 7 gün tamamlanırsa haftalık büyük ödül!
/// </summary>
public class DailyChallengeSystem : MonoBehaviour
{
    public static DailyChallengeSystem Instance { get; private set; }

    #region Events
    public static event Action<DailyChallenge> OnChallengeCompleted;
    public static event Action<List<DailyChallenge>> OnNewDailyChallenges;
    public static event Action OnWeeklyBonusClaimed;
    #endregion

    [Header("📋 Daily Challenge State")]
    [SerializeField] private DailyChallengeSaveData saveData;

    // Today's challenges
    private List<DailyChallenge> todayChallenges = new List<DailyChallenge>();

    // Challenge templates
    private List<ChallengeTemplate> challengeTemplates;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);

        InitializeChallengeTemplates();
        LoadProgress();
        GenerateDailyChallenges();
    }

    #region Challenge Templates

    private void InitializeChallengeTemplates()
    {
        challengeTemplates = new List<ChallengeTemplate>
        {
            // Easy challenges
            new ChallengeTemplate
            {
                templateId = "play_songs",
                titleFormat = "{0} Şarkı Çal",
                descFormat = "{0} şarkı çalarak pratik yap",
                icon = "🎵",
                challengeType = ChallengeType.PlaySongs,
                minTarget = 1, maxTarget = 3,
                baseXP = 50, baseCurrency = 15,
                difficultyWeight = 1
            },
            new ChallengeTemplate
            {
                templateId = "hit_notes",
                titleFormat = "{0} Nota Vur",
                descFormat = "Toplam {0} nota vur",
                icon = "🎹",
                challengeType = ChallengeType.HitNotes,
                minTarget = 50, maxTarget = 200,
                baseXP = 40, baseCurrency = 10,
                difficultyWeight = 1
            },
            new ChallengeTemplate
            {
                templateId = "combo_reach",
                titleFormat = "{0}x Combo Ulaş",
                descFormat = "Tek bir şarkıda {0}'lük combo yap",
                icon = "🔗",
                challengeType = ChallengeType.ReachCombo,
                minTarget = 10, maxTarget = 50,
                baseXP = 60, baseCurrency = 20,
                difficultyWeight = 2
            },

            // Medium challenges
            new ChallengeTemplate
            {
                templateId = "accuracy_above",
                titleFormat = "%{0} Doğruluk",
                descFormat = "Bir şarkıda %{0} veya üzeri doğruluk elde et",
                icon = "🎯",
                challengeType = ChallengeType.AccuracyAbove,
                minTarget = 70, maxTarget = 90,
                baseXP = 75, baseCurrency = 25,
                difficultyWeight = 2
            },
            new ChallengeTemplate
            {
                templateId = "perfect_hits",
                titleFormat = "{0} Perfect Hit",
                descFormat = "Toplam {0} perfect hit yap",
                icon = "💎",
                challengeType = ChallengeType.PerfectHits,
                minTarget = 20, maxTarget = 100,
                baseXP = 70, baseCurrency = 25,
                difficultyWeight = 2
            },
            new ChallengeTemplate
            {
                templateId = "play_difficulty",
                titleFormat = "{0} Zorlukta Oyna",
                descFormat = "{0} veya üstü zorlukta bir şarkı tamamla",
                icon = "⚔️",
                challengeType = ChallengeType.PlayDifficulty,
                minTarget = 1, maxTarget = 3,
                baseXP = 80, baseCurrency = 30,
                difficultyWeight = 2
            },

            // Hard challenges
            new ChallengeTemplate
            {
                templateId = "no_miss",
                titleFormat = "Hatasız Çal",
                descFormat = "Bir şarkıyı sıfır miss ile bitir",
                icon = "✨",
                challengeType = ChallengeType.NoMiss,
                minTarget = 1, maxTarget = 1,
                baseXP = 100, baseCurrency = 40,
                difficultyWeight = 3
            },
            new ChallengeTemplate
            {
                templateId = "earn_stars",
                titleFormat = "{0} Yıldız Kazan",
                descFormat = "Bugün toplam {0} yıldız kazan",
                icon = "⭐",
                challengeType = ChallengeType.EarnStars,
                minTarget = 5, maxTarget = 15,
                baseXP = 90, baseCurrency = 35,
                difficultyWeight = 3
            },
            new ChallengeTemplate
            {
                templateId = "specific_artist",
                titleFormat = "{0} Çal",
                descFormat = "{0} bestecisinin bir şarkısını çal",
                icon = "🎼",
                challengeType = ChallengeType.PlaySpecificArtist,
                minTarget = 1, maxTarget = 1,
                baseXP = 60, baseCurrency = 20,
                difficultyWeight = 1
            },
        };
    }

    #endregion

    #region Daily Generation

    private void GenerateDailyChallenges()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        // If already generated today, restore
        if (saveData.lastGeneratedDate == today && saveData.todayChallengeIds.Count > 0)
        {
            RestoreTodayChallenges();
            return;
        }

        // Generate new challenges
        todayChallenges.Clear();
        saveData.todayChallengeIds.Clear();
        saveData.todayChallengeCompleted.Clear();
        saveData.todayChallengeProgress.Clear();

        // Use day as seed for deterministic randomness
        int daySeed = today.GetHashCode();
        var rng = new System.Random(daySeed);

        // Select 3 challenges with different difficulty weights
        var selectedTemplates = new List<ChallengeTemplate>();
        var shuffled = new List<ChallengeTemplate>(challengeTemplates);

        // Shuffle with seed
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // Pick one easy, one medium, one hard
        var easy = shuffled.FindAll(t => t.difficultyWeight == 1);
        var medium = shuffled.FindAll(t => t.difficultyWeight == 2);
        var hard = shuffled.FindAll(t => t.difficultyWeight == 3);

        if (easy.Count > 0) selectedTemplates.Add(easy[rng.Next(easy.Count)]);
        if (medium.Count > 0) selectedTemplates.Add(medium[rng.Next(medium.Count)]);
        if (hard.Count > 0) selectedTemplates.Add(hard[rng.Next(hard.Count)]);

        // Fill remaining slots
        while (selectedTemplates.Count < 3 && shuffled.Count > 0)
        {
            var template = shuffled[rng.Next(shuffled.Count)];
            if (!selectedTemplates.Contains(template))
                selectedTemplates.Add(template);
        }

        // Create actual challenges from templates
        string[] artists = { "Bach", "Mozart", "Beethoven", "Brahms", "Satie" };

        for (int i = 0; i < selectedTemplates.Count; i++)
        {
            var template = selectedTemplates[i];
            int target = rng.Next(template.minTarget, template.maxTarget + 1);

            string title, description;
            if (template.challengeType == ChallengeType.PlaySpecificArtist)
            {
                string artist = artists[rng.Next(artists.Length)];
                title = string.Format(template.titleFormat, artist);
                description = string.Format(template.descFormat, artist);
            }
            else
            {
                title = string.Format(template.titleFormat, target);
                description = string.Format(template.descFormat, target);
            }

            var challenge = new DailyChallenge
            {
                challengeId = $"daily_{today}_{i}",
                templateId = template.templateId,
                title = title,
                description = description,
                icon = template.icon,
                challengeType = template.challengeType,
                targetValue = target,
                currentProgress = 0,
                isCompleted = false,
                rewardXP = template.baseXP * template.difficultyWeight,
                rewardCurrency = template.baseCurrency * template.difficultyWeight
            };

            todayChallenges.Add(challenge);
            saveData.todayChallengeIds.Add(challenge.challengeId);
        }

        saveData.lastGeneratedDate = today;
        SaveProgress();

        OnNewDailyChallenges?.Invoke(todayChallenges);
        Debug.Log($"📋 Generated {todayChallenges.Count} daily challenges for {today}");
    }

    private void RestoreTodayChallenges()
    {
        // Challenges are regenerated with same seed, just restore progress
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        int daySeed = today.GetHashCode();
        var rng = new System.Random(daySeed);

        // Re-run same generation logic (deterministic with same seed)
        GenerateDailyChallengesWithSeed(rng, today);

        // Restore progress and completion status
        for (int i = 0; i < todayChallenges.Count; i++)
        {
            if (i < saveData.todayChallengeProgress.Count)
                todayChallenges[i].currentProgress = saveData.todayChallengeProgress[i];
            if (saveData.todayChallengeCompleted.Contains(todayChallenges[i].challengeId))
                todayChallenges[i].isCompleted = true;
        }
    }

    private void GenerateDailyChallengesWithSeed(System.Random rng, string today)
    {
        todayChallenges.Clear();

        var shuffled = new List<ChallengeTemplate>(challengeTemplates);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        var selectedTemplates = new List<ChallengeTemplate>();
        var easy = shuffled.FindAll(t => t.difficultyWeight == 1);
        var medium = shuffled.FindAll(t => t.difficultyWeight == 2);
        var hard = shuffled.FindAll(t => t.difficultyWeight == 3);

        if (easy.Count > 0) selectedTemplates.Add(easy[rng.Next(easy.Count)]);
        if (medium.Count > 0) selectedTemplates.Add(medium[rng.Next(medium.Count)]);
        if (hard.Count > 0) selectedTemplates.Add(hard[rng.Next(hard.Count)]);

        while (selectedTemplates.Count < 3 && shuffled.Count > 0)
        {
            var template = shuffled[rng.Next(shuffled.Count)];
            if (!selectedTemplates.Contains(template))
                selectedTemplates.Add(template);
        }

        string[] artists = { "Bach", "Mozart", "Beethoven", "Brahms", "Satie" };

        for (int i = 0; i < selectedTemplates.Count; i++)
        {
            var template = selectedTemplates[i];
            int target = rng.Next(template.minTarget, template.maxTarget + 1);
            string title, description;

            if (template.challengeType == ChallengeType.PlaySpecificArtist)
            {
                string artist = artists[rng.Next(artists.Length)];
                title = string.Format(template.titleFormat, artist);
                description = string.Format(template.descFormat, artist);
            }
            else
            {
                title = string.Format(template.titleFormat, target);
                description = string.Format(template.descFormat, target);
            }

            todayChallenges.Add(new DailyChallenge
            {
                challengeId = $"daily_{today}_{i}",
                templateId = template.templateId,
                title = title,
                description = description,
                icon = template.icon,
                challengeType = template.challengeType,
                targetValue = target,
                rewardXP = template.baseXP * template.difficultyWeight,
                rewardCurrency = template.baseCurrency * template.difficultyWeight
            });
        }
    }

    #endregion

    #region Progress Tracking

    /// <summary>
    /// Bir şarkı sonucunu günlük görevlere raporlar
    /// </summary>
    public void ReportSongResult(GameplayStats stats, DifficultyLevel difficulty, int starsEarned, string artist)
    {
        foreach (var challenge in todayChallenges)
        {
            if (challenge.isCompleted) continue;

            switch (challenge.challengeType)
            {
                case ChallengeType.PlaySongs:
                    challenge.currentProgress++;
                    break;

                case ChallengeType.HitNotes:
                    challenge.currentProgress += stats.totalNotesHit;
                    break;

                case ChallengeType.ReachCombo:
                    challenge.currentProgress = Mathf.Max(challenge.currentProgress, stats.maxCombo);
                    break;

                case ChallengeType.AccuracyAbove:
                    if (stats.accuracy >= challenge.targetValue)
                        challenge.currentProgress = challenge.targetValue;
                    break;

                case ChallengeType.PerfectHits:
                    challenge.currentProgress += stats.perfectHits;
                    break;

                case ChallengeType.PlayDifficulty:
                    if ((int)difficulty >= challenge.targetValue)
                        challenge.currentProgress = challenge.targetValue;
                    break;

                case ChallengeType.NoMiss:
                    if (stats.missedNotes == 0 && stats.totalNotesHit > 0)
                        challenge.currentProgress = 1;
                    break;

                case ChallengeType.EarnStars:
                    challenge.currentProgress += starsEarned;
                    break;

                case ChallengeType.PlaySpecificArtist:
                    if (challenge.title.Contains(artist))
                        challenge.currentProgress = 1;
                    break;
            }

            // Check completion
            if (challenge.currentProgress >= challenge.targetValue && !challenge.isCompleted)
            {
                CompleteChallenge(challenge);
            }
        }

        // Save progress
        saveData.todayChallengeProgress.Clear();
        foreach (var c in todayChallenges)
        {
            saveData.todayChallengeProgress.Add(c.currentProgress);
        }
        SaveProgress();
    }

    private void CompleteChallenge(DailyChallenge challenge)
    {
        challenge.isCompleted = true;
        saveData.todayChallengeCompleted.Add(challenge.challengeId);
        saveData.totalChallengesCompleted++;

        // Award rewards
        PlayerProgressionSystem.Instance?.GainXPDirect(challenge.rewardXP);
        PlayerProgressionSystem.Instance?.GainCurrencyDirect(challenge.rewardCurrency);
        PlayerProgressionSystem.Instance?.GainXP("daily_challenge_complete");

        OnChallengeCompleted?.Invoke(challenge);
        Debug.Log($"✅ Daily Challenge Complete: {challenge.icon} {challenge.title}");

        // Check if all today's challenges are complete
        if (todayChallenges.TrueForAll(c => c.isCompleted))
        {
            saveData.consecutiveDaysAllCompleted++;
            CheckWeeklyBonus();
        }

        SaveProgress();
    }

    private void CheckWeeklyBonus()
    {
        if (saveData.consecutiveDaysAllCompleted >= 7 && !saveData.weeklyBonusClaimed)
        {
            saveData.weeklyBonusClaimed = true;
            saveData.consecutiveDaysAllCompleted = 0;

            // Huge weekly bonus
            PlayerProgressionSystem.Instance?.GainXPDirect(500);
            PlayerProgressionSystem.Instance?.GainCurrencyDirect(200);

            OnWeeklyBonusClaimed?.Invoke();
            Debug.Log("🎉 WEEKLY BONUS CLAIMED! 500 XP + 200 💰");
        }
    }

    #endregion

    #region Public API

    public List<DailyChallenge> GetTodayChallenges() => todayChallenges;

    public int GetCompletedToday() => todayChallenges.FindAll(c => c.isCompleted).Count;

    public int GetTotalChallengesCompleted() => saveData.totalChallengesCompleted;

    public bool AreAllTodayCompleted() => todayChallenges.Count > 0 && todayChallenges.TrueForAll(c => c.isCompleted);

    public int GetConsecutiveDays() => saveData.consecutiveDaysAllCompleted;

    #endregion

    #region Persistence

    private void LoadProgress()
    {
        string json = PlayerPrefs.GetString("DailyChallengeProgress", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                saveData = JsonUtility.FromJson<DailyChallengeSaveData>(json);
            }
            catch
            {
                saveData = new DailyChallengeSaveData();
            }
        }
        else
        {
            saveData = new DailyChallengeSaveData();
        }
    }

    private void SaveProgress()
    {
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString("DailyChallengeProgress", json);
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

#region Daily Challenge Data Classes

[Serializable]
public class DailyChallenge
{
    public string challengeId;
    public string templateId;
    public string title;
    public string description;
    public string icon;
    public ChallengeType challengeType;
    public int targetValue;
    public int currentProgress;
    public bool isCompleted;
    public int rewardXP;
    public int rewardCurrency;
}

public enum ChallengeType
{
    PlaySongs,
    HitNotes,
    ReachCombo,
    AccuracyAbove,
    PerfectHits,
    PlayDifficulty,
    NoMiss,
    EarnStars,
    PlaySpecificArtist
}

[Serializable]
public class ChallengeTemplate
{
    public string templateId;
    public string titleFormat;
    public string descFormat;
    public string icon;
    public ChallengeType challengeType;
    public int minTarget;
    public int maxTarget;
    public int baseXP;
    public int baseCurrency;
    public int difficultyWeight; // 1=easy, 2=medium, 3=hard
}

[Serializable]
public class DailyChallengeSaveData
{
    public string lastGeneratedDate = "";
    public List<string> todayChallengeIds = new List<string>();
    public List<string> todayChallengeCompleted = new List<string>();
    public List<int> todayChallengeProgress = new List<int>();
    public int totalChallengesCompleted = 0;
    public int consecutiveDaysAllCompleted = 0;
    public bool weeklyBonusClaimed = false;
}

#endregion
