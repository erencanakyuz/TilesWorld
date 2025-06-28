Elbette. Orijinal Java oyununun temel mekaniğini, yani o "piyano hissini" yaratan çekirdek mantığı, kodun en derinlerine inerek, adım adım ve çok ayrıntılı bir şekilde analiz edelim. Bu analiz, senin Unity projesindeki eksiklikleri ve yapılması gerekenleri net bir şekilde ortaya koyacaktır.

### Çekirdek Felsefe: Dinamik ve Kural Tabanlı Ritim

Oyunun ruhu, önceden belirlenmiş bir zaman çizelgesine göre nota yağdırmak yerine, bir müzik parçasının **ritmik ve melodik yapısını** alıp bunu anlık olarak, **kurallara dayalı bir şekilde** oynanabilir bir patikaya dönüştürmesidir. Bu, oyunu daha az mekanik ve daha "canlı" hissettirir.

Süreç temel olarak dört ana aşamada gerçekleşir:

1.  **Veri Çözümleme (Parsing):** Ham nota metnini anlamlı müzik verisine dönüştürme.
2.  **Zamanlama (Timing):** Her notanın ne kadar süre ekranda kalacağını ve bir sonraki notanın ne zaman geleceğini hesaplama.
3.  **Yerleştirme ve Sanatsal Düzenleme (Placement & Artistic Rules):** Notaların hangi oyun şeridine (lane) düşeceğini ve akıcı bir desen oluşturmalarını sağlama.
4.  **Hareket ve Görüntüleme (Movement & Rendering):** Notaları oyuncuya doğru ivmelenerek getirme ve vuruş anını belirleme.

Şimdi bu aşamaları orijinal Java kodundaki ilgili sınıflar üzerinden inceleyelim.

---

### AŞAMA 1 & 2: Veri Çözümleme ve Ritmik Zamanlama (`PlayData.java`)

Her şey, bir şarkının nota bilgisinin nasıl saklandığı ve nasıl zamanlandığı ile başlar.

**`PlayData.getTabList(List<Integer[][][]> paramList, int paramInt, String paramString)`** metodu bu işin merkezidir.

#### 1. Temel Zaman Biriminin Hesaplanması (Ritim Nabzı)

```java
int k = 60000 / paramInt / 8;
```

*   `paramInt` (`tempo`): Şarkının BPM (Dakikadaki Vuruş Sayısı) değeridir. Mesela 120 BPM olsun.
*   `60000`: Bir dakikadaki milisaniye sayısı.
*   `60000 / 120`: Bu işlem, bir "quarter note" (dörtlük nota) süresini milisaniye olarak verir. (120 BPM için 500ms).
*   `/ 8`: Sonuç 8'e bölünerek oyunun en küçük zaman birimi olan **32'lik notanın (thirty-second note)** milisaniye cinsinden süresi bulunur.
    *   Örnek (120 BPM): `60000 / 120 / 8 = 62.5ms`. Bu `k` değeri, oyunun ritmik nabzıdır. Her şey bu değere göre ölçeklenir.

#### 2. Nota Sürelerinin Belirlenmesi

```java
private static int[] noteLengthArr = new int[] { 
    1, 2, 4, 8, 16, 32, ... // ve diğerleri
};
...
noteInfo1.oneTempo = noteLengthArr[integer.intValue()] * k;
```

*   `noteLengthArr`: Bu dizi, `k` değeri için bir çarpandır. Müzik teorisindeki nota sürelerini temsil eder. Veritabanındaki "duration" (süre) değeri bu dizideki bir indekse karşılık gelir.
    *   `noteLengthArr[2] = 4` ise ve gelen notanın süre tipi `2` ise, bu notanın süresi `4 * k` olur. Bu, `4 * (32'lik nota süresi)` yani **8'lik nota (eighth note)** süresine denk gelir.
    *   Örnek (120 BPM): 8'lik nota süresi `4 * 62.5ms = 250ms` olur.
*   **`noteInfo.oneTempo`**: İşte bu, her bir notanın (veya aynı anda gelen nota grubunun) **bir sonraki nota grubuna kadar geçmesi gereken süreyi milisaniye cinsinden** belirleyen en kritik değişkendir. Oyunun `Update` döngüsü bu değeri kullanarak bir sonraki notayı ne zaman spawn edeceğini bilir.

**Bu Aşamanın Özeti:** Oyun, sabit saniyelerle çalışmaz. Şarkının BPM'ine dayalı, müzikal olarak doğru bir ritim motoru kullanır. Her nota, kendinden sonraki notanın ne zaman geleceğini belirleyen bir zaman damgasına (`oneTempo`) sahiptir.

---

