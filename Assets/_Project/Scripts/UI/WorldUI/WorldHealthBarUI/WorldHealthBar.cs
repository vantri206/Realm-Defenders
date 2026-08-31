using DG.Tweening;
using UnityEngine;

public class WorldHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer fillRenderer;

    [Header("Visual Settings")]
    [SerializeField] private float backgroundAlpha = 0.75f;
    [SerializeField] private float fillAlpha = 0.75f;

    private Transform fillTransform;
    private Vector3 fullFillScale;

    private float targetPercent = 1f;
    private float displayedPercent = 1f;

    private Tween fillTween;
    private float fillTweenTime = 0.2f;

    private bool isDead;
    private bool isInitialized;

    protected virtual void Awake()
    {
        Initialize();
    }

    protected virtual void OnEnable()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        displayedPercent = targetPercent;
        ApplyFill(displayedPercent);
        ApplyVisibility();
    }

    protected virtual void OnDisable()
    {
        KillFillTween();
    }

    public virtual void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        CacheReferences();

        displayedPercent = targetPercent;
        isInitialized = true;

        ApplyAlpha();
        ApplyFill(displayedPercent);
        ApplyVisibility();
    }

    public virtual void SetValue(float currentHealth, float maxHealth)
    {
        float maxHealthValue = Mathf.Max(0f, maxHealth);
        float currentHealthValue = Mathf.Clamp(currentHealth, 0f, maxHealthValue);

        if (maxHealthValue > 0f)
        {
            targetPercent = currentHealthValue / maxHealthValue;
        }
        else targetPercent = 0f;

        isDead = currentHealthValue <= 0f;

        AnimateFill(targetPercent);

        ApplyVisibility();
    }

    public virtual void SetDead()
    {
        isDead = true;
        KillFillTween();

        ApplyVisibility();
    }

    private void AnimateFill(float percent)
    {
        percent = Mathf.Clamp01(percent);
        KillFillTween();

        if (fillTransform == null || fillTweenTime <= 0f || Mathf.Approximately(displayedPercent, percent))
        {
            SetFillImmediate(percent);
            return;
        }

        fillTween = DOTween.To
        (
            () => displayedPercent,
            value =>
            {
                displayedPercent = value;
                ApplyFill(displayedPercent);
            },
            percent,
            fillTweenTime
        )
        .SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            displayedPercent = percent;
            ApplyFill(displayedPercent);
            fillTween = null;
        });
    }

    private void SetFillImmediate(float percent)
    {
        KillFillTween();
        displayedPercent = Mathf.Clamp01(percent);
        ApplyFill(displayedPercent);
    }

    private void ApplyFill(float percent)
    {
        if (fillTransform == null)
        {
            Debug.LogError("[WorldHealthBar] Fill transform is required to apply health bar fill.", this);
            return;
        }

        Vector3 fillScale = fullFillScale;
        fillScale.x = Mathf.Clamp01(percent);
        fillTransform.localScale = fillScale;
    }

    private void ApplyAlpha()
    {
        SetRendererAlpha(backgroundRenderer, backgroundAlpha);
        SetRendererAlpha(fillRenderer, fillAlpha);
    }

    private static void SetRendererAlpha(SpriteRenderer spriteRenderer, float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }

    private void KillFillTween()
    {
        if (fillTween == null || !fillTween.IsActive())
        {
            fillTween = null;
            return;
        }

        fillTween.Kill();
        fillTween = null;
    }

    private void ApplyVisibility()
    {
        if (isDead)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
    }

    protected virtual void SetVisible(bool isVisible)
    {
        if (backgroundRenderer != null)
        {
            backgroundRenderer.enabled = isVisible;
        }

        if (fillRenderer != null)
        {
            fillRenderer.enabled = isVisible;
        }
    }

    protected virtual void CacheReferences()
    {
        if (backgroundRenderer == null)
        {
            backgroundRenderer = GetComponent<SpriteRenderer>();
        }

        if (fillRenderer == null)
        {
            Debug.LogWarning("[WorldHealthBar] Missing fill SpriteRenderer.", this);
            return;
        }

        fillTransform = fillRenderer.transform;
        fullFillScale = new Vector3(1f, fillTransform.localScale.y, fillTransform.localScale.z);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        fillTweenTime = Mathf.Max(0f, fillTweenTime);
        backgroundAlpha = Mathf.Clamp01(backgroundAlpha);
        fillAlpha = Mathf.Clamp01(fillAlpha);

        ApplyAlpha();
    }
#endif
}
