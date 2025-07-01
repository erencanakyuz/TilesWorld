using UnityEngine;
using DG.Tweening;

/// <summary>
/// Manages all visual animations for a single note.
/// Responsible for spawn, flow, successful hit, and miss animations.
/// This script is attached to the note prefab and controlled by NoteRenderer.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class NoteAnimator : MonoBehaviour
{
    private Renderer noteRenderer;
    private Transform noteTransform;
    private NoteRenderer spawner;
    private GameNoteInfo noteInfo;
    private Material originalMaterial;
    private bool isAnimatingHit = false;

    [Header("Hit Animation Settings")]
    [SerializeField] private float hitScalePunchAmount = 0.5f;
    [SerializeField] private float hitScalePunchDuration = 0.3f;
    [SerializeField] private float hitFadeOutDuration = 0.3f;
    [SerializeField] private float hitRotationAmount = 360f;

    [Header("Miss Animation Settings")]
    [SerializeField] private float missDropDistance = 1.5f;
    [SerializeField] private float missDropDuration = 0.5f;
    [SerializeField] private float missScaleEndValue = 0f;

    void Awake()
    {
        noteRenderer = GetComponent<Renderer>();
        noteTransform = transform;
        originalMaterial = noteRenderer.material;
    }

    /// <summary>
    /// Called by NoteRenderer. Sets up necessary references and info.
    /// </summary>
    public void Initialize(NoteRenderer spawnerRef, GameNoteInfo info)
    {
        this.spawner = spawnerRef;
        this.noteInfo = info;
        isAnimatingHit = false;
    }

    /// <summary>
    /// Plays when note first spawns and starts flowing.
    /// Transitions from invisible to visible, scales up, and moves to target.
    /// </summary>
    public void AnimateSpawnAndFlow(Vector3 targetPosition, float duration)
    {
        // Reset state
        isAnimatingHit = false;
        noteTransform.localScale = Vector3.one * 0.5f;
        Color c = originalMaterial.color;
        noteRenderer.material.color = new Color(c.r, c.g, c.b, 0f);

        // Create animation sequence
        Sequence spawnSequence = DOTween.Sequence();

        // Fade in and scale up with bounce
        spawnSequence.Join(noteTransform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
        spawnSequence.Join(noteRenderer.material.DOFade(1f, 0.3f));

        // Flow to target position (linear, constant speed)
        noteTransform.DOMove(targetPosition, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => { if (!isAnimatingHit) AnimateMiss(); });
    }

    /// <summary>
    /// Called by HitZoneManager when note is successfully hit.
    /// </summary>
    public void AnimateHit(HitAccuracy quality)
    {
        if (isAnimatingHit) return;
        isAnimatingHit = true;

        // Kill any ongoing animations
        noteTransform.DOKill();
        noteRenderer.material.DOKill();

        // Create hit animation sequence
        Sequence hitSequence = DOTween.Sequence();

        switch (quality)
        {
            case HitAccuracy.Perfect:
                // Explosive perfect hit animation
                Color perfectColor = new Color(0f, 1f, 1f, 1f); // Cyan
                noteRenderer.material.DOColor(perfectColor, "_BaseColor", 0.1f);

                hitSequence.Append(noteTransform.DOPunchScale(Vector3.one * hitScalePunchAmount, hitScalePunchDuration, 2, 0.5f));
                hitSequence.Join(noteTransform.DORotate(new Vector3(0, 0, hitRotationAmount), hitScalePunchDuration, RotateMode.FastBeyond360));
                hitSequence.Join(noteRenderer.material.DOFade(0f, hitFadeOutDuration).SetDelay(0.1f));

                // Add emission flash
                if (noteRenderer.material.HasProperty("_EmissionColor"))
                {
                    hitSequence.Join(noteRenderer.material.DOColor(perfectColor * 2f, "_EmissionColor", 0.1f));
                }
                break;

            case HitAccuracy.Good:
                // Good hit animation - simpler but still satisfying
                Color goodColor = new Color(0f, 1f, 0f, 1f); // Green
                noteRenderer.material.DOColor(goodColor, "_BaseColor", 0.1f);

                hitSequence.Append(noteTransform.DOPunchScale(Vector3.one * (hitScalePunchAmount * 0.7f), hitScalePunchDuration, 1, 0.5f));
                hitSequence.Join(noteTransform.DORotate(new Vector3(0, 0, hitRotationAmount * 0.5f), hitScalePunchDuration, RotateMode.FastBeyond360));
                hitSequence.Join(noteRenderer.material.DOFade(0f, hitFadeOutDuration).SetDelay(0.1f));
                break;

            default: // Okay hit
                // Simple hit animation
                Color okayColor = new Color(1f, 1f, 0f, 1f); // Yellow
                noteRenderer.material.DOColor(okayColor, "_BaseColor", 0.1f);

                hitSequence.Append(noteTransform.DOScale(1.2f, 0.2f).SetEase(Ease.OutCubic));
                hitSequence.Join(noteRenderer.material.DOFade(0f, hitFadeOutDuration));
                break;
        }

        // Return to pool when animation completes
        hitSequence.OnComplete(ReturnToPool);
    }

    /// <summary>
    /// Plays when note is missed (triggered by DOMove OnComplete).
    /// </summary>
    private void AnimateMiss()
    {
        if (isAnimatingHit) return;
        isAnimatingHit = true;

        // Kill any ongoing animations
        noteTransform.DOKill();
        noteRenderer.material.DOKill();

        // Create miss animation sequence
        Sequence missSequence = DOTween.Sequence();

        // Grey out, shrink, and drop down
        missSequence.Join(noteRenderer.material.DOColor(Color.gray, "_BaseColor", missDropDuration * 0.5f));
        missSequence.Join(noteTransform.DOScale(missScaleEndValue, missDropDuration).SetEase(Ease.InBack));
        missSequence.Join(noteTransform.DOMoveY(transform.position.y - missDropDistance, missDropDuration).SetEase(Ease.InCubic));

        // Return to pool when animation completes
        missSequence.OnComplete(() =>
        {
            ReturnToPool();
            if (spawner != null)
            {
                spawner.ProcessMissedNote(noteInfo);
            }
        });
    }

    /// <summary>
    /// Returns the note to the object pool.
    /// </summary>
    private void ReturnToPool()
    {
        // Reset state before returning to pool
        isAnimatingHit = false;
        noteTransform.localScale = Vector3.one;
        noteRenderer.material.color = originalMaterial.color;

        if (noteRenderer.material.HasProperty("_EmissionColor"))
        {
            noteRenderer.material.SetColor("_EmissionColor", Color.black);
        }

        if (spawner != null)
        {
            spawner.ReturnNoteToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}