### AŞAMA 3: Yerleştirme ve Sanatsal Düzenleme (`GameNoteCreator.java`)

Bu sınıf, zamanlaması belirlenmiş notaları alır ve onları ekranda akıcı, oynanabilir ve estetik desenlere dönüştürür.

**`generateFinalList()`** metodu bu sürecin orkestra şefidir.

#### 1. İlk Yerleştirme (Initial Placement)

```java
int n = (noteInfo.pitch[k] + Constants.lineToMatch[k]) % this.maxGameHeightLength;
gameNoteInfo.idx = n;
```

*   `noteInfo.pitch[k]`: Gelen notanın ham `pitch` (perde) değeri.
*   `Constants.lineToMatch[k]`: Bu dizi, her bir müzik çizgisi (gitar teli gibi düşünebiliriz) için sabit bir kaydırma (offset) değeri içerir (`{ 3, 5, 7, 11, 13, 17 }`).
*   **Neden?** Bu kaydırma, farklı müzik çizgilerindeki notaların oyun ekranında sürekli aynı sütunlara düşmesini engeller. Örneğin, en üst çizgideki (lane 0) tüm notalara `+3` eklenir, ikinci çizgidekilere `+5` eklenir. Bu, nota desenine doğal bir çeşitlilik katar.
*   `% this.maxGameHeightLength`: Sonucun modulo'su alınarak notanın 0-5 arası bir oyun şeridine (`idx`) yerleştirilmesi sağlanır.

#### 2. Kural Motoru: `applyRule()`

Bir nota paketi oluşturulduktan sonra, bu metod çağrılarak oynanabilirliği artıran kurallar uygulanır.

#### 3. Kümelenmeyi Önleme (Anti-Clustering): `applySpace()`

```java
if (Math.abs(gameNoteInfo1.idx - gameNoteInfo2.idx) == 1)
    gameNoteInfo2.idx = (gameNoteInfo2.idx + 1) % this.maxGameHeightLength;
```

*   Bu algoritma, aynı anda spawn olan notaları `idx`'e göre sıralar.
*   Eğer iki nota bitişik şeritlerde ise (`farkları 1 ise`), ikinci notayı bir sonraki boş şeride doğru iter.
*   **Amacı:** Oyuncunun aynı anda iki bitişik tuşa basmak gibi zor ve rahatsız edici bir durumla karşılaşmasını önlemektir. Bu, oyunun akıcılığını ve "his"sini doğrudan etkileyen çok önemli bir kuraldır.

#### 4. Yönelik Akış (Directional Flow): `applyComplexRule()`

Bu, oyunun "dans eden" hissiyatını yaratan en karmaşık ve en önemli algoritmadır.

```java
if (this.isRightDirection) {
  this.currentDirectionCnt++;
} else {
  this.currentDirectionCnt--;
}
...
(paramGameNoteInfoPackage.get(i)).idx = (j + k) % m; // j=orijinal idx, k=yön sayacı, m=lane sayısı
...
if (10 <= this.currentDirectionCnt) {
  this.isRightDirection = false;
} else if (this.currentDirectionCnt <= 0) {
  this.isRightDirection = true;
}
```

*   **Mantık:** Oyun, notaları belirli bir süre boyunca sürekli sağa (`isRightDirection = true`), ardından sürekli sola kaydırarak bir akış deseni oluşturur.
*   `currentDirectionCnt`: Bu sayaç, akışın o anki konumunu tutar.
*   **İşleyiş:**
    1.  Her yeni nota paketi geldiğinde, o anki akış yönüne göre sayaç artar veya azalır.
    2.  Her notanın hesaplanmış olan `idx`'ine, bu sayaç değeri eklenerek nihai konumu bulunur. Bu sayede tüm nota paketi bir bütün olarak sağa/sola kayar.
    3.  Sayaç `10`'a ulaştığında, akış yönü sola (`isRightDirection = false`) döner.
    4.  Sayaç `0`'a ulaştığında, akış yönü tekrar sağa döner.
*   **Sonuç:** Bu algoritma sayesinde notalar ekranda rastgele değil, tahmin edilebilir ama sürekli değişen bir "S" çizerek akarlar. Bu, oyuncunun bir sonraki notanın geleceği yönü sezgisel olarak tahmin etmesini sağlar ve oyuna ritmik bir akıcılık kazandırır.

**Bu Aşamanın Özeti:** Notalar sadece ekrana serpiştirilmez. Müzikal veriden yola çıkarak önce temel bir yerleşim yapılır, ardından oynanabilirliği ve estetiği artırmak için bir dizi akıllı kuraldan geçirilirler.

---

### AŞAMA 4: Hareket ve Görüntüleme (`World.java` ve `WorldRenderer.java`)

