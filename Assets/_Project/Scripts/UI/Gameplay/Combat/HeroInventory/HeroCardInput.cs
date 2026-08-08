using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class HeroCardInput : MonoBehaviour
{
    private HeroInstance heroInstance;
    private GameInput gameInput;
    private PointerEventData pointerEventData;

    private bool isPrimaryPressed;
    private bool isHovering;
    private bool isInputEnabled = true;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    
    public event Action<HeroCardInput, Vector2> OnHoverEntered;
    public event Action<HeroCardInput, Vector2> OnHoverExited;
    public event Action<HeroCardInput, Vector2> OnPrimaryPerformed;
    public event Action<HeroCardInput, Vector2> OnPrimaryCanceled;

    public HeroInstance HeroInstance => heroInstance;

    private bool IsInputEnabled => isInputEnabled && gameInput != null;

    public void Initialize(HeroInstance instance)
    {
        heroInstance = instance;
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
            OnHoverEntered?.Invoke(this, screenPosition);
        }
        else
        {
            OnHoverExited?.Invoke(this, screenPosition);
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
            OnPrimaryPerformed?.Invoke(this, screenPosition);
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
        OnPrimaryCanceled?.Invoke(this, screenPosition);
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

        for (int i = 0; i < raycastResults.Count; i++)
        {
            HeroCardInput cardInput = raycastResults[i].gameObject.GetComponentInParent<HeroCardInput>();
            if (cardInput != null)
            {
                return cardInput == this;
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
