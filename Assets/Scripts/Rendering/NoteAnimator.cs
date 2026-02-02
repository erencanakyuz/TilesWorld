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
    private MaterialPropertyBlock propertyBlock;
    private Color originalColor;
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
        propertyBlock = new MaterialPropertyBlock();
        
        // Get original color from shared material (no instance creation)
        originalColor = noteRenderer.sharedMaterial.color;
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

    void OnDisable()
    {
        // Kill all tweens on this transform to prevent DOTween errors when object is destroyed
        if (noteTransform != null)
        {
            noteTransform.DOKill();
        }
    }

    /// <summary>
    /// Plays when note first spawns and starts flowing.
    /// Transitions from invisible to visible, scales up, and moves to target.
    /// </summary>
    public void AnimateSpawnAndFlow(Vector3 targetPosition, float duration)
    {
        // OPTIMIZATION: Kill any existing tweens on this note from previous pool usage
        noteTransform.DOKill(true);
        
        // Reset state
        isAnimatingHit = false;
        noteTransform.localScale = Vector3.one * 0.5f;
        
        // PERFORMANCE FIX: Set color directly instead of animating via lambda (was causing frame drops)
        propertyBlock.SetColor("_BaseColor", originalColor);
        noteRenderer.SetPropertyBlock(propertyBlock);

        // SIMPLIFIED ANIMATION: Just scale up, no fade (reduces per-frame lambda calls)
        noteTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetTarget(noteTransform);

        // Flow to target position (linear, constant speed)
        // Cache the callback reference to avoid creating new delegate each time
        noteTransform.DOMove(targetPosition, duration)
            .SetEase(Ease.Linear)
            .SetTarget(noteTransform)
            .OnComplete(OnMoveComplete);
    }
    
    // Cached callback to avoid delegate allocation
    private void OnMoveComplete()
    {
        if (!isAnimatingHit) AnimateMiss();
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

        // Get animation parameters for this hit quality
        var animParams = GetHitAnimationParams(quality);
        
        // Set color using PropertyBlock - DO THIS ONCE, NOT EVERY FRAME!
        Color hitColor = animParams.color;
        propertyBlock.SetColor("_BaseColor", hitColor);
        
        if (animParams.hasEmission && noteRenderer.sharedMaterial.HasProperty("_EmissionColor"))
        {
            propertyBlock.SetColor("_EmissionColor", hitColor * 2f);
        }
        noteRenderer.SetPropertyBlock(propertyBlock);

        // Create hit animation sequence - LINK TO TRANSFORM so DOKill() works
        Sequence hitSequence = DOTween.Sequence().SetTarget(noteTransform);

        if (animParams.usePunchScale)
        {
            hitSequence.Append(noteTransform.DOPunchScale(Vector3.one * animParams.scalePunch, hitScalePunchDuration, animParams.vibrato, 0.5f));
            hitSequence.Join(noteTransform.DORotate(new Vector3(0, 0, animParams.rotation), hitScalePunchDuration, RotateMode.FastBeyond360));
        }
        else
        {
            hitSequence.Append(noteTransform.DOScale(animParams.scalePunch + 1f, 0.2f).SetEase(Ease.OutCubic));
        }

        // PERFORMANCE FIX: Simple scale down instead of per-frame fade callback
        // This eliminates the lambda that was calling SetPropertyBlock every frame
        hitSequence.Append(noteTransform.DOScale(0f, hitFadeOutDuration).SetEase(Ease.InQuad));

        // Return to pool when animation completes
        hitSequence.OnComplete(ReturnToPool);
    }

    private struct HitAnimationParams
    {
        public Color color;
        public float scalePunch;
        public float rotation;
        public int vibrato;
        public bool usePunchScale;
        public bool hasEmission;
    }

    private HitAnimationParams GetHitAnimationParams(HitAccuracy quality)
    {
        return quality switch
        {
            HitAccuracy.Perfect => new HitAnimationParams
            {
                color = new Color(0f, 1f, 1f, 1f), // Cyan
                scalePunch = hitScalePunchAmount,
                rotation = hitRotationAmount,
                vibrato = 2,
                usePunchScale = true,
                hasEmission = true
            },
            HitAccuracy.Good => new HitAnimationParams
            {
                color = new Color(0f, 1f, 0f, 1f), // Green
                scalePunch = hitScalePunchAmount * 0.7f,
                rotation = hitRotationAmount * 0.5f,
                vibrato = 1,
                usePunchScale = true,
                hasEmission = false
            },
            _ => new HitAnimationParams // Okay
            {
                color = new Color(1f, 1f, 0f, 1f), // Yellow
                scalePunch = 0.2f,
                rotation = 0f,
                vibrato = 0,
                usePunchScale = false,
                hasEmission = false
            }
        };
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
        // No need to kill material animations since we use PropertyBlock

        // Create miss animation sequence - LINK TO TRANSFORM so DOKill() works
        Sequence missSequence = DOTween.Sequence().SetTarget(noteTransform);
        
        // Grey out using PropertyBlock
        Color grayColor = Color.gray;
        propertyBlock.SetColor("_BaseColor", grayColor);
        noteRenderer.SetPropertyBlock(propertyBlock);
        
        // Shrink and drop down
        missSequence.Join(noteTransform.DOScale(missScaleEndValue, missDropDuration).SetEase(Ease.InBack));
        missSequence.Join(noteTransform.DOMoveY(transform.position.y - missDropDistance, missDropDuration).SetEase(Ease.InCubic));

        // PERFORMANCE FIX: Use cached method reference instead of lambda
        missSequence.OnComplete(OnMissAnimationComplete);
    }
    
    // Cached callback for miss animation to avoid delegate allocation
    private void OnMissAnimationComplete()
    {
        if (spawner != null)
        {
            spawner.ProcessMissedNote(noteInfo);
        }
        ReturnToPool();
    }

    /// <summary>
    /// Returns the note to the object pool.
    /// </summary>
    private void ReturnToPool()
    {
        // CRITICAL: Kill ALL tweens before returning to pool to prevent accumulation
        noteTransform.DOKill();
        
        // Reset state before returning to pool
        isAnimatingHit = false;
        noteTransform.localScale = Vector3.one;
        
        // Reset colors using PropertyBlock (no material instance creation)
        propertyBlock.SetColor("_BaseColor", originalColor);
        if (noteRenderer.sharedMaterial.HasProperty("_EmissionColor"))
        {
            propertyBlock.SetColor("_EmissionColor", Color.black);
        }
        noteRenderer.SetPropertyBlock(propertyBlock);

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