using UnityEngine;
using System.Collections.Generic;

public class UIEffectPool : MonoBehaviour
{
    public static UIEffectPool Instance { get; private set; }

    private UIConfig config;
    private Transform effectParent;
    private Queue<GameObject> hitEffectPool;
    private List<ActiveHitEffect> activeEffects;
    [SerializeField] private int maxPoolSize = 64;
    private int totalCreated = 0;

    public void Initialize(UIConfig config, Transform effectParent)
    {
        Instance = this;
        this.config = config;
        this.effectParent = effectParent;

        // CRITICAL FIX: Clear old pool to avoid destroyed object references
        ClearPool();
        
        hitEffectPool ??= new Queue<GameObject>();
        activeEffects ??= new List<ActiveHitEffect>();

        InitializePool();
    }

    private void ClearPool()
    {
        // Clear active effects
        if (activeEffects != null)
        {
            foreach (var effect in activeEffects)
            {
                if (effect.effectObject != null)
                    Destroy(effect.effectObject);
            }
            activeEffects.Clear();
        }
        
        // Clear pooled effects
        if (hitEffectPool != null)
        {
            while (hitEffectPool.Count > 0)
            {
                var obj = hitEffectPool.Dequeue();
                if (obj != null)
                    Destroy(obj);
            }
            hitEffectPool.Clear();
        }
        totalCreated = 0;
    }

    public Transform DiscoverEffectParent(Canvas overlayCanvas, Canvas hudCanvas)
    {
        Canvas targetCanvas = overlayCanvas ?? hudCanvas;
        if (targetCanvas == null) return null;

        var allTransforms = targetCanvas.GetComponentsInChildren<Transform>();
        effectParent = System.Array.Find(allTransforms, t =>
            t.name.ToLower().Contains("effect")) ?? targetCanvas.transform;

        return effectParent;
    }

    public void ShowEffect(HitAccuracy accuracy, Vector2 screenPosition)
    {
        if (config == null || effectParent == null) return;
        
        // PERFORMANCE: Limit active effects to prevent GC pressure from too many UI objects
        const int maxActiveEffects = 24;
        if (activeEffects.Count >= maxActiveEffects)
        {
            // Skip spawning new effects when at capacity - this prevents GC spikes
            return;
        }

        // CRITICAL FIX: Use accuracy-specific prefab
        GameObject effect = GetPooledEffect(accuracy);
        if (effect == null) return;

        RectTransform effectRect = effect.GetComponent<RectTransform>();
        if (effectRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                effectParent.GetComponent<RectTransform>(),
                screenPosition,
                null,
                out Vector2 localPoint);

            effectRect.localPosition = localPoint;
        }

        effect.SetActive(true);

        activeEffects.Add(new ActiveHitEffect
        {
            effectObject = effect,
            elapsedTime = 0f,
            originalScale = effect.transform.localScale,
            canvasGroup = effect.GetComponent<CanvasGroup>()
        });
    }

    void Update()
    {
        if (activeEffects == null || activeEffects.Count == 0 || config == null) return;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var activeEffect = activeEffects[i];
            
            // CRITICAL FIX: Check for destroyed objects
            if (activeEffect.effectObject == null)
            {
                activeEffects.RemoveAt(i);
                continue;
            }
            
            activeEffect.elapsedTime += Time.deltaTime;

            if (activeEffect.elapsedTime >= config.effectDuration)
            {
                var particleSystem = activeEffect.effectObject.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                activeEffect.effectObject.SetActive(false);
                hitEffectPool.Enqueue(activeEffect.effectObject);
                activeEffects.RemoveAt(i);
            }
            else
            {
                float progress = activeEffect.elapsedTime / config.effectDuration;
                float scale = config.fadeAnimation != null ? config.fadeAnimation.Evaluate(progress) : progress;
                activeEffect.effectObject.transform.localScale = activeEffect.originalScale * scale;

                if (activeEffect.canvasGroup != null)
                {
                    activeEffect.canvasGroup.alpha = 1f - progress;
                }
            }
        }
    }

    private void InitializePool()
    {
        if (config?.perfectHitEffect != null && effectParent != null)
        {
            for (int i = 0; i < 10; i++)
            {
                GameObject effect = Instantiate(config.perfectHitEffect, effectParent);
                PreparePooledEffect(effect);
                effect.SetActive(false);
                hitEffectPool.Enqueue(effect);
                totalCreated++;
            }
        }
    }

    private GameObject GetEffectPrefab(HitAccuracy accuracy)
    {
        return accuracy switch
        {
            HitAccuracy.Perfect => config.perfectHitEffect,
            HitAccuracy.Good => config.goodHitEffect,
            HitAccuracy.Miss => config.missEffect,
            _ => config.perfectHitEffect
        };
    }

    // CRITICAL FIX: Accept accuracy parameter
    private GameObject GetPooledEffect(HitAccuracy accuracy = HitAccuracy.Perfect)
    {
        // Try to get from pool first
        while (hitEffectPool.Count > 0)
        {
            var pooledObj = hitEffectPool.Dequeue();
            // CRITICAL FIX: Skip destroyed objects
            if (pooledObj != null)
            {
                PreparePooledEffect(pooledObj);
                return pooledObj;
            }
        }

        // Pool empty - instantiate correct prefab based on accuracy
        GameObject prefab = GetEffectPrefab(accuracy);
        if (prefab != null && effectParent != null)
        {
            if (totalCreated >= maxPoolSize)
            {
                return null;
            }
            var effect = Instantiate(prefab, effectParent);
            PreparePooledEffect(effect);
            totalCreated++;
            return effect;
        }

        return null;
    }

    private void PreparePooledEffect(GameObject effect)
    {
        if (effect == null) return;

        var autoDestroy = effect.GetComponent<ParticleAutoDestroy>();
        if (autoDestroy != null)
        {
            Destroy(autoDestroy);
        }

        var particleSystem = effect.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private class ActiveHitEffect
    {
        public GameObject effectObject;
        public float elapsedTime;
        public Vector3 originalScale;
        public CanvasGroup canvasGroup;
    }

    public int GetPoolCount()
    {
        return hitEffectPool != null ? hitEffectPool.Count : 0;
    }

    public int GetActiveCount()
    {
        return activeEffects != null ? activeEffects.Count : 0;
    }
}
