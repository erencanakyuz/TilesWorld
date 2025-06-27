using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using System.Diagnostics;

public class AudioLatencyTester : MonoBehaviour
{
    [Header("Audio Test Settings")]
    public AudioClip testClip;
    public AudioSource audioSource;
    public AudioMixerGroup sfxMixerGroup; // PianoGameMixer'dan SFX group

    [Header("Latency Results")]
    public float lastMeasuredLatency;
    public int testCount = 0;
    public float averageLatency = 0f;
    public float minLatency = float.MaxValue;
    public float maxLatency = 0f;

    [Header("Test Controls")]
    public KeyCode testKey = KeyCode.Space;
    public KeyCode resetKey = KeyCode.R;
    public int autoTestCount = 10; // Otomatik test sayısı

    private Stopwatch stopwatch = new Stopwatch();
    private float totalLatency = 0f;

    void Start()
    {
        InitializeAudioSource();
        LogTestInstructions();
    }

    void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Audio Mixer bağlantısı (Main.md setup'ına uygun)
        if (sfxMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
        }

        // Rhythm game için optimal ayarlar
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 1.0f;
    }

    void LogTestInstructions()
    {
        UnityEngine.Debug.Log("🎵 === AUDIO LATENCY TESTER READY ===");
        UnityEngine.Debug.Log("Press SPACE to test audio latency");
        UnityEngine.Debug.Log("Press R to reset results");
        UnityEngine.Debug.Log("Press T for automatic 10-test sequence");
        UnityEngine.Debug.Log("🎯 Target: <20ms for rhythm games");
    }

    void Update()
    {
        // New Input System - Keyboard input
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TestAudioLatency();
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetResults();
            }

            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                StartCoroutine(AutoTestSequence());
            }
        }
    }

    void TestAudioLatency()
    {
        if (testClip == null)
        {
            UnityEngine.Debug.LogError("❌ Test clip not assigned! Assign a short audio clip.");
            return;
        }

        // Precision timing measurement
        stopwatch.Restart();
        audioSource.PlayOneShot(testClip);
        stopwatch.Stop();

        float currentLatency = (float)stopwatch.Elapsed.TotalMilliseconds;
        RecordLatencyResult(currentLatency);
        LogLatencyResult(currentLatency);
        EvaluateLatencyThreshold();
    }

    void RecordLatencyResult(float latency)
    {
        lastMeasuredLatency = latency;
        testCount++;
        totalLatency += latency;
        averageLatency = totalLatency / testCount;

        if (latency < minLatency) minLatency = latency;
        if (latency > maxLatency) maxLatency = latency;
    }

    void LogLatencyResult(float latency)
    {
        UnityEngine.Debug.Log($"🎵 Test #{testCount}:");
        UnityEngine.Debug.Log($"   Current: {latency:F2}ms");
        UnityEngine.Debug.Log($"   Average: {averageLatency:F2}ms");
        UnityEngine.Debug.Log($"   Min: {minLatency:F2}ms | Max: {maxLatency:F2}ms");
    }

    void EvaluateLatencyThreshold()
    {
        // Main.md Karar #2 - Critical thresholds
        if (averageLatency <= 15f)
        {
            UnityEngine.Debug.Log("🎯 EXCELLENT! Latency perfect for rhythm games.");
        }
        else if (averageLatency <= 20f)
        {
            UnityEngine.Debug.Log("✅ GOOD! Latency acceptable for rhythm games.");
        }
        else if (averageLatency <= 30f)
        {
            UnityEngine.Debug.LogWarning("⚠️ WARNING! Latency borderline for rhythm games.");
            UnityEngine.Debug.LogWarning("   Consider FMOD integration for better performance.");
        }
        else
        {
            UnityEngine.Debug.LogError("❌ CRITICAL! Latency too high for rhythm games!");
            UnityEngine.Debug.LogError("   FMOD integration or native plugins REQUIRED.");
            UnityEngine.Debug.LogError("   Current latency will break rhythm game feel.");
        }
    }

    void ResetResults()
    {
        testCount = 0;
        totalLatency = 0f;
        averageLatency = 0f;
        minLatency = float.MaxValue;
        maxLatency = 0f;
        UnityEngine.Debug.Log("🔄 Test results reset.");
    }

    System.Collections.IEnumerator AutoTestSequence()
    {
        UnityEngine.Debug.Log($"🔄 Starting automatic test sequence ({autoTestCount} tests)...");
        ResetResults();

        for (int i = 0; i < autoTestCount; i++)
        {
            TestAudioLatency();
            yield return new WaitForSeconds(0.1f); // 100ms between tests
        }

        UnityEngine.Debug.Log("📊 === AUTO TEST SEQUENCE COMPLETE ===");
        LogFinalResults();
    }

    void LogFinalResults()
    {
        UnityEngine.Debug.Log($"📊 Final Results ({testCount} tests):");
        UnityEngine.Debug.Log($"   Average Latency: {averageLatency:F2}ms");
        UnityEngine.Debug.Log($"   Min Latency: {minLatency:F2}ms");
        UnityEngine.Debug.Log($"   Max Latency: {maxLatency:F2}ms");
        UnityEngine.Debug.Log($"   Latency Range: {(maxLatency - minLatency):F2}ms");

        // Final recommendation based on Main.md decisions
        if (averageLatency <= 20f)
        {
            UnityEngine.Debug.Log("🎯 RECOMMENDATION: Use Unity native AudioSource");
            UnityEngine.Debug.Log("   Performance acceptable for rhythm game development.");
        }
        else
        {
            UnityEngine.Debug.Log("🎯 RECOMMENDATION: Integrate FMOD for Unity");
            UnityEngine.Debug.Log("   Native Unity audio insufficient for rhythm game timing.");
        }
    }
}