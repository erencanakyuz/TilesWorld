# Nota Yorumlama (Parsing) Mekaniği Refaktör Planı

## 1. Amaç

Bu belgenin amacı, orijinal Java oyununun ham nota verisini (`MUSIC_COLUMNS.csv`) okuyup, zamanlaması yapılmış, oynanabilir nota paketlerine dönüştürme mantığını detaylı bir şekilde analiz etmek ve bu mantığı Unity projesindeki `GameNoteCreator.cs` script'ine hatasız bir şekilde uygulamak için bir yol haritası çizmektir.

Mevcut sistemin en büyük yanılgısı, nota dizilimini (`line1`, `line2` vs.) yatay olarak okumasıdır. Orijinal oyun ise tüm şeritleri **dikey bir zaman dilimi** (vertical time slice) olarak, senkronize bir şekilde işler. Bu refaktör, bu temel mantık hatasını düzeltecektir.

---

## 2. Orijinal Java Oyununun Veri Akışı ve Mantığı

Analiz, orijinal oyunun nota verisini `classicplayer.db` veritabanından alıp `GameNoteCreator.java`'ya nasıl ilettiğini adım adım takip eder.

### Adım 2.1: Veritabanından Ham Verinin Okunması (`UseDataBase.java`)

Her şey, `UseDataBase.java` sınıfının veritabanından nota string'lerini okumasıyla başlar.

-   **`getMusicColumnsList()` metodu:** Bu metod, `MUSIC_COLUMNS` tablosundan bir şarkıya ait tüm sıralı nota verilerini (`seq` 0, 1, 2...) çeker.
-   **`makeLine()` metodu:** Veritabanından gelen her bir `line` string'i (örn: `"2,2/_,18/4,1/..."`) bu metodda işlenir.
    -   `line.split("/")`: String, `/` karakterinden bölünerek zaman dilimlerine (subdivision) ayrılır. Örn: `["2,2", "_,18", "4,1"]`.
    -   `part.split(",")`: Her bir dilim de `,` karakterinden bölünerek `[pitch, duration]` çiftlerine ayrılır.
-   **Çıktı:** Bu sürecin sonunda, her bir şarkı sekansı için `List<Integer[][][]>` yapısında, yani `[Şerit][Zaman Dilimi][Pitch/Duration]` şeklinde üç boyutlu bir tam sayı dizisi oluşturulur. Bu yapı, ham verinin ilk işlenmiş halidir.

### Adım 2.2: Verinin Anlamlı Nota Paketlerine Dönüştürülmesi (`PlayData.getTabList`)

Bu, tüm sürecin kalbidir. Ham 3D dizi, burada oynanabilir nota listesine dönüşür.

#### A. Temel Zaman Biriminin Hesaplanması

Metodun başında, oyunun en küçük ritim birimi olan **32'lik nota süresi** milisaniye cinsinden hesaplanır. Bu, tüm zamanlamanın temelini oluşturur.

```java
int k = 60000 / tempo / 8; // (60000ms / 120BPM) / 8 = 62.5ms
```

#### B. Dikey Zaman Dilimi (Vertical Slicing) Mantığı

Bu, anlaşılması gereken en kritik konsepttir. `getTabList`, şeritleri tek tek okumaz. Bunun yerine, tüm şeritleri aynı anda, dikey olarak dilimleyerek ilerler.

Aşağıdaki `turkish_delight_mozart.json` örneğini ele alalım:

```
line1: "2,2/_,18/4,1/2,1/..."
line2: "_,_/6,1/4,1/3,1/..."
line3: "_,_/_,_/_,_/_,_/..."
...
```

Metod, bu veriyi şöyle işler:

-   **Zaman Dilimi 0:**
    -   line1: `2,2`
    -   line2: `_,_`
    -   line3: `_,_`
    -   ...tüm `line`'lardaki 0. indeksteki veriyi alır.

-   **Zaman Dilimi 1:**
    -   line1: `_,18`
    -   line2: `6,1`
    -   line3: `_,_`
    -   ...tüm `line`'lardaki 1. indeksteki veriyi alır.

Bu "dikey tarama", hangi notaların **aynı anda** çalınması gerektiğini belirler.

#### C. Paket Zamanlaması (`oneTempo`)

