using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSession : MonoBehaviour
{
    [SerializeField] private HeroProgressionConfig progressionConfig;

    [SerializeField] private StarterHeroRoster startHeroRoster = new StarterHeroRoster();
    [SerializeField] private StarterInventoryConfig startInventory = new StarterInventoryConfig();
    [SerializeField] private bool loadStartPlayerData = true;

    private HeroProgression progression;
    private HeroRoster heroRoster;
    private PlayerInventory playerInventory;

    public HeroProgression Progression => progression;
    public HeroRoster HeroRoster => heroRoster;
    public int ExperiencePoints => playerInventory != null ? playerInventory.ExperiencePoints : 0;

    public event Action<HeroInstance, HeroChangeType> OnHeroChanged;
    public event Action<int> OnExperiencePointsChanged;

    private void Awake()
    {
        progression = new HeroProgression();

        progression.Initialize(progressionConfig);

        heroRoster = new HeroRoster();

        playerInventory = new PlayerInventory();

        if (loadStartPlayerData)
        {
            LoadStartPlayerData();
        }
    }

    private void LoadStartPlayerData()
    {
        ClearSessionData();
        LoadStartRoster();
        LoadStartInventory(heroRoster);
    }

    private void ClearSessionData()
    {
        for (int i = 0; i < playerInventory.GearCount; i++)
        {
            GearInstance gear = playerInventory.Gears[i];
            if (gear != null)
            {
                switch (gear.Definition.GearType)
                {
                    case GearType.Weapon:
                        if (gear.EquippedHero != null)
                        {
                            UnequipWeapon(gear.EquippedHero);
                        }
                        break;
                    case GearType.Armor:
                        if (gear.EquippedHero != null)
                        {
                            UnequipArmor(gear.EquippedHero);
                        }
                        break;
                }
            }
        }

        if (playerInventory != null)
        {
            playerInventory.TryClearInventory();
        }

        if (heroRoster != null)
        {
            heroRoster.TryClearRoster();
        }
    }

    public void LoadStartRoster()
    {
        if (startHeroRoster == null || !startHeroRoster.HasStartHeroes)
        {
            return;
        }

        if (heroRoster == null)
        {
            heroRoster = new HeroRoster();
        }

        heroRoster.LoadInitialRoster(startHeroRoster);
        RefreshHeroProgression();
    }

    public void LoadStartInventory(HeroRoster heroRoster)
    {
        if (startInventory == null)
        {
            return;
        }

        if (playerInventory == null)
        {
            playerInventory = new PlayerInventory();
        }

        IReadOnlyList<StarterHeroWeaponAssignment> assignments = playerInventory.LoadInitialInventory(startInventory);
        OnExperiencePointsChanged?.Invoke(playerInventory.ExperiencePoints);

        for (int i = 0; i < assignments.Count; i++)
        {
            StarterHeroWeaponAssignment assignment = assignments[i];

            if (assignment == null)
            {
                continue;
            }

            HeroInstance hero = heroRoster.GetHeroByDefinition(assignment.HeroDefinition);
            GearInstance gear = assignment.GearInstance;

            if (hero == null || !hero.IsValid)
            {
                Debug.LogWarning($"[PlayerSession] Cannot equip gear {gear.Definition.GearName} to hero {assignment.HeroDefinition.HeroName} because the hero is not in the roster.");
                continue;
            }

            if (gear == null || !gear.IsValid)
            {
                Debug.LogWarning($"[PlayerSession] Cannot equip gear to hero {hero.Definition.HeroName} because the gear instance is invalid.");
                continue;
            }

            if (gear.Definition.GearType == GearType.Weapon)
            {
                EquipWeapon(hero, gear);
            }
            else if (gear.Definition.GearType == GearType.Armor)
            {
                EquipArmor(hero, gear);
            }
        }
    }

    public void RefreshHeroProgression()
    {
        if (progression == null || !progression.IsInitialized || heroRoster == null)
        {
            return;
        }

        for (int i = 0; i < heroRoster.Heroes.Count; i++)
        {
            progression.RefreshHeroLevel(heroRoster.Heroes[i]);
        }
    }

    public bool HasRosterTest()
    {
        return heroRoster != null && heroRoster.HasHeroes;
    }

    public bool TryAddExperiencePoints(int amount)
    {
        if (playerInventory == null || !playerInventory.AddExperiencePoints(amount))
        {
            return false;
        }

        OnExperiencePointsChanged?.Invoke(playerInventory.ExperiencePoints);
        return true;
    }

    public HeroLevelUpgradeResult TryUpgradeHeroLevel(HeroInstance hero)
    {
        if (hero == null || !hero.IsValid || heroRoster == null || !heroRoster.ContainsHero(hero))
        {
            return HeroLevelUpgradeResult.Exception;
        }

        if (progression == null || !progression.IsInitialized || playerInventory == null)
        {
            return HeroLevelUpgradeResult.Exception;
        }

        if (progression.IsMaxLevel(hero.Level))
        {
            return HeroLevelUpgradeResult.MaxLevel;
        }

        int upgradeCost = progression.GetExperienceToLevelUp(hero.Level);
        if (!playerInventory.TrySpendExperiencePoints(upgradeCost))
        {
            return HeroLevelUpgradeResult.NotEnoughExperiencePoints;
        }

        if (!progression.UpgradeHeroLevel(hero))
        {
            playerInventory.AddExperiencePoints(upgradeCost);
            return HeroLevelUpgradeResult.Exception;
        }

        OnExperiencePointsChanged?.Invoke(playerInventory.ExperiencePoints);
        NotifyHeroProgressionChanged(hero);
        return HeroLevelUpgradeResult.Success;
    }

    public void EquipWeapon(HeroInstance hero, GearInstance weapon)
    {
        if (!IsWeaponCanEquip(hero, weapon))
        {
            return;
        }

        HeroInstance previousEquippedHero = weapon.EquippedHero;
        if (previousEquippedHero == hero)
        {
            return;
        }

        GearInstance previousWeapon = hero.EquippedWeapon;

        if (previousEquippedHero != null)
        {
            ApplyUnequipment(previousEquippedHero, GearType.Weapon);
        }

        if (previousWeapon != null)
        {
            ApplyUnequipment(hero, GearType.Weapon);
        }

        ApplyEquipment(hero, weapon);

        if (previousEquippedHero != null && previousWeapon != null)
        {
            ApplyEquipment(previousEquippedHero, previousWeapon);
        }

        NotifyHeroEquipmentChanged(hero);

        if (previousEquippedHero != null)
        {
            NotifyHeroEquipmentChanged(previousEquippedHero);
        }
    }

    public void EquipArmor(HeroInstance hero, GearInstance armor)
    {
        if (!IsArmorCanEquip(hero, armor))
        {
            return;
        }

        HeroInstance previousEquippedHero = armor.EquippedHero;
        if (previousEquippedHero == hero)
        {
            return;
        }

        GearInstance previousArmor = hero.EquippedArmor;

        if (previousEquippedHero != null)
        {
            ApplyUnequipment(previousEquippedHero, GearType.Armor);
        }

        if (previousArmor != null)
        {
            ApplyUnequipment(hero, GearType.Armor);
        }

        ApplyEquipment(hero, armor);

        if (previousEquippedHero != null && previousArmor != null)
        {
            ApplyEquipment(previousEquippedHero, previousArmor);
        }

        NotifyHeroEquipmentChanged(hero);

        if (previousEquippedHero != null)
        {
            NotifyHeroEquipmentChanged(previousEquippedHero);
        }
    }

    public void UnequipWeapon(HeroInstance hero)
    {
        if (hero == null || !hero.IsValid || hero.EquippedWeapon == null)
        {
            return;
        }

        ApplyUnequipment(hero, GearType.Weapon);
        NotifyHeroEquipmentChanged(hero);
    }

    public void UnequipArmor(HeroInstance hero)
    {
        if (hero == null || !hero.IsValid || hero.EquippedArmor == null)
        {
            return;
        }

        GearInstance armor = hero.EquippedArmor;

        hero.OnUnequipArmor();
        armor.OnUnequip();

        OnHeroChanged?.Invoke(hero, HeroChangeType.Equipment);
        OnHeroChanged?.Invoke(hero, HeroChangeType.Stats);
    }

    private void ApplyEquipment(HeroInstance hero, GearInstance gear)
    {
        if (gear.Definition.GearType == GearType.Weapon)
        {
            hero.EquipWeapon(gear);
            gear.OnEquip(hero);
        }
        else if (gear.Definition.GearType == GearType.Armor)
        {
            hero.EquipArmor(gear);
            gear.OnEquip(hero);
        }
    }

    private void ApplyUnequipment(HeroInstance hero, GearType gearType)
    {
        if (gearType == GearType.Weapon)
        {
            hero.EquippedWeapon.OnUnequip();
            hero.OnUnequipWeapon();
        }
        else if (gearType == GearType.Armor)
        {
            hero.EquippedArmor.OnUnequip();
            hero.OnUnequipArmor();
        }
    }

    private void NotifyHeroProgressionChanged(HeroInstance hero)
    {
        OnHeroChanged?.Invoke(hero, HeroChangeType.Progression);
        OnHeroChanged?.Invoke(hero, HeroChangeType.Stats);
    }

    private void NotifyHeroEquipmentChanged(HeroInstance hero)
    {
        OnHeroChanged?.Invoke(hero, HeroChangeType.Equipment);
        OnHeroChanged?.Invoke(hero, HeroChangeType.Stats);
    }

    private bool IsWeaponCanEquip(HeroInstance hero, GearInstance weapon)
    {
        bool isWeaponValid = weapon != null && weapon.IsValid && playerInventory.ContainsGear(weapon) && weapon.Definition.GearType == GearType.Weapon;

        bool isHeroValid = hero != null && heroRoster.ContainsHero(hero) && hero.IsValid;

        return isWeaponValid && isHeroValid;
    }

    private bool IsArmorCanEquip(HeroInstance hero, GearInstance armor)
    {
        bool isArmorValid = armor != null && armor.IsValid && playerInventory.ContainsGear(armor) && armor.Definition.GearType == GearType.Armor;

        bool isHeroValid = hero != null && heroRoster.ContainsHero(hero) && hero.IsValid;

        return isArmorValid && isHeroValid;
    }

    public IReadOnlyList<GearInstance> GetAllGears()
    {
        return playerInventory.Gears;
    }
}

public enum HeroLevelUpgradeResult
{
    Success,
    MaxLevel,
    Exception,
    NotEnoughExperiencePoints,
}
