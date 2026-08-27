using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIOverlayRoot : SingletonMB<UIOverlayRoot>
{
    [Header("Overlay Views")]
    [SerializeField] private Tooltip tooltip;
    [SerializeField] private MessageBox messageBox;
    [SerializeField] private QuickNotification quickNotification;

    [Header("Input Blocker")]
    [SerializeField] private GameObject inputBlocker;

    [Header("Tooltip Interaction")]
    [SerializeField, Min(0f)] private float tooltipHideDelay = 0.1f;

    private GameInput gameInput;
    private GameInput blockedGameInput;

    private EventSystem tooltipEventSystem;
    private PointerEventData tooltipRaycastData;
    private readonly List<RaycastResult> tooltipRaycastResults = new List<RaycastResult>();
    private TooltipTrigger activeTooltipTrigger;
    private string activeTooltipText;

    private float tooltipHideTimer = -1f;

    private bool isGameInputEnabled;
    private bool isInputBlocked;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[UIOverlayRoot] Only one active UIOverlayRoot is allowed per scene.", this);
            enabled = false;
            return;
        }

        CacheReferences();

        gameInput = GameInput.Instance;

        RegisterMessageBoxEvent();
        ResetOverlayState();
    }

    private void Update()
    {
        UpdateTooltipHover();
    }

    private void OnDisable()
    {
        UnregisterMessageBoxEvent();

        HideActiveTooltip();

        if (messageBox != null)
        {
            messageBox.HideImmediate();
        }

        if (quickNotification != null)
        {
            quickNotification.HideImmediate();
        }

        tooltipEventSystem = null;
        tooltipRaycastData = null;
        tooltipRaycastResults.Clear();

        gameInput = null;
        SetInputBlocked(false);
    }

    private void OnDestroy()
    {
        UnregisterMessageBoxEvent();
    }

    public void ShowTooltip(string text, Vector2 screenPosition, TooltipTrigger trigger)
    {
        if (!isActiveAndEnabled || isInputBlocked || tooltip == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(text) || trigger == null)
        {
            HideActiveTooltip();
            return;
        }

        CancelTooltipHide();
        tooltip.Show(text, screenPosition);
        activeTooltipTrigger = trigger;
        activeTooltipText = text;
    }

    public void HideTooltip(TooltipTrigger trigger)
    {
        if (activeTooltipTrigger == trigger)
        {
            HideActiveTooltip();
        }
    }

    public bool TryShowMessageBox(string title, Sprite icon, string text)
    {
        if (!isActiveAndEnabled || messageBox == null || messageBox.IsOpening || isInputBlocked)
        {
            return false;
        }

        if (inputBlocker == null)
        {
            Debug.LogError("[UIOverlayRoot] InputBlocker is required before showing a MessageBox.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("[UIOverlayRoot] MessageBox text is required.", this);
            return false;
        }

        HideActiveTooltip();

        SetInputBlocked(true);

        if (messageBox.Show(title, icon, text))
        {
            return true;
        }

        SetInputBlocked(false);
        return false;
    }

    public bool TryShowQuickNotification(string text)
    {
        if (!isActiveAndEnabled || quickNotification == null || string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return quickNotification.Show(text);
    }

    private void HandleMessageBoxClosed()
    {
        SetInputBlocked(false);
    }

    private void ResetOverlayState()
    {
        HideActiveTooltip();

        if (messageBox != null)
        {
            messageBox.HideImmediate();
        }

        if (quickNotification != null)
        {
            quickNotification.HideImmediate();
        }

        SetInputBlocked(false);
    }

    private void SetInputBlocked(bool shouldBlock)
    {
        SetInputBlockerVisible(shouldBlock);

        if (isInputBlocked == shouldBlock)
        {
            return;
        }

        isInputBlocked = shouldBlock;

        if (shouldBlock)
        {
            blockedGameInput = GameInput.Instance;
            if (blockedGameInput == null)
            {
                return;
            }

            isGameInputEnabled = blockedGameInput.GameplayActions.enabled || blockedGameInput.SystemActions.enabled;

            blockedGameInput.GameplayActions.Disable();
            blockedGameInput.SystemActions.Disable();

            return;
        }

        if (blockedGameInput != null)
        {
            if (isGameInputEnabled)
            {
                blockedGameInput.GameplayActions.Enable();
                blockedGameInput.SystemActions.Enable();
            }
        }

        blockedGameInput = null;
        isGameInputEnabled = false;
    }

    private void UpdateTooltipHover()
    {
        if (isInputBlocked || tooltip == null)
        {
            return;
        }

        if (gameInput == null)
        {
            gameInput = GameInput.Instance;
        }

        if (gameInput == null)
        {
            return;
        }

        Vector2 screenPosition = gameInput.MouseScreenPosition;

        if (activeTooltipText != null && activeTooltipTrigger == null)
        {
            HideActiveTooltip();
            return;
        }

        if (activeTooltipTrigger != null && !activeTooltipTrigger.isActiveAndEnabled)
        {
            HideActiveTooltip();
            return;
        }

        if (activeTooltipText != null && tooltip.ContainsScreenPosition(screenPosition))
        {
            CancelTooltipHide();
            return;
        }

        TooltipTrigger hoveredTrigger = FindHoveredTooltipTrigger(screenPosition);
        if (hoveredTrigger == null)
        {
            if (activeTooltipText != null)
            {
                ScheduleOrHideTooltip();
            }

            return;
        }

        CancelTooltipHide();

        if (hoveredTrigger != activeTooltipTrigger || activeTooltipText != hoveredTrigger.TooltipText)
        {
            tooltip.Show(hoveredTrigger.TooltipText, screenPosition);
            activeTooltipTrigger = hoveredTrigger;
            activeTooltipText = hoveredTrigger.TooltipText;

            return;
        }

        tooltip.UpdatePosition(screenPosition);
    }

    private TooltipTrigger FindHoveredTooltipTrigger(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return null;
        }

        if (tooltipRaycastData == null || tooltipEventSystem != eventSystem)
        {
            tooltipEventSystem = eventSystem;
            tooltipRaycastData = new PointerEventData(eventSystem);
        }

        tooltipRaycastData.position = screenPosition;
        tooltipRaycastResults.Clear();
        eventSystem.RaycastAll(tooltipRaycastData, tooltipRaycastResults);

        for (int i = 0; i < tooltipRaycastResults.Count; i++)
        {
            TooltipTrigger trigger = tooltipRaycastResults[i].gameObject.GetComponentInParent<TooltipTrigger>();
            if (trigger != null && trigger.isActiveAndEnabled && trigger.HasText)
            {
                return trigger;
            }
        }

        return null;
    }

    private void HideActiveTooltip()
    {
        activeTooltipTrigger = null;
        activeTooltipText = null;
        CancelTooltipHide();
        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }

    private void ScheduleOrHideTooltip()
    {
        if (tooltipHideDelay <= 0f)
        {
            HideActiveTooltip();
            return;
        }

        if (tooltipHideTimer < 0f)
        {
            tooltipHideTimer = Time.unscaledTime + tooltipHideDelay;
            return;
        }

        if (Time.unscaledTime >= tooltipHideTimer)
        {
            HideActiveTooltip();
        }
    }

    private void CancelTooltipHide()
    {
        tooltipHideTimer = -1f;
    }

    private void SetInputBlockerVisible(bool isVisible)
    {
        if (inputBlocker == null)
        {
            return;
        }

        if (inputBlocker == gameObject)
        {
            Debug.LogError("[UIOverlayRoot] InputBlocker must be a child object, not the UIOverlayRoot object.", this);
            return;
        }

        inputBlocker.SetActive(isVisible);
    }

    private void RegisterMessageBoxEvent()
    {
        if (messageBox == null)
        {
            return;
        }

        messageBox.OnClosed -= HandleMessageBoxClosed;
        messageBox.OnClosed += HandleMessageBoxClosed;
    }

    private void UnregisterMessageBoxEvent()
    {
        if (messageBox != null)
        {
            messageBox.OnClosed -= HandleMessageBoxClosed;
        }
    }

    private void CacheReferences()
    {
        if (tooltip == null)
        {
            tooltip = GetComponentInChildren<Tooltip>(true);
        }

        if (messageBox == null)
        {
            messageBox = GetComponentInChildren<MessageBox>(true);
        }

        if (quickNotification == null)
        {
            quickNotification = GetComponentInChildren<QuickNotification>(true);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        tooltipHideDelay = Mathf.Max(0f, tooltipHideDelay);
        CacheReferences();
    }
#endif
}
