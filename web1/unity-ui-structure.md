# 🎮 Unity Mobile UI Design Tester - Modular Structure Plan

## 📁 File Structure

```
unity-ui-tester/
├── index.html                    # Ana sayfa - sadece temel yapı
├── css/
│   ├── base.css                 # Temel stiller, reset, common
│   ├── layout.css               # Grid, container, responsive
│   ├── components/
│   │   ├── main-menu.css        # Ana menü stilleri
│   │   ├── world-tour.css       # 3D dünya haritası
│   │   ├── shop.css             # Shop ve upgrade sistemi
│   │   ├── customization.css    # Enstrüman/stil seçimi
│   │   ├── leaderboard.css      # Liderlik tablosu
│   │   ├── hud.css              # Oyun içi HUD
│   │   ├── level-complete.css   # Level tamamlama
│   │   └── settings.css         # Ayarlar paneli
│   └── animations.css           # Tüm animasyonlar
├── js/
│   ├── app.js                   # Ana uygulama koordinatörü
│   ├── core/
│   │   ├── preview-manager.js   # Preview sistemi yönetimi
│   │   ├── modal-handler.js     # Modal açma/kapama
│   │   ├── interaction-handler.js # Click/hover olayları
│   │   └── progress-tracker.js  # Test ilerlemesi
│   ├── components/
│   │   ├── world-tour.js        # 3D globe etkileşimleri
│   │   ├── customization.js     # Kategori ve özellik yönetimi
│   │   ├── shop.js             # Shop etkileşimleri
│   │   └── leaderboard.js      # Sıralama etkileşimleri
│   └── utils/
│       ├── animations.js        # Animasyon yardımcıları
│       ├── responsive.js        # Responsive yardımcılar
│       └── constants.js         # Sabitler ve konfigürasyon
├── data/
│   ├── mockups.js              # Tüm component mockup'ları
│   ├── component-data.js       # Component bilgileri
│   └── analysis-data.js        # Analiz ve implement bilgileri
└── assets/
    ├── icons/                  # SVG iconlar
    ├── images/                 # Resimler
    └── fonts/                  # Özel fontlar (eğer varsa)
```

## 🎯 Dosya Sorumlulukları

### **index.html** (Ana Koordinatör)
- Temel HTML yapısı
- CSS ve JS dosyalarını import
- Loading ve error handling
- Meta tags ve SEO

### **CSS Dosyaları**
- **base.css**: Reset, variables, typography
- **layout.css**: Grid system, containers, responsive breakpoints
- **components/**: Her component'in kendi stili
- **animations.css**: Keyframes, transitions, hover effects

### **JavaScript Dosyaları**
- **app.js**: Ana koordinatör, initialization
- **core/**: Temel sistem fonksiyonları
- **components/**: Component-specific logic
- **utils/**: Yardımcı fonksiyonlar

### **Data Dosyaları**
- **mockups.js**: HTML template'leri
- **component-data.js**: Component metadata
- **analysis-data.js**: Implementation guides

## 🚀 Implementation Plan

### **Phase 1**: Core Structure
1. Ana index.html oluştur
2. Base CSS ve layout sistemi
3. App.js koordinatör sistemi
4. Preview manager temel yapısı

### **Phase 2**: Component Migration
1. Main Menu → ayrı dosyalar
2. World Tour → ayrı dosyalar  
3. Shop System → ayrı dosyalar
4. Customization → ayrı dosyalar

### **Phase 3**: Advanced Features
1. Leaderboard → ayrı dosyalar
2. HUD & Level Complete → ayrı dosyalar
3. Settings → ayrı dosyalar
4. Animation system optimization

### **Phase 4**: Polish & Optimization
1. Code splitting optimization
2. Lazy loading for components
3. Performance monitoring
4. Error handling enhancement

## 💡 Benefits

- ✅ **Maintainable**: Her component ayrı dosyada
- ✅ **Scalable**: Yeni component'ler kolayca eklenebilir
- ✅ **Collaborative**: Takım çalışması için uygun
- ✅ **Performance**: Lazy loading ile optimize
- ✅ **Debug**: Hata ayıklama çok kolay
- ✅ **Reusable**: Component'ler tekrar kullanılabilir

## 🔧 Next Steps

1. **Ana index.html** dosyasını minimalist halde oluştur
2. **Base CSS** sistemini kur
3. **App.js** koordinatörünü oluştur
4. Component'leri tek tek migrate et
5. Testing ve optimization

Bu yapı ile hem development hem de maintenance çok daha kolay olacak!
