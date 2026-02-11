using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// Logs every played note to a CSV file in the project root.
/// File is flushed after every note so no data is lost if you stop early.
/// Enable/disable via Inspector toggle.
/// </summary>
public class NoteDebugLogger : MonoBehaviour
{
    public static NoteDebugLogger Instance { get; private set; }

    [Header("Note Debug Logger")]
    [SerializeField] private bool enableLogging = false; // Enable via Inspector when debugging

    private StreamWriter writer;
    private int noteCounter;
    private float sessionStartTime;
    private string logFilePath;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        if (!enableLogging) return;
        StartNewSession();
    }

    void OnDisable()
    {
        CloseSession();
    }

    void OnApplicationQuit()
    {
        CloseSession();
    }

    void OnDestroy()
    {
        CloseSession();
    }

    public void StartNewSession()
    {
        CloseSession();

        noteCounter = 0;
        sessionStartTime = Time.realtimeSinceStartup;

        // Write to project root in Editor, persistentDataPath on device
#if UNITY_EDITOR
        string dir = Path.GetDirectoryName(Application.dataPath);
#else
        string dir = Application.persistentDataPath;
#endif
        logFilePath = Path.Combine(dir, "note_debug_log.csv");

        try
        {
            writer = new StreamWriter(logFilePath, false, Encoding.UTF8);
            writer.AutoFlush = true; // Flush every write - no data loss on crash/stop

            // CSV Header
            writer.WriteLine("Index,GameTime,DspTime,Instrument,InputLine,InputPitch,FinalPitch,MaxClipIdx,ClipName,Volume,JavaMapping,WasClamped,ClampedFrom,Status");

            Debug.Log($"[NoteDebugLogger] Session started. Log: {logFilePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NoteDebugLogger] Failed to create log file: {ex.Message}");
            writer = null;
        }
    }

    /// <summary>
    /// Log a note play attempt. Call this from AudioManager.PlayNoteInternal.
    /// </summary>
    public void LogNote(
        string instrument,
        int inputLine,
        int inputPitch,
        int finalPitch,
        int maxClipIndex,
        string clipName,
        float volume,
        bool useJavaMapping,
        string status)
    {
        if (!enableLogging || writer == null) return;

        noteCounter++;
        float gameTime = Time.realtimeSinceStartup - sessionStartTime;
        double dspTime = AudioSettings.dspTime;

        bool wasClamped = (inputPitch != finalPitch);
        string clampedFrom = wasClamped ? inputPitch.ToString() : "";

        string line = $"{noteCounter},{gameTime:F3},{dspTime:F4},{instrument},{inputLine},{inputPitch},{finalPitch},{maxClipIndex},{clipName},{volume:F2},{useJavaMapping},{wasClamped},{clampedFrom},{status}";

        try
        {
            writer.WriteLine(line);
        }
        catch (System.Exception)
        {
            // Silently fail - don't break gameplay
        }
    }

    /// <summary>
    /// Log a dropped note (no clip, no voice, etc.)
    /// </summary>
    public void LogDropped(string instrument, int inputLine, int inputPitch, string reason)
    {
        if (!enableLogging || writer == null) return;

        noteCounter++;
        float gameTime = Time.realtimeSinceStartup - sessionStartTime;
        double dspTime = AudioSettings.dspTime;

        string line = $"{noteCounter},{gameTime:F3},{dspTime:F4},{instrument},{inputLine},{inputPitch},,,,,,,, DROPPED:{reason}";

        try
        {
            writer.WriteLine(line);
        }
        catch (System.Exception) { }
    }

    private void CloseSession()
    {
        if (writer != null)
        {
            try
            {
                writer.WriteLine($"# SESSION END: {noteCounter} notes logged");
                writer.Flush();
                writer.Close();
                Debug.Log($"[NoteDebugLogger] Session closed. {noteCounter} notes logged to: {logFilePath}");
            }
            catch (System.Exception) { }
            writer = null;
        }
    }
}