Her bir dikey zaman dilimi için:
1.  O dilimdeki **tüm notaların `duration` (süre) değerleri** incelenir.
2.  Bu sürelerden **en büyüğü** (`maxDuration`) bulunur.
3.  Bu `maxDuration` değeri, `noteLengthArr` (müzik teorisindeki nota süre çarpanları dizisi) kullanılarak milisaniye cinsinden bir süreye çevrilir. Bu süre, oluşturulan nota paketinin `oneTempo` değeri olur.

**`oneTempo`'nun Anlamı:** O anki nota paketinden sonra, bir sonraki nota paketinin spawn olması için **beklenmesi gereken süredir.** Bu, oyunun ritmini ve senkronizasyonunu sağlar.

#### D. Çıktı: `List<NoteInfo>`

Bu sürecin sonunda `PlayData.getTabList`, `GameNoteCreator`'a verilmek üzere bir `List<NoteInfo>` döndürür. Bu listedeki her bir `NoteInfo` objesi şunları içerir:
-   `int[] pitch`: O dikey zaman dilimindeki 6 şeridin `pitch` değerlerini içeren bir dizi (`-1` ise nota yok).
-   `int oneTempo`: Bir sonraki `NoteInfo`'ya kadar geçecek süre (ms).

---

## 3. Unity'ye Doğru Uyarlama Planı (`GameNoteCreator.cs`)

Mevcut `GameNoteCreator.cs`'deki `GetSubdivisionData` ve ilgili mantıklar, yukarıda açıklanan dikey dilimleme ve `oneTempo` hesaplamasını doğru yapmamaktadır. Bu nedenle, bu script'i orijinal mantığı %100 yansıtacak şekilde yeniden düzenlemeliyiz.

### Adım 3.1: Yeni Bir Veri İşleme Metodu Oluştur

`GameNoteCreator.cs` içinde, `PlayData.getTabList`'in işlevini üstlenecek yeni bir `private` metod oluşturulmalıdır. Örneğin: `private List<TemporalNoteInfo> ProcessRawChart(List<NoteChartSequence> rawChart, int tempo)`

### Adım 3.2: Bu Metodun İç Mantığı

Bu yeni metodun izlemesi gereken adımlar şunlar olmalıdır:

1.  **Tüm Veriyi Topla:** Gelen `rawChart` listesindeki tüm sekansları (`seq` 0, 1, 2...) tek bir yapı altında birleştir.
2.  **Maksimum Dilim Sayısını Bul:** Tüm şeritlerdeki `/` ile ayrılmış en fazla parça sayısını bularak toplam dikey zaman dilimi sayısını belirle.
3.  **Dikey Dilimleri Haritala:** Bir `Dictionary<int, List<(int pitch, int duration)>>` gibi bir yapı oluştur. `int` anahtarı, zaman diliminin indeksini temsil eder.
4.  **Haritayı Doldur:**
    -   Her bir `line` (şerit) için `line.Split('/')` yap.
    -   Elde edilen her bir `pitch,duration` parçasını, zaman dilimi indeksini anahtar olarak kullanarak Dictionary'e ekle.
5.  **Anlamlı Paketlere Dönüştür:**
    -   Şimdi bu Dictionary üzerinde `0`'dan maksimum dilim sayısına kadar dön.
    -   Her bir zaman dilimi (`i`) için:
        -   Yeni bir geçici nota paketi (`TemporalNoteInfo`) oluştur.
        -   Bu zaman dilimindeki tüm notaların `duration` değerlerinden en büyüğünü (`maxDuration`) bul.
        -   Temel zaman birimini (`baseTimingMs`) kullanarak ve `noteLengthArr` çarpanını `maxDuration` ile çarparak paketin `oneNote` (veya `timingMs`) değerini hesapla.
        -   Bu zaman dilimindeki 6 şeride ait `pitch` değerlerini paket içindeki bir `int[]` dizisine ata.
        -   Eğer dilimde en az bir nota varsa, bu paketi nihai listeye ekle.
6.  **Nihai Listeyi Döndür:** Bu işlem bittiğinde, orijinal oyundaki `List<NoteInfo>`'ya birebir karşılık gelen, doğru zamanlanmış ve gruplanmış bir paket listesi elde etmiş olacağız.

Bu plan, nota yorumlama mantığını orijinal oyunun kanıtlanmış ve doğru çalışan sistemine geri döndürerek mevcut senkronizasyon ve nota gruplama sorunlarını kökünden çözecektir. 