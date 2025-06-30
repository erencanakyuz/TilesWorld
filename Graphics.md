# TilesWorld - Görsel Geliştirme ve Yenileme Planı ("Project Aura")

Bu döküman, TilesWorld oyununun görsel estetiğini modern, dinamik ve ritimle bütünleşik bir yapıya kavuşturma planını detaylandırmaktadır. Amaç, asset kullanımını minimumda tutarak, kod, shader ve DOTween gibi araçlarla etkileyici ve performanslı bir görsel dünya yaratmaktır.

## 1. Genel Konsept: "Synth-Grid" - Soyut ve Ritmik Görsel Dünya

Mevcut gerçekçi piyano temasından uzaklaşıp, TRON ve synthwave estetiğinden ilham alan, soyut, neon ve enerjik bir konsepte geçiyoruz.

*   **Renk Paleti:** Zemin ve arkaplan için koyu lacivert ve mor tonları. Notalar, vuruş bölgesi ve efektler için canlı, parlak ve "emissive" (ışık yayan) renkler (cyan, magenta, sarı).
*   **Temel Prensip:** Oyundaki her görsel element, ritme veya oyuncunun aksiyonlarına tepki verecek. Statik hiçbir şey kalmayacak. Dünya, müzikle birlikte nefes alacak.

---

## 2. Nota (Tile) Tasarımı: "Enerji Kristalleri"

Mevcut basit prefab'lar yerine, her biri yaşayan birer enerji parçası gibi görünen notalar oluşturacağız.

*   **Geometri:** Basit küp veya dörtgen prefab'ını koruyacağız. Karmaşık modellere gerek yok. Güç, materyalden ve efektlerden gelecek.
*   **Shader ile Dönüşüm (Kod Odaklı):**
    *   **Yeni Materyal:** Unity'nin Shader Graph'ını kullanarak özel bir "Kristal" shader'ı oluşturulacak.
    *   **Özellikler:**
        *   **Emissive Core (Işık Yayan Çekirdek):** Notaların merkezi, enstrümana veya notanın sıklığına göre atanmış parlak bir renkte ışık yayacak. Bu, `HDR` renkler kullanılarak "Bloom" post-processing efektiyle birleştiğinde harika görünecek.
        *   **Fresnel Glow (Kenar Işıması):** Notanın kenarlarına yaklaştıkça artan, farklı renkte bir ışıma efekti. Bu, notaya hacim ve "enerji alanı" hissi katacak.
        *   **Scanlines (Tarama Çizgileri):** Shader'ın içine eklenen, nota yüzeyinde aşağı doğru yavaşça akan ince, parlak çizgiler. Hızı, şarkının temposuna (BPM) bağlanabilir.
*   **Doğuş ve Hareket Animasyonu (DOTween):**
    *   **Kod Entegrasyonu:** `NoteRenderer.cs` -> `SpawnNote()` metodu içinde:
        *   Nota spawn olduğunda `transform.DOScale()` ile anlık olarak küçükten normale doğru büyüyecek (0.2 saniye gibi bir sürede).
        *   Materyalinin alpha değeri `DOFade()` ile sıfırdan normale gelecek. Bu, notaların "yoktan var olma" efektini yaratır.
*   **Parçacık Efekti (Particle System):**
    *   Her notanın prefab'ına, arkasından ince bir "iz" bırakan bir `ParticleSystem` alt-objesi eklenecek.
    *   Bu parçacıkların rengi, notanın ait olduğu lane'in rengiyle aynı olacak ve yavaşça kaybolacaklar.

---

## 3. Vuruş Bölgesi (Hit Zone) Tasarımı: "Ritim Halkası"

Mevcut görünmez collider yerine, oyuncunun odak noktası olacak canlı bir hedef alanı tasarlayacağız.

*   **Geometri:** Sahnede bulunan her `HitZoneTrigger` objesinin altına, ince bir `Torus` (simit) veya yatay bir `Quad` (çizgi) yerleştirilecek.
*   **Görsel Stil:** Notalar için oluşturulan "Kristal" shader'ının bir varyasyonu kullanılacak. Sürekli olarak parlak ve belirgin olacak.
*   **Ritmik Davranış (Kod Odaklı):**
    *   **Yeni Script:** `HitZoneVisualizer.cs` adında yeni bir script oluşturulacak ve bu görsel objelere eklenecek.
    *   **Kod Entegrasyonu:**
        *   Bu script, `AudioManager` veya `GameplayManager`'dan şarkının mevcut BPM'ini alacak.
        *   `Update()` içinde, `transform.DOScale` veya `Mathf.Sin` kullanarak objenin boyutunu veya materyalinin parlaklığını tempo ile senkronize bir şekilde sürekli olarak "vurduracak" (pulse efekti).
        *   Oyuncu bir notaya **başarıyla vurduğunda**, `HitZoneManager`'dan bir event fırlatılacak. `HitZoneVisualizer` bu event'i dinleyecek ve `transform.DOPunchScale()` ile anlık bir büyüme/parlama efekti yapacak. Vuruşun hassasiyetine (`Perfect`, `Good`) göre parlama rengi ve şiddeti değişebilir.

