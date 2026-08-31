using UnityEngine;

public class CombatStatHUD : WorldHealthHUD
{
    [Header("Combat Stat References")]
    [SerializeField] private Shield shield;
    [SerializeField] private CombatStatBar combatStatBar;

    private bool isShieldEventSubscribed;

    public override void Initialize()
    {
        UnregisterShieldEvent();
        base.Initialize();

        RegisterShieldEvent();
        RefreshShield();
        SetSkillCharge(0f, 0f);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RegisterShieldEvent();
        RefreshShield();
    }

    protected override void OnDisable()
    {
        UnregisterShieldEvent();
        base.OnDisable();
    }

    protected override void CacheReferences()
    {
        base.CacheReferences();

        if (combatStatBar == null)
        {
            combatStatBar = healthBar as CombatStatBar;
        }

        if (shield == null && health != null)
        {
            shield = health.Shield;
        }
    }

    public void SetSkillCharge(float cooldownRemaining, float cooldownTime)
    {
        if (combatStatBar != null)
        {
            combatStatBar.SetSkillCharge(cooldownRemaining, cooldownTime);
        }
    }

    private void RegisterShieldEvent()
    {
        if (isShieldEventSubscribed || !isActiveAndEnabled || shield == null)
        {
            return;
        }

        shield.OnShieldValueChanged += HandleShieldValueChanged;
        isShieldEventSubscribed = true;
    }

    private void UnregisterShieldEvent()
    {
        if (!isShieldEventSubscribed)
        {
            return;
        }

        if (shield != null)
        {
            shield.OnShieldValueChanged -= HandleShieldValueChanged;
        }

        isShieldEventSubscribed = false;
    }

    private void RefreshShield()
    {
        if (combatStatBar == null || health == null)
        {
            return;
        }

        float currentShield = shield != null ? shield.CurrentShield : 0f;
        combatStatBar.SetShieldValue(currentShield, health.MaxHealth);
    }

    private void HandleShieldValueChanged(float currentShield)
    {
        if (combatStatBar != null && health != null)
        {
            combatStatBar.SetShieldValue(currentShield, health.MaxHealth);
        }
    }
}
