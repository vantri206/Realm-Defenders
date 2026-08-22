using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class WorldActionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Action")]
    [SerializeField] private HeroActionType actionType;
    [SerializeField] private bool isScaleUpHover = false;

    [Header("References")]
    [SerializeField] private SpriteRenderer buttonRenderer;
    [SerializeField] private Collider2D col;

    private Vector3 initialScale;
    private Color initialColor;
    private Tween scaleTween;
    private Sequence clickColorSequence;
    private bool isProcessingClick;

    private float scaleTweenTime = 0.2f;
    private float scaleHover = 1.2f;
    private float clickProcessTime = 0.2f;
    private Color clickedColor = Color.red;

    public event Action<HeroActionType> OnClicked;

    private void Awake()
    {
        CacheReferences();
        initialScale = transform.localScale;

        if (buttonRenderer != null)
        {
            initialColor = buttonRenderer.color;
        }
    }

    private void OnDisable()
    {
        KillAllTweens();
        isProcessingClick = false;
        transform.localScale = initialScale;

        if (buttonRenderer != null)
        {
            buttonRenderer.color = initialColor;
        }
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isScaleUpHover)
        {
            return;
        }

        TweenScale(initialScale * scaleHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isScaleUpHover)
        {
            return;
        }

        TweenScale(initialScale);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (isProcessingClick)
        {
            return;
        }

        PlayClickFeedback();
    }

    private void PlayClickFeedback()
    {
        if (buttonRenderer == null)
        {
            OnClicked?.Invoke(actionType);
            return;
        }

        isProcessingClick = true;
        clickColorSequence?.Kill();
        buttonRenderer.color = initialColor;

        clickColorSequence = DOTween.Sequence();
        clickColorSequence.Append(buttonRenderer.DOColor(clickedColor, clickProcessTime / 2));
        clickColorSequence.Append(buttonRenderer.DOColor(initialColor, clickProcessTime / 2));
        clickColorSequence.SetUpdate(true);
        clickColorSequence.OnComplete(() =>
        {
            clickColorSequence = null;
            isProcessingClick = false;
            OnClicked?.Invoke(actionType);
        });
    }

    private void TweenScale(Vector3 targetScale)
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(targetScale, scaleTweenTime)
        .SetEase(Ease.OutQuad)
        .SetUpdate(true)
        .OnComplete(() => scaleTween = null);
    }

    private void CacheReferences()
    {
        if (buttonRenderer == null)
        {
            buttonRenderer = GetComponent<SpriteRenderer>();
        }

        if (col == null)
        {
            col = GetComponent<Collider2D>();
        }

        if (buttonRenderer == null)
        {
            Debug.LogError("[WorldActionButton] SpriteRenderer is required.", this);
        }

        if (col == null)
        {
            Debug.LogError("[WorldActionButton] Collider2D is required to receive pointer input.", this);
        }
    }

    private void KillAllTweens()
    {
        scaleTween?.Kill();
        scaleTween = null;

        clickColorSequence?.Kill();
        clickColorSequence = null;
    }
}