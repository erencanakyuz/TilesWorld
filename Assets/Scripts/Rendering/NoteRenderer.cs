using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using DG.Tweening;

/// <summary>
/// NoteRenderer - Visual Heart of the Game
/// Based on original WorldRenderer.java with perspective "conveyor belt" effect
/// Implements: Z-depth movement, perspective scaling, rotation effects
/// </summary>
public class NoteRenderer : MonoBehaviour
{
    [Header("🔧 Debugging")]
    [SerializeField] private bool showDebugLogs = false;

    [Header("🎨 Rendering")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform noteParent;
    [SerializeField] private Color[] laneColors;
    [SerializeField] private float noteLengthMultiplier = 1.0f;
    [SerializeField] private float cameraAngle = 45f;

    [Header("📏 Sizing and Positioning")]
    [SerializeField] private int laneCount = 6;
    [SerializeField] private float laneWidth = 2.4f;       // Genişletildi: 1.8f → 2.4f
    [Tooltip("The Z-coordinate of the hit line where notes should arrive.")]
    [SerializeField] private float hitZoneZ = 0.0f;

    [Header("🚀 Note Movement")]
    [Tooltip("The constant speed at which notes travel towards the player.")]
    [SerializeField] private float speedMultiplier = 12.0f;    // Increased default speed
    [Tooltip("The Z-coordinate where notes are spawned.")]
    [SerializeField] private float spawnZ = 25f;

    [Header("📊 Performance & Debug")]
    [SerializeField] private bool enableObjectPooling = true;
    [SerializeField] private int poolSize = 50;

    // Object pooling system (from MD analysis)
    private Queue<GameObject> notePool;
    // DEĞİŞİKLİK: activeNotes listesi artık animasyonları yönetmek için kullanılmıyor. Sadece debug için tutulabilir.
    private List<GameObject> activeNotesForDebug;
    private int totalNotesRendered = 0;

    private Camera mainCamera;
    private Vector3[] lanePositions;
    private int activeNoteCount = 0;

    void Awake()
    {
        InitializeRenderer();
    }

    void Start()
    {
        SetupLanes();
        SetupCamera();
        CheckSceneLighting();
    }

    // DEĞİŞİKLİK: Update() metodu ve ilgili yardımcıları (UpdateActiveNotes, UpdateNoteTextures vs.) TAMAMEN SİLİNDİ.
    // Artık nota hareketi DOTween tarafından yönetiliyor.

    void CheckSceneLighting()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        if (lights.Length == 0)
        {
            Debug.LogWarning("🎨 No lights found in scene! Notes may not be visible.");
        }
    }

    void InitializeRenderer()
    {
        notePool = new Queue<GameObject>();
        activeNotesForDebug = new List<GameObject>(); // Sadece debug için

        if (enableObjectPooling)
            CreateNotePool();
    }

