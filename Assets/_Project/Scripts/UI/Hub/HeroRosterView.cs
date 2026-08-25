using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class HeroRosterView : MonoBehaviour
{

    private const string BlackColorTag = "#000000";
    private const string WhiteColorTag = "#FFFFFF";
    private const string BonusColorTag = "#00A651";

    [Header("Player Session")]
    [SerializeField] private PlayerSession playerSession;

    [Header("Screen")]
    [SerializeField] private GameObject viewRoot;
    [SerializeField] private Button closeButton;

    [Header("Hero Cards")]
    [SerializeField] private Transform heroCardContainer;
    [SerializeField] private RosterHeroCard heroCardPrefab;

    [Header("Hero Detail")]
    [SerializeField] private GameObject heroDetailRoot;
    [SerializeField] private Image detailHeroImage;
    [SerializeField] private UIValueTextBinding detailHeroName = new UIValueTextBinding();

    [Header("Hero Experience")]
    [SerializeField] private UIValueTextBinding currentExperienceText = new UIValueTextBinding();
    [SerializeField] private Image experienceFill;

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

    [Header("Hero Skills")]
    [SerializeField] private SkillSlotBinding passiveSkillSlot;
    [SerializeField] private SkillSlotBinding activeSkillSlot;

    private readonly List<RosterHeroCard> spawnedCards = new List<RosterHeroCard>();
    private RosterHeroCard selectedCard;

    public event Action OnCloseRequested;

    private void Awake()
    {
        CacheReferences();
        HideDetail();
    }

    private void OnEnable()
    {
        RegisterCloseButtonEvent();
    }

    private void OnDisable()
    {
        UnregisterCloseButtonEvent();
    }

    private void OnDestroy()
    {
        UnregisterCloseButtonEvent();
        UnregisterCardEvents();
    }

    public void Show(HeroRoster heroRoster)
    {
        viewRoot.SetActive(true);
        ResetHeroCards(heroRoster);
    }

    public void Hide()
    {
        viewRoot.SetActive(false);
    }

    private void ResetHeroCards(HeroRoster heroRoster)
    {
        ClearSpawnedCards();
        HideDetail();

        if (heroRoster == null || !heroRoster.HasHeroes)
        {
            return;
        }

        if (heroCardContainer == null || heroCardPrefab == null)
        {
            Debug.LogError("[HeroRosterView] Hero card container and prefab are required to display the team.", this);
            return;
        }

        IReadOnlyList<HeroInstance> heroes = heroRoster.Heroes;

        for (int i = 0; i < heroes.Count; i++)
        {
            HeroInstance hero = heroes[i];
            if (hero == null || !hero.IsValid)
            {
                continue;
            }

            RosterHeroCard card = Instantiate(heroCardPrefab, heroCardContainer);
            card.BindHeroData(hero);
            card.OnCardClicked += HandleHeroCardClicked;
            spawnedCards.Add(card);
        }
    }

    private void HandleHeroCardClicked(RosterHeroCard card, HeroInstance hero)
    {
        if (hero == null || !hero.IsValid || hero.Definition == null)
        {
            return;
        }

        if (selectedCard != null && selectedCard != card)
        {
            selectedCard.SetActive(true);
        }

        selectedCard = card;
        selectedCard.SetActive(false);
        ShowDetail(hero);
    }

    private void ShowDetail(HeroInstance hero)
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

        HeroProgression progression = playerSession != null ? playerSession.Progression : null;
        if (progression != null && progression.IsInitialized)
        {
            int currentLevel = progression.GetLevelForExperience(hero.Experience);

            if (progression.IsMaxLevel(currentLevel))
            {
                int experienceForMaxLevel = progression.GetExperienceForLevel(currentLevel);
                SetBindingText(currentExperienceText, $"{experienceForMaxLevel} / MAX");
                SetFillAmount(experienceFill, 1f);
            }
            else
            {
                int currentLevelThreshold = currentLevel <= 1 ? 0 : progression.GetExperienceForLevel(currentLevel);
                int nextLevelThreshold = progression.GetExperienceForLevel(currentLevel + 1);
                int currentLevelExperience = Mathf.Max(0, hero.Experience - currentLevelThreshold);
                int experienceToNextLevel = Mathf.Max(0, nextLevelThreshold - currentLevelThreshold);

                SetBindingText(currentExperienceText, $"{currentLevelExperience.ToString()}/{experienceToNextLevel.ToString()}");
                SetFillAmount(experienceFill, experienceToNextLevel > 0 ? currentLevelExperience / (float)experienceToNextLevel : 1f);
            }
        }
        else
        {
            RefreshBinding(currentExperienceText);
            SetFillAmount(experienceFill, 0f);
        }

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

    private void HideDetail()
    {
        selectedCard = null;

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

        RefreshBinding(currentExperienceText);
        SetFillAmount(experienceFill, 0f);

        RefreshStat(maxHealthStat);
        RefreshStat(attackStat);
        RefreshStat(defenseStat);
        RefreshStat(specialDefenseStat);
        RefreshStat(attackIntervalStat);
        RefreshStat(blockCountStat);
        RefreshStat(deployCostStat);
        RefreshStat(redeployTimeStat);
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
            SetBindingText(slot.CooldownText, "PASSIVE");
        }
        else
        {
            SetBindingInt(slot.CooldownText, skill.Cooldown);
        }

        SetBindingText(slot.SkillName, skill.SkillName);
        SetBindingText(slot.Description, skill.SkillDescription);
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

        if (useFloat)
        {
            if (statBinding.TotalText.Text != null)
            {
                statBinding.TotalText.SetTextColor(Color.black);
                statBinding.TotalText.SetFloat(stat.FinalValue, "0.##");
            }

            baseValueText = FormatNumber(stat.BaseValue);
            bonusValueText = FormatNumber(Mathf.Abs(stat.BonusValue));
            isNegativeBonus = stat.BonusValue < 0f;
        }
        else
        {
            int finalValue = Mathf.RoundToInt(stat.FinalValue);
            int baseValue = Mathf.RoundToInt(stat.BaseValue);

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

    private static void SetFillAmount(Image image, float fillAmount)
    {
        if (image != null)
        {
            image.fillAmount = Mathf.Clamp01(fillAmount);
        }
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

    private void ClearSpawnedCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            RosterHeroCard card = spawnedCards[i];
            if (card == null)
            {
                continue;
            }

            card.OnCardClicked -= HandleHeroCardClicked;
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }

        spawnedCards.Clear();
        selectedCard = null;
    }

    private void HandleCloseButtonClicked()
    {
        OnCloseRequested?.Invoke();
    }

    private void RegisterCloseButtonEvent()
    {
        if (closeButton == null)
        {
            return;
        }

        closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
        closeButton.onClick.AddListener(HandleCloseButtonClicked);
    }

    private void UnregisterCloseButtonEvent()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
        }
    }

    private void UnregisterCardEvents()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
            {
                spawnedCards[i].OnCardClicked -= HandleHeroCardClicked;
            }
        }
    }

    private void CacheReferences()
    {
        if (viewRoot == null)
        {
            viewRoot = gameObject;
        }
    }
}
