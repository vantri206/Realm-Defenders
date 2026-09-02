using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombatStageHUDController : MonoBehaviour
{
    private const float pauseSpeedMultiplier = 0f;
    private const float normalSpeedMultiplier = 1f;
    private const float fastSpeedMultiplier = 1.5f;
    private const string defaultActionText = "ACTION";
    private const string deployActionText = "DEPLOY";
    private const string retreatActionText = "RECALL";
    private const string victoryText = "VICTORY!";
    private const string defeatedText = "DEFEATED!";
    private const string clearedText = "CLEARED";
    private const string failedText = "FAILED";
    private const string headquartersSceneName = "GuildHeadquartersScene";

    private static readonly Color victoryColor = new Color(0.142f, 1f, 0f, 1f);
    private static readonly Color defeatedColor = Color.red;

    [Header("UI Elements")]
    [SerializeField] private Button retreatButton;
    [SerializeField] private Button speedControlButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button actionButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text actionButtonText;

    [Header("Stage Result")]
    [SerializeField] private GameObject stageResultPanel;
    [SerializeField] private TMP_Text stageResultText;
    [SerializeField] private Button backButton;

    [Header("Speed Control")]
    [SerializeField] private Image normalSpeedIcon;
    [SerializeField] private Image fastSpeedIcon;

    private StageSystem stageSystem;
    private PlayerCombatAction playerCombatAction;
    private CombatUIController combatUIController;
    private CombatTimeController combatTime;
    private GameInput gameInput;
    private string stageId;
    private bool isFastSpeed;
    private bool isStageEnded;
    private float previousPauseSpeed = normalSpeedMultiplier;

    private bool isInitialized;

    public void Initialize(string stageId, StageSystem stageSystem, PlayerCombatAction playerCombatAction, CombatUIController combatUIController,
                           CombatTimeController combatTime)
    {
        if (stageSystem == null || playerCombatAction == null || combatUIController == null || combatTime == null || speedControlButton == null ||
            stageResultPanel == null || stageResultText == null)
        {
            Debug.LogError("[CombatStageHUDController] Combat systems, speed control, and stage result UI references are required to initialize combat stage HUD.", this);
            return;
        }

        UnregisterButtonEvents();
        UnregisterRuntimeEvents();

        this.stageSystem = stageSystem;
        this.playerCombatAction = playerCombatAction;
        this.combatUIController = combatUIController;
        this.combatTime = combatTime;
        this.stageId = stageId;
        gameInput = GameInput.Instance;
        if (actionButtonText == null && actionButton != null)
        {
            actionButtonText = actionButton.GetComponentInChildren<TMP_Text>(true);
        }
        stageResultPanel.SetActive(false);

        isFastSpeed = false;
        isStageEnded = false;
        ApplySpeedState();
        previousPauseSpeed = combatTime.CombatSpeedMultiplier;

        isInitialized = true;
        RegisterButtonEvents();
        RegisterRuntimeEvents();
        UpdateActionButtonText(playerCombatAction.CurrentMode);
    }

    private void OnEnable()
    {
        if (isInitialized)
        {
            RegisterButtonEvents();
            RegisterRuntimeEvents();
            UpdateActionButtonText(playerCombatAction.CurrentMode);
        }
    }

    private void OnDisable()
    {
        UnregisterButtonEvents();
        UnregisterRuntimeEvents();
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
        UnregisterRuntimeEvents();
    }

    private void RegisterButtonEvents()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(HandlePauseRequested);
            pauseButton.onClick.AddListener(HandlePauseRequested);
        }
        if (speedControlButton != null)
        {
            speedControlButton.onClick.RemoveListener(HandleSpeedControlButtonClicked);
            speedControlButton.onClick.AddListener(HandleSpeedControlButtonClicked);
        }
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleActionButtonClicked);
            actionButton.onClick.AddListener(HandleActionButtonClicked);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(HandleCancelButtonClicked);
            cancelButton.onClick.AddListener(HandleCancelButtonClicked);
        }
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackButtonClicked);
            backButton.onClick.AddListener(HandleBackButtonClicked);
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
            pauseButton.onClick.RemoveListener(HandlePauseRequested);
        }
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleActionButtonClicked);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(HandleCancelButtonClicked);
        }
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackButtonClicked);
        }
    }

    private void RegisterRuntimeEvents()
    {
        if (playerCombatAction != null)
        {
            playerCombatAction.OnModeChanged -= HandleCombatActionModeChanged;
            playerCombatAction.OnModeChanged += HandleCombatActionModeChanged;
        }

        if (gameInput != null)
        {
            gameInput.OnPausePerformed -= HandlePauseRequested;
            gameInput.OnPausePerformed += HandlePauseRequested;
        }

        if (stageSystem != null)
        {
            stageSystem.OnStageEnded -= HandleStageEnded;
            stageSystem.OnStageEnded += HandleStageEnded;
        }
    }

    private void UnregisterRuntimeEvents()
    {
        if (playerCombatAction != null)
        {
            playerCombatAction.OnModeChanged -= HandleCombatActionModeChanged;
        }

        if (gameInput != null)
        {
            gameInput.OnPausePerformed -= HandlePauseRequested;
        }

        if (stageSystem != null)
        {
            stageSystem.OnStageEnded -= HandleStageEnded;
        }
    }

    private void HandleSpeedControlButtonClicked()
    {
        if (!isInitialized || isStageEnded)
        {
            return;
        }

        isFastSpeed = !isFastSpeed;
        ApplySpeedState();
    }

    private void HandlePauseRequested()
    {
        if (!isInitialized || isStageEnded)
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

    private void HandleActionButtonClicked()
    {
        if (!isInitialized)
        {
            return;
        }

        playerCombatAction.PerformAction();
    }

    private void HandleCancelButtonClicked()
    {
        if (!isInitialized)
        {
            return;
        }

        combatUIController.CancelCurrentAction();
    }

    private void HandleBackButtonClicked()
    {
        if (!isInitialized || !isStageEnded)
        {
            return;
        }

        GameAudioManager audioManager = GameAudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.PlayUIButtonClick();
        }

        combatTime.SetSpeedMultiplier(normalSpeedMultiplier);
        SceneManager.LoadScene(headquartersSceneName);
    }

    private void HandleCombatActionModeChanged(PlayerCombatActionMode mode)
    {
        UpdateActionButtonText(mode);
    }

    private void HandleStageEnded(CombatStageResult result)
    {
        isStageEnded = true;

        string displayStageId = string.Empty;
        if (!string.IsNullOrWhiteSpace(stageId))
        {
            displayStageId = stageId.Trim().ToUpperInvariant();
        }

        if (result == CombatStageResult.Win)
        {
            stageResultText.SetText($"{victoryText}\n{displayStageId} {clearedText}");
            stageResultText.color = victoryColor;

            GameAudioManager audioManager = GameAudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.PlayVictory();
            }
        }
        else
        {
            stageResultText.SetText($"{defeatedText}\n{displayStageId} {failedText}");
            stageResultText.color = defeatedColor;

            GameAudioManager audioManager = GameAudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.PlayDefeat();
            }
        }

        stageResultPanel.SetActive(true);
    }

    private void UpdateActionButtonText(PlayerCombatActionMode mode)
    {
        if (actionButtonText == null)
        {
            return;
        }

        switch (mode)
        {
            case PlayerCombatActionMode.DeployingHero:
            case PlayerCombatActionMode.SelectingDeployDirection:
                actionButtonText.SetText(deployActionText);
                break;
            case PlayerCombatActionMode.SelectedDeployedHero:
                actionButtonText.SetText(retreatActionText);
                break;
            default:
                actionButtonText.SetText(defaultActionText);
                break;
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