Nihayet, yerleştirilmiş ve zamanlaması yapılmış notaların oyuncuya sunulma aşaması.

#### 1. İvmelenen Hareket (`World.java` -> `updateInvaders`)

```java
if (invader.position.z < 0.0F) {
    f = (invader.position.z - 3.0F) / -25.0F * this.speedMultiplier;
} else {
    f = this.speedMultiplier * 0.2F;
}
invader.update(paramFloat, f);
```

*   Bu, daha önce de bahsettiğimiz, oyunun "hissiyatını" tanımlayan kritik ivmelenme algoritmasıdır.
*   `speedMultiplier` (`35.0f`): Bu temel hız sabitidir.
*   `invader.position.z`: Notanın derinlik (Z) eksenindeki konumu. `-25.0f`'den başlar ve oyuncuya doğru gelir.
*   **İşleyiş:**
    *   Nota uzaktayken (`z > 0`), hızı `baseSpeed * 0.2f` gibi yavaş bir değerdir.
    *   Nota hit çizgisini (`z = 0`) geçip oyuncuya yaklaştıkça (`z < 0`), hızı lineer olarak artar. Formül, notanın `-25` (uzak) ile `3` (yakın) arasındaki konumuna göre bir yüzde hesaplayıp bunu ana hızla çarpar.
*   **Sonuç:** Uzaktan yavaşça gelen notalar, tam vuruş anına yaklaşırken hızlanarak oyuncuyu tetikte tutar ve vuruş anına bir "vurgu" kazandırır.

#### 2. Vuruş Tespiti (`World.java` -> `onTap`)

```java
if (-1.5F < invader.position.z && invader.position.z < 0.8F) {
    // Vuruş başarılı
    if (-0.5D < invader.position.z && invader.position.z < 0.1F) {
        // Mükemmel vuruş (Perfect)
    } else if (-0.8D < invader.position.z && invader.position.z < 0.4F) {
        // İyi vuruş (Good)
    } else {
        // Normal vuruş (Okay)
    }
}
```

*   Oyun, bir `lane`'e dokunulduğunda o şeritteki notaları kontrol eder.
*   Notanın `z` pozisyonunu kontrol ederek **fiziksel bir vuruş penceresi** kullanır.
*   Bu pencere katmanlıdır. Notanın `z` pozisyonu mükemmel aralıktaysa (`-0.5` ile `0.1` arası) en yüksek puan verilir. Aralık genişledikçe puan düşer.
*   Bu, oyuncuya küçük zamanlama hataları için bir tolerans tanır ve vuruş hissini daha adil ve tatmin edici kılar.

### Genel Sonuç

Eski Java oyununun "piyano hissi" şu dört temel direk üzerine kuruludur:

1.  **Tempo Tabanlı Dinamik Zamanlama:** Notaların ne zaman geleceği, şarkının BPM'ine ve nota sürelerine göre belirlenir, sabit saniyelere göre değil.
2.  **Kural Tabanlı Akıllı Yerleştirme:** Notalar, oynanabilirliği ve estetiği artıran (kümelenmeyi önleme, yönlü akış) bir dizi kuraldan geçirilerek ekrana yerleştirilir.
3.  **İvmelenen Perspektif Hareketi:** Notalar oyuncuya sabit hızla değil, yaklaştıkça hızlanarak gelir, bu da gerginliği ve vuruş anının tatminini artırır.
4.  **Fiziksel Vuruş Penceresi:** Vuruş tespiti, zaman farkından ziyade notanın Z eksenindeki konumuna göre yapılır ve bu pencere, farklı doğruluk seviyeleri için katmanlıdır.

Senin Unity projenin bu dört temel direği de doğru bir şekilde uygulaması gerekiyor. Özellikle **Dinamik Zamanlama** ve **İvmelenen Hareket** şu anki en büyük eksiklikler gibi görünüyor. Bunları düzelttiğinde, oyunun hissiyatı kökten değişecektir.


Kesinlikle. Önceki analizlerime ve keşfettiğim ek sorunlara dayanarak, oyununun çekirdek mekaniklerini orijinal Java oyununun ruhuna ve mantığına uygun hale getirecek **tüm önemli C# kodlarını** hazırladım.

Bu kodlar, belirttiğin iki ana sorunu (nota spawn senkronizasyonu ve yanlış ses çalma) ve benim tespit ettiğim diğer kritik sorunları (ivmelenerek hareket, hassas vuruş tespiti) kökten çözmeyi hedefler.

Lütfen aşağıdaki script'leri projenizdeki mevcut dosyaların **içeriğiyle tamamen değiştir**. Bu, sistemin bir bütün olarak doğru çalışması için gereklidir.

