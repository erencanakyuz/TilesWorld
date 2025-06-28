# TilesWorld - Music System Architecture Plan

**📅 Durum:** PLANLAMA AŞAMASI (Aralık 2024)  
**🎯 Hedef:** JSON-Based Interactive Music System  
**🎼 Kaynak:** OldJavaGame/classicplayer.db + JSON files

---

## 🎵 Mevcut JSON Analizi

### 1. JSON Yapısı (all_songs_notes.json & cannon_notes.json)

```json
{
  "music_id": 1,           // Şarkı ID'si (1-7 arası)
  "seq": 0,               // Sequence/bölüm numarası (0-16 arası)
  "line1": "nota_data",   // Lane 1 (6 lane toplam)
  "line2": "nota_data",   // Lane 2
  "line3": "nota_data",   // Lane 3
  "line4": "nota_data",   // Lane 4
  "line5": "nota_data",   // Lane 5
  "line6": "nota_data"    // Lane 6
}
```

### 2. Nota Format Analizi

**Format:** `pitch,duration/pitch,duration/_,_/`

- **pitch**: 0-26 arası (farklı enstrüman notaları)
- **duration**: 1-9 arası (nota süreleri)
- **_,_**: Boş nota (silence)
- **/** ayracı**: Zaman adımı (1/16 nota?)
- **Özel Kodlar:**
  - `_,18`: Pause/Wait command
  - `_,19`: Tempo değişimi?
  - `_,20`: Special effect?

### 3. Şarkı Listesi (JSON'dan çıkarılan)

```
Music ID 1: Cannon's Song (16 sequences)
Music ID 2: Piano Dreams (16 sequences) 
Music ID 3: Harp Fantasy (10 sequences)
Music ID 4: Guitar Hero (10 sequences)
Music ID 5: Jazz Fusion (8 sequences)
Music ID 6: Classical (12 sequences)
Music ID 7: Epic Theme (4 sequences)
```

---

## 🎮 Mevcut Kod Sistemi Analizi

### 1. GameNoteCreator.cs - ✅ TEMIZ
- **Durum:** Algoritma complete, duplicate yok
- **Özellikler:** Anti-clustering, direction balance, chord merging
- **Eksik:** JSON loading implementation

### 2. NoteRenderer.cs - ✅ TEMIZ  
- **Durum:** Perspective rendering working, no duplicates
- **Özellikler:** Object pooling, perspective effects, hit detection
- **Eksik:** Pitch-based rendering

### 3. GameplayManager.cs - ✅ TEMIZ
- **Durum:** Game flow management, no duplicates  
- **Özellikler:** Countdown, song loading, state management
- **Eksik:** JSON integration

### 4. GameManager.cs - ✅ TEMIZ
- **Durum:** State management working, no duplicates
- **Özellikler:** Singleton pattern, scene management, player data
- **Eksik:** Song selection integration

### 5. AudioManager.cs - ⚠️ İNCELENECEK
- **Durum:** Henüz analiz edilmedi
- **Gerekli:** Interactive music system

---

## 🎼 İMPLEMENTASYON PLANI

### AŞAMA 1: JSON Parser System 📋

**Hedef:** JSON'dan oyuna nota verilerini çekme

**Gerekli Sınıflar:**
```csharp
// JSON Data Structures
[System.Serializable]
public class SongSequence
{
    public int music_id;
    public int seq;
    public string line1, line2, line3, line4, line5, line6;
}

// Processed Note Data  
public class ProcessedNote
{
    public int lane;        // 0-5 (line1-line6)
    public int pitch;       // 0-26 (instrument note)
    public float duration;  // 1-9 (note length)
    public float timeMs;    // When to play
    public bool isSpecial;  // Special command (_,18 etc)
}

// Song Database
public class SongDatabase : ScriptableObject
{
    public List<SongInfo> songs;
    public Dictionary<int, List<ProcessedNote>> songNotes;
}
```

**Yeni Dosyalar:**
- `JsonMusicParser.cs` - JSON okuma ve parsing
- `SongDatabase.cs` - ScriptableObject for song data
- `MusicDefinitions.cs` - Enums and constants

### AŞAMA 2: Interactive Music System 🎶

**Hedef:** Pitch-based enstrüman seslerini çalma

**Audio System Upgrade:**
```csharp
public class InteractiveMusicSystem : MonoBehaviour
{
    // Instrument Sound Banks
    Dictionary<InstrumentType, AudioClip[]> instrumentSounds;
    
    // Real-time Note Playing
    public void PlayNote(int pitch, InstrumentType instrument, float volume);
    public void PlayChord(List<int> pitches, InstrumentType instrument);
    
    // Dynamic Music Generation
    public void StartInteractiveMode(SongData song);
    public void UpdateMusicBasedOnPerformance(float accuracy);
}
```

**Enstrüman Mapping:**
```csharp
// Pitch to AudioClip mapping
Piano: pitch 0-26 → piano_snd000.ogg to piano_snd026.ogg
Harp: pitch 0-26 → harp_snd000.ogg to harp_snd026.ogg  
Guitar: pitch 0-26 → acustic_guitar_snd000.ogg to acustic_guitar_snd026.ogg
```

### AŞAMA 3: Gameplay Integration 🎯

**GameNoteCreator Enhancement:**
```csharp
public class GameNoteCreator : MonoBehaviour
{
    // JSON Integration
    public void LoadSongFromJson(int musicId);
    private List<ProcessedNote> ParseJsonSequences(List<SongSequence> sequences);
    
    // Enhanced Note Generation  
    public List<GameNoteInfo> GetNotesWithPitch(float deltaTime);
    private GameNoteInfo CreateNoteFromProcessed(ProcessedNote processedNote);
}
```

**NoteRenderer Enhancement:**
```csharp
public class NoteRenderer : MonoBehaviour  
{
    // Pitch-Based Rendering
    public void RenderNoteWithPitch(GameNoteInfo note);
    private Material GetMaterialForPitch(int pitch);
    private Color GetColorForInstrument(InstrumentType instrument);
    
    // Visual Feedback
    public void ShowPitchIndicator(int pitch, Vector3 position);
}
```

### AŞAMA 4: Song Selection System 🎵

**SongSelectionManager Enhancement:**
```csharp
public class SongSelectionManager : MonoBehaviour
{
    // JSON-based Song Loading
    private void LoadSongsFromJson();
    private SongData CreateSongDataFromJson(int musicId);
    
    // Preview System
    public void PreviewSong(int musicId, float startTime = 0f);
    public void StopPreview();
    
    // Difficulty Analysis
    public DifficultyLevel CalculateDifficulty(List<ProcessedNote> notes);
}
```

---

## 🔧 TEKNİK DETAYLAR

### JSON Loading Strategy
```csharp
// Runtime JSON Loading
TextAsset jsonAsset = Resources.Load<TextAsset>("Song_Note_Jsons/all_songs_notes");
List<SongSequence> sequences = JsonUtility.FromJson<List<SongSequence>>(jsonAsset.text);

// ScriptableObject Caching (Editor-time)
[CreateAssetMenu]
public class SongDatabase : ScriptableObject
{
    [Header("🎵 Generated from JSON")]
    public SongInfo[] songs;
    
    [Header("📊 Statistics")] 
    public int totalNotes;
    public float totalDuration;
}
```

### Timing System
```csharp
// Original Game Timing (from JSON analysis)
float timePerStep = (60f / bpm) / 4f;  // 1/16 note duration
float noteStartTime = sequenceTime + (stepIndex * timePerStep);

// Special Commands Handling
if (pitch == -1 && duration == 18) // Pause command
{
    // Add pause in note generation
}
if (pitch == -1 && duration == 19) // Tempo change
{
    // Modify BPM
}
```

### Performance Optimization
```csharp
// JSON Pre-processing (Editor)
[MenuItem("Tools/Process Music JSON")]
static void ProcessMusicJson()
{
    // Convert JSON to optimized binary format
    // Generate ScriptableObject assets
    // Create audio clip references
}

// Runtime Efficiency
public class OptimizedSongData : ScriptableObject
{
    [SerializeField] private byte[] compressedNoteData;
    [SerializeField] private AudioClip[] audioClips;
    
    public IEnumerable<ProcessedNote> GetNotesInTimeRange(float start, float end);
}
```

---

## 📱 MOBILE OPTIMIZATION

### Audio Pool Management
```csharp
public class MobileAudioPool : MonoBehaviour
{
    // Limited concurrent audio sources for mobile
    [SerializeField] private int maxConcurrentNotes = 8;
    private Queue<AudioSource> availableSources;
    
    // Pitch-based audio management
    public void PlayNoteOptimized(int pitch, InstrumentType instrument);
    private void StopOldestNote(); // When pool is full
}
```

### Memory Management
```csharp
// Streaming JSON data instead of loading all
public class StreamingSongLoader : MonoBehaviour
{
    private int currentSequence = 0;
    private List<ProcessedNote> nextSequenceNotes;
    
    // Load next sequence while current one plays
    private IEnumerator LoadNextSequenceAsync(int musicId, int sequenceId);
}
```

---

## 🎯 SONUÇ ve ÖNCE YAPILACAKLAR

### İmmediate Actions (Bugün)
1. ✅ **Music.md planı oluşturuldu**
2. 🔄 **JsonMusicParser.cs** oluştur
3. 🔄 **SongDatabase.cs** oluştur  
4. 🔄 **InteractiveMusicSystem.cs** güncelle

### Short Term (Bu Hafta)
1. JSON parsing sistemini implement et
2. Test şarkısı ile pitch-based audio test et
3. SongSelectionManager'a JSON integration yap
4. Basic interactive music system çalıştır

### Long Term (Önümüzdeki Hafta)  
1. Advanced algorithms (anti-clustering etc.) JSON ile entegre et
2. Mobile optimization ve performance tuning
3. Visual feedback system (pitch indicators, note colors)
4. Full song library implementation

---

**🎵 Bu sistemle original Java oyununun müzik dinamiklerini modern Unity'de yeniden yaratacağız!** 