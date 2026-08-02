using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : SingletonMB<GameInput>
{
    private InputSystem_Actions inputActions;

    public event Action<Vector2> OnPrimaryPerformed;
    public event Action<Vector2> OnPrimaryCanceled;
    public event Action<Vector2> OnSecondaryPerformed;
    public event Action OnSellPerformed;
    public event Action OnRelocatePerformed;
    public event Action OnPausePerformed;

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

    private void HandleRelocatePerformed(InputAction.CallbackContext context)
    {
        OnRelocatePerformed?.Invoke();
    }

    private void HandlePausePerformed(InputAction.CallbackContext context)
    {
        OnPausePerformed?.Invoke();
    }

    private void RegisterInputEvents()
    {
        inputActions.Gameplay.PointerPosition.performed += HandlePointerPosition;
        inputActions.Gameplay.PrimaryPress.performed += HandlePrimaryPerformed;
        inputActions.Gameplay.PrimaryPress.canceled += HandlePrimaryCanceled;
        inputActions.Gameplay.SecondaryPress.performed += HandleSecondaryPerformed;
        inputActions.Gameplay.Sell.performed += HandleSellPerformed;
        inputActions.Gameplay.Relocate.performed += HandleRelocatePerformed;
        inputActions.System.Pause.performed += HandlePausePerformed;
    }

    private void UnregisterInputEvents()
    {
        inputActions.Gameplay.PointerPosition.performed -= HandlePointerPosition;
        inputActions.Gameplay.PrimaryPress.performed -= HandlePrimaryPerformed;
        inputActions.Gameplay.PrimaryPress.canceled -= HandlePrimaryCanceled;
        inputActions.Gameplay.SecondaryPress.performed -= HandleSecondaryPerformed;
        inputActions.Gameplay.Sell.performed -= HandleSellPerformed;
        inputActions.Gameplay.Relocate.performed -= HandleRelocatePerformed;
        inputActions.System.Pause.performed -= HandlePausePerformed;
    }

    private void UpdateMousePosition()
    {
        MouseScreenPosition = inputActions.Gameplay.PointerPosition.ReadValue<Vector2>();
    }
}