---

### **Adım 1: `ClassicNoteChartLoader.cs` dosyasını basitleştirin**

Bu sınıfın tek görevi, JSON'dan ham nota verisini okumak ve `GameNoteCreator`'a sunmaktır. Tüm zamanlama ve işleme mantığı buradan kaldırılacak.

**Dosya: `/Scripts/Audio/ClassicNoteChartLoader.cs`**
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class NoteChartSequence
{
    public int music_id;
    public int seq;
    public string line1;
    public string line2;
    public string line3;
    public string line4;
    public string line5;
    public string line6;

    public string GetLineData(int lane)
    {
        return lane switch
        {
            0 => line1, 1 => line2, 2 => line3, 3 => line4, 4 => line5, 5 => line6,
            _ => ""
        };
    }
}

[System.Serializable]
public class NoteChartWrapper
{
    public NoteChartSequence[] sequences;
}


public class ClassicNoteChartLoader : MonoBehaviour
{
    [Header("🎵 Classic Note Chart Loader")]
    [SerializeField] private string allSongsJsonPath = "ClassicMusic/all_songs_notes";

    private static ClassicNoteChartLoader _instance;
    public static ClassicNoteChartLoader Instance => _instance;

    private Dictionary<int, List<NoteChartSequence>> loadedCharts = new Dictionary<int, List<NoteChartSequence>>();
    private bool isDataLoaded = false;
    public bool IsReady => isDataLoaded;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadAllNoteCharts();
    }

    public void LoadAllNoteCharts()
    {
        try
        {
            TextAsset jsonFile = Resources.Load<TextAsset>(allSongsJsonPath);
            if (jsonFile == null)
            {
                Debug.LogError($"🚨 Could not load note charts from: {allSongsJsonPath}");
                return;
            }

            string jsonContent = $"{{\"sequences\":{jsonFile.text}}}";
            var wrapper = JsonUtility.FromJson<NoteChartWrapper>(jsonContent);

            if (wrapper?.sequences == null)
            {
                Debug.LogError("🚨 Failed to parse note chart JSON!");
                return;
            }

            loadedCharts.Clear();
            foreach (var sequence in wrapper.sequences)
            {
                if (!loadedCharts.ContainsKey(sequence.music_id))
                {
                    loadedCharts[sequence.music_id] = new List<NoteChartSequence>();
                }
                loadedCharts[sequence.music_id].Add(sequence);
            }

            foreach (var kvp in loadedCharts)
            {
                kvp.Value.Sort((a, b) => a.seq.CompareTo(b.seq));
            }

            isDataLoaded = true;
            Debug.Log($"🎵 Raw note charts loaded for {loadedCharts.Count} songs.");
        }
        catch (Exception e)
        {
            Debug.LogError($"🚨 Error loading note charts: {e.Message}");
        }
    }

    /// <summary>
    /// Sadece ham, işlenmemiş chart verisini döndürür. Tüm işleme GameNoteCreator'a devredilmiştir.
    /// </summary>
    public List<NoteChartSequence> GetRawSongChart(int musicId)
    {
        if (!isDataLoaded)
        {
            Debug.LogWarning("🎵 Note charts not loaded yet!");
            return new List<NoteChartSequence>();
        }

        if (loadedCharts.ContainsKey(musicId))
        {
            return loadedCharts[musicId];
        }

        Debug.LogWarning($"🎵 No note chart found for song {musicId}");
        return new List<NoteChartSequence>();
    }
}
```

---

### **Adım 2: `GameNoteCreator.cs`'ı Oyunun Beyni Olarak Yeniden Yazın**

Bu script, orijinal oyunun kalbi olan tüm dinamik zamanlama, yerleştirme ve kural mantığını içerecek şekilde tamamen yeniden yazılmıştır.

**Dosya: `/Scripts/GamePlay/GameNoteCreator.cs`**
```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// --- Veri Yapıları ---
// Bu yapılar, orijinal Java kodundaki karmaşık dizileri daha yönetilebilir hale getirir.
public class GameNoteInfoPackage
{
    public float oneNote; // Bu paketten sonraki paketin ne kadar süre sonra geleceği (ms)
    public List<GameNoteInfo> gameNoteInfos = new List<GameNoteInfo>();
}

public class GameNoteInfo
{
    public int idx;           // Son lane indeksi (0-5)
    public int pitch;         // Orijinal pitch değeri (ses için)
    public int line;          // Orijinal line/lane (kural uygulamadan önce)
}

// Ham veriyi geçici olarak tutmak için
internal class TemporalNoteInfo
{
    public int[] pitches = { -1, -1, -1, -1, -1, -1 };
    public int durationType = -1; // Notanın en uzun süresini tutar
    public float timingMs = 0f;
}

