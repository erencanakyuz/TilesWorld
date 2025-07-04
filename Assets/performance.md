# Performans ve Refactoring Görev Listesi

Bu doküman, projenin mevcut durumunda ele alınması gereken performans, kod kalitesi ve "oyun hissi" ile ilgili kalan görevleri listeler.

====================================================================
### 🚩 ÖNCELİKLİ GÖREVLER (MİMARİ)
====================================================================

Bu maddeler, oyunun temel yapısını ve hissini en çok etkileyecek olan, daha önce konuşulmuş ama ertelenmiş görevlerdir.

2.  **Referans Yönetimi (`FindFirstObjectByType` Kaldırma)**
    - **Sorun:** `GameplayManager` gibi sınıflar, diğer sistemlere olan referansları oyun başında arayarak buluyor. Bu hem yavaş hem de kırılgandır.
    - **Görev:** Bu referansları `[SerializeField]` olarak bırakıp, `Awake`/`Start` içindeki atamaları kaldırarak Unity Inspector üzerinden manuel olarak atamak.

3.  **Merkezi Loglama Sistemi**//test 1
    - **Sorun:** `Debug.Log` çağrıları projenin çeşitli yerlerine dağılmış durumda ve release build'lerde bile performans maliyeti oluşturuyor.
    - **Görev:** Tüm loglama işlemlerini, release build'lerde otomatik olarak devre dışı kalacak merkezi bir `Logger` sınıfı üzerinden yapmak.

====================================================================
### ⚠️ KALAN DİĞER İYİLEŞTİRMELER
====================================================================

Yukarıdaki büyük görevler tamamlandıktan sonra ele alınabilecek, daha küçük çaplı ama önemli diğer maddeler:

-   **`GameManager`:**
    -   `PlayerPrefs.Save()` çağrısını daha kontrollü bir hale getirmek (örn: sadece ayarlar menüsünden çıkarken).
    -   Debug amaçlı tuş kontrollerini (`K` tuşu gibi) `#if UNITY_EDITOR` bloğuna almak.
    -   Düşük hareketli menü ekranlarında GPU ve pil tasarrufu için `targetFrameRate`'i `30`'a düşürmek. (Bu madde kısmen yapıldı, ancak tam kontrolü ve doğruluğu sağlanmalı).

-   **`GameplayManager`:**
    -   Şarkı süresini tahmin etmek (`EstimateDuration`) yerine, `SongDatabase`'den gelen gerçek süreyi kullanmak.

-   **`GameNoteCreator`:**
    -   `NOTE_LENGTH_FACTORS` dizisindeki kullanılmayan (15 ve sonrası) elemanları kaldırarak çok küçük bir bellek tasarrufu sağlamak.
    -   GC (Çöp Toplama) yükünü azaltmak için `GameNoteInfoPackage` için bir `ObjectPool` kullanmayı değerlendirmek.

-   **`SongDatabaseProcessor` (Editor Script):**
    -   Mevcut basit CSV parser'ının, gelecekte tırnak içinde virgül içeren şarkı adları gibi durumlarda hata verebileceğini unutmamak. (Şu an için bir sorun değil).

