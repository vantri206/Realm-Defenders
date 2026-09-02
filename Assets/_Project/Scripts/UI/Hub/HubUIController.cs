using System.Globalization;
using UnityEngine;

public class HubUIController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerSession playerSession;

    [Header("Resources HUD")]
    [SerializeField] private UIValueTextBinding experiencePointsText = new UIValueTextBinding();

    [Header("Navigation")]
    [SerializeField] private HubNavigationButton[] navigationButtons;

    [Header("Screens")]
    [SerializeField] private GameObject screenPanel;
    [SerializeField] private HeroRosterView rosterView;
    [SerializeField] private EnemyDictonaryView enemyDictonaryView;
    [SerializeField] private QuestStageView questStageView;

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

        if (enemyDictonaryView != null)
        {
            enemyDictonaryView.Hide();
        }

        if (questStageView != null)
        {
            questStageView.Hide();
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
        RefreshExperiencePoints();
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

        if (enemyDictonaryView != null)
        {
            enemyDictonaryView.Hide();
        }

        if (questStageView != null)
        {
            questStageView.Hide();
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

                if (enemyDictonaryView != null)
                {
                    enemyDictonaryView.Hide();
                }

                if (questStageView != null)
                {
                    questStageView.Hide();
                }

                activeScreenId = HubScreenId.RosterHero;
                UpdateScreenPanelVisibility();
                rosterView.Show(playerSession != null ? playerSession.HeroRoster : null);
                SetActiveNavigationButton(sourceButton);
                break;

            case HubScreenId.EnemyDictionary:
                if (enemyDictonaryView == null)
                {
                    Debug.LogError("[HubUIController] EnemyDictonaryView reference is required to open the Enemy Dictionary screen.", this);
                    return;
                }

                if (rosterView != null)
                {
                    rosterView.Hide();
                }

                if (questStageView != null)
                {
                    questStageView.Hide();
                }

                activeScreenId = HubScreenId.EnemyDictionary;
                UpdateScreenPanelVisibility();
                enemyDictonaryView.Show();
                SetActiveNavigationButton(sourceButton);
                break;

            case HubScreenId.Quest:
                if (questStageView == null)
                {
                    Debug.LogError("[HubUIController] QuestStageView reference is required to open the Quest screen.", this);
                    return;
                }

                if (rosterView != null)
                {
                    rosterView.Hide();
                }

                if (enemyDictonaryView != null)
                {
                    enemyDictonaryView.Hide();
                }

                activeScreenId = HubScreenId.Quest;
                UpdateScreenPanelVisibility();
                questStageView.Show();
                SetActiveNavigationButton(sourceButton);
                break;

            case HubScreenId.Guide:
                Debug.LogWarning("[HubUIController] Guide screen is not implemented yet.", this);
                CloseActiveScreen();
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

        if (enemyDictonaryView != null)
        {
            enemyDictonaryView.OnCloseRequested -= CloseActiveScreen;
            enemyDictonaryView.OnCloseRequested += CloseActiveScreen;
        }

        if (questStageView != null)
        {
            questStageView.OnCloseRequested -= CloseActiveScreen;
            questStageView.OnCloseRequested += CloseActiveScreen;
        }

        if (playerSession != null)
        {
            playerSession.OnExperiencePointsChanged -= HandleExperiencePointsChanged;
            playerSession.OnExperiencePointsChanged += HandleExperiencePointsChanged;
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

        if (enemyDictonaryView != null)
        {
            enemyDictonaryView.OnCloseRequested -= CloseActiveScreen;
        }

        if (questStageView != null)
        {
            questStageView.OnCloseRequested -= CloseActiveScreen;
        }

        if (playerSession != null)
        {
            playerSession.OnExperiencePointsChanged -= HandleExperiencePointsChanged;
        }

        isEventsRegistered = false;
    }

    private void HandleExperiencePointsChanged(int experiencePoints)
    {
        SetExperiencePointsText(experiencePoints);
    }

    private void RefreshExperiencePoints()
    {
        if (playerSession == null)
        {
            if (experiencePointsText != null && experiencePointsText.Text != null)
            {
                experiencePointsText.Refresh();
            }

            return;
        }

        SetExperiencePointsText(playerSession.ExperiencePoints);
    }

    private void SetExperiencePointsText(int experiencePoints)
    {
        if (experiencePointsText == null || experiencePointsText.Text == null)
        {
            return;
        }

        experiencePointsText.SetText(experiencePoints.ToString("N0", CultureInfo.InvariantCulture));
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

        if (enemyDictonaryView == null)
        {
            enemyDictonaryView = GetComponentInChildren<EnemyDictonaryView>(true);
        }

        if (questStageView == null)
        {
            questStageView = GetComponentInChildren<QuestStageView>(true);
        }
    }
}