    void CreateNotePool()
    {
        if (notePrefab == null || noteParent == null) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject note = Instantiate(notePrefab, noteParent);
            // YENİ: Prefab'e NoteAnimator component'ini eklediğinizden emin olun!
            if (note.GetComponent<NoteAnimator>() == null)
            {
                note.AddComponent<NoteAnimator>();
            }
            note.SetActive(false);
            notePool.Enqueue(note);
        }
    }

    void SetupLanes()
    {
        lanePositions = new Vector3[laneCount];
        for (int i = 0; i < laneCount; i++)
        {
            float xOffset = (i - 2.5f) * 1.8f;
            lanePositions[i] = new Vector3(xOffset, 0, 0);
        }
    }

    void SetupCamera()
    {
        mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        if (mainCamera != null)
        {
            mainCamera.transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);
        }
    }

    #region Note Management

    public void SpawnNotes(List<GameNoteInfo> notes, double dspTime)
    {
        foreach (var note in notes)
        {
            SpawnNote(note); // dspTime artık animasyon için kullanılmıyor, süre hesaplanıyor.
        }
    }

    // DEĞİŞİKLİK: SpawnNote metodu artık animatörü başlatıyor.
    void SpawnNote(GameNoteInfo noteInfo)
    {
        GameObject noteObject = GetPooledNote();
        if (noteObject == null) return;

        Renderer noteRenderer = noteObject.GetComponent<Renderer>();
        if (noteRenderer == null) return;

        // Spawn pozisyonunu hesapla
        Vector3 spawnPosition;
        if (lanePositions != null && noteInfo.idx >= 0 && noteInfo.idx < lanePositions.Length)
        {
            spawnPosition = lanePositions[noteInfo.idx];
        }
        else
        {
            float xOffset = (noteInfo.idx - (laneCount - 1) * 0.5f) * laneWidth;
            spawnPosition = new Vector3(xOffset, 0, 0);
        }
        spawnPosition.z = this.spawnZ; // Uzakta spawn et

        // Obje transformunu ayarla
        noteObject.transform.position = spawnPosition;
        float noteScale = laneWidth * 0.7f;
        noteObject.transform.localScale = new Vector3(noteScale, 1.0f, noteScale * noteLengthMultiplier);
        noteObject.SetActive(true);

        noteObject.tag = "Note";
        var wrapper = noteObject.GetComponent<NoteWrapper>() ?? noteObject.AddComponent<NoteWrapper>();
        wrapper.gameNoteInfo = noteInfo;

        // YENİ ANİMASYON MANTIĞI
        var animator = noteObject.GetComponent<NoteAnimator>();
        animator.Initialize(this, noteInfo);

        // Hedef pozisyon ve süreyi hesapla
        // DEĞİŞİKLİK: Hedef artık vuruş çizgisi değil, onun arkasındaki bir "imha" noktası.
        Vector3 hitPosition = new Vector3(spawnPosition.x, spawnPosition.y, hitZoneZ);
        float missDistance = 5f; // Vuruş çizgisinden ne kadar sonra kaybolacağı
        Vector3 missPosition = new Vector3(spawnPosition.x, spawnPosition.y, hitZoneZ - missDistance);

        // Vuruş çizgisine olan süreyi temel alıyoruz, ama hareket imha noktasına kadar devam ediyor.
        float travelTime = GetNoteTravelTime();
        // Toplam süreyi, mesafeyle orantılı olarak hesaplıyoruz ki hız sabit kalsın.
        float totalTravelTime = travelTime * (Vector3.Distance(spawnPosition, missPosition) / Vector3.Distance(spawnPosition, hitPosition));

        // wrapper.dspHitTime'ı doğru hesaplamak için VURUŞ ÇİZGİSİNE olan süreyi kullanmalıyız.
        var travelTimeToHitZone = GetNoteTravelTime();
        wrapper.dspHitTime = AudioSettings.dspTime + travelTimeToHitZone;

        // Animatörü başlat
        animator.AnimateSpawnAndFlow(missPosition, totalTravelTime);

        activeNotesForDebug.Add(noteObject);
        totalNotesRendered++;
    }

    GameObject GetPooledNote()
    {
        if (enableObjectPooling)
        {
            if (notePool.Count == 0)
            {
                if (showDebugLogs) Debug.LogWarning("Pool empty, expanding is not implemented. Increase pool size.");
                // Pool'u dinamik genişletme eklenebilir. Şimdilik hata vermemesi için yeni obje oluşturuyoruz.
                return Instantiate(notePrefab, noteParent);
            }
            return notePool.Dequeue();
        }
        else
        {
            return Instantiate(notePrefab, noteParent);
        }
    }

    public void ReturnNoteToPool(GameObject noteObject)
    {
        if (noteObject == null) return;

        activeNotesForDebug.Remove(noteObject);

        if (enableObjectPooling)
        {
            noteObject.SetActive(false);
            notePool.Enqueue(noteObject);
        }
        else
        {
            Destroy(noteObject);
        }
    }

    // DEĞİŞİKLİK: Bu metodun adı daha anlamlı hale getirildi. Artık NoteAnimator tarafından çağrılıyor.
    public void ProcessMissedNote(GameNoteInfo noteInfo)
    {
        // Kaçırılan notanın oyun mantığı üzerindeki etkileri burada işlenir.
        // Örneğin: Kombo sıfırlama, can azaltma vs.
        if (GameManager.Instance != null)
        {
            // Bu kısım gelecekte skorlama mantığına bağlanabilir. Şimdilik debug için loglayalım.
            // GameManager.Instance.UpdateCombo(0);
        }
        if (showDebugLogs)
        {
            Debug.Log($"NOTE MISSED: Lane {noteInfo.line}, Pitch {noteInfo.pitch}");
        }
    }

    // DEĞİŞİKLİK: Bu metod artık doğrudan kullanılmıyor. İşlevselliği NoteAnimator'a devredildi.
    // Yine de birisi çağırırsa diye boş bırakmak veya uyarı vermek iyi bir pratiktir.
    public void ProcessHitNote(GameObject noteObject)
    {
        // Bu metodun sorumluluğu HitZoneManager'dan NoteAnimator'a geçti.
        // Bu metod artık kullanılmamalıdır.
        if (showDebugLogs)
        {
            Debug.LogWarning("NoteRenderer.ProcessHitNote() çağrıldı, ancak bu metod artık geçerli değil. Çağrıyı HitZoneManager'dan kontrol edin.");
        }
    }

    #endregion

    #region Public Interface

    public int GetActiveNoteCount() => activeNoteCount = activeNotesForDebug.Count;

    public float GetNoteTravelTime()
    {
        return Mathf.Abs(spawnZ - hitZoneZ) / Mathf.Max(0.01f, speedMultiplier);
    }

    public void ClearAllNotes()
    {
        // Aktif tüm notaların animasyonlarını durdur ve havuza geri gönder
        foreach (var noteObject in activeNotesForDebug)
        {
            noteObject.transform.DOKill();
            ReturnNoteToPool(noteObject);
        }
        activeNotesForDebug.Clear();
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0.1f, multiplier);
    }
    #endregion
}