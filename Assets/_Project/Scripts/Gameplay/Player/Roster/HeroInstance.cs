using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HeroInstance
{
    [SerializeField] private HeroDefinition definition;
    [SerializeField] private int level = 1;
    [SerializeField] private UnitStats stats = new UnitStats();

    [NonSerialized] private GearInstance equippedWeapon;
    [NonSerialized] private GearInstance equippedArmor;

    public HeroDefinition Definition => definition;
    public int Level => level;
    public UnitStats Stats => stats;
    public GearInstance EquippedWeapon => equippedWeapon;
    public GearInstance EquippedArmor => equippedArmor;

    public bool IsValid => definition != null;

    public event Action<HeroInstance> OnProgressionChanged;

    public HeroInstance() { }

    public HeroInstance(HeroDefinition definition) : this(definition, 1)
    {

    }

    public HeroInstance(HeroDefinition definition, int level)
    {
        Initialize(definition, level);
    }

    private void Initialize(HeroDefinition definition, int level)
    {
        if (definition == null)
        {
            Debug.LogError("[HeroInstance] HeroDefinition cannot be null.");
            return;
        }

        this.definition = definition;
        this.level = Mathf.Max(1, level);

        stats = new UnitStats(GetDefaultStats(definition));

        equippedWeapon = null;
        equippedArmor = null;
    }

    private void SetLevel(int level)
    {
        level = Mathf.Max(1, level);

        if (this.level == level)
        {
            return;
        }

        this.level = level;
    }

    private void SetBaseStats(UnitBaseStats baseStats)
    {
        if (baseStats == null)
        {
            Debug.LogError("[HeroInstance] Base stats cannot be null.");
            return;
        }

        if (stats == null)
        {
            stats = new UnitStats(baseStats);
            return;
        }

        stats.SetBaseStats(baseStats);
    }

    public void ApplyProgression(int level, UnitBaseStats baseStats)
    {
        SetLevel(level);
        SetBaseStats(baseStats);

        OnProgressionChanged?.Invoke(this);
    }

    public void ApplyModifiers(IReadOnlyList<UnitStatModifier> modifiers)
    {
        if (modifiers == null || modifiers.Count == 0)
        {
            return;
        }

        foreach (var modifier in modifiers)
        {
            ApplyModifier(modifier);
        }
    }

    public void RemoveModifiers(IReadOnlyList<UnitStatModifier> modifiers)
    {
        if (stats == null || modifiers == null || modifiers.Count == 0)
        {
            return;
        }

        stats.RemoveModifiers(modifiers);
    }

    private void ApplyModifier(UnitStatModifier modifier)
    {
        if (stats == null || !modifier.IsValid)
        {
            Debug.LogWarning("[HeroInstance] Cannot apply an invalid modifier.");
            return;
        }

        stats.AddModifier(modifier);
    }

    public void EquipWeapon(GearInstance weapon)
    {
        if (weapon == null || !weapon.IsValid || weapon.Definition.GearType != GearType.Weapon)
        {
            Debug.LogWarning("[HeroInstance] Cannot equip an invalid weapon.");
            return;
        }

        equippedWeapon = weapon;

        ApplyModifiers(weapon.Definition.StatModifiers);
    }

    public void EquipArmor(GearInstance armor)
    {
        if (armor == null || !armor.IsValid || armor.Definition.GearType != GearType.Armor)
        {
            Debug.LogWarning("[HeroInstance] Cannot equip an invalid armor.");
            return;
        }

        equippedArmor = armor;

        ApplyModifiers(armor.Definition.StatModifiers);
    }

    public void OnUnequipWeapon()
    {
        if (equippedWeapon == null)
        {
            return;
        }

        if (equippedWeapon.IsValid)
        {
            RemoveModifiers(equippedWeapon.Definition.StatModifiers);
        }

        equippedWeapon = null;
    }

    public void OnUnequipArmor()
    {
        if (equippedArmor == null)
        {
            return;
        }

        if (equippedArmor.IsValid)
        {
            RemoveModifiers(equippedArmor.Definition.StatModifiers);
        }

        equippedArmor = null;
    }

    private static UnitBaseStats GetDefaultStats(HeroDefinition definition)
    {
        return new UnitBaseStats(
            definition.MaxHealth,
            definition.Attack,
            definition.AttackInterval,
            definition.Defense,
            definition.SpecialDefense,
            definition.MoveSpeed,
            definition.BlockCount);
    }
}
