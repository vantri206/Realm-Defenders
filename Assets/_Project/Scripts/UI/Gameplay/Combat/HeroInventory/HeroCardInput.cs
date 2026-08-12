using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HeroCardInput : MonoBehaviour
{
    private HeroCard heroCard;
    private GameInput gameInput;
    private PointerEventData pointerEventData;

    private bool isPrimaryPressed;
    private bool isHovering;
    private bool isInputEnabled = true;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    
    public event Action<HeroCard> OnHoverEntered;
    public event Action<HeroCard> OnHoverExited;
    public event Action<HeroCard, Vector2> OnPrimaryPerformed;
    public event Action<HeroCard, Vector2> OnPrimaryCanceled;

    public HeroCard HeroCard => heroCard;

    private bool IsInputEnabled => isInputEnabled && gameInput != null;

    public void Initialize(HeroCard card)
    {
        if (card == null)
        {
            Debug.LogError("[HeroCardInput] HeroCard is required to initialize card input.", this);
            return;
        }

        heroCard = card;
    }

    private void Awake()
    {
        gameInput = GameInput.Instance;
    }

    private void OnEnable()
    {
        gameInput = GameInput.Instance;
        RegisterInputEvents();
    }

    private void OnDisable()
    {
        UnregisterInputEvents();
        gameInput = null;
    }

    private void Update()
    {
        if (!IsInputEnabled)
        {
            return;
        }

        Vector2 mousePosition = gameInput.MouseScreenPosition;

        UpdateHover(mousePosition);
    }

    private void RegisterInputEvents()
    {
        if (gameInput == null)
        {
            Debug.LogError("[HeroCardInput] GameInput is required to register hero card input events.", this);
            return;
        }

        gameInput.OnPrimaryPerformed += HandlePrimaryPerformed;
        gameInput.OnPrimaryCanceled += HandlePrimaryCanceled;
    }

    private void UnregisterInputEvents()
    {
        if (gameInput == null)
        {
            return;
        }

        gameInput.OnPrimaryPerformed -= HandlePrimaryPerformed;
        gameInput.OnPrimaryCanceled -= HandlePrimaryCanceled;
    }

    private void UpdateHover(Vector2 screenPosition)
    {
        if (!IsInputEnabled || isPrimaryPressed)
        {
            return;
        }

        bool isTopTarget = IsTopRaycastTarget(screenPosition);
        
        if (isHovering == isTopTarget)
        {
            return;
        }

        isHovering = isTopTarget;

        if (isHovering)
        {
            OnHoverEntered?.Invoke(heroCard);
        }
        else
        {
            OnHoverExited?.Invoke(heroCard);
        }
    }

    private void HandlePrimaryPerformed(Vector2 screenPosition)
    {
        if (!IsInputEnabled)
        {
            return;
        }

        if (IsTopRaycastTarget(screenPosition))
        {
            isPrimaryPressed = true;
            OnPrimaryPerformed?.Invoke(heroCard, screenPosition);
        }
    }

    private void HandlePrimaryCanceled(Vector2 screenPosition)
    {
        if (!IsInputEnabled)
        {
            return;
        }

        if (!isPrimaryPressed)
        {
            return;
        }

        isPrimaryPressed = false;
        OnPrimaryCanceled?.Invoke(heroCard, screenPosition);
    }

    private bool IsTopRaycastTarget(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("[HeroCardInput] EventSystem is required to raycast hero card input.", this);
            return false;
        }

        if (pointerEventData == null)
        {
            pointerEventData = new PointerEventData(eventSystem);
        }

        pointerEventData.position = screenPosition;
        raycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            HeroCard card = raycastResults[i].gameObject.GetComponentInParent<HeroCard>();
            if (card != null)
            {
                return card == heroCard;
            }
        }

        return false;
    }

    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
    }

    public void SetState(HeroDeployState newState)
    {
        if (newState == HeroDeployState.Available)
        {
            SetInputEnabled(true);
        }
        else
        {
            SetInputEnabled(false);
        }
    }
}
