# Merkezi Şarkı Veritabanı Mimarisi Planı (`SongDatabasePlan.md`)

## 1. Hedef ve Gerekçe

**Hedef:** Orijinal oyunda `classicplayer.db` veritabanının gördüğü işlevi, modern ve yönetilebilir bir yapıyla Unity'de yeniden oluşturmak. `MUSIC.csv` dosyasındaki tüm şarkı meta verilerini (ID, başlık, sanatçı, tempo vb.) tek, merkezi ve oyun içindeki her script'in kolayca erişebileceği bir sistemde toplamak.

**Gerekçe:** Mevcut sistemde şarkı verileri farklı script'ler tarafından ayrı ayrı okunuyor ve yönetiliyor. Bu durum, veri tutarsızlıklarına, kod tekrarına ve bakım zorluklarına yol açıyor. Merkezi bir veritabanı, projenin veri katmanını sağlamlaştıracak, genişletilebilir hale getirecek ve olası hataları en aza indirecektir.

---

## 2. Mevcut Sistem Analizi ve Sorunlar

-   **Dağınık Veri Okuma:** `SongSelectionManager.cs`, `MUSIC.csv` dosyasını doğrudan okuyarak şarkı listesini kendisi oluşturuyor. Bu, bu veriye ihtiyaç duyan diğer sistemlerin (`GameplayManager` gibi) dolaylı yollardan erişmesine neden oluyor.
-   **Tanım Çakışması:** Projede birden fazla `SongData` tanımı bulunuyor (biri `ScriptableObject`, diğeri normal `class`). Bu, kod karmaşasına ve potansiyel hatalara yol açıyor.
-   **Kullanılmayan Script'ler:** `JsonMusicParser.cs` gibi eski ve artık işlevini yitirmiş script'ler projede kafa karışıklığı yaratıyor.

---

## 3. Önerilen Çözüm Mimarisi: 3 Adımlı Plan

### Adım 1: Merkezi Veri Dosyasını Oluşturan Editör Aracı

**`SongDatabaseProcessor.cs` adında yeni bir editör script'i oluşturulacak.**

-   **Görevi:**
    1.  `Assets/Resources/Database csv/MUSIC.csv` dosyasını okuyacak.
    2.  Her bir satırı, tüm bilgileri içeren standart bir `SongInfo` sınıfına (Adım 2'de tanımlanacak) ayrıştıracak (parse edecek).
    3.  Tüm şarkılardan oluşan bir `List<SongInfo>`'yu, `songs_database.json` adında tek bir JSON dosyası olarak `Assets/Resources/` klasörüne kaydedecek.
-   **Faydası:** Bu "tek seferlik" işlem sayesinde, oyunun her açılışında CSV dosyasını tekrar tekrar parse etme yükünden kurtulacağız. Oyun, optimize edilmiş ve hazır olan tek bir JSON dosyasından beslenecek.

### Adım 2: Veritabanı Yöneticisi (Singleton)

**`SongDatabase.cs` adında yeni bir singleton (tekil) yönetici sınıfı oluşturulacak.**

-   **Görevi:**
    1.  Oyunun herhangi bir yerinden ilk kez `SongDatabase.Instance` çağrıldığında, `Resources` klasöründeki `songs_database.json` dosyasını yükleyecek ve içeriğini hafızadaki bir `List<SongInfo>`'ya deserialze edecek.
    2.  Tüm diğer script'lerin şarkı bilgilerine erişebilmesi için basit ve kullanışlı metodlar sunacak:
        -   `public SongInfo GetSongById(int musicId)`
        -   `public List<SongInfo> GetAllSongs()`
        -   `public int GetTempoForSong(int musicId)`
-   **Faydası:** Oyun içindeki herhangi bir script, dosya okuma veya JSON parse etme gibi işlemlerle uğraşmadan, `SongDatabase.Instance.GetSongById(10)` gibi basit bir komutla "Turkish Delight" şarkısının tüm bilgilerine anında erişebilecek.

### Adım 3: Mevcut Script'lerin Yeni Sisteme Entegrasyonu

**Mevcut script'ler, artık veriyi doğrudan dosyadan okumak yerine `SongDatabase`'den alacak şekilde refaktör edilecek.**

-   **`DataStructures.cs`:** Tüm projede kullanılacak olan tek ve standart `SongInfo` sınıfı burada tanımlanacak. Diğer script'lerdeki mükerrer tanımlar silinecek.
-   **`SongSelectionManager.cs`:**
    -   `LoadSongsFromDatabase()` metodu tamamen değiştirilecek. Artık CSV okumak yerine, `songDropdown.Populate(SongDatabase.Instance.GetAllSongs())` gibi tek bir satırla şarkı listesini dolduracak.
-   **`GameplayManager.cs`:**
    -   Bir şarkı seçildiğinde, artık `SongData` objesi yerine sadece `musicId` alacak.
    -   Oyun seansını başlatırken, `int tempo = SongDatabase.Instance.GetTempoForSong(musicId);` gibi bir komutla doğru tempoyu merkezi veritabanından çekecek ve bunu `GameNoteCreator.LoadAndPrepareSong()` metoduna parametre olarak geçecek.

Bu plan, projenin veri temelini sağlam, merkezi ve profesyonel bir yapıya kavuşturacaktır. 