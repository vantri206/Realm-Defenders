using UnityEngine;

public class HeadquartersUIController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private RunSession runSession;

    [Header("Navigation")]
    [SerializeField] private NavigationButton[] navigationButtons;

    [Header("Screens")]
    [SerializeField] private GameObject screenPanel;
    [SerializeField] private TeamView teamView;

    private NavigationButton activeNavigationButton;
    private HeadquartersScreenId activeScreenId = HeadquartersScreenId.None;
    private bool isEventsRegistered = false;

    private void Awake()
    {
        CacheReferences();

        if (teamView != null)
        {
            teamView.Hide();
        }

        activeScreenId = HeadquartersScreenId.None;
        UpdateScreenPanelVisibility();
    }

    private void Start()
    {
        SetActiveNavigationButton(null);
    }

    private void OnEnable()
    {
        CacheReferences();

        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    private void OnDestroy()
    {
        UnregisterEvents();
    }

    public void CloseActiveScreen()
    {
        if (teamView != null)
        {
            teamView.Hide();
        }

        activeScreenId = HeadquartersScreenId.None;
        SetActiveNavigationButton(null);
        UpdateScreenPanelVisibility();
    }

    private void HandleNavigationRequested(NavigationButton navigationButton)
    {
        if (navigationButton == null)
        {
            return;
        }

        OpenScreen(navigationButton.TargetScreenId, navigationButton);
    }

    private void OpenScreen(HeadquartersScreenId screenId, NavigationButton sourceButton)
    {
        switch (screenId)
        {
            case HeadquartersScreenId.Team:
                if (teamView == null)
                {
                    Debug.LogError("[HeadquartersUIController] TeamView reference is required to open the Team screen.", this);
                    return;
                }

                activeScreenId = HeadquartersScreenId.Team;
                UpdateScreenPanelVisibility();
                teamView.Show(runSession != null ? runSession.HeroRoster : null);
                SetActiveNavigationButton(sourceButton);
                break;

            case HeadquartersScreenId.None:
            default:
                CloseActiveScreen();
                break;
        }
    }

    private void UpdateScreenPanelVisibility()
    {
        if (screenPanel != null)
        {
            screenPanel.SetActive(activeScreenId != HeadquartersScreenId.None);
        }
    }

    private void SetActiveNavigationButton(NavigationButton activeButton)
    {
        activeNavigationButton = activeButton;

        if (navigationButtons == null)
        {
            return;
        }

        for (int i = 0; i < navigationButtons.Length; i++)
        {
            NavigationButton navigationButton = navigationButtons[i];
            if (navigationButton != null)
            {
                navigationButton.SetActive(navigationButton == activeNavigationButton);
            }
        }
    }

    private void RegisterEvents()
    {
        if (navigationButtons != null)
        {
            for (int i = 0; i < navigationButtons.Length; i++)
            {
                NavigationButton navigationButton = navigationButtons[i];
                if (navigationButton == null)
                {
                    continue;
                }

                navigationButton.OnNavigationRequested -= HandleNavigationRequested;
                navigationButton.OnNavigationRequested += HandleNavigationRequested;
            }
        }

        if (teamView != null)
        {
            teamView.OnCloseRequested -= CloseActiveScreen;
            teamView.OnCloseRequested += CloseActiveScreen;
        }

        isEventsRegistered = true;
    }

    private void UnregisterEvents()
    {
        if (!isEventsRegistered)
        {
            return;
        }

        if (navigationButtons != null)
        {
            for (int i = 0; i < navigationButtons.Length; i++)
            {
                if (navigationButtons[i] != null)
                {
                    navigationButtons[i].OnNavigationRequested -= HandleNavigationRequested;
                }
            }
        }

        if (teamView != null)
        {
            teamView.OnCloseRequested -= CloseActiveScreen;
        }

        isEventsRegistered = false;
    }

    private void CacheReferences()
    {
        if (navigationButtons == null || navigationButtons.Length == 0)
        {
            navigationButtons = GetComponentsInChildren<NavigationButton>(true);
        }

        if (teamView == null)
        {
            teamView = GetComponentInChildren<TeamView>(true);
        }
    }
}