---

## 4. Zemin ve Arkaplan Tasarımı: "Sonsuz Neon Grid"

Oyuncuya hız ve derinlik hissi veren, dinamik bir zemin oluşturulacak.

*   **Geometri:** Sadece basit bir `Plane` (düzlem) objesi yeterli.
*   **Shader ile Sonsuzluk (Kod Odaklı):**
    *   **Yeni Materyal:** Shader Graph ile bir "Grid" shader'ı oluşturulacak.
    *   **Özellikler:**
        *   Basitçe parlak çizgilerden oluşan bir grid çizecek.
        *   En önemli özellik: Shader'ın **UV Offset**'i olacak.
*   **Hareket (Kod Odaklı):**
    *   **Yeni Script:** `ScrollingTexture.cs` adında yeni bir script oluşturulacak ve bu zemin objesine eklenecek.
    *   **Kod Entegrasyonu:**
        *   Bu script, `Update()` içinde, materyalin UV offset'ini sürekli olarak değiştirecek (`material.mainTextureOffset += ...`).
        *   Kayma hızı, `NoteRenderer`'daki `speedMultiplier` değişkenine bağlanabilir. Notalar hızlandıkça, zemin de daha hızlı akacak.
*   **Arkaplan:** Uzaklara, yavaşça hareket eden, `Particle System` ile yapılmış basit "yıldız tarlası" veya "nebula" efektleri eklenebilir.

---

## 5. Efektler ve Geri Bildirimler: "Juice"

Oyunun daha tatmin edici ve "canlı" hissettirmesi için yapılacaklar.

*   **Vuruş Efektleri:**
    *   `UIManager`'daki `perfectHitEffect`, `goodHitEffect` prefab'ları, daha modern `ParticleSystem`'ler ile değiştirilecek. Örneğin, "Perfect" vuruşta etrafa yayılan bir halka (shockwave) ve parlak parçacıklar. "Good" vuruşta ise daha sade bir parlama.
*   **UI Animasyonları (DOTween):**
    *   **Kod Entegrasyonu:** `UIManager.cs` içinde:
        *   `UpdateScore()`: Skor güncellendiğinde, `scoreText` objesi `transform.DOPunchScale()` ile anlık olarak büyüyüp küçülecek.
        *   `UpdateCombo()`: Combo sayısı arttığında, `comboText` parlayacak ve sallanacak (`DOShakePosition`). Her 10'lu combo'da daha büyük bir efekt olacak.
*   **Kamera Efektleri:**
    *   **Kod Entegrasyonu:** `GameManager` veya ayrı bir `CameraManager` script'i içinde:
        *   **Perfect Hit Shake:** "Perfect" bir vuruş yapıldığında, `mainCamera`'ya çok kısa ve hafif bir "sallanma" efekti (`transform.DOShakePosition`) uygulanacak. Bu, vuruşun etkisini ve gücünü artırır.
        *   **Hız Çizgileri:** Yüksek combo'lara ulaşıldığında, ekranın kenarlarından merkeze doğru akan, `ParticleSystem` ile yapılmış hız çizgileri belirebilir.

## 6. Uygulama Planı

1.  **Altyapı:** Projeye `DOTween` kütüphanesini Asset Store'dan ekle ve kur.
2.  **Shader Geliştirme:** İlk olarak Shader Graph kullanarak **Kristal Shader** ve **Grid Shader**'larını oluştur.
3.  **Prefab Güncelleme:** `NotePrefab`'i yeni shader ve parçacık sistemiyle güncelle. `HitZoneTrigger` objelerine görsel alt-objeleri ekle.
4.  **Temel Scripting:** `ScrollingTexture.cs` ve `HitZoneVisualizer.cs` scriptlerini oluştur ve temel mantıklarını yaz.
5.  **Entegrasyon ve Animasyon:** `NoteRenderer`, `UIManager`, `GameManager` gibi mevcut script'lere girerek ilgili DOTween animasyonlarını (`DOScale`, `DOPunchScale` vb.) ve efekt tetikleme kodlarını ekle.
6.  **Post-Processing:** Sahneye bir `Volume` objesi ekle. `Bloom` (parlamalar için en önemlisi) ve `Vignette` gibi efektleri ekleyerek son dokunuşları yap.
7.  **Test ve Ayar:** Bütün sistemi çalıştırıp, animasyon sürelerini, renkleri ve efektlerin yoğunluğunu oyunun hissiyatına en uygun hale getirene kadar ayarla. 