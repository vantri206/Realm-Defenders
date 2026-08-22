using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HeroCardView : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private Canvas sortingCanvas;
    [SerializeField] private float selectedScale = 1.25f;
    [SerializeField] private float selectedYOffset = 4f;

    [Header("Info")]
    [SerializeField] private Image heroIcon;
    [SerializeField] private Image classIcon;

    [Header("Cost")]
    [SerializeField] private UIValueTextBinding costText = new UIValueTextBinding();

    [Header("State Overlay")]
    [SerializeField] private Image unavailableOverlay;
    [SerializeField] private Image countdownOverlay;
    [SerializeField] private Image countdownCircle;
    [SerializeField] private UIValueTextBinding countdownText = new UIValueTextBinding();

    private HeroCombatState combatState;
    
    private Tween selectedTween;
    private Vector2 defaultAnchoredPosition;
    private Vector3 defaultLocalPosition;
    private bool hasDefaultLocalPos;

    private float selectedTweenDuration = 0.2f;
    private int selectedSortingOrder = 100;

    private void Awake()
    {
        CacheDefaultPosition();
        Clear();
    }

    private void OnDestroy()
    {
        selectedTween?.Kill();
    }

    public void SetData(HeroCombatState state)
    {
        combatState = state;
        HideAllOverlays();
        Refresh();
    }

    public void Clear()
    {
        combatState = null;
        SetSelected(false);
        SetImage(heroIcon, null);
        SetImage(classIcon, null);
        costText.Refresh();
        HideAllOverlays();
    }

    public void Refresh()
    {
        if (combatState == null || !combatState.IsValid)
        {
            Clear();
            return;
        }

        LoadData(combatState);
        RefreshCountdown();
    }

    public void LoadData(HeroCombatState state)
    {
        if (state == null || !state.IsValid)
        {
            Clear();
            return;
        }

        HeroDefinition definition = state.Definition;
        SetImage(heroIcon, definition.HeroIcon);
        SetImage(classIcon, definition.HeroClass.Icon);
        costText.SetInt(state.DeployCost);
    }

    public void SetSelected(bool isSelected)
    {
        if (sortingCanvas != null)
        {
            sortingCanvas.overrideSorting = isSelected;
            sortingCanvas.sortingOrder = isSelected ? selectedSortingOrder : 0;
        }

        if (visualRoot == null)
        {
            Debug.LogError("[HeroCardView] Visual root is required to update selection visuals.", this);
            return;
        }

        CacheDefaultPosition();

        selectedTween?.Kill();

        Sequence sequence = DOTween.Sequence();
        sequence.Join(visualRoot.DOScale(isSelected ? selectedScale : 1f, selectedTweenDuration));

        float targetY = isSelected ? selectedYOffset : 0f;
        if (visualRoot != null)
        {
            sequence.Join(visualRoot.DOAnchorPosY(defaultAnchoredPosition.y + targetY, selectedTweenDuration));
        }
        else
        {
            sequence.Join(visualRoot.DOLocalMoveY(defaultLocalPosition.y + targetY, selectedTweenDuration));
        }

        selectedTween = sequence.SetEase(Ease.OutQuad);
    }

    private void CacheDefaultPosition()
    {
        if (hasDefaultLocalPos || visualRoot == null)
        {
            if (visualRoot == null)
            {
                Debug.LogError("[HeroCardView] Visual root is required to cache card position.", this);
            }

            return;
        }

        if (visualRoot != null)
        {
            defaultAnchoredPosition = visualRoot.anchoredPosition;
        }

        defaultLocalPosition = visualRoot.localPosition;
        hasDefaultLocalPos = true;
    }

    public void HideAllOverlays()
    {
        SetUnavailableOverlay(false);
        SetCountdownOverlay(false);
        countdownText.SetVisible(false);
        if (countdownCircle != null)
        {
            countdownCircle.gameObject.SetActive(false);
        }
    }

    public void SetUnavailableOverlay(bool isVisible)
    {
        if (unavailableOverlay != null)
        {
            unavailableOverlay.gameObject.SetActive(isVisible);
        }
    }

    public void ShowUnavailableOverlay()
    {
        SetUnavailableOverlay(true);
    }

    public void HideUnavailableOverlay()
    {
        SetUnavailableOverlay(false);
    }

    private void SetCountdownOverlay(bool isVisible)
    {
        if (countdownOverlay != null)
        {
            countdownOverlay.gameObject.SetActive(isVisible);
        }

        countdownText.SetVisible(isVisible);
        if (countdownCircle == null)
        {
            if (isVisible)
            {
                Debug.LogError("[HeroCardView] Countdown circle Image is required to show countdown overlay.", this);
            }

            return;
        }

        countdownCircle.gameObject.SetActive(isVisible);
    }

    private void ShowCountdown(float remainingTime, float totalTime)
    {
        remainingTime = Mathf.Max(0f, remainingTime);
        totalTime = Mathf.Max(0f, totalTime);

        bool hasCountdown = remainingTime > 0f && totalTime > 0f;
        SetCountdownOverlay(hasCountdown);

        if (!hasCountdown)
        {
            ClearCountdown();
            return;
        }

        if (countdownCircle != null)
        {
            countdownCircle.fillAmount = Mathf.Clamp01(remainingTime / totalTime);
        }

        countdownText.SetNumber(remainingTime);
    }

    public void ClearCountdown()
    {
        if (countdownCircle != null)
        {
            countdownCircle.fillAmount = 0f;
        }

        countdownText.Refresh();

        SetCountdownOverlay(false);
    }

    public void RefreshCountdown()
    {
        if (combatState == null || combatState.IsReadyDeploy)
        {
            ClearCountdown();
            return;
        }

        ShowCountdown(combatState.RedeployCountdownTime, combatState.RedeployTime);
    }

    public void Tick(float deltaTime)
    {
        if (combatState == null)
        {
            return;
        }

        RefreshCountdown();
    }

    private void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        if (sprite == null)
        {
            image.sprite = null;
            image.enabled = false;
            return;
        }

        image.sprite = sprite;
        image.enabled = true;
    }

    public void SetState(HeroDeployState newState)
    {
        if (combatState == null)
        {
            HideAllOverlays();
            return;
        }

        switch (newState)
        {
            case HeroDeployState.Available:
                HideAllOverlays();
                break;
            case HeroDeployState.Unavailable:
                ShowUnavailableOverlay();
                break;
            case HeroDeployState.Countdown:
                ShowCountdown(combatState.RedeployCountdownTime, combatState.RedeployTime);
                break;
            case HeroDeployState.Deployed:
                HideAllOverlays();
                break;
            default:
                HideAllOverlays();
                break;
        }
    }
}
