using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonFeeling : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private enum FeedbackMode
    {
        ScaleGlow,
        SpriteSwap
    }

    private enum VisualState
    {
        Enabled,
        Hovered,
        Pressed,
        Active,
        Disabled
    }

    private static readonly int outlineColorId = Shader.PropertyToID("_OutlineColor");

    private const float transitionDuration = 0.05f;
    private const float pressedHoldDuration = 0.1f;

    [Header("Mode")]
    [SerializeField] private FeedbackMode feedbackMode = FeedbackMode.ScaleGlow;

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private CanvasGroup visualCanvasGroup;

    [Header("Scale Glow Mode")]
    [SerializeField, Min(1f)] private float hoveredScale = 1.2f;
    [SerializeField] private Graphic outlineGraphic;
    [SerializeField] private Color outlineColor = new Color(1f, 0.78f, 0.3f, 1f);

    [Header("Sprite Swap Mode")]
    [SerializeField] private Image spriteSwapTarget;
    [SerializeField] private Sprite enabledSprite;
    [SerializeField] private Sprite hoveredSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Disabled Visual")]
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.4f;

    private bool isActive;
    private bool isInteractable = true;

    private bool isPointerInside;
    private bool isPointerPressed;
    private bool isClickFeedbackPlaying;

    private Vector3 initialScale = Vector3.one;
    private float initialCanvasGroupAlpha = 1f;

    private Tween scaleTween;
    private Tween pressedHoldTween;

    private Material runtimeOutlineMaterial;

    private bool isInitialized;

    public bool IsActive => isActive;
    public bool IsInteractable => isInteractable;

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        CacheReferences();

        initialScale = visualRoot != null ? visualRoot.localScale : Vector3.one;

        if (visualCanvasGroup != null)
        {
            initialCanvasGroupAlpha = visualCanvasGroup.alpha;
        }

        if (spriteSwapTarget != null && enabledSprite == null)
        {
            enabledSprite = spriteSwapTarget.sprite;
        }

        CreateOutlineMaterialInstance();

        isInitialized = true;
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        isPointerInside = false;
        isPointerPressed = false;
        isClickFeedbackPlaying = false;
        isInteractable = button != null && button.interactable;
        ApplyVisual();
    }

    private void OnDisable()
    {
        KillTweens();
        isPointerInside = false;
        isPointerPressed = false;
        isClickFeedbackPlaying = false;
        ResetVisual();
    }

    private void OnDestroy()
    {
        KillTweens();

        if (runtimeOutlineMaterial != null)
        {
            Destroy(runtimeOutlineMaterial);
            runtimeOutlineMaterial = null;
        }
    }

    public void SetActive(bool value)
    {
        isActive = value;
        ApplyVisual();
    }

    public void SetInteractable(bool value)
    {
        isInteractable = value;

        if (button != null)
        {
            button.interactable = value;
        }

        if (!value)
        {
            isPointerPressed = false;
            isClickFeedbackPlaying = false;
            pressedHoldTween?.Kill();
            pressedHoldTween = null;
        }

        ApplyVisual();
    }

    public bool TryPlayClickFeedback()
    {
        if (!isInteractable || isClickFeedbackPlaying)
        {
            return false;
        }

        isClickFeedbackPlaying = true;
        ApplyVisual();

        pressedHoldTween?.Kill();

        pressedHoldTween = DOVirtual.DelayedCall(pressedHoldDuration, CompleteClickFeedback, true);
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;

        if (isInteractable && !isClickFeedbackPlaying)
        {
            ApplyVisual();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        isPointerPressed = false;

        if (!isClickFeedbackPlaying)
        {
            ApplyVisual();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isInteractable || (eventData != null && eventData.button != PointerEventData.InputButton.Left))
        {
            return;
        }

        isPointerPressed = true;
        ApplyVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isPointerPressed = false;

        if (!isClickFeedbackPlaying)
        {
            ApplyVisual();
        }
    }

    private void CompleteClickFeedback()
    {
        pressedHoldTween = null;
        isClickFeedbackPlaying = false;
        isPointerPressed = false;
        ApplyVisual();
    }

    private VisualState ResolveVisualState()
    {
        if (!isInteractable)
        {
            return VisualState.Disabled;
        }

        if (isClickFeedbackPlaying || isPointerPressed)
        {
            return VisualState.Pressed;
        }

        if (isActive)
        {
            return VisualState.Active;
        }

        if (isPointerInside)
        {
            return VisualState.Hovered;
        }

        return VisualState.Enabled;
    }

    private void ApplyVisual()
    {
        if (!isInitialized || !isActiveAndEnabled)
        {
            return;
        }

        VisualState state = ResolveVisualState();
        bool isHover = (state == VisualState.Hovered || state == VisualState.Pressed);
        bool isDisabled = (state == VisualState.Disabled);

        ApplyDisabledVisual(isDisabled);

        if (feedbackMode == FeedbackMode.ScaleGlow)
        {
            ApplyScaleGlowVisual(state, isHover);
        }
        else
        {
            ApplySpriteSwapVisual(state, isHover);
        }
    }

    private void ApplyScaleGlowVisual(VisualState state, bool isHover)
    {
        Vector3 targetScale = isHover ? initialScale * hoveredScale : initialScale;
        TweenScale(targetScale);
        SetOutlineVisible(isHover && state != VisualState.Disabled);
    }

    private void ApplySpriteSwapVisual(VisualState state, bool isHover)
    {
        TweenScale(initialScale);
        SetOutlineVisible(false);

        if (spriteSwapTarget == null)
        {
            return;
        }

        if (state == VisualState.Pressed && pressedSprite != null)
        {
            spriteSwapTarget.sprite = pressedSprite;
        }
        else if (isHover && hoveredSprite != null)
        {
            spriteSwapTarget.sprite = hoveredSprite;
        }
        else
        {
            spriteSwapTarget.sprite = enabledSprite;
        }
    }

    private void ApplyDisabledVisual(bool isDisabled)
    {
        if (visualCanvasGroup != null)
        {
            if (isDisabled)
            {
                visualCanvasGroup.alpha = initialCanvasGroupAlpha * disabledAlpha;
            }
            else
            {
                visualCanvasGroup.alpha = initialCanvasGroupAlpha;
            }
        }
    }

    private void TweenScale(Vector3 targetScale)
    {
        if (visualRoot == null)
        {
            return;
        }

        scaleTween?.Kill();
        scaleTween = null;

        scaleTween = visualRoot.DOScale(targetScale, transitionDuration)
                            .SetEase(Ease.OutQuad)
                            .SetUpdate(true)
                            .OnComplete(() => scaleTween = null);
    }

    private void SetOutlineVisible(bool isVisible)
    {
        if (runtimeOutlineMaterial == null)
        {
            return;
        }

        Color color = outlineColor;
        if (!isVisible)
        {
            color.a = 0f;
        }
        else
        {
            color.a = outlineColor.a;
        }
        runtimeOutlineMaterial.SetColor(outlineColorId, color);
    }

    private void CreateOutlineMaterialInstance()
    {
        if (outlineGraphic == null || outlineGraphic.material == null ||  !outlineGraphic.material.HasProperty(outlineColorId))
        {
            return;
        }

        runtimeOutlineMaterial = new Material(outlineGraphic.material);
        outlineGraphic.material = runtimeOutlineMaterial;
        SetOutlineVisible(false);
    }

    private void ResetVisual()
    {
        if (visualRoot != null)
        {
            visualRoot.localScale = initialScale;
        }

        if (visualCanvasGroup != null)
        {
            visualCanvasGroup.alpha = initialCanvasGroupAlpha;
        }

        if (spriteSwapTarget != null)
        {
            spriteSwapTarget.sprite = enabledSprite;
        }

        SetOutlineVisible(false);
    }

    private void KillTweens()
    {
        scaleTween?.Kill();
        scaleTween = null;

        pressedHoldTween?.Kill();
        pressedHoldTween = null;
    }

    private void CacheReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (visualRoot == null)
        {
            visualRoot = transform as RectTransform;
        }

        if (visualCanvasGroup == null)
        {
            visualCanvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
