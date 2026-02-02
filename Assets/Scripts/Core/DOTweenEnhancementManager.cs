using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// DOTweenEnhancementManager - Runtime Configuration System
/// 
/// Manages DOTween settings, material configurations, and visual enhancements at runtime.
/// This class implements the runtime portion of the DOTween.md enhancement plan.
/// </summary>
public class DOTweenEnhancementManager : MonoBehaviour
{
    [Header("🎮 DOTween Configuration")]
    [SerializeField] private bool autoInitializeDOTween = true;
    [SerializeField] private int maxSimultaneousTweens = 200;
    [SerializeField] private int maxSequences = 50;
    [SerializeField] private LogBehaviour logBehaviour = LogBehaviour.Default;

    [Header("🎨 Enhanced Materials")]
    [SerializeField] private Material enhancedNoteMaterial;
    [SerializeField] private Material enhancedHitzoneMaterial;
    [SerializeField] private Material enhancedParticleBaseMaterial;

    [Header("✨ Specialized Particle Materials")]
    [SerializeField] private Material perfectHitParticleMaterial;
    [SerializeField] private Material goodHitParticleMaterial;
    [SerializeField] private Material missParticleMaterial;
    [SerializeField] private Material sparkParticleMaterial;

    [Header("🎯 Runtime Settings")]
    [SerializeField] private bool enableMaterialAutoLoad = true;
    [SerializeField] private bool enablePerformanceOptimizations = true;
    [SerializeField] private bool enableDebugLogs = false;

    public static DOTweenEnhancementManager Instance { get; private set; }

    // Public accessors for materials
    public Material EnhancedNoteMaterial => enhancedNoteMaterial;
    public Material EnhancedHitzoneMaterial => enhancedHitzoneMaterial;
    public Material EnhancedParticleBaseMaterial => enhancedParticleBaseMaterial;
    public Material PerfectHitParticleMaterial => perfectHitParticleMaterial;
    public Material GoodHitParticleMaterial => goodHitParticleMaterial;
    public Material MissParticleMaterial => missParticleMaterial;
    public Material SparkParticleMaterial => sparkParticleMaterial;

    private Dictionary<HitAccuracy, Material> hitEffectMaterials;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        if (enableDebugLogs)
            Debug.Log("🚀 Initializing DOTween Enhancement Manager...");

        InitializeDOTween();
        LoadEnhancedMaterials();
        SetupHitEffectMaterials();
        ApplyPerformanceOptimizations();

