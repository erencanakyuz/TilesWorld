using UnityEngine;
using System.Text;

public class PerformanceDiagnostics : MonoBehaviour
{
    [Header("Logging")]
    [SerializeField] private float logIntervalSeconds = 2f;
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private bool showOnScreen = true;

    private float timer = 0f;
    private string lastReportLine = string.Empty;

    private NoteRenderer noteRenderer;
    private AudioManager audioManager;
    private HitZoneManager hitZoneManager;
    private UIEffectPool uiEffectPool;
    private GameNoteCreator noteCreator;
    private HitZoneTrigger[] hitZones;
    
    // GC tracking to detect garbage collection spikes
    private int lastGC0Count = 0;
    private int lastGC1Count = 0;
    private int lastGC2Count = 0;

    private readonly StringBuilder sb = new StringBuilder(640);

    void Start()
    {
        noteRenderer = FindFirstObjectByType<NoteRenderer>();
        audioManager = AudioManager.Instance;
        hitZoneManager = FindFirstObjectByType<HitZoneManager>();
        uiEffectPool = UIEffectPool.Instance ?? FindFirstObjectByType<UIEffectPool>();
        noteCreator = FindFirstObjectByType<GameNoteCreator>();
        hitZones = FindObjectsByType<HitZoneTrigger>(FindObjectsSortMode.None);
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer < logIntervalSeconds) return;
        timer = 0f;

        string line = BuildReportLine();
        lastReportLine = line;
        if (logToConsole)
        {
            Debug.Log(line);
        }
    }

    private string BuildReportLine()
    {
        sb.Clear();
        float fps = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f;
        long memBytes = System.GC.GetTotalMemory(false);
        float memMb = memBytes / (1024f * 1024f);

        int activeNotes = noteRenderer != null ? noteRenderer.GetActiveNoteCount() : 0;
        int notePool = noteRenderer != null ? noteRenderer.GetNotePoolCount() : 0;
        int activeAudio = audioManager != null ? audioManager.GetActiveSourceCount() : 0;
        int pooledAudio = audioManager != null ? audioManager.GetPooledSourceCount() : 0;

        int droppedNoClip = 0;
        int droppedNotLoaded = 0;
        int droppedNoVoice = 0;
        int stolenVoices = 0;
        if (audioManager != null)
        {
            audioManager.GetDropStats(out droppedNoClip, out droppedNotLoaded, out droppedNoVoice, out stolenVoices);
        }
        int queueCount = noteCreator != null ? noteCreator.GetQueueCount() : 0;

        int totalInside = 0;
        if (hitZones != null)
        {
            for (int i = 0; i < hitZones.Length; i++)
            {
                var zone = hitZones[i];
                if (zone != null)
                {
                    totalInside += zone.insideNotes.Count;
                }
            }
        }

        int perfectPool = 0;
        int goodPool = 0;
        int perfectActive = 0;
        int goodActive = 0;
        if (hitZoneManager != null)
        {
            hitZoneManager.GetParticleCounts(out perfectPool, out goodPool, out perfectActive, out goodActive);
        }

        int uiPool = uiEffectPool != null ? uiEffectPool.GetPoolCount() : 0;
        int uiActive = uiEffectPool != null ? uiEffectPool.GetActiveCount() : 0;
        
        // DOTween active tween count for debugging tween accumulation
        int activeTweens = DG.Tweening.DOTween.TotalActiveTweens();
        int playingTweens = DG.Tweening.DOTween.TotalPlayingTweens();
        
        // GC tracking - detect if garbage collection happened since last frame
        int currentGC0 = System.GC.CollectionCount(0);
        int currentGC1 = System.GC.CollectionCount(1);
        int currentGC2 = System.GC.CollectionCount(2);
        int gc0Delta = currentGC0 - lastGC0Count;
        int gc1Delta = currentGC1 - lastGC1Count;
        int gc2Delta = currentGC2 - lastGC2Count;
        lastGC0Count = currentGC0;
        lastGC1Count = currentGC1;
        lastGC2Count = currentGC2;

        sb.Append("[PERF]");
        sb.Append(" FPS:").Append(fps.ToString("F1"));
        sb.Append(" dt:").Append((Time.unscaledDeltaTime * 1000f).ToString("F0")).Append("ms");
        sb.Append(" MemMB:").Append(memMb.ToString("F1"));
        sb.Append(" Notes(active/pool/inside):").Append(activeNotes).Append('/')
            .Append(notePool).Append('/').Append(totalInside);
        sb.Append(" Queue:").Append(queueCount);
        sb.Append(" Audio(active/pool):").Append(activeAudio).Append('/').Append(pooledAudio);
        sb.Append(" AudioDrops(nc/nl/nv|stolen):").Append(droppedNoClip).Append("/")
            .Append(droppedNotLoaded).Append("/").Append(droppedNoVoice).Append("|").Append(stolenVoices);
        sb.Append(" Particles(P/G active|pool):")
            .Append(perfectActive).Append('/').Append(perfectPool)
            .Append('|')
            .Append(goodActive).Append('/').Append(goodPool);
        sb.Append(" UIEffects(active/pool):").Append(uiActive).Append('/').Append(uiPool);
        sb.Append(" Tweens(active/playing):").Append(activeTweens).Append('/').Append(playingTweens);
        sb.Append(" GC(0/1/2):+").Append(gc0Delta).Append("/+").Append(gc1Delta).Append("/+").Append(gc2Delta);

        return sb.ToString();
    }

    void OnGUI()
    {
        if (!showOnScreen) return;
        if (string.IsNullOrEmpty(lastReportLine)) return;
        GUI.color = Color.white;
        GUI.Box(new Rect(10, 10, 420, 130), "Performance");
        GUI.Label(new Rect(20, 35, 400, 20), lastReportLine);
    }
}
