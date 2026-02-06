using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// WorldTourSystem - Dünya Turu Modu
/// Oyuncuyu farklı şehirlerde konser vermeye götürür.
/// Her şehrin kendi şarkı setlisti, koşulları ve ödülleri var.
/// Viyana → Paris → Londra → Roma → İstanbul → Tokyo → New York → Rio → Kahire → Sydney
/// </summary>
public class WorldTourSystem : MonoBehaviour
{
    public static WorldTourSystem Instance { get; private set; }

    #region Events
    public static event Action<TourCity> OnCityUnlocked;
    public static event Action<TourCity> OnCityCompleted;
    public static event Action<TourCity, int> OnConcertCompleted; // city, concertIndex
    public static event Action OnWorldTourCompleted;
    public static event Action<string> OnEncoreUnlocked;
    #endregion

    [Header("🌍 World Tour State")]
    [SerializeField] private int currentCityIndex = 0;
    [SerializeField] private WorldTourSaveData saveData;

    // All cities in the world tour
    private List<TourCity> tourCities;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);

        InitializeTourCities();
        LoadProgress();
    }

    #region City Definitions

    private void InitializeTourCities()
    {
        tourCities = new List<TourCity>
        {
            // ===== STAGE 1: Classical Europe =====
            new TourCity
            {
                cityId = "vienna",
                cityName = "Viyana",
                country = "Avusturya",
                description = "Klasik müziğin başkenti. Mozart ve Beethoven'ın izinde...",
                iconEmoji = "🏰",
                backgroundTheme = "classical_golden",
                requiredLevel = 1,
                concerts = new List<TourConcert>
                {
                    new TourConcert { concertName = "Sokak Performansı", songKeys = new List<string> { "fur_elise_beethoven" }, requiredStars = 0, venue = "Stephansplatz" },
                    new TourConcert { concertName = "Kahve Evi Konseri", songKeys = new List<string> { "moon_light_beethoven" }, requiredStars = 2, venue = "Café Central" },
                    new TourConcert { concertName = "Konser Salonu", songKeys = new List<string> { "moonlight_sonata_op_27_no_2_beethoven" }, requiredStars = 3, venue = "Musikverein" },
                },
                encoreSong = "moonlight_sonata_op_27_no_2_beethoven",
                cityRewardXP = 500,
                cityRewardCurrency = 200
            },
            new TourCity
            {
                cityId = "paris",
                cityName = "Paris",
                country = "Fransa",
                description = "Romantizmin kalbi. Satie'nin hayalperest dünyası...",
                iconEmoji = "🗼",
                backgroundTheme = "romantic_blue",
                requiredLevel = 5,
                concerts = new List<TourConcert>
                {
                    new TourConcert { concertName = "Seine Kenarı", songKeys = new List<string> { "gymnopedie_no_1_erik_satie" }, requiredStars = 0, venue = "Pont des Arts" },
                    new TourConcert { concertName = "Salon Konseri", songKeys = new List<string> { "salut_d`amour_elgar" }, requiredStars = 2, venue = "Salon Pleyel" },
                    new TourConcert { concertName = "Operada Gece", songKeys = new List<string> { "cannon_pachelbel" }, requiredStars = 3, venue = "Palais Garnier" },
                },
                encoreSong = "cannon_pachelbel",
                cityRewardXP = 600,
                cityRewardCurrency = 250
            },
            new TourCity
            {
                cityId = "london",
                cityName = "Londra",
                country = "İngiltere",
                description = "Kraliyet saraylarında yankılanan melodiler...",
                iconEmoji = "🎡",
                backgroundTheme = "royal_purple",
                requiredLevel = 10,
                concerts = new List<TourConcert>
                {
                    new TourConcert { concertName = "Hyde Park Açık Hava", songKeys = new List<string> { "greensleeves_anonymous" }, requiredStars = 0, venue = "Hyde Park" },
                    new TourConcert { concertName = "Kilise Konseri", songKeys = new List<string> { "air_on_a_g_string_bach" }, requiredStars = 2, venue = "Westminster Abbey" },
                    new TourConcert { concertName = "Royal Albert Hall", songKeys = new List<string> { "noel_bach" }, requiredStars = 3, venue = "Royal Albert Hall" },
                },
                encoreSong = "toccata_and_fugue_bach",
                cityRewardXP = 700,
                cityRewardCurrency = 300
            },

            // ===== STAGE 2: Mediterranean & South =====
            new TourCity
            {
                cityId = "rome",
                cityName = "Roma",
                country = "İtalya",
                description = "Antik güzellik ve İtalyan tutkusu...",
                iconEmoji = "🏛️",
                backgroundTheme = "warm_terracotta",
                requiredLevel = 15,
                concerts = new List<TourConcert>
                {
                    new TourConcert { concertName = "Piazza Navona", songKeys = new List<string> { "romance_tarrega" }, requiredStars = 0, venue = "Piazza Navona" },
                    new TourConcert { concertName = "Villa Konseri", songKeys = new List<string> { "vidalita_traditional" }, requiredStars = 2, venue = "Villa Borghese" },
                    new TourConcert { concertName = "Teatro dell'Opera", songKeys = new List<string> { "cathedral_agustin_barrios" }, requiredStars = 3, venue = "Teatro dell'Opera" },
                },
                encoreSong = "cathedral_agustin_barrios",
                cityRewardXP = 800,
                cityRewardCurrency = 350
            },
            new TourCity
            {
                cityId = "madrid",
                cityName = "Madrid",
                country = "İspanya",
                description = "Flamenco ateşi ve İspanyol gitarın büyüsü...",
                iconEmoji = "💃",
                backgroundTheme = "spanish_red",
                requiredLevel = 20,
                concerts = new List<TourConcert>
                {
                    new TourConcert { concertName = "Plaza Mayor", songKeys = new List<string> { "el_noi_de_la_mare_miguel_llobet" }, requiredStars = 0, venue = "Plaza Mayor" },
                    new TourConcert { concertName = "Flamenco Kulübü", songKeys = new List<string> { "asturias_isaac_albeniz" }, requiredStars = 2, venue = "Corral de la Morería" },
                    new TourConcert { concertName = "Palacio Real", songKeys = new List<string> { "the_bees_agustin_barrios" }, requiredStars = 3, venue = "Palacio Real" },
                },
                encoreSong = "asturias_isaac_albeniz",
                cityRewardXP = 900,
                cityRewardCurrency = 400
            },

            // ===== STAGE 3: Germanic Mastery =====
            new TourCity
            {
                cityId = "berlin",
                cityName = "Berlin",
                country = "Almanya",
                description = "Bach'ın mirası ve Alman hassasiyeti...",
                iconEmoji = "🏗️",
                backgroundTheme = "industrial_gray",
                requiredLevel = 25,
                concerts = new List<TourConcert>
                {
                    new TourConcert { concertName = "Brandenburg Kapısı", songKeys = new List<string> { "minuet_bach" }, requiredStars = 0, venue = "Brandenburger Tor" },
                    new TourConcert { concertName = "Kilise Orgatını", songKeys = new List<string> { "toccata_and_fugue_bach" }, requiredStars = 2, venue = "Berliner Dom" },
                    new TourConcert { concertName = "Filarmoni", songKeys = new List<string> { "hungarian_danse_no_5_brahms" }, requiredStars = 3, venue = "Berliner Philharmonie" },
                },
                encoreSong = "hungarian_danse_no_5_brahms",
                cityRewardXP = 1000,
                cityRewardCurrency = 450
            },

            // ===== STAGE 4: Istanbul - East Meets West =====
            new TourCity
            {
                cityId = "istanbul",
                cityName = "İstanbul",
                country = "Türkiye",
                description = "Doğu ve Batı'nın buluştuğu efsanevi şehir...",
                iconEmoji = "🕌",
                backgroundTheme = "ottoman_gold",
                requiredLevel = 30,
                concerts = new List<TourConcert>
                {
                    new TourConcert { concertName = "Galata Kulesi", songKeys = new List<string> { "the_entertainer_scott_joplin" }, requiredStars = 0, venue = "Galata Kulesi" },
                    new TourConcert { concertName = "Topkapı Sarayı", songKeys = new List<string> { "turkish_delight_mozart" }, requiredStars = 2, venue = "Topkapı Sarayı" },
                    new TourConcert { concertName = "Aya İrini", songKeys = new List<string> { "sinfonia_40_mozart" }, requiredStars = 3, venue = "Aya İrini" },
                },
                encoreSong = "sinfonia_40_mozart",
                cityRewardXP = 1200,
                cityRewardCurrency = 500
            },

            // ===== STAGE 5: Tokyo - Eastern Precision =====
            new TourCity
            {
                cityId = "tokyo",
                cityName = "Tokyo",
                country = "Japonya",
                description = "Mükemmelliğin peşinde, Japon hassasiyetiyle...",
                iconEmoji = "🗾",
                backgroundTheme = "cherry_blossom",
                requiredLevel = 35,
                concerts = new List<TourConcert>
                {
                    new TourConcert { concertName = "Sakura Parkı", songKeys = new List<string> { "gymnopedie_no_1_erik_satie" }, requiredStars = 0, venue = "Ueno Park" },
                    new TourConcert { concertName = "Tapınak Konseri", songKeys = new List<string> { "ciacona_s_l_weiss" }, requiredStars = 3, venue = "Meiji Shrine" },
                    new TourConcert { concertName = "Suntory Hall", songKeys = new List<string> { "moonlight_sonata_op_27_no_2_beethoven" }, requiredStars = 4, venue = "Suntory Hall" },
                },
                encoreSong = "moonlight_sonata_op_27_no_2_beethoven",
                cityRewardXP = 1500,
                cityRewardCurrency = 600
            },

            // ===== STAGE 6: New York - Grand Finale =====
            new TourCity
            {
                cityId = "new_york",
                cityName = "New York",
                country = "ABD",
                description = "Büyük sahne! Carnegie Hall'da son konser...",
                iconEmoji = "🗽",
                backgroundTheme = "manhattan_night",
                requiredLevel = 40,
                concerts = new List<TourConcert>
                {
                    new TourConcert { concertName = "Central Park", songKeys = new List<string> { "the_entertainer_scott_joplin" }, requiredStars = 0, venue = "Central Park" },
                    new TourConcert { concertName = "Lincoln Center", songKeys = new List<string> { "hungarian_danse_no_5_brahms" }, requiredStars = 3, venue = "Lincoln Center" },
                    new TourConcert { concertName = "Carnegie Hall", songKeys = new List<string> { "sinfonia_40_mozart" }, requiredStars = 4, venue = "Carnegie Hall" },
                },
                encoreSong = "sinfonia_40_mozart",
                cityRewardXP = 2000,
                cityRewardCurrency = 800
            },

            // ===== SECRET STAGE: Rio =====
            new TourCity
            {
                cityId = "rio",
                cityName = "Rio de Janeiro",
                country = "Brezilya",
                description = "🌟 Gizli Sahne! Latin ateşiyle son dans...",
                iconEmoji = "🏖️",
                backgroundTheme = "carnival_vibrant",
                requiredLevel = 50,
                isSecret = true,
                concerts = new List<TourConcert>
                {
                    new TourConcert { concertName = "Copacabana", songKeys = new List<string> { "vidalita_traditional", "romance_tarrega" }, requiredStars = 3, venue = "Copacabana Beach" },
                    new TourConcert { concertName = "Christ Redeemer", songKeys = new List<string> { "asturias_isaac_albeniz", "the_bees_agustin_barrios" }, requiredStars = 4, venue = "Cristo Redentor" },
                },
                encoreSong = "the_bees_agustin_barrios",
                cityRewardXP = 3000,
                cityRewardCurrency = 1000
            },
        };
    }

    #endregion

    #region Tour Progress

    /// <summary>
    /// Mevcut şehri döndürür
    /// </summary>
    public TourCity GetCurrentCity()
    {
        if (currentCityIndex < tourCities.Count)
            return tourCities[currentCityIndex];
        return null;
    }

    /// <summary>
    /// Tüm şehirleri döndürür (UI için)
    /// </summary>
    public List<TourCity> GetAllCities() => tourCities;

    /// <summary>
    /// Bir şehrin kilidinin açık olup olmadığını kontrol eder
    /// </summary>
    public bool IsCityUnlocked(string cityId)
    {
        var city = tourCities.FirstOrDefault(c => c.cityId == cityId);
        if (city == null) return false;

        // Secret city requires special unlock
        if (city.isSecret)
        {
            return saveData.completedCities.Count >= tourCities.Count(c => !c.isSecret) - 1;
        }

        // Normal city: check level and previous city completion
        int cityIndex = tourCities.IndexOf(city);
        if (cityIndex == 0) return true; // First city always unlocked

        int playerLevel = PlayerProgressionSystem.Instance?.GetLevel() ?? 1;
        if (playerLevel < city.requiredLevel) return false;

        // Previous non-secret city must be completed
        for (int i = cityIndex - 1; i >= 0; i--)
        {
            if (!tourCities[i].isSecret)
            {
                return saveData.completedCities.Contains(tourCities[i].cityId);
            }
        }
        return true;
    }

    /// <summary>
    /// Bir konser tamamlandığında çağrılır
    /// </summary>
    public void CompleteConcert(string cityId, int concertIndex, int starsEarned)
    {
        var city = tourCities.FirstOrDefault(c => c.cityId == cityId);
        if (city == null) return;

        if (concertIndex < 0 || concertIndex >= city.concerts.Count) return;

        string concertKey = $"{cityId}_{concertIndex}";

        // Update best stars
        if (!saveData.concertStars.ContainsKey(concertKey) || saveData.concertStars[concertKey] < starsEarned)
        {
            saveData.concertStars[concertKey] = starsEarned;
        }

        // Mark as completed
        if (!saveData.completedConcerts.Contains(concertKey))
        {
            saveData.completedConcerts.Add(concertKey);
            OnConcertCompleted?.Invoke(city, concertIndex);

            // XP reward
            PlayerProgressionSystem.Instance?.GainXP("world_tour_city_complete");
        }

        // Check if all concerts in this city are completed
        CheckCityCompletion(city);

        SaveProgress();
    }

    private void CheckCityCompletion(TourCity city)
    {
        bool allCompleted = true;
        for (int i = 0; i < city.concerts.Count; i++)
        {
            string concertKey = $"{city.cityId}_{i}";
            if (!saveData.completedConcerts.Contains(concertKey))
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted && !saveData.completedCities.Contains(city.cityId))
        {
            saveData.completedCities.Add(city.cityId);

            // Award city completion rewards
            PlayerProgressionSystem.Instance?.GainXPDirect(city.cityRewardXP);
            PlayerProgressionSystem.Instance?.GainCurrencyDirect(city.cityRewardCurrency);

            OnCityCompleted?.Invoke(city);

            // Unlock encore
            OnEncoreUnlocked?.Invoke(city.encoreSong);

            // Move to next city
            int currentIndex = tourCities.IndexOf(city);
            if (currentIndex < tourCities.Count - 1)
            {
                currentCityIndex = currentIndex + 1;
                OnCityUnlocked?.Invoke(tourCities[currentCityIndex]);
            }

            // Check if world tour is complete
            int nonSecretCount = tourCities.Count(c => !c.isSecret);
            int completedNonSecret = saveData.completedCities.Count(id =>
                tourCities.Any(c => c.cityId == id && !c.isSecret));

            if (completedNonSecret >= nonSecretCount)
            {
                OnWorldTourCompleted?.Invoke();
                Debug.Log("🌍🎉 WORLD TOUR COMPLETED!");
            }
        }
    }

    /// <summary>
    /// Bir konserin toplam yıldız sayısını döndürür
    /// </summary>
    public int GetConcertStars(string cityId, int concertIndex)
    {
        string concertKey = $"{cityId}_{concertIndex}";
        return saveData.concertStars.GetValueOrDefault(concertKey, 0);
    }

    /// <summary>
    /// Bir şehirdeki toplam yıldız sayısını döndürür
    /// </summary>
    public int GetCityTotalStars(string cityId)
    {
        var city = tourCities.FirstOrDefault(c => c.cityId == cityId);
        if (city == null) return 0;

        int total = 0;
        for (int i = 0; i < city.concerts.Count; i++)
        {
            total += GetConcertStars(cityId, i);
        }
        return total;
    }

    /// <summary>
    /// Tur ilerleme yüzdesi
    /// </summary>
    public float GetTourProgress()
    {
        int totalConcerts = tourCities.Where(c => !c.isSecret).Sum(c => c.concerts.Count);
        if (totalConcerts == 0) return 0f;

        int completedConcerts = 0;
        foreach (var city in tourCities.Where(c => !c.isSecret))
        {
            for (int i = 0; i < city.concerts.Count; i++)
            {
                if (saveData.completedConcerts.Contains($"{city.cityId}_{i}"))
                    completedConcerts++;
            }
        }

        return (float)completedConcerts / totalConcerts;
    }

    public bool IsCityCompleted(string cityId) => saveData.completedCities.Contains(cityId);

    #endregion

    #region Persistence

    private void LoadProgress()
    {
        string json = PlayerPrefs.GetString("WorldTourProgress", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                saveData = JsonUtility.FromJson<WorldTourSaveData>(json);
                saveData.RestoreDictionaries();
            }
            catch
            {
                saveData = new WorldTourSaveData();
            }
        }
        else
        {
            saveData = new WorldTourSaveData();
        }

        // Restore currentCityIndex
        currentCityIndex = 0;
        for (int i = tourCities.Count - 1; i >= 0; i--)
        {
            if (saveData.completedCities.Contains(tourCities[i].cityId))
            {
                currentCityIndex = Mathf.Min(i + 1, tourCities.Count - 1);
                break;
            }
        }
    }

    private void SaveProgress()
    {
        saveData.PrepareSerialization();
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString("WorldTourProgress", json);
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

#region World Tour Data Classes

[Serializable]
public class TourCity
{
    public string cityId;
    public string cityName;
    public string country;
    public string description;
    public string iconEmoji;
    public string backgroundTheme;
    public int requiredLevel;
    public bool isSecret = false;

    public List<TourConcert> concerts = new List<TourConcert>();
    public string encoreSong;

    public int cityRewardXP;
    public int cityRewardCurrency;
}

[Serializable]
public class TourConcert
{
    public string concertName;
    public List<string> songKeys = new List<string>();
    public int requiredStars; // Minimum yıldız, konser açılsın
    public string venue;
}

[Serializable]
public class WorldTourSaveData
{
    public List<string> completedCities = new List<string>();
    public List<string> completedConcerts = new List<string>();

    // Dictionary serialization
    [NonSerialized] public Dictionary<string, int> concertStars = new Dictionary<string, int>();
    public List<string> concertStarsKeys = new List<string>();
    public List<int> concertStarsValues = new List<int>();

    public void PrepareSerialization()
    {
        concertStarsKeys.Clear();
        concertStarsValues.Clear();
        if (concertStars != null)
        {
            foreach (var kvp in concertStars)
            {
                concertStarsKeys.Add(kvp.Key);
                concertStarsValues.Add(kvp.Value);
            }
        }
    }

    public void RestoreDictionaries()
    {
        concertStars = new Dictionary<string, int>();
        if (concertStarsKeys != null && concertStarsValues != null)
        {
            for (int i = 0; i < Mathf.Min(concertStarsKeys.Count, concertStarsValues.Count); i++)
            {
                concertStars[concertStarsKeys[i]] = concertStarsValues[i];
            }
        }
    }
}

#endregion
