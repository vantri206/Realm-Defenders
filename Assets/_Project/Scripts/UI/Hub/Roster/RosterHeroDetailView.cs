using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class RosterHeroDetailView : MonoBehaviour
{
    private const string BlackColorTag = "#000000";
    private const string WhiteColorTag = "#FFFFFF";
    private const string BonusColorTag = "#00A651";

    [Header("Hero Detail")]
    [SerializeField] private GameObject heroDetailRoot;
    [SerializeField] private Image detailHeroImage;
    [SerializeField] private UIValueTextBinding detailHeroName = new UIValueTextBinding();

    [Header("Hero Stats")]
    [SerializeField] private StatBinding maxHealthStat;
    [SerializeField] private StatBinding attackStat;
    [SerializeField] private StatBinding defenseStat;
    [SerializeField] private StatBinding specialDefenseStat;
    [SerializeField] private StatBinding attackIntervalStat;
    [SerializeField] private StatBinding blockCountStat;
    [SerializeField] private StatBinding deployCostStat;
    [SerializeField] private StatBinding redeployTimeStat;

    [Header("Hero Identity")]
    [SerializeField] private UIValueTextBinding detailHeroDescription = new UIValueTextBinding();
    [SerializeField] private Image detailClassIcon;
    [SerializeField] private UIValueTextBinding detailClassText = new UIValueTextBinding();
    [SerializeField] private Image detailAttackTypeIcon;
    [SerializeField] private UIValueTextBinding detailAttackTypeText = new UIValueTextBinding();
    [SerializeField] private AttackTypeDefinition[] attackTypeDefinitions;

    [Header("Hero Gear")]
    [SerializeField] private GearSlotBinding weaponSlot;
    [SerializeField] private GearSlotBinding armorSlot;

    [Header("Hero Skills")]
    [SerializeField] private SkillSlotBinding passiveSkillSlot;
    [SerializeField] private SkillSlotBinding activeSkillSlot;

    public event Action<GearType> OnGearPickerRequested;

    private void OnEnable()
    {
        RegisterGearSlotEvents();
    }

    private void OnDisable()
    {
        UnregisterGearSlotEvents();
    }

    private void OnDestroy()
    {
        UnregisterGearSlotEvents();
    }

    public void Show(HeroInstance hero)
    {
        HeroDefinition definition = hero.Definition;

        if (heroDetailRoot != null)
        {
            heroDetailRoot.SetActive(true);
        }

        if (detailHeroImage != null)
        {
            if (definition.HeroDisplaySprite != null)
            {
                detailHeroImage.sprite = definition.HeroDisplaySprite;
                detailHeroImage.enabled = true;
            }
            else
            {
                detailHeroImage.sprite = null;
                detailHeroImage.enabled = false;
            }
        }

        if (detailHeroName != null)
        {
            SetBindingText(detailHeroName, definition.HeroName.ToUpper());
        }

        SetBindingText(detailHeroDescription, definition.HeroDescription);

        ClassDefinition heroClass = definition.HeroClass;
        SetImage(detailClassIcon, heroClass != null ? heroClass.Icon : null);
        SetBindingText(detailClassText, heroClass != null ? heroClass.ClassId.ToUpper() : null);

        AttackTypeDefinition attackTypeDefinition = null;
        if (definition.NormalAttackDefinition != null)
        {
            UnitAttackType attackType = definition.NormalAttackDefinition.AttackType;
            attackTypeDefinition = GetAttackTypeDefinition(attackType);
        }

        SetImage(detailAttackTypeIcon, attackTypeDefinition != null ? attackTypeDefinition.Icon : null);
        SetBindingText(detailAttackTypeText, attackTypeDefinition != null ? attackTypeDefinition.AttackType.ToString().ToUpper() : null);

        SetSkillSlot(passiveSkillSlot, definition.PassiveSkill);
        SetSkillSlot(activeSkillSlot, definition.ActiveSkill);

        SetGearSlot(weaponSlot, hero.EquippedWeapon);
        SetGearSlot(armorSlot, hero.EquippedArmor);

        UnitBreakdownStats stats = hero.Stats != null ? hero.Stats.FinalStats : null;

        if (stats != null)
        {
            SetStat(maxHealthStat, stats.GetBreakdown(UnitStatType.MaxHealth));
            SetStat(attackStat, stats.GetBreakdown(UnitStatType.Attack));
            SetStat(defenseStat, stats.GetBreakdown(UnitStatType.Defense));
            SetStat(specialDefenseStat, stats.GetBreakdown(UnitStatType.SpecialDefense));
            SetStat(attackIntervalStat, stats.GetBreakdown(UnitStatType.AttackInterval), true);
            SetStat(blockCountStat, stats.GetBreakdown(UnitStatType.BlockCount));
        }
        else
        {
            RefreshStat(maxHealthStat);
            RefreshStat(attackStat);
            RefreshStat(defenseStat);
            RefreshStat(specialDefenseStat);
            RefreshStat(attackIntervalStat);
            RefreshStat(blockCountStat);
        }

        SetStat(deployCostStat, CreateBaseStat(definition.BaseDeployCost));
        SetStat(redeployTimeStat, CreateBaseStat(definition.BaseRedeployTime));
    }

    public void Hide()
    {
        if (heroDetailRoot != null)
        {
            heroDetailRoot.SetActive(false);
        }

        if (detailHeroImage != null)
        {
            detailHeroImage.sprite = null;
            detailHeroImage.enabled = false;
        }

        if (detailHeroName != null)
        {
            RefreshBinding(detailHeroName);
        }

        RefreshBinding(detailHeroDescription);
        SetImage(detailClassIcon, null);
        RefreshBinding(detailClassText);
        SetImage(detailAttackTypeIcon, null);
        RefreshBinding(detailAttackTypeText);

        SetSkillSlot(passiveSkillSlot, null);
        SetSkillSlot(activeSkillSlot, null);

        SetGearSlot(weaponSlot, null);
        SetGearSlot(armorSlot, null);

        RefreshStat(maxHealthStat);
        RefreshStat(attackStat);
        RefreshStat(defenseStat);
        RefreshStat(specialDefenseStat);
        RefreshStat(attackIntervalStat);
        RefreshStat(blockCountStat);
        RefreshStat(deployCostStat);
        RefreshStat(redeployTimeStat);
    }

    private void HandleWeaponSlotClicked()
    {
        OnGearPickerRequested?.Invoke(GearType.Weapon);
    }

    private void HandleArmorSlotClicked()
    {
        OnGearPickerRequested?.Invoke(GearType.Armor);
    }

    private void RegisterGearSlotEvents()
    {
        weaponSlot.OnClicked += HandleWeaponSlotClicked;
        armorSlot.OnClicked += HandleArmorSlotClicked;
    }

    private void UnregisterGearSlotEvents()
    {
        weaponSlot.OnClicked -= HandleWeaponSlotClicked;
        armorSlot.OnClicked -= HandleArmorSlotClicked;
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

    private static void SetSkillSlot(SkillSlotBinding slot, SkillDefinition skill)
    {
        if (slot == null)
        {
            return;
        }

        slot.gameObject.SetActive(skill != null);

        SetImage(slot.Icon, skill != null ? skill.SkillIcon : null);

        if (skill == null)
        {
            if (slot.CooldownIcon != null)
            {
                slot.CooldownIcon.SetActive(false);
            }

            RefreshBinding(slot.CooldownText);
            RefreshBinding(slot.SkillName);
            RefreshBinding(slot.Description);
            return;
        }

        bool isPassive = skill.SkillType == SkillType.Passive;
        if (slot.CooldownIcon != null)
        {
            slot.CooldownIcon.SetActive(!isPassive);
        }

        if (isPassive)
        {
            SetBindingText(slot.CooldownText, " PASS" + "\n" + "IVE");
        }
        else
        {
            SetBindingInt(slot.CooldownText, skill.Cooldown);
        }

        SetBindingText(slot.SkillName, skill.SkillName);
        SetBindingText(slot.Description, skill.SkillDescription);
    }

    private static void SetGearSlot(GearSlotBinding slot, GearInstance gear)
    {
        if (slot == null)
        {
            return;
        }

        bool isEquip = gear != null && gear.IsValid;

        if (slot.UnequippedGearFrame != null)
        {
            slot.UnequippedGearFrame.gameObject.SetActive(!isEquip);
        }

        if (slot.EquippedGearFrame != null)
        {
            slot.EquippedGearFrame.gameObject.SetActive(isEquip);
        }

        if (isEquip && slot.GearIcon != null)
        {
            slot.GearIcon.sprite = gear.Definition.GearIcon;
            slot.GearIcon.enabled = true;
            slot.GearIcon.gameObject.SetActive(true);
        }
        else
        {
            slot.GearIcon.sprite = null;
            slot.GearIcon.enabled = false;
            slot.GearIcon.gameObject.SetActive(false);
        }

        slot.SetGearTooltip(gear);
    }

    private static UnitStatBreakdown CreateBaseStat(float value)
    {
        return new UnitStatBreakdown(value, 0f, 0f, 1f, value);
    }

    private static void SetStat(StatBinding statBinding, UnitStatBreakdown stat, bool useFloat = false)
    {
        string baseValueText;
        string bonusValueText;
        bool isNegativeBonus;

        if (statBinding == null || statBinding.TotalText == null || statBinding.DetailText == null)
        {
            return;
        }

        float displayedBaseValue = stat.BaseValue + stat.FlatBase;
        float displayedBonusValue = stat.FinalValue - displayedBaseValue;

        if (useFloat)
        {
            if (statBinding.TotalText.Text != null)
            {
                statBinding.TotalText.SetTextColor(Color.black);
                statBinding.TotalText.SetFloat(stat.FinalValue, "0.##");
            }

            baseValueText = FormatNumber(displayedBaseValue);
            bonusValueText = FormatNumber(Mathf.Abs(displayedBonusValue));
            isNegativeBonus = displayedBonusValue < 0f;
        }
        else
        {
            int finalValue = Mathf.RoundToInt(stat.FinalValue);
            int baseValue = Mathf.RoundToInt(displayedBaseValue);

            int bonusValue = finalValue - baseValue;

            if (statBinding.TotalText.Text != null)
            {
                statBinding.TotalText.SetTextColor(Color.black);
                statBinding.TotalText.SetInt(finalValue);
            }

            baseValueText = baseValue.ToString();
            bonusValueText = Mathf.Abs(bonusValue).ToString();
            isNegativeBonus = bonusValue < 0;
        }

        if (statBinding.DetailText.Text == null)
        {
            return;
        }

        string operation = isNegativeBonus ? "-" : "+";

        statBinding.DetailText.SetTextColor(Color.black);

        statBinding.DetailText.SetText(
            $"(<color={WhiteColorTag}>{baseValueText}</color>" +
            $"{operation}" +
            $"<color={BonusColorTag}>{bonusValueText}</color>)");
    }

    private static void RefreshStat(StatBinding binding)
    {
        if (binding == null)
        {
            return;
        }

        RefreshBinding(binding.TotalText);
        RefreshBinding(binding.DetailText);
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private static void SetBindingText(UIValueTextBinding binding, string value)
    {
        if (binding != null && binding.Text != null)
        {
            binding.SetText(value);
        }
    }

    private static void SetBindingInt(UIValueTextBinding binding, float value)
    {
        if (binding != null && binding.Text != null)
        {
            binding.SetInt(value);
        }
    }

    private static string FormatNumber(float value)
    {
        return value.ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static void RefreshBinding(UIValueTextBinding binding)
    {
        if (binding != null && binding.Text != null)
        {
            binding.Refresh();
        }
    }
}
