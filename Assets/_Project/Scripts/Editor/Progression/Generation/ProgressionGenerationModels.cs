using System;
using System.Collections.Generic;
using UnityEngine;

internal static class ProgressionGenerationLimits
{
    public const float MaxRandomness = 0.05f;
}

[Serializable]
internal sealed class UnitStatFinalBreakdown
{
    public float maxHealthMultiplier = 1f;
    public float attackMultiplier = 1f;
    public float attackIntervalMultiplier = 1f;
    public float defenseMultiplier = 1f;
    public float specialDefenseMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public int blockCount;

    public float GetMultiplier(UnitStatType statType)
    {
        return statType switch
        {
            UnitStatType.MaxHealth => maxHealthMultiplier,
            UnitStatType.Attack => attackMultiplier,
            UnitStatType.AttackInterval => attackIntervalMultiplier,
            UnitStatType.Defense => defenseMultiplier,
            UnitStatType.SpecialDefense => specialDefenseMultiplier,
            UnitStatType.MoveSpeed => moveSpeedMultiplier,
            _ => 1f
        };
    }
}

[Serializable]
internal sealed class UnitStatLevelBreakdown
{
    public int level = 2;
    public List<UnitStatBreakdownTarget> targets = new List<UnitStatBreakdownTarget>();
}

[Serializable]
internal sealed class UnitStatBreakdownTarget
{
    public UnitStatType statType;
    public float multiplier = 1f;
    public int directValue;
}

internal static class UnitStatGenerationRules
{
    public static bool IsRoundedStat(UnitStatType statType)
    {
        return statType == UnitStatType.MaxHealth
               || statType == UnitStatType.Attack
               || statType == UnitStatType.Defense
               || statType == UnitStatType.SpecialDefense;
    }

    public static bool AllowsRandomness(UnitStatType statType)
    {
        return IsRoundedStat(statType);
    }

    public static float GetBaseValue(UnitBaseStats stats, UnitStatType statType)
    {
        return statType == UnitStatType.BlockCount ? stats.BlockCount : stats.GetValue(statType);
    }

    public static float NormalizeGeneratedValue(UnitStatType statType, float value)
    {
        value = Mathf.Max(0f, value);
        return IsRoundedStat(statType) ? Mathf.Round(value) : value;
    }
}