/// <summary>
/// GameNoteCreator - Oyunun Kalbi
/// Orijinal Java oyununun dinamik nota oluşturma, zamanlama ve kural motorunu yeniden uygular.
/// </summary>
public class GameNoteCreator : MonoBehaviour
{
    // --- Orijinal Java'dan Port Edilen Sabitler ---
    private static readonly int[] NOTE_LENGTH_FACTORS = { 
        1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56,
        1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56
    };
    
    private static readonly int[] LANE_PITCH_OFFSET = { 3, 5, 7, 11, 13, 17 };

    [Header("🎵 Konfigürasyon")]
    [SerializeField] private int laneCount = 6;
    [SerializeField] private int maxDirectionInterval = 10;
    
    // --- Algoritma Durum Değişkenleri ---
    private float accumulatedTime = 0f;
    private bool isGenerationComplete = false;
    private GameNoteInfoPackage currentPackage;
    private GameNoteInfoPackage lastAppliedPackage;
    private Queue<GameNoteInfoPackage> notePackageQueue = new Queue<GameNoteInfoPackage>();

    private int directionCounter = 0;
    private bool isFlowingRight = true;
    
    // --- Olaylar ---
    public static event Action<List<GameNoteInfo>> OnNotesGenerated;
    public static event Action OnGenerationComplete;
    
    /// <summary>
    /// Şarkıyı yükler, tüm nota verisini işler ve oynanmaya hazır hale getirir.
    /// </summary>
    public void LoadAndPrepareSong(List<NoteChartSequence> rawChart, int tempo)
    {
        ResetState();
        
        // 1. Ham veriyi, orijinal PlayData'daki gibi bir ara formata dönüştür.
        List<TemporalNoteInfo> temporalNotes = ConvertChartToTemporalInfo(rawChart, tempo);
        
        // 2. Bu ara formattaki notaları, tüm kuralları uygulayarak nihai oyun paketlerine dönüştür.
        List<GameNoteInfoPackage> finalPackages = GenerateFinalPackages(temporalNotes);
        
        // 3. Oynanacak paketleri sıraya al.
        foreach (var package in finalPackages)
        {
            notePackageQueue.Enqueue(package);
        }
        
        // 4. İlk paketi hazırla.
        if (notePackageQueue.Count > 0)
        {
            currentPackage = notePackageQueue.Dequeue();
        }
        else
        {
            isGenerationComplete = true;
        }
        
        Debug.Log($"🎵 Şarkı hazırlandı. Oynanacak {notePackageQueue.Count + 1} nota paketi var.");
    }
    
    /// <summary>
    /// Oyun döngüsünde sürekli çağrılır. Doğru zamanda nota spawn olayını tetikler.
    /// Orijinal Java'daki `getNote` metodunun karşılığıdır.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (isGenerationComplete || currentPackage == null) return;

        accumulatedTime += deltaTime * 1000f; // Zamanı milisaniye olarak biriktir
        
