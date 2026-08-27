using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonFeedback : MonoBehaviour
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

    [Header("Sprite Swap Mode")]
    [SerializeField] private Image spriteSwapTarget;
    [SerializeField] private Sprite enabledSprite;
    [SerializeField] private Sprite hoveredSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Disabled Visual")]
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.75f;

    private bool isActive;

    private GameInput gameInput;
    private PointerEventData pointerEventData;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private bool isPointerInside;
    private bool isPointerPressed;
    private bool isClickFeedbackPlaying;

    private Vector3 initialScale = Vector3.one;
    private float initialCanvasGroupAlpha = 1f;

    private Tween scaleTween;
    private Tween pressedHoldTween;

    private bool isInitialized;

    public bool IsActive => isActive;
    public bool IsInteractable => button != null && button.interactable;

    public event Action OnClicked;

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

        isInitialized = true;
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        gameInput = GameInput.Instance;
        RegisterInputEvents();
        RegisterButtonEvent();
        isPointerInside = gameInput != null && IsTopRaycastTarget(gameInput.UIActions.Point.ReadValue<Vector2>());
        isPointerPressed = false;
        isClickFeedbackPlaying = false;
        ApplyVisual();
    }

    private void OnDisable()
    {
        UnregisterInputEvents();
        UnregisterButtonEvent();
        KillTweens();
        isPointerInside = false;
        isPointerPressed = false;
        isClickFeedbackPlaying = false;
        gameInput = null;
        ResetVisual();
    }

    private void OnDestroy()
    {
        UnregisterInputEvents();
        UnregisterButtonEvent();
        KillTweens();
    }

    public void SetActive(bool value)
    {
        isActive = value;
        ApplyVisual();
    }

    public void SetInteractable(bool value)
    {
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

    private void HandleButtonClicked()
    {
        if (!TryBeginClickFeedback())
        {
            return;
        }

        OnClicked?.Invoke();
    }

    private bool TryBeginClickFeedback()
    {
        if (!IsInteractable || isClickFeedbackPlaying)
        {
            return false;
        }

        isClickFeedbackPlaying = true;
        ApplyVisual();

        pressedHoldTween?.Kill();

        pressedHoldTween = DOVirtual.DelayedCall(pressedHoldDuration, CompleteClickFeedback, true);
        return true;
    }

    private void HandlePointPerformed(InputAction.CallbackContext context)
    {
        bool isInside = IsTopRaycastTarget(context.ReadValue<Vector2>());
        if (isPointerInside == isInside)
        {
            return;
        }

        isPointerInside = isInside;

        if (!isPointerInside)
        {
            isPointerPressed = false;
        }

        if (!isClickFeedbackPlaying)
        {
            ApplyVisual();
        }
    }

    private void HandleLeftClickPerformed(InputAction.CallbackContext context)
    {
        if (!IsInteractable || !IsTopRaycastTarget(gameInput.UIActions.Point.ReadValue<Vector2>()))
        {
            return;
        }

        isPointerPressed = true;
        ApplyVisual();
    }

    private void HandleLeftClickCanceled(InputAction.CallbackContext context)
    {
        if (!isPointerPressed)
        {
            return;
        }

        isPointerPressed = false;

        if (!isClickFeedbackPlaying)
        {
            ApplyVisual();
        }
    }

    private bool IsTopRaycastTarget(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        if (pointerEventData == null)
        {
            pointerEventData = new PointerEventData(eventSystem);
        }

        pointerEventData.position = screenPosition;
        raycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, raycastResults);

        if (raycastResults.Count == 0)
        {
            return false;
        }

        return raycastResults[0].gameObject.GetComponentInParent<UIButtonFeedback>() == this;
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
        if (!IsInteractable)
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
    }

    private void ApplySpriteSwapVisual(VisualState state, bool isHover)
    {
        TweenScale(initialScale);

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
    }

    private void KillTweens()
    {
        scaleTween?.Kill();
        scaleTween = null;

        pressedHoldTween?.Kill();
        pressedHoldTween = null;
    }

    private void RegisterButtonEvent()
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(HandleButtonClicked);
        button.onClick.AddListener(HandleButtonClicked);
    }

    private void RegisterInputEvents()
    {
        if (gameInput == null)
        {
            return;
        }

        gameInput.UIActions.Point.performed += HandlePointPerformed;
        gameInput.UIActions.LeftClick.performed += HandleLeftClickPerformed;
        gameInput.UIActions.LeftClick.canceled += HandleLeftClickCanceled;
    }

    private void UnregisterInputEvents()
    {
        if (gameInput == null)
        {
            return;
        }

        gameInput.UIActions.Point.performed -= HandlePointPerformed;
        gameInput.UIActions.LeftClick.performed -= HandleLeftClickPerformed;
        gameInput.UIActions.LeftClick.canceled -= HandleLeftClickCanceled;
    }

    private void UnregisterButtonEvent()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
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
