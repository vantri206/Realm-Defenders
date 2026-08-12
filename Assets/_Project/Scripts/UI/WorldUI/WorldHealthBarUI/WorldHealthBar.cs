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

    [Header("Visibility")]
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private float visibleDuration = 2f;

    private Transform fillTransform;
    private Vector3 fullFillScale;
    private int maxFillPixels;

    private float targetPercent = 1f;
    private float displayedPercent = 1f;
    private CountdownTimer visibleTimer;

    private Tween fillTween;
    [SerializeField]
    private float fillTweenTime = 0.2f;

    private bool isDead;
    private bool isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        displayedPercent = targetPercent;
        ApplyFill(displayedPercent);
        ApplyVisibility();
    }

    private void OnDisable()
    {
        KillFillTween();
    }

    private void Update()
    {
        if (visibleTimer == null || !visibleTimer.IsRunning)
        {
            return;
        }

        visibleTimer.Tick(Time.deltaTime);

        if (!visibleTimer.IsRunning)
        {
            ApplyVisibility();
        }
    }

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        CacheReferences();

        visibleTimer = new CountdownTimer(visibleDuration);
        displayedPercent = targetPercent;
        isInitialized = true;

        ApplyAlpha();
        ApplyFill(displayedPercent);
        ApplyVisibility();
    }

    public void SetValue(float currentHealth, float maxHealth, bool isShowBar)
    {
        float maxHealthValue = Mathf.Max(0f, maxHealth);
        float currentHealthValue = Mathf.Clamp(currentHealth, 0f, maxHealthValue);

        if (maxHealthValue > 0f)
        {
            targetPercent = currentHealthValue / maxHealthValue;
        }
        else targetPercent = 0f;

        isDead = currentHealthValue <= 0f;

        if (isShowBar)
        {
            AnimateFill(targetPercent);
        }
        else
        {
            SetFillImmediate(targetPercent);
        }

        if (isShowBar && !isDead)
        {
            Show();
            return;
        }

        ApplyVisibility();
    }

    public void SetDead()
    {
        isDead = true;
        KillFillTween();

        if (visibleTimer != null)
        {
            visibleTimer.StopTimer();
        }

        ApplyVisibility();
    }

    public void Show()
    {
        if (isDead || visibleTimer == null)
        {
            return;
        }

        visibleTimer.StartTimer();
        SetVisible(true);
    }

    private void CacheReferences()
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
        fullFillScale = fillTransform.localScale;
        maxFillPixels = CalculateMaxFillPixels();
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
        fillScale.x = fullFillScale.x * GetPixelSnappedPercent(percent);
        fillTransform.localScale = fillScale;
    }

    private float GetPixelSnappedPercent(float percent)
    {
        percent = Mathf.Clamp01(percent);

        if (maxFillPixels <= 0)
        {
            return percent;
        }

        int visiblePixels = Mathf.RoundToInt(maxFillPixels * percent);
        if (percent > 0f && visiblePixels == 0)
        {
            visiblePixels = 1;
        }

        return visiblePixels / (float)maxFillPixels;
    }

    private int CalculateMaxFillPixels()
    {
        if (fillRenderer == null || fillRenderer.sprite == null)
        {
            return 0;
        }

        float pixelWidth = fillRenderer.sprite.rect.width * Mathf.Abs(fullFillScale.x);
        return Mathf.Max(1, Mathf.RoundToInt(pixelWidth));
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

        if (hideWhenFull && targetPercent >= 0.99f)
        {
            SetVisible(false);
            return;
        }

        if (visibleTimer == null || !visibleTimer.IsRunning)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
    }

    private void SetVisible(bool isVisible)
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
#if UNITY_EDITOR
    private void OnValidate()
    {
        fillTweenTime = Mathf.Max(0f, fillTweenTime);
        visibleDuration = Mathf.Max(0f, visibleDuration);
        backgroundAlpha = Mathf.Clamp01(backgroundAlpha);
        fillAlpha = Mathf.Clamp01(fillAlpha);

        ApplyAlpha();
    }
#endif
}