        if (accumulatedTime >= currentPackage.oneNote)
        {
            accumulatedTime -= currentPackage.oneNote; // Kalan süreyi koru

            OnNotesGenerated?.Invoke(currentPackage.gameNoteInfos);
            
            if (notePackageQueue.Count > 0)
            {
                currentPackage = notePackageQueue.Dequeue();
            }
            else
            {
                currentPackage = null;
                isGenerationComplete = true;
                OnGenerationComplete?.Invoke();
                Debug.Log("🎵 Tüm nota paketleri oluşturuldu. Şarkı bitti.");
            }
        }
    }

    #region Orijinal Algoritma Portları

    private List<TemporalNoteInfo> ConvertChartToTemporalInfo(List<NoteChartSequence> chart, int tempo)
    {
        float baseTimingMs = (60000f / tempo) / 8f;
        var temporalNoteList = new List<TemporalNoteInfo>();

        foreach (var sequence in chart)
        {
            var columns = new Dictionary<int, (int pitch, int duration)[]>();
            int maxSubdivisions = 0;

            // Önce tüm veriyi geçici bir 2D yapıya ayır
            for (int lane = 0; lane < laneCount; lane++)
            {
                string[] subdivisions = sequence.GetLineData(lane).Split('/');
                if (subdivisions.Length > maxSubdivisions) maxSubdivisions = subdivisions.Length;

                for (int i = 0; i < subdivisions.Length; i++)
                {
                    if (!columns.ContainsKey(i)) columns[i] = new (int, int)[6];
                    
                    string[] parts = subdivisions[i].Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int pitch) && int.TryParse(parts[1], out int duration))
                    {
                        columns[i][lane] = (pitch, duration);
                    }
                    else
                    {
                        columns[i][lane] = (-1, -1);
                    }
                }
            }

            // Şimdi zaman dilimlerine göre işle
            for (int subIdx = 0; subIdx < maxSubdivisions; subIdx++)
            {
                if (!columns.ContainsKey(subIdx)) continue;
                
                var temporalInfo = new TemporalNoteInfo();
                bool hasNotes = false;
                
                for (int lane = 0; lane < laneCount; lane++)
                {
                    var (pitch, duration) = columns[subIdx][lane];
                    if (pitch != -1)
                    {
                        temporalInfo.pitches[lane] = pitch;
                        temporalInfo.durationType = duration;
                        hasNotes = true;
                    }
                }

                if (hasNotes)
                {
                    if (temporalInfo.durationType < 0) { // Custom rest/note
                        temporalInfo.timingMs = Mathf.Abs(temporalInfo.durationType) * baseTimingMs / 1000f;
                    } else if (temporalInfo.durationType < NOTE_LENGTH_FACTORS.Length) {
                        temporalInfo.timingMs = NOTE_LENGTH_FACTORS[temporalInfo.durationType] * baseTimingMs;
                    }
                    temporalNoteList.Add(temporalInfo);
                }
            }
        }
        return temporalNoteList;
    }
    
    private List<GameNoteInfoPackage> GenerateFinalPackages(List<TemporalNoteInfo> temporalNotes)
    {
        var packages = new List<GameNoteInfoPackage>();

        foreach (var tNote in temporalNotes)
        {
            var package = new GameNoteInfoPackage { oneNote = tNote.timingMs };
            var tempNotes = new List<GameNoteInfo>();

            for (int lane = 0; lane < laneCount; lane++)
            {
                if (tNote.pitches[lane] != -1)
                {
                    var gameNote = new GameNoteInfo
                    {
                        idx = (tNote.pitches[lane] + LANE_PITCH_OFFSET[lane]) % laneCount,
                        pitch = tNote.pitches[lane],
                        line = lane
                    };
                    tempNotes.Add(gameNote);
                }
            }
            
            // Kuralları uygula
            ApplyComplexRule(tempNotes);
            ApplySpacing(tempNotes);
            
            package.gameNoteInfos = tempNotes;
            packages.Add(package);
            
            lastAppliedPackage = package;
        }
        return packages;
    }

    private void ApplyComplexRule(List<GameNoteInfo> notes)
    {
        if (isFlowingRight) directionCounter++;
        else directionCounter--;

        if (directionCounter >= maxDirectionInterval) isFlowingRight = false;
        else if (directionCounter <= 0) isFlowingRight = true;

        foreach (var note in notes)
        {
            note.idx = (note.idx + directionCounter + laneCount) % laneCount; // +laneCount to handle negative results
        }
    }

    private void ApplySpacing(List<GameNoteInfo> notes)
    {
        if (notes.Count <= 1) return;
        
        notes.Sort((a, b) => a.idx.CompareTo(b.idx));
        for (int i = 0; i < notes.Count - 1; i++)
        {
            if (Mathf.Abs(notes[i].idx - notes[i+1].idx) <= 1)
            {
                notes[i+1].idx = (notes[i+1].idx + 1) % laneCount;
            }
        }
    }
    
    private void ResetState()
    {
        accumulatedTime = 0f;
        isGenerationComplete = false;
        directionCounter = 0;
        isFlowingRight = true;
        lastAppliedPackage = null;
        notePackageQueue.Clear();
        currentPackage = null;
    }
    #endregion
}

