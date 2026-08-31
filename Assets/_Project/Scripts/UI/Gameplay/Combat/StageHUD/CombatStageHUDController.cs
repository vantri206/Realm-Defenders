using UnityEngine;
using UnityEngine.UI;

public class CombatStageHUDController : MonoBehaviour
{
    private const float pauseSpeedMultiplier = 0f;
    private const float normalSpeedMultiplier = 1f;
    private const float fastSpeedMultiplier = 1.5f;

    [Header("UI Elements")]
    [SerializeField] private Button retreatButton;
    [SerializeField] private Button speedControlButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button actionButton;
    [SerializeField] private Button cancelButton;

    [Header("Speed Control")]
    [SerializeField] private Image normalSpeedIcon;
    [SerializeField] private Image fastSpeedIcon;

    private StageSystem stageSystem;
    private PlayerCombatAction playerCombatAction;
    private CombatTimeController combatTime;
    private bool isFastSpeed;
    private float previousPauseSpeed = normalSpeedMultiplier;

    private bool isInitialized;

    public void Initialize(StageSystem stageSystem, PlayerCombatAction playerCombatAction, CombatTimeController combatTime)
    {
        if (stageSystem == null || playerCombatAction == null || combatTime == null || speedControlButton == null)
        {
            Debug.LogError("[CombatStageHUDController] StageSystem, PlayerCombatAction, CombatTimeController, and speedControlButton are required to initialize combat stage HUD.", this);
            return;
        }

        UnregisterButtonEvents();

        this.stageSystem = stageSystem;
        this.playerCombatAction = playerCombatAction;
        this.combatTime = combatTime;

        isFastSpeed = false;
        ApplySpeedState();
        previousPauseSpeed = combatTime.CombatSpeedMultiplier;

        isInitialized = true;
        RegisterButtonEvents();
    }

    private void OnEnable()
    {
        if (isInitialized)
        {
            RegisterButtonEvents();
        }
    }

    private void OnDisable()
    {
        UnregisterButtonEvents();
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
    }

    private void RegisterButtonEvents()
    {
        if (speedControlButton == null)
        {
            return;
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(HandlePauseButtonClicked);
            pauseButton.onClick.AddListener(HandlePauseButtonClicked);
        }
        if (speedControlButton != null)
        {
            speedControlButton.onClick.RemoveListener(HandleSpeedControlButtonClicked);
            speedControlButton.onClick.AddListener(HandleSpeedControlButtonClicked);
        }
    }

    private void UnregisterButtonEvents()
    {
        if (speedControlButton != null)
        {
            speedControlButton.onClick.RemoveListener(HandleSpeedControlButtonClicked);
        }
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(HandlePauseButtonClicked);
        }
    }

    private void HandleSpeedControlButtonClicked()
    {
        if (!isInitialized)
        {
            return;
        }

        isFastSpeed = !isFastSpeed;
        ApplySpeedState();
    }

    private void HandlePauseButtonClicked()
    {
        if (!isInitialized)
        {
            return;
        }

        if (combatTime.IsCombatPaused)
        {
            combatTime.SetSpeedMultiplier(previousPauseSpeed);
        }
        else
        {
            previousPauseSpeed = combatTime.CombatSpeedMultiplier;
            combatTime.SetSpeedMultiplier(pauseSpeedMultiplier);
        }
    }

    private void ApplySpeedState()
    {
        float speedMultiplier = isFastSpeed ? fastSpeedMultiplier : normalSpeedMultiplier;
        combatTime.SetSpeedMultiplier(speedMultiplier);

        if (normalSpeedIcon != null && fastSpeedIcon != null)
        {
            normalSpeedIcon.enabled = !isFastSpeed;
            fastSpeedIcon.enabled = isFastSpeed;
        }
    }
}
