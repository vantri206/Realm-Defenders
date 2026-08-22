using UnityEngine;

public class HeroActionHUD : MonoBehaviour
{
    [SerializeField] private WorldActionButton[] actionButtons;

    private PlayerCombatAction playerCombatAction;

    private bool isInitialized;
    private bool isRegisteredButtonEvents;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        RegisterButtonEvents();
    }

    private void OnDisable()
    {
        UnregisterButtonEvents();
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
    }

    public void Initialize(PlayerCombatAction playerCombatAction)
    {
        if (playerCombatAction == null)
        {
            Debug.LogError("[HeroActionHUD] PlayerCombatAction is required to initialize the action HUD.", this);
            return;
        }

        if (isInitialized && this.playerCombatAction == playerCombatAction)
        {
            return;
        }

        UnregisterButtonEvents();
        CacheReferences();

        this.playerCombatAction = playerCombatAction;
        isInitialized = true;

        if (isActiveAndEnabled)
        {
            RegisterButtonEvents();
        }
    }

    public void Show(PlayerCombatAction playerCombatAction)
    {
        Initialize(playerCombatAction);
        
        if (!isInitialized)
        {
            return;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void HandleActionClicked(HeroActionType actionType)
    {
        if (!isInitialized || playerCombatAction == null)
        {
            return;
        }

        playerCombatAction.HandleCurrentHeroAction(actionType);
    }

    private void RegisterButtonEvents()
    {
        if (isRegisteredButtonEvents || !isInitialized || actionButtons == null)
        {
            return;
        }

        for (int i = 0; i < actionButtons.Length; i++)
        {
            WorldActionButton actionButton = actionButtons[i];
            if (actionButton == null)
            {
                continue;
            }

            actionButton.OnClicked -= HandleActionClicked;
            actionButton.OnClicked += HandleActionClicked;
        }

        isRegisteredButtonEvents = true;
    }

    private void UnregisterButtonEvents()
    {
        if (!isRegisteredButtonEvents || actionButtons == null)
        {
            return;
        }

        for (int i = 0; i < actionButtons.Length; i++)
        {
            WorldActionButton actionButton = actionButtons[i];
            if (actionButton != null)
            {
                actionButton.OnClicked -= HandleActionClicked;
            }
        }

        isRegisteredButtonEvents = false;
    }

    private void CacheReferences()
    {
        if (actionButtons == null || actionButtons.Length == 0)
        {
            actionButtons = GetComponentsInChildren<WorldActionButton>(true);
        }
    }
}
