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

    /// <summary>
    /// Plays when note first spawns and starts flowing.
    /// Transitions from invisible to visible, scales up, and moves to target.
    /// </summary>
    public void AnimateSpawnAndFlow(Vector3 targetPosition, float duration)
    {
        // Reset state
        isAnimatingHit = false;
        noteTransform.localScale = Vector3.one * 0.5f;
        
        // Set transparent using PropertyBlock (no material instance creation)
        Color transparentColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        propertyBlock.SetColor("_BaseColor", transparentColor);
        noteRenderer.SetPropertyBlock(propertyBlock);

        // Create animation sequence
        Sequence spawnSequence = DOTween.Sequence();

        // Fade in and scale up with bounce
        spawnSequence.Join(noteTransform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
        spawnSequence.Join(DOTween.To(() => transparentColor.a, x => {
            transparentColor.a = x;
            propertyBlock.SetColor("_BaseColor", transparentColor);
            noteRenderer.SetPropertyBlock(propertyBlock);
        }, 1f, 0.3f));

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
        // No need to kill material animations since we use PropertyBlock

        // Create hit animation sequence
        Sequence hitSequence = DOTween.Sequence();

        switch (quality)
        {
            case HitAccuracy.Perfect:
                // Explosive perfect hit animation
                Color perfectColor = new Color(0f, 1f, 1f, 1f); // Cyan
                
                // Set colors using PropertyBlock
                propertyBlock.SetColor("_BaseColor", perfectColor);
                if (noteRenderer.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    propertyBlock.SetColor("_EmissionColor", perfectColor * 2f);
                }
                noteRenderer.SetPropertyBlock(propertyBlock);

                hitSequence.Append(noteTransform.DOPunchScale(Vector3.one * hitScalePunchAmount, hitScalePunchDuration, 2, 0.5f));
                hitSequence.Join(noteTransform.DORotate(new Vector3(0, 0, hitRotationAmount), hitScalePunchDuration, RotateMode.FastBeyond360));
                
                // Fade out using PropertyBlock
                hitSequence.Join(DOTween.To(() => perfectColor.a, x => {
                    perfectColor.a = x;
                    propertyBlock.SetColor("_BaseColor", perfectColor);
                    noteRenderer.SetPropertyBlock(propertyBlock);
                }, 0f, hitFadeOutDuration).SetDelay(0.1f));
                break;

            case HitAccuracy.Good:
                // Good hit animation - simpler but still satisfying
                Color goodColor = new Color(0f, 1f, 0f, 1f); // Green
                
                // Set color using PropertyBlock
                propertyBlock.SetColor("_BaseColor", goodColor);
                noteRenderer.SetPropertyBlock(propertyBlock);

                hitSequence.Append(noteTransform.DOPunchScale(Vector3.one * (hitScalePunchAmount * 0.7f), hitScalePunchDuration, 1, 0.5f));
                hitSequence.Join(noteTransform.DORotate(new Vector3(0, 0, hitRotationAmount * 0.5f), hitScalePunchDuration, RotateMode.FastBeyond360));
                
                // Fade out using PropertyBlock
                hitSequence.Join(DOTween.To(() => goodColor.a, x => {
                    goodColor.a = x;
                    propertyBlock.SetColor("_BaseColor", goodColor);
                    noteRenderer.SetPropertyBlock(propertyBlock);
                }, 0f, hitFadeOutDuration).SetDelay(0.1f));
                break;

            default: // Okay hit
                // Simple hit animation
                Color okayColor = new Color(1f, 1f, 0f, 1f); // Yellow
                
                // Set color using PropertyBlock
                propertyBlock.SetColor("_BaseColor", okayColor);
                noteRenderer.SetPropertyBlock(propertyBlock);

                hitSequence.Append(noteTransform.DOScale(1.2f, 0.2f).SetEase(Ease.OutCubic));
                
                // Fade out using PropertyBlock
                hitSequence.Join(DOTween.To(() => okayColor.a, x => {
                    okayColor.a = x;
                    propertyBlock.SetColor("_BaseColor", okayColor);
                    noteRenderer.SetPropertyBlock(propertyBlock);
                }, 0f, hitFadeOutDuration));
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
        // No need to kill material animations since we use PropertyBlock

        // Create miss animation sequence
        Sequence missSequence = DOTween.Sequence();
        
        // Grey out using PropertyBlock
        Color grayColor = Color.gray;
        propertyBlock.SetColor("_BaseColor", grayColor);
        noteRenderer.SetPropertyBlock(propertyBlock);
        
        // Shrink and drop down
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