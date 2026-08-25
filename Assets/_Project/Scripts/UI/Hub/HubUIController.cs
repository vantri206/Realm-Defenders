using UnityEngine;

public class HubUIController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerSession playerSession;

    [Header("Navigation")]
    [SerializeField] private HubNavigationButton[] navigationButtons;

    [Header("Screens")]
    [SerializeField] private GameObject screenPanel;
    [SerializeField] private HeroRosterView rosterView;

    private HubNavigationButton activeNavigationButton;
    private HubScreenId activeScreenId = HubScreenId.None;
    private bool isEventsRegistered = false;

    private void Awake()
    {
        CacheReferences();

        if (rosterView != null)
        {
            rosterView.Hide();
        }

        activeScreenId = HubScreenId.None;
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
        if (rosterView != null)
        {
            rosterView.Hide();
        }

        activeScreenId = HubScreenId.None;
        SetActiveNavigationButton(null);
        UpdateScreenPanelVisibility();
    }

    private void HandleNavigationRequested(HubNavigationButton navigationButton)
    {
        if (navigationButton == null)
        {
            return;
        }

        OpenScreen(navigationButton.TargetScreenId, navigationButton);
    }

    private void OpenScreen(HubScreenId screenId, HubNavigationButton sourceButton)
    {
        switch (screenId)
        {
            case HubScreenId.RosterHero:
                if (rosterView == null)
                {
                    Debug.LogError("[HubUIController] RosterView reference is required to open the Roster screen.", this);
                    return;
                }

                activeScreenId = HubScreenId.RosterHero;
                UpdateScreenPanelVisibility();
                rosterView.Show(playerSession != null ? playerSession.HeroRoster : null);
                SetActiveNavigationButton(sourceButton);
                break;

            case HubScreenId.None:
            default:
                CloseActiveScreen();
                break;
        }
    }

    private void UpdateScreenPanelVisibility()
    {
        if (screenPanel != null)
        {
            screenPanel.SetActive(activeScreenId != HubScreenId.None);
        }
    }

    private void SetActiveNavigationButton(HubNavigationButton activeButton)
    {
        activeNavigationButton = activeButton;

        if (navigationButtons == null)
        {
            return;
        }

        for (int i = 0; i < navigationButtons.Length; i++)
        {
            HubNavigationButton navigationButton = navigationButtons[i];
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
                HubNavigationButton navigationButton = navigationButtons[i];
                if (navigationButton == null)
                {
                    continue;
                }

                navigationButton.OnNavigationRequested -= HandleNavigationRequested;
                navigationButton.OnNavigationRequested += HandleNavigationRequested;
            }
        }

        if (rosterView != null)
        {
            rosterView.OnCloseRequested -= CloseActiveScreen;
            rosterView.OnCloseRequested += CloseActiveScreen;
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

        if (rosterView != null)
        {
            rosterView.OnCloseRequested -= CloseActiveScreen;
        }

        isEventsRegistered = false;
    }

    private void CacheReferences()
    {
        if (navigationButtons == null || navigationButtons.Length == 0)
        {
            navigationButtons = GetComponentsInChildren<HubNavigationButton>(true);
        }

        if (rosterView == null)
        {
            rosterView = GetComponentInChildren<HeroRosterView>(true);
        }
    }
}
