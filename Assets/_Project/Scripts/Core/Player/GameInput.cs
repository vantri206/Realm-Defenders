using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class GameInput : SingletonMB<GameInput>
{
    private InputSystem_Actions inputActions;

    public event Action<Vector2> OnPrimaryPerformed;
    public event Action<Vector2> OnPrimaryCanceled;
    public event Action<Vector2> OnSecondaryPerformed;
    public event Action OnSellPerformed;
    public event Action OnPausePerformed;
    public event Action<Vector2Int> OnDirectionPerformed;
    public event Action OnActionPerformed;

    public Vector2 MouseScreenPosition { get; private set; }
    public bool IsPrimaryHold { get; private set; }

    public InputSystem_Actions.GameplayActions GameplayActions => inputActions.Gameplay;
    public InputSystem_Actions.SystemActions SystemActions => inputActions.System;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        RegisterInputEvents();
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            Debug.LogError("[GameInput] Input actions are required before enabling input.", this);
            return;
        }

        inputActions.Gameplay.Enable();
        inputActions.System.Enable();
        UpdateMousePosition();
    }

    private void OnDisable()
    {
        if (inputActions == null)
        {
            return;
        }

        IsPrimaryHold = false;
        inputActions.Gameplay.Disable();
        inputActions.System.Disable();
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            UnregisterInputEvents();
            inputActions.Dispose();
            inputActions = null;
        }
    }

    private void HandlePointerPosition(InputAction.CallbackContext context)
    {
        MouseScreenPosition = context.ReadValue<Vector2>();
    }

    private void HandlePrimaryPerformed(InputAction.CallbackContext context)
    {
        IsPrimaryHold = true;
        UpdateMousePosition();
        OnPrimaryPerformed?.Invoke(MouseScreenPosition);
    }

    private void HandlePrimaryCanceled(InputAction.CallbackContext context)
    {
        IsPrimaryHold = false;
        UpdateMousePosition();
        OnPrimaryCanceled?.Invoke(MouseScreenPosition);
    }

    private void HandleSecondaryPerformed(InputAction.CallbackContext context)
    {
        UpdateMousePosition();
        OnSecondaryPerformed?.Invoke(MouseScreenPosition);
    }

    private void HandleSellPerformed(InputAction.CallbackContext context)
    {
        OnSellPerformed?.Invoke();
    }

    private void HandlePausePerformed(InputAction.CallbackContext context)
    {
        OnPausePerformed?.Invoke();
    }

    private void HandleActionPerformed(InputAction.CallbackContext context)
    {
        OnActionPerformed?.Invoke();
    }

    private void HandleDirectionPerformed(InputAction.CallbackContext context)
    {
        if (context.control is ButtonControl buttonControl && !buttonControl.isPressed)
        {
            return;
        }

        Vector2Int direction = GetDirection(context.control, context.ReadValue<Vector2>());

        if (direction == Vector2Int.zero)
        {
            return;
        }

        RaiseDirectionPerformed(direction);
    }

    public void RaiseDirectionPerformed(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        OnDirectionPerformed?.Invoke(direction);
    }

    private void RegisterInputEvents()
    {
        inputActions.Gameplay.PointerPosition.performed += HandlePointerPosition;
        inputActions.Gameplay.PrimaryPress.performed += HandlePrimaryPerformed;
        inputActions.Gameplay.PrimaryPress.canceled += HandlePrimaryCanceled;
        inputActions.Gameplay.SecondaryPress.performed += HandleSecondaryPerformed;
        inputActions.Gameplay.Sell.performed += HandleSellPerformed;
        inputActions.System.Pause.performed += HandlePausePerformed;
        inputActions.Gameplay.Direction.performed += HandleDirectionPerformed;
        inputActions.Gameplay.Action.performed += HandleActionPerformed;
    }

    private void UnregisterInputEvents()
    {
        inputActions.Gameplay.PointerPosition.performed -= HandlePointerPosition;
        inputActions.Gameplay.PrimaryPress.performed -= HandlePrimaryPerformed;
        inputActions.Gameplay.PrimaryPress.canceled -= HandlePrimaryCanceled;
        inputActions.Gameplay.SecondaryPress.performed -= HandleSecondaryPerformed;
        inputActions.Gameplay.Sell.performed -= HandleSellPerformed;
        inputActions.System.Pause.performed -= HandlePausePerformed;
        inputActions.Gameplay.Direction.performed -= HandleDirectionPerformed;
        inputActions.Gameplay.Action.performed -= HandleActionPerformed;
    }

    private void UpdateMousePosition()
    {
        MouseScreenPosition = inputActions.Gameplay.PointerPosition.ReadValue<Vector2>();
    }

    private Vector2Int GetDirection(InputControl control, Vector2 fallbackValue)
    {
        Vector2Int direction = Vector2Int.zero;

        if (control == null)
        {
            return GetFallbackDirection(fallbackValue);
        }

        switch (control.name)
        {
            case "w":
            case "upArrow":
            case "up":
                direction = Vector2Int.up;
                break;
            case "s":
            case "downArrow":
            case "down":
                direction = Vector2Int.down;
                break;
            case "a":
            case "leftArrow":
            case "left":
                direction = Vector2Int.left;
                break;
            case "d":
            case "rightArrow":
            case "right":
                direction = Vector2Int.right;
                break;
            default:
                return GetFallbackDirection(fallbackValue);
        }

        return direction;
    }

    private Vector2Int GetFallbackDirection(Vector2 value)
    {
        if (value == Vector2.zero)
        {
            return Vector2Int.zero;
        }

        if (Mathf.Abs(value.x) > Mathf.Abs(value.y))
        {
            return value.x > 0 ? Vector2Int.right : Vector2Int.left;
        }

        if (Mathf.Abs(value.y) > Mathf.Abs(value.x))
        {
            return value.y > 0 ? Vector2Int.up : Vector2Int.down;
        }

        return Vector2Int.zero;
    }
}
