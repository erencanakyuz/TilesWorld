using UnityEngine;
using System.Collections.Generic;

public class UIEffectPool : MonoBehaviour
{
    public static UIEffectPool Instance { get; private set; }

    private UIConfig config;
    private Transform effectParent;
    private Queue<GameObject> hitEffectPool;
    private List<ActiveHitEffect> activeEffects;

    public void Initialize(UIConfig config, Transform effectParent)
    {
        Instance = this;
        this.config = config;
        this.effectParent = effectParent;

        hitEffectPool ??= new Queue<GameObject>();
        activeEffects ??= new List<ActiveHitEffect>();

        InitializePool();
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

        GameObject effect = GetPooledEffect();
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
            activeEffect.elapsedTime += Time.deltaTime;

            if (activeEffect.elapsedTime >= config.effectDuration)
            {
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
                effect.SetActive(false);
                hitEffectPool.Enqueue(effect);
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

    private GameObject GetPooledEffect()
    {
        if (hitEffectPool.Count > 0)
        {
            return hitEffectPool.Dequeue();
        }

        GameObject prefab = GetEffectPrefab(HitAccuracy.Perfect);
        if (prefab != null)
        {
            return Instantiate(prefab, effectParent);
        }

        return null;
    }

    private class ActiveHitEffect
    {
        public GameObject effectObject;
        public float elapsedTime;
        public Vector3 originalScale;
        public CanvasGroup canvasGroup;
    }
}
