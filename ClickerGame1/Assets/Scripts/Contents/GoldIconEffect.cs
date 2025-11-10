using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// Individual gold icon behavior
public class GoldIconEffect : MonoBehaviour
{
    RectTransform rect;
    Image img;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        img = GetComponent<Image>();
    }

    void OnEnable()
    {
        // Ensure starting alpha and transform are reset when reused
        if (img != null)
        {
            var c = img.color;
            img.color = new Color(c.r, c.g, c.b, 1f);
        }
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }

    // Plays animation: smooth move to endPos, scale pop, rotate slightly and fade out
    public void Play(Vector2 endAnchoredPos, float duration)
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (img == null) img = GetComponent<Image>();

        // small random jitter to start so icons don't overlap exactly
        Vector2 jitter = Random.insideUnitCircle * 10f;
        rect.anchoredPosition += jitter;

        // Slight stagger per icon for organic feel
        float stagger = Random.Range(0f, duration * 0.12f);

        // Kill previous tweens to avoid conflicts
        rect.DOKill();
        if (img != null) img.DOKill();

        // Move directly to target with a smooth ease (no pause)
        rect.DOAnchorPos(endAnchoredPos, duration).SetEase(Ease.OutCubic).SetDelay(stagger);

        // Scale: quick pop then settle toward 0.9 over the duration
        rect.localScale = Vector3.one * 0.6f;
        rect.DOScale(Vector3.one * 0.95f, duration * 0.6f).SetEase(Ease.OutBack).SetDelay(stagger);

        // Slight rotation for visual variety
        float rot = Random.Range(-30f, 30f);
        rect.DOLocalRotate(new Vector3(0, 0, rot), duration).SetEase(Ease.OutCubic).SetDelay(stagger);

        // Fade out towards the end
        if (img != null)
        {
            img.DOFade(0f, duration * 0.8f).SetEase(Ease.InQuad).SetDelay(stagger + duration * 0.15f)
                .OnComplete(() => {
                    if (GoldEffectManager.Instance != null && GoldEffectManager.Instance.usePooling)
                        GoldEffectManager.Instance.Recycle(this.gameObject);
                    else
                        Destroy(gameObject);
                });
        }
        else
        {
            // fallback return/destroy
            DOVirtual.DelayedCall(stagger + duration, () => {
                if (GoldEffectManager.Instance != null && GoldEffectManager.Instance.usePooling)
                    GoldEffectManager.Instance.Recycle(this.gameObject);
                else
                    Destroy(gameObject);
            });
        }
    }

    void OnDisable()
    {
        // Kill tweens when disabled to avoid stray callbacks
        rect.DOKill();
        if (img != null) img.DOKill();
    }
}
