using UnityEngine;
using UnityEngine.Serialization;

public class HeroProgressionView : MonoBehaviour
{
    private const string NotEnoughExperienceMessage = "NOT ENOUGH EXPERIENCE POINTS";

    [Header("Level Upgrade")]
    [SerializeField] private UIValueTextBinding levelUpCostText = new UIValueTextBinding();
    [SerializeField] private UIButtonFeedback upgradeButton;

    private PlayerSession playerSession;
    private HeroInstance selectedHero;

    private void OnEnable()
    {
        upgradeButton.OnClicked += HandleUpgradeButtonClicked;
    }

    private void OnDisable()
    {
        upgradeButton.OnClicked -= HandleUpgradeButtonClicked;
    }

    public void Show(HeroInstance hero, PlayerSession playerSession)
    {
        selectedHero = hero;
        this.playerSession = playerSession;

        HeroProgression progression = playerSession != null ? playerSession.Progression : null;
        if (hero != null && hero.IsValid && progression != null && progression.IsInitialized)
        {
            if (progression.IsMaxLevel(hero.Level))
            {
                SetBindingText(levelUpCostText, "MAX");
                upgradeButton.SetInteractable(false);
            }
            else
            {
                SetBindingText(levelUpCostText, progression.GetExperienceToLevelUp(hero.Level).ToString());
                upgradeButton.SetInteractable(true);
            }
        }
        else
        {
            RefreshBinding(levelUpCostText);
            upgradeButton.SetInteractable(false);
        }
    }

    public void Hide()
    {
        selectedHero = null;
        playerSession = null;
        RefreshBinding(levelUpCostText);
        upgradeButton.SetInteractable(false);
    }

    private void HandleUpgradeButtonClicked()
    {
        if (selectedHero == null || playerSession == null)
        {
            Debug.LogWarning("[HeroProgressionView] No hero is currently selected for upgrade.");
            return;
        }

        TryUpgradeHero();
    }

    private static void SetBindingText(UIValueTextBinding binding, string value)
    {
        if (binding != null && binding.Text != null)
        {
            binding.SetText(value);
        }
    }

    private static void RefreshBinding(UIValueTextBinding binding)
    {
        if (binding != null && binding.Text != null)
        {
            binding.Refresh();
        }
    }

    [ContextMenu("Upgrade Hero")]
    private void TryUpgradeHero()
    {
        if (selectedHero == null || !selectedHero.IsValid)
        {
            Debug.LogWarning("[HeroProgressionView] No valid hero is currently selected for upgrade.", this);
            return;
        }

        HeroLevelUpgradeResult result = playerSession.TryUpgradeHeroLevel(selectedHero);
        if (result == HeroLevelUpgradeResult.Success || result == HeroLevelUpgradeResult.MaxLevel)
        {
            Show(selectedHero, playerSession);
            return;
        }

        if (result == HeroLevelUpgradeResult.NotEnoughExperiencePoints)
        {
            UIOverlayRoot.Instance.TryShowQuickNotification(NotEnoughExperienceMessage);
        }
    }
}
