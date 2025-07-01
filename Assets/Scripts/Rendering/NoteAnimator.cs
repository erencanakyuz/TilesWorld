using UnityEngine;
using DG.Tweening;

/// <summary>
/// Tek bir notanın tüm görsel animasyonlarını yönetir.
/// Doğma, akış, başarılı vuruş ve kaçırılma animasyonlarından sorumludur.
/// Bu script, NoteRenderer tarafından kontrol edilir ve nota prefab'ine eklenir.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class NoteAnimator : MonoBehaviour
{
    private Renderer noteRenderer;
    private Transform noteTransform;
    private NoteRenderer spawner; // Notayı havuza geri göndermek için referans
    private GameNoteInfo noteInfo;

    void Awake()
    {
        noteRenderer = GetComponent<Renderer>();
        noteTransform = transform;
    }

    /// <summary>
    /// NoteRenderer tarafından çağrılır. Gerekli referansları ve bilgileri ayarlar.
    /// </summary>
    public void Initialize(NoteRenderer spawnerRef, GameNoteInfo info)
    {
        this.spawner = spawnerRef;
        this.noteInfo = info;
    }

    /// <summary>
    /// Nota ilk oluştuğunda ve akmaya başladığında çalışır.
    /// Notayı görünmezden görünür hale getirir, büyütür ve hedefe doğru hareket ettirir.
    /// </summary>
    public void AnimateSpawnAndFlow(Vector3 targetPosition, float duration)
    {
        // Başlangıç durumunu ayarla (görünmez, hafif küçük ve varsayılan renkte)
        Color c = noteRenderer.material.color;
        noteRenderer.material.color = new Color(c.r, c.g, c.b, 0f);
        noteTransform.localScale = Vector3.one * 0.5f;

        // Eş zamanlı olarak büyüt ve görünür yap
        noteTransform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        noteRenderer.material.DOFade(1f, 0.3f);

        // Hedefe doğru akış (lineer, sabit hızda)
        noteTransform.DOMove(targetPosition, duration)
            .SetEase(Ease.Linear)
            .OnComplete(AnimateMiss); // Eğer bu animasyon normal şekilde biterse, nota kaçırılmış demektir.
    }

    /// <summary>
    /// Nota başarıyla vurulduğunda HitZoneManager tarafından çağrılır.
    /// </summary>
    public void AnimateHit(HitAccuracy quality)
    {
        // Önce çalışan tüm animasyonları (özellikle DOMove) anında öldür ki miss animasyonu tetiklenmesin.
        noteTransform.DOKill();

        switch (quality)
        {
            case HitAccuracy.Perfect:
                // "Patlama" efekti: Büyü, titre ve parlak renkle kaybol.
                noteTransform.DOPunchScale(new Vector3(0.5f, 0.5f, 0.5f), 0.3f, 1, 0.5f);
                noteRenderer.material.DOColor(Color.cyan, 0.1f);
                noteRenderer.material.DOFade(0f, 0.3f).SetDelay(0.1f).OnComplete(ReturnToPool);
                break;

            case HitAccuracy.Good:
            case HitAccuracy.Okay:
                // "Vurma" efekti: Hafifçe zıpla ve kaybol.
                noteTransform.DOScale(1.2f, 0.2f).SetEase(Ease.OutCubic).SetLoops(2, LoopType.Yoyo);
                noteRenderer.material.DOFade(0f, 0.3f).SetDelay(0.1f).OnComplete(ReturnToPool);
                break;

            default: // Miss veya diğer durumlar (güvenlik için)
                noteRenderer.material.DOFade(0f, 0.2f).OnComplete(ReturnToPool);
                break;
        }
    }

    /// <summary>
    /// Nota kaçırıldığında çalışır (DOMove'un OnComplete'i ile tetiklenir).
    /// </summary>
    private void AnimateMiss()
    {
        noteTransform.DOKill(); // Güvenlik için

        // Rengi griye döner, küçülür ve düşerek kaybolur.
        noteRenderer.material.DOColor(Color.gray, 0.5f);
        noteTransform.DOScale(0f, 0.5f).SetEase(Ease.InBack);
        noteTransform.DOMoveY(transform.position.y - 1.5f, 0.5f).SetEase(Ease.InCubic).OnComplete(ReturnToPool);

        // Kaçırma olayını oyun mantığına bildir.
        if (spawner != null)
        {
            spawner.ProcessMissedNote(noteInfo);
        }
    }

    /// <summary>
    /// Animasyon bitince objeyi havuza geri gönderir.
    /// </summary>
    private void ReturnToPool()
    {
        // Havuza dönmeden önce rengi ve ölçeği sıfırla
        noteTransform.localScale = Vector3.one;
        Color c = noteRenderer.material.color;
        noteRenderer.material.color = new Color(c.r, c.g, c.b, 1f); // Alfayı resetle

        if (spawner != null)
        {
            spawner.ReturnNoteToPool(gameObject);
        }
        else
        {
            Destroy(gameObject); // Fallback
        }
    }
}