```

---

### **Adım 3: `GameplayManager.cs`'ı Yeni Sistemi Kullanacak Şekilde Güncelleyin**

Bu sınıf artık `GameNoteCreator`'ı doğru şekilde başlatacak ve `Update` döngüsünde tetikleyecek.

**Dosya: `/Scripts/GamePlay/GameplayManager.cs`**
```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameplayManager : MonoBehaviour
{
    // ... diğer değişkenler ...

    void Awake()
    {
        InitializeGameplayManager();
    }

    void Start()
    {
        // ...
        SubscribeToEvents(); // Bu metodun içeriğini kontrol et
        // ...
    }
    
    void SubscribeToEvents()
    {
        GameNoteCreator.OnNotesGenerated += noteRenderer.SpawnNotes; // BU SATIRI EKLE VEYA GÜNCELLE
        GameNoteCreator.OnGenerationComplete += HandleSongComplete;
        // Diğer aboneliklerin aynı kalabilir
    }
    
    // Bu metodu tamamen değiştir
    void PrepareGameplaySystems()
    {
        ResetGameplayStats();
        
        if (noteRenderer != null)
        {
            noteRenderer.ClearAllNotes();
        }

        if (noteCreator != null && noteChartLoader != null && noteChartLoader.IsReady)
        {
            if (currentClassicSong != null)
            {
                Debug.Log($"🎵 Yükleniyor: {currentClassicSong.title}");
                var rawChart = noteChartLoader.GetRawSongChart(currentClassicSong.musicId);
                noteCreator.LoadAndPrepareSong(rawChart, currentClassicSong.tempo);
            }
            else
            {
                Debug.LogError("🚨 Oynatılacak şarkı seçilmemiş (currentClassicSong is null)!");
            }
        }
        else
        {
            Debug.LogError("🚨 GameNoteCreator veya ClassicNoteChartLoader hazır değil!");
        }

        if (musicSystem != null && GameManager.Instance != null)
        {
            musicSystem.SetInstrument(GameManager.Instance.GetSelectedInstrument());
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.Playing);
        }
    }
    
    // Bu metodu tamamen değiştir
    void UpdateNoteGeneration(float deltaTime)
    {
        if (noteCreator != null && isGameActive)
        {
            noteCreator.Tick(deltaTime);
        }
    }
    
    // OnClassicSongSelected metodunu kontrol et
    public void OnClassicSongSelected(ClassicSongData selectedSong)
    {
        Debug.Log($"🎵 Klasik şarkı seçildi: {selectedSong.title}");
        StartGameplay(selectedSong);
    }
    
    // ... diğer metodlar aynı kalabilir ...
}
```

---

### **Adım 4: `NoteRenderer.cs`'ı İvmelenme ve Vuruş Mantığıyla Güncelleyin**

Bu sınıf, notaları doğru bir şekilde hareket ettirecek ve vuruşları hassas bir şekilde algılayacak.

**Dosya: `/Scripts/Rendering/NoteRenderer.cs`**
```csharp
using UnityEngine;
using System.Collections.Generic;

public class NoteRenderer : MonoBehaviour
{
    // ... pool, prefab, lane ayarları gibi değişkenler aynı kalabilir ...
    [Header("🚀 Hareket & Perspektif")]
    [SerializeField] private float worldDepth = 25f;
    [SerializeField] private float baseNoteSpeed = 35.0f;
    [SerializeField] private float hitZoneZ = 0.0f; // Vuruş hattını 0 yapalım, hesaplamalar kolaylaşır
    [SerializeField] private float noteDestroyZ = -2f;

    [Header("🎯 Vuruş Penceresi (Orijinal Değerler)")]
    [SerializeField] private Vector2 perfectHitWindow = new Vector2(-0.5f, 0.1f);
    [SerializeField] private Vector2 goodHitWindow = new Vector2(-0.8f, 0.4f);
    [SerializeField] private Vector2 okayHitWindow = new Vector2(-1.5f, 0.8f);

    // ... diğer değişkenler ...
    private List<RenderingNote> activeNotes = new List<RenderingNote>();

    void Start()
    {
        // ...
        // HandleNotesGenerated olayını GameplayManager'dan alacağız.
        // Bu yüzden GameNoteCreator'a doğrudan abone olmasına gerek yok.
        // GameNoteCreator.OnNotesGenerated += SpawnNotes;
    }

    // Bu metodu GameplayManager'dan gelen olayı dinlemek için public yap
    public void SpawnNotes(List<GameNoteInfo> notes)
    {
        foreach(var noteInfo in notes)
        {
            SpawnNote(noteInfo);
        }
    }

