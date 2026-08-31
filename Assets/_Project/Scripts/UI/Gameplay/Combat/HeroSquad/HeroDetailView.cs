using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HeroDetailView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject viewRoot;

    [Header("Identity")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private UIValueTextBinding heroName = new UIValueTextBinding();

    [Header("Trait")]
    [SerializeField] private Image classIcon;
    [SerializeField] private UIValueTextBinding className = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding attackTypeText;
    [SerializeField] private AttackTypeDefinition[] attackTypeDefinitions;

    [Header("Health")]
    [SerializeField] private UIValueTextBinding currentHealth = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding maxHealth = new UIValueTextBinding();

    [Header("Stats")]
    [SerializeField] private UIValueTextBinding attack = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding defense = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding specialDefense = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding attackSpeed = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding block = new UIValueTextBinding();

    [Header("Deployment")]
    [SerializeField] private UIValueTextBinding deployCost = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding redeployTime = new UIValueTextBinding();

    [Header("Equipment")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image armorIcon;

    [Header("Skills")]
    [SerializeField] private Image passiveSkillIcon;
    [SerializeField] private Image activeSkillIcon;

    [Header("Tooltip Data")]
    [SerializeField] private UnitStatDescriptionTable statDescriptionTable;

    [Header("Tooltip Triggers")]
    [SerializeField] private TooltipTrigger avatarTooltip;
    [SerializeField] private TooltipTrigger classTooltip;
    [SerializeField] private TooltipTrigger attackTypeTooltip;
    [SerializeField] private TooltipTrigger healthTooltip;
    [SerializeField] private TooltipTrigger attackTooltip;
    [SerializeField] private TooltipTrigger defenseTooltip;
    [SerializeField] private TooltipTrigger specialDefenseTooltip;
    [SerializeField] private TooltipTrigger attackSpeedTooltip;
    [SerializeField] private TooltipTrigger blockTooltip;
    [SerializeField] private TooltipTrigger redeployTimeTooltip;
    [SerializeField] private TooltipTrigger weaponTooltip;
    [SerializeField] private TooltipTrigger armorTooltip;
    [SerializeField] private TooltipTrigger passiveSkillTooltip;
    [SerializeField] private TooltipTrigger activeSkillTooltip;

    private HeroCombatState currentHero;
    private HeroRuntime currentHeroRuntime;

    private void Awake()
    {
        Refresh();
    }

    private void OnEnable()
    {
        RegisterRuntimeEvents();
    }

    private void OnDisable()
    {
        UnregisterRuntimeEvents();
    }

    private void OnDestroy()
    {
        UnregisterRuntimeEvents();
    }

    public void Show(HeroCombatState combatState)
    {
        if (combatState == null || !combatState.IsValid)
        {
            return;
        }

        Refresh();
        SetData(combatState);

        // Debug.Log($"Showing hero detail view for {combatState.Definition.HeroName}");
    }

    public void Show(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null || !heroRuntime.IsInitialized)
        {
            return;
        }

        Refresh();
        SetData(heroRuntime);
    }

    public void Refresh()
    {
        UnregisterRuntimeEvents();
        currentHero = null;
        currentHeroRuntime = null;

        SetAvatar(null);
        heroName.Refresh();
        SetClassIcon(null);
        className.Refresh();
        attackTypeText.Refresh();
        currentHealth.Refresh();
        maxHealth.Refresh();
        attack.Refresh();
        defense.Refresh();
        specialDefense.Refresh();
        attackSpeed.Refresh();
        block.Refresh();
        deployCost.Refresh();
        redeployTime.Refresh();

        SetGearSlot(weaponIcon, weaponTooltip, null);
        SetGearSlot(armorIcon, armorTooltip, null);
        SetSkillSlot(passiveSkillIcon, passiveSkillTooltip, null);
        SetSkillSlot(activeSkillIcon, activeSkillTooltip, null);

        SetTooltipText(avatarTooltip, null);
        SetTooltipText(classTooltip, null);
        SetTooltipText(attackTypeTooltip, null);
        SetTooltipText(healthTooltip, null);
        SetTooltipText(attackTooltip, null);
        SetTooltipText(defenseTooltip, null);
        SetTooltipText(specialDefenseTooltip, null);
        SetTooltipText(attackSpeedTooltip, null);
        SetTooltipText(blockTooltip, null);
        SetTooltipText(redeployTimeTooltip, null);
    }

    public void Hide()
    {
        Refresh();

        if (viewRoot != null)
        {
            viewRoot.SetActive(false);
            return;
        }

        gameObject.SetActive(false);
    }

    public void SetData(HeroCombatState combatState)
    {
        if (combatState == null || !combatState.IsValid)
        {
            Refresh();
            return;
        }

        UnregisterRuntimeEvents();
        currentHeroRuntime = null;

        SetHeroData(combatState, combatState.FinalStats);
        currentHealth.SetInt(combatState.FinalStats.MaxHealth);
    }

    public void SetData(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null || !heroRuntime.IsInitialized)
        {
            Refresh();
            return;
        }

        UnregisterRuntimeEvents();
        currentHeroRuntime = heroRuntime;

        SetHeroData(heroRuntime.CombatState, heroRuntime.Stats.FinalStats);
        SetHealthData(heroRuntime.Health.CurrentData);
        RegisterRuntimeEvents();
    }

    private void SetHeroData(HeroCombatState combatState, UnitBreakdownStats stats)
    {
        currentHero = combatState;
        HeroInstance currentInstance = combatState.HeroInstance;

        HeroDefinition definition = combatState.Definition;
        ClassDefinition heroClass = definition.HeroClass;
        UnitAttackType attackType = definition.NormalAttackDefinition.AttackType;
        AttackTypeDefinition attackTypeDefinition = GetAttackTypeDefinition(attackType);

        // Identity

        SetAvatar(definition.HeroIcon);
        SetClassIcon(heroClass != null ? heroClass.Icon : null);
        heroName.SetText(definition.HeroName.ToUpper());
        className.SetText(heroClass != null ? heroClass.ClassId.ToUpper() : string.Empty);
        attackTypeText.SetText(attackType.ToString().ToUpper());

        // Stats
        SetStatsData(stats);
        deployCost.SetInt(combatState.DeployCost);
        redeployTime.SetSeconds(combatState.RedeployTime);

        // Equipment and Skills
        SetGearSlot(weaponIcon, weaponTooltip, currentInstance.EquippedWeapon);
        SetGearSlot(armorIcon, armorTooltip, currentInstance.EquippedArmor);
        SetSkillSlot(passiveSkillIcon, passiveSkillTooltip, definition.PassiveSkill);
        SetSkillSlot(activeSkillIcon, activeSkillTooltip, definition.ActiveSkill);

        SetTooltipText(avatarTooltip, definition.HeroDescription);
        SetTooltipText(classTooltip, heroClass != null ? heroClass.Description : null);
        SetTooltipText(attackTypeTooltip, attackTypeDefinition != null ? attackTypeDefinition.Description : null);
        SetStatTooltipData();
    }

    private void RegisterRuntimeEvents()
    {
        if (currentHeroRuntime == null)
        {
            return;
        }

        if (currentHeroRuntime.Health != null)
        {
            currentHeroRuntime.Health.OnHealthChanged -= HandleHealthChanged;
            currentHeroRuntime.Health.OnHealthChanged += HandleHealthChanged;
        }

        if (currentHeroRuntime.Stats != null)
        {
            currentHeroRuntime.Stats.OnStatsChanged -= HandleStatsChanged;
            currentHeroRuntime.Stats.OnStatsChanged += HandleStatsChanged;
        }
    }

    private void UnregisterRuntimeEvents()
    {
        if (currentHeroRuntime == null)
        {
            return;
        }

        if (currentHeroRuntime.Health != null)
        {
            currentHeroRuntime.Health.OnHealthChanged -= HandleHealthChanged;
        }

        if (currentHeroRuntime.Stats != null)
        {
            currentHeroRuntime.Stats.OnStatsChanged -= HandleStatsChanged;
        }
    }

    private void HandleHealthChanged(HealthData healthData)
    {
        SetHealthData(healthData);
    }

    private void HandleStatsChanged()
    {
        if (currentHeroRuntime == null || currentHeroRuntime.Stats == null)
        {
            return;
        }

        SetStatsData(currentHeroRuntime.Stats.FinalStats);

        if (currentHeroRuntime.Health != null)
        {
            SetHealthData(currentHeroRuntime.Health.CurrentData);
        }
    }

    private void SetHealthData(HealthData healthData)
    {
        currentHealth.SetInt(healthData.CurrentHealth);
        maxHealth.SetInt(healthData.MaxHealth);
    }

    private void SetStatTooltipData()
    {
        if (statDescriptionTable == null)
        {
            return;
        }

        SetTooltipText(healthTooltip, statDescriptionTable.HealthDescription);
        SetTooltipText(attackTooltip, statDescriptionTable.AttackDescription);
        SetTooltipText(defenseTooltip, statDescriptionTable.DefenseDescription);
        SetTooltipText(specialDefenseTooltip, statDescriptionTable.SpecialDefenseDescription);
        SetTooltipText(attackSpeedTooltip, statDescriptionTable.AttackSpeedDescription);
        SetTooltipText(blockTooltip, statDescriptionTable.BlockDescription);
        SetTooltipText(redeployTimeTooltip, statDescriptionTable.RedeployTimeDescription);
    }

    private AttackTypeDefinition GetAttackTypeDefinition(UnitAttackType attackType)
    {
        if (attackTypeDefinitions == null)
        {
            return null;
        }

        for (int i = 0; i < attackTypeDefinitions.Length; i++)
        {
            AttackTypeDefinition definition = attackTypeDefinitions[i];
            if (definition != null && definition.AttackType == attackType)
            {
                return definition;
            }
        }

        return null;
    }

    private static void SetGearSlot(Image icon, TooltipTrigger tooltip, GearInstance gear)
    {
        bool hasGear = gear != null && gear.IsValid;
        if (hasGear && gear.Definition == null)
        {
            hasGear = false;
        }

        if (icon != null)
        {
            icon.gameObject.SetActive(hasGear);
        }

        if (tooltip != null && (icon == null || tooltip.gameObject != icon.gameObject))
        {
            tooltip.gameObject.SetActive(hasGear);
        }

        if (!hasGear)
        {
            SetSlotIcon(icon, null);
            SetTooltipText(tooltip, null);
            return;
        }

        SetSlotIcon(icon, gear.Definition.GearIcon);
        SetTooltipText(tooltip, TooltipHelper.GetGearTooltipText(gear));
    }

    private static void SetSkillSlot(Image icon, TooltipTrigger tooltip, SkillDefinition skill)
    {
        if (skill == null)
        {
            SetSlotIcon(icon, null);
            SetTooltipText(tooltip, null);
            return;
        }

        SetSlotIcon(icon, skill.SkillIcon);
        SetTooltipText(tooltip, TooltipHelper.GetSkillTooltipText(skill));
    }

    private static void SetSlotIcon(Image icon, Sprite sprite)
    {
        if (icon == null)
        {
            return;
        }

        icon.sprite = sprite;
        icon.enabled = sprite != null;
    }

    private static void SetTooltipText(TooltipTrigger tooltip, string text)
    {
        if (tooltip != null)
        {
            tooltip.SetText(text);
        }
    }

    private void SetStatsData(UnitBreakdownStats stats)
    {
        maxHealth.SetInt(stats.MaxHealth);
        attack.SetInt(stats.Attack);
        defense.SetInt(stats.Defense);
        specialDefense.SetInt(stats.SpecialDefense);
        attackSpeed.SetSeconds(stats.AttackInterval);
        block.SetInt(stats.BlockCount);
    }

    private void SetAvatar(Sprite sprite)
    {
        if (avatarImage == null)
        {
            return;
        }

        if (sprite == null)
        {
            avatarImage.sprite = null;
            avatarImage.enabled = false;
            return;
        }

        avatarImage.sprite = sprite;
        avatarImage.enabled = true;
    }

    private void SetClassIcon(Sprite sprite)
    {
        if (classIcon == null)
        {
            return;
        }

        if (sprite == null)
        {
            classIcon.sprite = null;
            classIcon.enabled = false;
            return;
        }

        classIcon.sprite = sprite;
        classIcon.enabled = true;
    }
}