        if (enableDebugLogs)
            Debug.Log("✅ DOTween Enhancement Manager initialized successfully!");
    }

    private void InitializeDOTween()
    {
        if (!autoInitializeDOTween) return;

        // Initialize DOTween with optimized settings
        DOTween.Init(false, true, logBehaviour)
            .SetCapacity(maxSimultaneousTweens, maxSequences);

        // Set default ease for smooth animations
        DOTween.defaultEaseType = Ease.OutQuad;

        // Enable safe mode for better error handling
        DOTween.useSafeMode = true;

        if (enableDebugLogs)
            Debug.Log("🎯 DOTween initialized with enhanced settings");
    }

    private void LoadEnhancedMaterials()
    {
        if (!enableMaterialAutoLoad) return;

        // Auto-load materials if not assigned in inspector
        LoadMaterialIfNull(ref enhancedNoteMaterial, "Enhanced_NoteMaterial");
        LoadMaterialIfNull(ref enhancedHitzoneMaterial, "Enhanced_HitzoneMaterial");
        LoadMaterialIfNull(ref enhancedParticleBaseMaterial, "Enhanced_ParticleBaseMaterial");
        LoadMaterialIfNull(ref perfectHitParticleMaterial, "PerfectHit_ParticleMaterial");
        LoadMaterialIfNull(ref goodHitParticleMaterial, "GoodHit_ParticleMaterial");
        LoadMaterialIfNull(ref missParticleMaterial, "Miss_ParticleMaterial");
        LoadMaterialIfNull(ref sparkParticleMaterial, "Spark_ParticleMaterial");

        if (enableDebugLogs)
            Debug.Log("🎨 Enhanced materials loaded");
    }

    private void LoadMaterialIfNull(ref Material material, string materialName)
    {
        if (material == null)
        {
            material = Resources.Load<Material>($"Materials/{materialName}");
            if (material == null)
            {
                // Try alternative path
                material = Resources.Load<Material>(materialName);
            }
        }
    }

    private void SetupHitEffectMaterials()
    {
        hitEffectMaterials = new Dictionary<HitAccuracy, Material>
        {
            { HitAccuracy.Perfect, perfectHitParticleMaterial },
            { HitAccuracy.Good, goodHitParticleMaterial },
            { HitAccuracy.Okay, goodHitParticleMaterial }, // Use good material for okay hits
        };
    }

    private void ApplyPerformanceOptimizations()
    {
        if (!enablePerformanceOptimizations) return;

        // Optimize DOTween for mobile performance
        DOTween.SetTweensCapacity(maxSimultaneousTweens, maxSequences);

        // Enable recycling of tweens
        DOTween.useSmoothDeltaTime = true;

        if (enableDebugLogs)
            Debug.Log("⚡ Performance optimizations applied");
    }

    #region Public API

    /// <summary>
    /// Gets the appropriate particle material for a hit accuracy level
    /// </summary>
    public Material GetParticleMaterialForAccuracy(HitAccuracy accuracy)
    {
        if (hitEffectMaterials.TryGetValue(accuracy, out Material material))
        {
            return material;
        }
        return enhancedParticleBaseMaterial;
    }

    /// <summary>
    /// Gets the color for a specific hit accuracy
    /// </summary>
    public Color GetColorForAccuracy(HitAccuracy accuracy)
    {
        return accuracy switch
        {
            HitAccuracy.Perfect => new Color(0f, 1f, 1f, 0.8f), // Cyan
            HitAccuracy.Good => new Color(0f, 1f, 0f, 0.8f),    // Green
            HitAccuracy.Okay => new Color(1f, 1f, 0f, 0.8f),    // Yellow
            _ => Color.white
        };
    }

    /// <summary>
    /// Gets the emission color for a specific hit accuracy
    /// </summary>
    public Color GetEmissionColorForAccuracy(HitAccuracy accuracy)
    {
        return accuracy switch
        {
            HitAccuracy.Perfect => new Color(0f, 2f, 2f, 1f), // Bright Cyan
            HitAccuracy.Good => new Color(0f, 2f, 0f, 1f),    // Bright Green
            HitAccuracy.Okay => new Color(2f, 2f, 0f, 1f),    // Bright Yellow
            _ => Color.white
        };
    }

    /// <summary>
    /// Creates a standardized hit animation sequence
    /// </summary>
    public Sequence CreateHitAnimationSequence(Transform target, Renderer targetRenderer, HitAccuracy accuracy)
    {
        Sequence seq = DOTween.Sequence();

        Color targetColor = GetColorForAccuracy(accuracy);
        Color emissionColor = GetEmissionColorForAccuracy(accuracy);

        switch (accuracy)
        {
            case HitAccuracy.Perfect:
                // Explosive perfect animation
                seq.Append(target.DOPunchScale(Vector3.one * 0.5f, 0.3f, 2, 0.5f));
                seq.Join(target.DORotate(new Vector3(0, 0, 360f), 0.3f, RotateMode.FastBeyond360));
                seq.Join(targetRenderer.material.DOColor(targetColor, "_BaseColor", 0.1f));
                if (targetRenderer.material.HasProperty("_EmissionColor"))
                {
                    seq.Join(targetRenderer.material.DOColor(emissionColor, "_EmissionColor", 0.1f));
                }
                break;

            case HitAccuracy.Good:
                // Good hit animation
                seq.Append(target.DOPunchScale(Vector3.one * 0.35f, 0.25f, 1, 0.5f));
                seq.Join(target.DORotate(new Vector3(0, 0, 180f), 0.25f, RotateMode.FastBeyond360));
                seq.Join(targetRenderer.material.DOColor(targetColor, "_BaseColor", 0.1f));
                break;

            default: // Okay
                // Simple hit animation
                seq.Append(target.DOScale(1.2f, 0.2f).SetEase(Ease.OutCubic));
                seq.Join(targetRenderer.material.DOColor(targetColor, "_BaseColor", 0.1f));
                break;
        }

        return seq;
    }

    /// <summary>
    /// Creates a standardized miss animation sequence
    /// </summary>
    public Sequence CreateMissAnimationSequence(Transform target, Renderer targetRenderer)
    {
        Sequence seq = DOTween.Sequence();

        seq.Join(targetRenderer.material.DOColor(Color.gray, "_BaseColor", 0.5f));
        seq.Join(target.DOScale(0f, 0.5f).SetEase(Ease.InBack));
        seq.Join(target.DOMoveY(target.position.y - 1.5f, 0.5f).SetEase(Ease.InCubic));

        return seq;
    }

    /// <summary>
    /// Applies enhanced material to a renderer
    /// </summary>
    public void ApplyEnhancedMaterial(Renderer renderer, MaterialType materialType)
    {
        Material targetMaterial = materialType switch
        {
            MaterialType.Note => enhancedNoteMaterial,
            MaterialType.HitZone => enhancedHitzoneMaterial,
            MaterialType.ParticleBase => enhancedParticleBaseMaterial,
            MaterialType.PerfectHit => perfectHitParticleMaterial,
            MaterialType.GoodHit => goodHitParticleMaterial,
            MaterialType.Miss => missParticleMaterial,
            MaterialType.Spark => sparkParticleMaterial,
            _ => null
        };

        if (targetMaterial != null && renderer != null)
        {
            renderer.material = targetMaterial;
        }
    }

    #endregion

    #region Development Tools

    [ContextMenu("Test Perfect Hit Animation")]
    private void TestPerfectHitAnimation()
    {
        var testObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var renderer = testObj.GetComponent<Renderer>();
        ApplyEnhancedMaterial(renderer, MaterialType.Note);

        var seq = CreateHitAnimationSequence(testObj.transform, renderer, HitAccuracy.Perfect);
        seq.OnComplete(() => Destroy(testObj));
    }

    [ContextMenu("Test Miss Animation")]
    private void TestMissAnimation()
    {
        var testObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var renderer = testObj.GetComponent<Renderer>();
        ApplyEnhancedMaterial(renderer, MaterialType.Note);

        var seq = CreateMissAnimationSequence(testObj.transform, renderer);
        seq.OnComplete(() => Destroy(testObj));
    }

    #endregion

    void OnDestroy()
    {
        // Kill all active tweens to prevent errors
        DOTween.KillAll();
        
        // Clear instance reference
        if (Instance == this)
        {
            Instance = null;
        }
        
        // Clear material dictionary
        hitEffectMaterials?.Clear();
    }
}

/// <summary>
/// Enum for different material types in the enhancement system
/// </summary>
public enum MaterialType
{
    Note,
    HitZone,
    ParticleBase,
    PerfectHit,
    GoodHit,
    Miss,
    Spark
}