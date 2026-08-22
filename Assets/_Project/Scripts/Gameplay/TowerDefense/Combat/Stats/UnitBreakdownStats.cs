using System;
using UnityEngine;

public readonly struct UnitStatBreakdown
{
    public float BaseValue { get; }
    public float FlatBase { get; }
    public float AdditivePercent { get; }
    public float FinalMultiplier { get; }
    public float BonusValue => FinalValue - BaseValue;
    public float FinalValue { get; }

    public UnitStatBreakdown(float baseValue, float flatBase, float additivePercent, float finalMultiplier, float finalValue)
    {
        BaseValue = baseValue;
        FlatBase = flatBase;
        AdditivePercent = additivePercent;
        FinalMultiplier = finalMultiplier;
        FinalValue = finalValue;
    }
}

public class UnitBreakdownStats
{
    private readonly UnitStatBreakdown maxHealth;
    private readonly UnitStatBreakdown attack;
    private readonly UnitStatBreakdown attackInterval;
    private readonly UnitStatBreakdown defense;
    private readonly UnitStatBreakdown specialDefense;
    private readonly UnitStatBreakdown moveSpeed;
    private readonly UnitStatBreakdown blockCount;

    public float MaxHealth => maxHealth.FinalValue;
    public float Attack => attack.FinalValue;
    public float AttackInterval => attackInterval.FinalValue;
    public float Defense => defense.FinalValue;
    public float SpecialDefense => specialDefense.FinalValue;
    public float MoveSpeed => moveSpeed.FinalValue;
    public int BlockCount => Mathf.FloorToInt(blockCount.FinalValue);

    public UnitBreakdownStats(UnitStatBreakdown maxHealth, UnitStatBreakdown attack, UnitStatBreakdown attackInterval, UnitStatBreakdown defense,
                              UnitStatBreakdown specialDefense, UnitStatBreakdown moveSpeed, UnitStatBreakdown blockCount)
    {
        this.maxHealth = maxHealth;
        this.attack = attack;
        this.attackInterval = attackInterval;
        this.defense = defense;
        this.specialDefense = specialDefense;
        this.moveSpeed = moveSpeed;
        this.blockCount = blockCount;
    }

    public UnitStatBreakdown GetBreakdown(UnitStatType statType)
    {
        return statType switch
        {
            UnitStatType.MaxHealth => maxHealth,
            UnitStatType.Attack => attack,
            UnitStatType.AttackInterval => attackInterval,
            UnitStatType.Defense => defense,
            UnitStatType.SpecialDefense => specialDefense,
            UnitStatType.MoveSpeed => moveSpeed,
            UnitStatType.BlockCount => blockCount,
            _ => throw new ArgumentOutOfRangeException(nameof(statType), statType, null)
        };
    }

    public float GetValue(UnitStatType statType)
    {
        return GetBreakdown(statType).FinalValue;
    }
}
