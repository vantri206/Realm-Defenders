using DG.Tweening;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class QuickNotification : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private RectTransform viewRoot;
    [SerializeField] private TMP_Text messageText;

    [Header("Animation")]
    [SerializeField] private Vector2 hiddenOffset = new Vector2(0f, 160f);
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private float visibleDuration = 2f;

    private CanvasGroup canvasGroup;
    private Sequence sequence;
    private Vector2 initialAnchoredPosition;
    private bool hasInitialAnchoredPosition;

    private void Awake()
    {
        CacheReferences();
        CacheInitialAnchoredPosition();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnDisable()
    {
        KillSequence();
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    public bool Show(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        CacheReferences();
        CacheInitialAnchoredPosition();

        if (viewRoot == null || messageText == null || canvasGroup == null)
        {
            Debug.LogError("[QuickNotification] ViewRoot, MessageText, and CanvasGroup references are required.", this);
            return false;
        }

        KillSequence();

        messageText.text = text;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        viewRoot.anchoredPosition = initialAnchoredPosition + hiddenOffset;
        viewRoot.gameObject.SetActive(true);

        sequence = DOTween.Sequence()
                        .SetUpdate(true)
                        .Append(viewRoot.DOAnchorPos(initialAnchoredPosition, slideDuration).SetEase(Ease.OutCubic))
                        .AppendInterval(visibleDuration)
                        .Append(viewRoot.DOAnchorPos(initialAnchoredPosition + hiddenOffset, slideDuration).SetEase(Ease.InCubic))
                        .OnComplete(HandleSequenceCompleted);

        return true;
    }

    public void HideImmediate()
    {
        KillSequence();
        CacheReferences();
        CacheInitialAnchoredPosition();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (viewRoot != null)
        {
            viewRoot.anchoredPosition = initialAnchoredPosition;
            viewRoot.gameObject.SetActive(false);
        }
    }

    private void HandleSequenceCompleted()
    {
        sequence = null;
        HideImmediate();
    }

    private void KillSequence()
    {
        sequence?.Kill();
        sequence = null;
    }

    private void CacheInitialAnchoredPosition()
    {
        if (hasInitialAnchoredPosition || viewRoot == null)
        {
            return;
        }

        initialAnchoredPosition = viewRoot.anchoredPosition;
        hasInitialAnchoredPosition = true;
    }

    private void CacheReferences()
    {
        if (viewRoot == null)
        {
            viewRoot = transform as RectTransform;
        }

        if (messageText == null)
        {
            messageText = GetComponentInChildren<TMP_Text>(true);
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif
}