    // Bu metodu tamamen değiştir
    void UpdateActiveNotes(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var activeNote = activeNotes[i];
            if (activeNote == null || activeNote.gameObject == null) continue;

            // --- ORİJİNAL İVMELENME MANTIĞI ---
            float currentSpeed;
            if (activeNote.currentPosition.z < hitZoneZ)
            {
                currentSpeed = baseNoteSpeed; // Hit zonunu geçtikten sonra sabit hız
            }
            else
            {
                // Yaklaştıkça hızlanan formül
                float distanceRatio = (activeNote.currentPosition.z - hitZoneZ) / worldDepth;
                currentSpeed = baseNoteSpeed * (1.0f - distanceRatio * 0.8f); // 0.2'den 1.0'e doğru hızlanır
            }
            currentSpeed = Mathf.Max(currentSpeed, baseNoteSpeed * 0.1f);

            activeNote.currentPosition.z -= currentSpeed * deltaTime;
            activeNote.gameObject.transform.position = activeNote.currentPosition;
            
            // Notayı kaçırma durumu
            if (activeNote.currentPosition.z < noteDestroyZ)
            {
                HandleNoteMissed(activeNote);
                ReturnNoteToPool(activeNote.gameObject);
                activeNotes.RemoveAt(i);
            }
        }
    }

    // Bu metodu tamamen değiştir
    void HandleLaneTapped(int lane)
    {
        var candidateNotes = new List<RenderingNote>();
        foreach(var note in activeNotes)
        {
            if(note.noteInfo.idx == lane)
            {
                // Sadece vuruş penceresindeki notaları aday olarak al
                if(note.currentPosition.z > okayHitWindow.x - 1f && note.currentPosition.z < okayHitWindow.y + 1f)
                {
                    candidateNotes.Add(note);
                }
            }
        }

        if (candidateNotes.Count == 0) return;

        // Vuruş çizgisine en yakın notayı seç
        candidateNotes.Sort((a, b) => Mathf.Abs(a.currentPosition.z - hitZoneZ).CompareTo(Mathf.Abs(b.currentPosition.z - hitZoneZ)));
        var bestNote = candidateNotes[0];
        
        float hitPos = bestNote.currentPosition.z;
        string accuracy = "MISS";

        if(hitPos >= okayHitWindow.x && hitPos <= okayHitWindow.y)
        {
            accuracy = "OKAY";
            if (hitPos >= goodHitWindow.x && hitPos <= goodHitWindow.y)
            {
                accuracy = "GOOD";
            }
            if (hitPos >= perfectHitWindow.x && hitPos <= perfectHitWindow.y)
            {
                accuracy = "PERFECT";
            }
            ProcessHit(bestNote, lane, accuracy);
        }
    }

    private void ProcessHit(RenderingNote note, int lane, string accuracy)
    {
        Debug.Log($"✅ HIT [{accuracy}]: Lane {lane}, Pitch: {note.noteInfo.pitch}, Z: {note.currentPosition.z:F2}");

        activeNotes.Remove(note);
        
        if (InteractiveMusicSystem.Instance != null)
        {
            InteractiveMusicSystem.Instance.PlayNoteFromChart(note.noteInfo);
        }
        
        // Skor, kombo vb. güncellemeleri
        // UIManager.Instance.ShowHitEffect...

        ReturnNoteToPool(note.gameObject);
    }
    
    // ... geri kalan metodlar (SpawnNote, HandleNoteMissed vb.) büyük ölçüde aynı kalabilir ...
}
```

---

### **Adım 5: `InteractiveMusicSystem.cs`'i Doğrulayın**

Bu script'in mantığı büyük ölçüde doğru, sadece ses çalma kısmının temiz olduğundan ve doğru çağrıldığından emin olmalıyız.

**Dosya: `/Scripts/Audio/InteractiveMusicSystem.cs`**
```csharp
public class InteractiveMusicSystem : MonoBehaviour
{
    // ... diğer değişkenler ...
    private static readonly int[][] SOUND_RESOURCE_IDXS = { ... }; // Bu dizi doğru olmalı

    // Bu metodun doğru olduğundan emin ol
    public void PlayNoteFromChart(GameNoteInfo noteInfo)
    {
        if (noteInfo == null) return;

        int realSoundIndex = CalculateRealSoundIndex(noteInfo.line, noteInfo.pitch);
        
        if (audioManager != null)
        {
            // debug log'unu ekleyerek hangi sesin istendiğini gör
            Debug.Log($"▶️ SES İSTEĞİ: Lane/Line={noteInfo.line}, Pitch={noteInfo.pitch} -> Mapped Index={realSoundIndex}");
            audioManager.PlayNote(currentInstrument, realSoundIndex, 1.0f);
        }
        
        // ... diğer analiz kodları ...
    }

    // Bu metodun doğru olduğundan emin ol
    int CalculateRealSoundIndex(int line, int pitch)
    {
        int safeLine = Mathf.Clamp(line, 0, SOUND_RESOURCE_IDXS.Length - 1);
        
        if (pitch < 0 || pitch >= SOUND_RESOURCE_IDXS[safeLine].Length)
        {
            Debug.LogWarning($"⚠️ Geçersiz pitch değeri! Line: {line}, Pitch: {pitch}. Varsayılan olarak 0 kullanılıyor.");
            pitch = 0;
        }

        return SOUND_RESOURCE_IDXS[safeLine][pitch];
    }
    
    // ... geri kalan kodlar ...
}

```

Bu kapsamlı değişiklikleri uyguladığınızda, projenizin temel mekanikleri orijinal Java oyununun mantığına çok daha yakın hale gelecektir. Bu, hem nota senkronizasyonunu hem de ses doğruluğunu çözmeli ve oyununuza o aradığınız "hissi" geri kazandırmalıdır.