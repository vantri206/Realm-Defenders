using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public class HeroCardView : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Canvas sortingCanvas;
    [SerializeField] private float selectedScale = 1.25f;

    [Header("Info")]
    [SerializeField] private Image heroIcon;
    [SerializeField] private Image classIcon;

    [Header("Cost")]
    [SerializeField] private UIValueTextBinding costText = new UIValueTextBinding();

    [Header("State Overlay")]
    [SerializeField] private Image unavailableOverlay;
    [SerializeField] private Image countdownOverlay;
    [SerializeField] private Image countdownIcon;
    [SerializeField] private UIValueTextBinding countdownText = new UIValueTextBinding();

    private HeroInstance heroInstance;
    private Tween selectedTween;

    private float selectedTweenDuration = 0.2f;
    private int selectedSortingOrder = 100;

    private void Awake()
    {
        Clear();
    }

    private void OnDestroy()
    {
        selectedTween?.Kill();
    }

    public void SetData(HeroInstance instance)
    {
        heroInstance = instance;
        HideAllOverlays();
        Refresh();
    }

    public void Clear()
    {
        heroInstance = null;
        SetSelected(false);
        SetImage(heroIcon, null);
        SetImage(classIcon, null);
        costText.Refresh();
        HideAllOverlays();
    }

    public void Refresh()
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            Clear();
            return;
        }

        LoadData(heroInstance);
        RefreshCountdown();
    }

    public void LoadData(HeroInstance instance)
    {
        if (instance == null || !instance.IsValid)
        {
            Clear();
            return;
        }

        HeroDefinition definition = instance.Definition;
        SetImage(heroIcon, definition.HeroIcon);
        SetImage(classIcon, definition.HeroClass.Icon);
        costText.SetInt(instance.DeployCost);
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
            return;
        }

        selectedTween?.Kill();
        selectedTween = visualRoot.DOScale(isSelected ? selectedScale : 1f, selectedTweenDuration).SetEase(Ease.OutQuad);
    }

    public void HideAllOverlays()
    {
        SetUnavailableOverlay(false);
        SetCooldownOverlay(false);
        countdownText.SetVisible(false);
        countdownIcon.gameObject.SetActive(false);
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

    public void SetCooldownOverlay(bool isVisible)
    {
        if (countdownOverlay != null)
        {
            countdownOverlay.gameObject.SetActive(isVisible);
        }

        countdownText.SetVisible(isVisible);
    }

    public void ShowCountdown(float remainingTime, float totalTime)
    {
        remainingTime = Mathf.Max(0f, remainingTime);
        totalTime = Mathf.Max(0f, totalTime);

        bool hasCountdown = remainingTime > 0f && totalTime > 0f;
        SetCooldownOverlay(hasCountdown);

        if (!hasCountdown)
        {
            ClearCountdown();
            return;
        }

        if (countdownOverlay != null)
        {
            countdownOverlay.fillAmount = Mathf.Clamp01(remainingTime / totalTime);
        }

        countdownIcon.gameObject.SetActive(true);

        countdownText.SetNumber(remainingTime);
    }

    public void ClearCountdown()
    {
        if (countdownOverlay != null)
        {
            countdownOverlay.fillAmount = 0f;
        }

        countdownText.Refresh();

        countdownIcon.gameObject.SetActive(false);

        SetCooldownOverlay(false);
    }

    public void RefreshCountdown()
    {
        if (heroInstance == null || heroInstance.IsReadyDeploy)
        {
            ClearCountdown();
            return;
        }

        ShowCountdown(heroInstance.RedeployCountdownTime, heroInstance.RedeployTime);
    }

    public void Tick(float deltaTime)
    {
        if (heroInstance == null)
        {
            return;
        }

        heroInstance.TickRedeployTimer(deltaTime);
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

}
